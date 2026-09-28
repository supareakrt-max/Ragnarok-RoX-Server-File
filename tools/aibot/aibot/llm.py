"""LLM access with free-tier friendly quota handling.

Several providers can be listed; they are tried in order. A provider is
skipped while it has no API key, hit its daily limit, or is cooling down
after a rate-limit error. When every provider is unavailable the caller gets
None and the bots fall back to template phrases, so running out of free quota
never breaks the game.

Supported "provider" values:
  gemini             Google AI Studio (free tier), key in GEMINI_API_KEY
  groq               Groq (free tier), key in GROQ_API_KEY
  anthropic          Claude Messages API, key in ANTHROPIC_API_KEY
  openai_compatible  any /chat/completions server (OpenAI, Ollama, LM Studio...)

Usage counters are saved to "usage_file" so restarting the brain does not
reset the daily count. Only the Python standard library is used."""

import asyncio
import datetime
import json
import logging
import os
import time
import urllib.error
import urllib.request

log = logging.getLogger("aibot.llm")

PRESETS = {
    "gemini": {
        "base_url": "https://generativelanguage.googleapis.com/v1beta/openai",
        "chat_path": "/chat/completions",
        "api_key_env": "GEMINI_API_KEY",
        "model": "gemini-2.5-flash",
        "daily_limit": 1000,
        "rpm": 8,
    },
    "groq": {
        "base_url": "https://api.groq.com/openai/v1",
        "chat_path": "/chat/completions",
        "api_key_env": "GROQ_API_KEY",
        "model": "openai/gpt-oss-20b",
        "reasoning_effort": "low",
        "daily_limit": 300,
        "rpm": 20,
    },
    "anthropic": {
        "base_url": "https://api.anthropic.com",
        "api_key_env": "ANTHROPIC_API_KEY",
        "model": "claude-haiku-4-5-20251001",
        "daily_limit": 0,
        "rpm": 30,
    },
    "openai_compatible": {
        "base_url": "http://127.0.0.1:11434",
        "chat_path": "/v1/chat/completions",
        "api_key_env": "OPENAI_API_KEY",
        "model": "llama3.1",
        "daily_limit": 0,
        "rpm": 0,
    },
}


class LLMUnavailable(Exception):
    pass


class Provider:
    def __init__(self, cfg, defaults):
        kind = cfg.get("provider", "gemini")
        preset = dict(PRESETS.get(kind, PRESETS["openai_compatible"]))
        for key in ("max_tokens", "timeout", "temperature"):
            if key in defaults:
                preset.setdefault(key, defaults[key])
        preset.update(cfg)
        self.cfg = preset
        self.kind = kind
        self.name = preset.get("name") or kind
        self.model = preset["model"]
        self.base_url = preset["base_url"].rstrip("/")
        self.chat_path = preset.get("chat_path", "/v1/chat/completions")
        self.key_env = preset.get("api_key_env", "")
        self.api_key = preset.get("api_key") or os.environ.get(self.key_env, "")
        self.daily_limit = int(preset.get("daily_limit", 0))  # 0 = unlimited
        self.rpm = float(preset.get("rpm", 0))
        self.max_tokens = int(preset.get("max_tokens", 120))
        self.timeout = float(preset.get("timeout", 15))
        self.temperature = preset.get("temperature", 0.9)
        self.reasoning_effort = preset.get("reasoning_effort", self._default_reasoning())
        self.enabled = preset.get("enabled", True)
        self.cooldown_until = 0.0
        self.consecutive_429 = 0
        self.last_call = 0.0
        self.disabled_reason = None

        if self.kind != "openai_compatible" and not self.api_key:
            self.disabled_reason = "no API key (set env %s)" % self.key_env

    def _default_reasoning(self):
        # Gemini thinking eats the output budget and adds latency. 2.5 models can
        # turn it off; newer ones only go down to "minimal".
        if self.kind == "gemini":
            return "none" if self.model.startswith("gemini-2.5") else "minimal"
        return None

    def available(self, usage_today):
        if not self.enabled or self.disabled_reason:
            return False
        if time.monotonic() < self.cooldown_until:
            return False
        if self.daily_limit and usage_today >= self.daily_limit:
            return False
        return True

    def wait_time(self):
        if self.rpm <= 0:
            return 0.0
        return max(0.0, 60.0 / self.rpm - (time.monotonic() - self.last_call))

    # -- HTTP -------------------------------------------------------------
    def request(self, system, messages, max_tokens=None):
        max_tokens = max_tokens or self.max_tokens
        if self.kind == "anthropic":
            url = self.base_url + "/v1/messages"
            body = {"model": self.model, "max_tokens": max_tokens, "system": system, "messages": messages}
            headers = {"content-type": "application/json", "x-api-key": self.api_key, "anthropic-version": "2023-06-01"}
        else:
            url = self.base_url + self.chat_path
            body = {
                "model": self.model,
                "max_tokens": max_tokens,
                "temperature": self.temperature,
                "messages": [{"role": "system", "content": system}] + messages,
            }
            if self.reasoning_effort:
                body["reasoning_effort"] = self.reasoning_effort
            headers = {"content-type": "application/json"}
            if self.api_key:
                headers["authorization"] = "Bearer " + self.api_key

        req = urllib.request.Request(url, data=json.dumps(body).encode("utf-8"), headers=headers, method="POST")
        with urllib.request.urlopen(req, timeout=self.timeout) as resp:
            data = json.loads(resp.read().decode("utf-8"))

        if self.kind == "anthropic":
            return "".join(b.get("text", "") for b in data.get("content", []) if b.get("type") == "text")
        choices = data.get("choices") or []
        if not choices:
            return None
        return (choices[0].get("message") or {}).get("content")


class LLMClient:
    """Router over the configured providers with daily quotas."""

    def __init__(self, cfg, base_dir="."):
        cfg = cfg or {}
        self.enabled = bool(cfg.get("enabled"))
        self.only_bots = {n.lower() for n in cfg.get("only_bots", [])}
        self.max_concurrency = int(cfg.get("max_concurrency", 2))
        self._sem = asyncio.Semaphore(self.max_concurrency)
        usage_file = cfg.get("usage_file", "llm_usage.json")
        self.usage_file = usage_file if os.path.isabs(usage_file) else os.path.join(base_dir, usage_file)
        self.stats = {"calls": 0, "ok": 0, "failed": 0, "no_provider": 0}

        provider_cfgs = cfg.get("providers")
        if not provider_cfgs:
            # old single-provider format: {"provider": "...", "model": "..."}
            single = {k: v for k, v in cfg.items() if k not in ("enabled", "only_bots", "max_concurrency", "usage_file")}
            provider_cfgs = [single] if single.get("provider") else []
        defaults = {k: cfg[k] for k in ("max_tokens", "timeout", "temperature") if k in cfg}
        self.providers = [Provider(p, defaults) for p in provider_cfgs]

        self.usage = self._load_usage()

        if self.enabled:
            usable = [p for p in self.providers if not p.disabled_reason and p.enabled]
            for p in self.providers:
                if p.disabled_reason:
                    log.warning("LLM provider %s disabled: %s", p.name, p.disabled_reason)
            if not usable:
                log.warning("LLM enabled but no provider is usable, bots will use template phrases")
                self.enabled = False
            else:
                log.info("LLM providers: %s", ", ".join(
                    "%s(%s, %s/day)" % (p.name, p.model, p.daily_limit or "unlimited") for p in usable))

    # -- usage bookkeeping ---------------------------------------------------
    @staticmethod
    def _today():
        return datetime.date.today().isoformat()

    def _load_usage(self):
        try:
            with open(self.usage_file, encoding="utf-8") as fp:
                data = json.load(fp)
            if data.get("date") == self._today():
                return data
        except (OSError, ValueError):
            pass
        return {"date": self._today(), "providers": {}}

    def _save_usage(self):
        try:
            with open(self.usage_file, "w", encoding="utf-8") as fp:
                json.dump(self.usage, fp, indent=1)
        except OSError as exc:
            log.debug("cannot save LLM usage: %s", exc)

    def used_today(self, provider):
        if self.usage.get("date") != self._today():
            self.usage = {"date": self._today(), "providers": {}}
        return self.usage["providers"].get(provider.name, 0)

    def _count(self, provider):
        self.used_today(provider)
        self.usage["providers"][provider.name] = self.usage["providers"].get(provider.name, 0) + 1
        self._save_usage()

    def summary(self):
        parts = []
        for p in self.providers:
            state = "off" if (p.disabled_reason or not p.enabled) else (
                "cooldown" if time.monotonic() < p.cooldown_until else "ok")
            parts.append("%s %d/%s %s" % (p.name, self.used_today(p), p.daily_limit or "-", state))
        return ", ".join(parts)

    def allowed(self, bot_name):
        return self.enabled and (not self.only_bots or bot_name.lower() in self.only_bots)

    # -- main entry ----------------------------------------------------------
    async def reply(self, system, messages, bot_name=None):
        """One line of in-game chat, or None."""
        return await self.complete(system, messages, bot_name, raw=False)

    async def complete(self, system, messages, bot_name=None, raw=True, max_tokens=None):
        """Raw model output (raw=True) or a cleaned chat line; None when no provider answered."""
        if not self.enabled or (bot_name and not self.allowed(bot_name)):
            return None
        async with self._sem:
            for provider in self.providers:
                if not provider.available(self.used_today(provider)):
                    continue
                wait = provider.wait_time()
                if wait > 5:
                    continue  # too busy, try the next provider
                if wait > 0:
                    await asyncio.sleep(wait)
                provider.last_call = time.monotonic()
                self.stats["calls"] += 1
                self._count(provider)
                try:
                    text = await asyncio.to_thread(provider.request, system, messages, max_tokens)
                except urllib.error.HTTPError as exc:
                    self._handle_http_error(provider, exc)
                    self.stats["failed"] += 1
                    continue
                except Exception as exc:
                    log.warning("LLM %s request failed: %s", provider.name, exc)
                    provider.cooldown_until = time.monotonic() + 30
                    self.stats["failed"] += 1
                    continue
                provider.consecutive_429 = 0
                text = (text or "").strip() if raw else clean_reply(text)
                if text:
                    self.stats["ok"] += 1
                    return text
            self.stats["no_provider"] += 1
            return None

    def _handle_http_error(self, provider, exc):
        try:
            detail = exc.read().decode("utf-8", "replace")[:300]
        except Exception:
            detail = ""
        if exc.code == 429:
            provider.consecutive_429 += 1
            # short cooldown for per-minute limits, long one when it keeps happening (daily quota)
            cooldown = 60 if provider.consecutive_429 < 3 else 3600
            provider.cooldown_until = time.monotonic() + cooldown
            log.warning("LLM %s rate limited (429), pausing %ds", provider.name, cooldown)
        elif exc.code in (401, 403):
            provider.disabled_reason = "HTTP %d (check the API key): %s" % (exc.code, detail)
            log.error("LLM %s disabled: %s", provider.name, provider.disabled_reason)
        elif exc.code in (400, 404):
            provider.cooldown_until = time.monotonic() + 600
            log.error("LLM %s rejected the request (HTTP %d, wrong model name?): %s", provider.name, exc.code, detail)
        else:
            provider.cooldown_until = time.monotonic() + 60
            log.warning("LLM %s HTTP %d: %s", provider.name, exc.code, detail)


def clean_reply(text):
    import re

    if not text:
        return None
    lines = [l.strip() for l in text.strip().splitlines() if l.strip()]
    if not lines:
        return None
    text = lines[0].strip('"').strip()
    # never let the model speak for others or leak role markers
    text = re.sub(r"^[^:]{1,24}\s:\s", "", text)
    return text[:120] or None
