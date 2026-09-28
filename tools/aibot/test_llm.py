#!/usr/bin/env python3
"""Check that your LLM API keys work before starting the bots.

    set GEMINI_API_KEY=xxxx          (Windows)   /   export GEMINI_API_KEY=xxxx   (Linux)
    python test_llm.py -c config.json
"""

import argparse
import asyncio
import json
import logging
import os
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from aibot.llm import LLMClient  # noqa: E402
from aibot.personality import load_personalities  # noqa: E402
from aibot.social import system_prompt  # noqa: E402


async def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("-c", "--config", default="config.json")
    ap.add_argument("-m", "--message", default="หวัดดีครับ เก็บเวลที่ไหนดี")
    args = ap.parse_args()
    logging.basicConfig(level=logging.INFO, format="%(levelname)s %(name)s: %(message)s")

    with open(args.config, "rb") as fp:
        raw = fp.read()
    try:
        cfg = json.loads(raw.decode("utf-8-sig"))
    except UnicodeDecodeError:
        cfg = json.loads(raw.decode("cp874"))  # saved as Thai ANSI by Notepad
    llm_cfg = dict(cfg.get("llm", {}))
    llm_cfg["enabled"] = True
    llm_cfg["only_bots"] = []
    llm_cfg["usage_file"] = llm_cfg.get("usage_file", "llm_usage.json")

    base = os.path.dirname(os.path.abspath(args.config))
    personality = load_personalities(cfg.get("personalities"))["chill"]
    system = system_prompt("NongFern", personality, "Lv.25 อยู่แมพ prontera กำลังเดินเล่นคุยกับเพื่อน")

    for provider in LLMClient(llm_cfg, base).providers:
        single = dict(llm_cfg)
        single["providers"] = [provider.cfg]
        client = LLMClient(single, base)
        if not client.enabled:
            print("[%s] SKIP: %s" % (provider.name, provider.disabled_reason or "disabled"))
            continue
        start = time.perf_counter()
        reply = await client.reply(system, [{"role": "user", "content": "Player1: " + args.message}])
        ms = (time.perf_counter() - start) * 1000
        if reply:
            print("[%s] OK %.0fms (%s): %s" % (provider.name, ms, provider.model, reply))
        else:
            print("[%s] FAILED (%s) - see the log above" % (provider.name, provider.model))


if __name__ == "__main__":
    asyncio.run(main())
