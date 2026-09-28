"""Chat brain: rule based replies, with the LLM (see llm.py) used when available."""

import logging
import random
import re

from .llm import LLMClient, clean_reply  # noqa: F401  (re-exported)

log = logging.getLogger("aibot.social")

GREETING = re.compile(r"(สวัสดี|หวัดดี|ดีครับ|ดีค่ะ|ดีจ้า|\bhi\b|\bhello\b|\bhey\b|\byo\b)", re.I)
THANKS = re.compile(r"(ขอบคุณ|ขอบใจ|\bty\b|\bthx\b|thank)", re.I)
PARTY = re.compile(r"(ปาร์ตี้|ปาตี้|\bpt\b|party|ตี้)", re.I)
TRADE = re.compile(r"(ขาย|ซื้อ|ราคา|\bS>|\bB>|กี่ z|zeny|เซนี่)", re.I)
QUESTION = re.compile(r"(\?|ไหม|มั้ย|หรอ|รึเปล่า|อะไร|ที่ไหน|ยังไง|เท่าไ)")


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
