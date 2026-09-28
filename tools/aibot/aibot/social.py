"""Chat brain: rule based replies with an optional LLM backend.

LLM providers (config "llm"):
  - "anthropic"          : Claude Messages API (needs ANTHROPIC_API_KEY or api_key_env)
  - "openai_compatible"  : any /v1/chat/completions server (OpenAI, Ollama, LM Studio...)
Only the Python standard library is used."""

import asyncio
import json
import logging
import os
import random
import re
import time
import urllib.request

log = logging.getLogger("aibot.social")

GREETING = re.compile(r"(สวัสดี|หวัดดี|ดีครับ|ดีค่ะ|ดีจ้า|\bhi\b|\bhello\b|\bhey\b|\byo\b)", re.I)
THANKS = re.compile(r"(ขอบคุณ|ขอบใจ|\bty\b|\bthx\b|thank)", re.I)
PARTY = re.compile(r"(ปาร์ตี้|ปาตี้|\bpt\b|party|ตี้)", re.I)
TRADE = re.compile(r"(ขาย|ซื้อ|ราคา|\bS>|\bB>|กี่ z|zeny|เซนี่)", re.I)
QUESTION = re.compile(r"(\?|ไหม|มั้ย|หรอ|รึเปล่า|อะไร|ที่ไหน|ยังไง|เท่าไ)")


class LLMClient:
    def __init__(self, cfg):
        self.cfg = cfg or {}
        self.enabled = bool(self.cfg.get("enabled"))
        self.provider = self.cfg.get("provider", "anthropic")
        self.model = self.cfg.get("model", "claude-haiku-4-5-20251001")
        self.max_tokens = int(self.cfg.get("max_tokens", 80))
        self.timeout = float(self.cfg.get("timeout", 8))
        self.min_interval = float(self.cfg.get("min_interval", 2.0))
        self.base_url = self.cfg.get("base_url", "")
        self.api_key = os.environ.get(self.cfg.get("api_key_env", "ANTHROPIC_API_KEY" if self.provider == "anthropic" else "OPENAI_API_KEY"), "")
        self.max_concurrency = int(self.cfg.get("max_concurrency", 4))
        self._sem = asyncio.Semaphore(self.max_concurrency)
        self._last_call = 0.0
        if self.enabled and self.provider == "anthropic" and not self.api_key:
            log.warning("LLM enabled but no API key in env %s, falling back to templates", self.cfg.get("api_key_env", "ANTHROPIC_API_KEY"))
            self.enabled = False

    def _request(self, system, messages):
        if self.provider == "anthropic":
            url = (self.base_url or "https://api.anthropic.com") + "/v1/messages"
            body = {"model": self.model, "max_tokens": self.max_tokens, "system": system, "messages": messages}
            headers = {"content-type": "application/json", "x-api-key": self.api_key, "anthropic-version": "2023-06-01"}
        else:
            url = (self.base_url or "http://127.0.0.1:11434") + "/v1/chat/completions"
            body = {"model": self.model, "max_tokens": self.max_tokens, "messages": [{"role": "system", "content": system}] + messages}
            headers = {"content-type": "application/json"}
            if self.api_key:
                headers["authorization"] = "Bearer " + self.api_key
        req = urllib.request.Request(url, data=json.dumps(body).encode("utf-8"), headers=headers, method="POST")
        with urllib.request.urlopen(req, timeout=self.timeout) as resp:
            data = json.loads(resp.read().decode("utf-8"))
        if self.provider == "anthropic":
            return "".join(block.get("text", "") for block in data.get("content", []) if block.get("type") == "text")
        return data["choices"][0]["message"]["content"]

    async def reply(self, system, messages):
        if not self.enabled:
            return None
        async with self._sem:
            wait = self.min_interval - (time.monotonic() - self._last_call)
            if wait > 0:
                await asyncio.sleep(wait)
            self._last_call = time.monotonic()
            try:
                text = await asyncio.to_thread(self._request, system, messages)
            except Exception as exc:
                log.warning("LLM request failed: %s", exc)
                return None
        return clean_reply(text)


def clean_reply(text):
    if not text:
        return None
    text = text.strip().splitlines()[0].strip().strip('"')
    # never let the model speak for others or leak role markers
    text = re.sub(r"^[^:]{1,24}\s:\s", "", text)
    return text[:120] or None


class ChatMemory:
    """Short per-bot conversation history for LLM context."""

    def __init__(self, size=8):
        self.size = size
        self.lines = []

    def add(self, speaker, text):
        self.lines.append((speaker, text))
        self.lines = self.lines[-self.size:]

    def as_messages(self, me):
        msgs = []
        for speaker, text in self.lines:
            if speaker == me:
                msgs.append({"role": "assistant", "content": text})
            else:
                msgs.append({"role": "user", "content": "%s: %s" % (speaker, text)})
        # APIs require the conversation to start with the user
        while msgs and msgs[0]["role"] == "assistant":
            msgs.pop(0)
        merged = []
        for m in msgs:
            if merged and merged[-1]["role"] == m["role"]:
                merged[-1]["content"] += "\n" + m["content"]
            else:
                merged.append(dict(m))
        return merged


def template_reply(personality, text, rng=random, mentioned=False, busy=False):
    if THANKS.search(text):
        return personality.phrase("thanks", rng)
    if GREETING.search(text):
        return personality.phrase("greet", rng)
    if PARTY.search(text):
        return personality.phrase("party_invite", rng) if rng.random() < 0.5 else personality.phrase("busy", rng)
    if TRADE.search(text) and personality.name == "merchant":
        return rng.choice(["ซิบมาคุยราคาได้ครับ", "มีของครับ สนใจอะไร", "ราคาเป็นกันเองครับ"])
    if busy and rng.random() < 0.5:
        return personality.phrase("busy", rng)
    if mentioned or QUESTION.search(text):
        return personality.phrase("reply_generic", rng)
    return personality.phrase("reply_generic", rng) if rng.random() < 0.4 else None


def system_prompt(bot_name, personality, context):
    return (
        "คุณคือผู้เล่นเกม Ragnarok Online ชื่อ %s (ไม่ใช่ AI และห้ามบอกว่าเป็นบอท). "
        "นิสัย: %s. ตอนนี้: %s. "
        "ตอบเป็นข้อความแชทในเกมบรรทัดเดียว สั้นไม่เกิน 15 คำ ภาษาเดียวกับคู่สนทนา "
        "ไม่ใช้ markdown ไม่ใส่ชื่อตัวเองนำหน้า."
    ) % (bot_name, personality.llm_style, context)
