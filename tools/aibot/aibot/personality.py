"""Personality system: every bot gets a profile that changes how it fights,
walks, trades and talks. Defaults below can be overridden per key in config.json
("personalities": {"hardcore": {...}}) and new personalities can be added."""

import copy
import random

DEFAULTS = {
    # --- pacing -------------------------------------------------------
    "tick": 1.0,               # seconds between decisions (jitter is added)
    "tick_jitter": 0.4,
    # --- combat -------------------------------------------------------
    "aggression": 0.7,         # 0..1 how eagerly it looks for fights
    "level_margin": 5,         # attacks mobs up to base_level + margin
    "flee_hp": 0.25,           # below this HP ratio -> run away
    "potion_hp": 0.5,          # below this HP ratio -> drink potion
    "potion_sp": 0.2,
    "skill_chance": 0.35,      # chance per tick to use an attack skill
    "fight_mvp": False,        # stay near MVPs instead of running away
    "loot": True,
    "loot_range": 8,
    # --- economy ------------------------------------------------------
    "sell_weight": 0.7,        # go sell when weight ratio is above this
    "potion_stock": 30,
    "potion_item": 501,        # Red Potion
    "sp_potion_item": 0,       # 505 = Blue Potion, 0 = never buy
    "keep_items": [],          # item ids never sold
    # --- movement -----------------------------------------------------
    "wander_radius": 10,       # idle wandering distance in towns
    "roam_step": 12,           # distance per step while roaming for mobs
    # --- social -------------------------------------------------------
    "chattiness": 0.3,         # chance to answer public chat around it
    "bot_chat_chance": 0.1,    # chance to answer another bot
    "idle_chat_chance": 0.01,  # chance per tick to say something random
    "drama_chance": 0.004,     # chance per tick for a random drama event
    "emote_chance": 0.02,
    "accept_party": 0.6,       # chance to accept a party invite
    "invite_party": 0.002,     # chance per tick to invite a nearby player
    "typing_cps": 8.0,         # characters per second when "typing"
    "llm_style": "ผู้เล่น Ragnarok ทั่วไป พิมพ์สั้นๆ ภาษาไทยปนคำเกม",
    # --- daily routine ------------------------------------------------
    "schedule": [
        {"from": "00:00", "to": "07:00", "activity": "rest"},
        {"from": "07:00", "to": "12:00", "activity": "farm"},
        {"from": "12:00", "to": "13:00", "activity": "town"},
        {"from": "13:00", "to": "18:00", "activity": "farm"},
        {"from": "18:00", "to": "20:00", "activity": "social"},
        {"from": "20:00", "to": "23:00", "activity": "farm"},
        {"from": "23:00", "to": "24:00", "activity": "social"},
    ],
    "schedule_offset_minutes": 45,  # each bot shifts its schedule randomly by +/- this
    # --- phrases ------------------------------------------------------
    "phrases": {
        "greet": ["หวัดดีครับ", "ดีจ้า", "hi~", "สวัสดีครับ"],
        "reply_generic": ["55555", "จริงดิ", "อืมๆ", "ใช่ๆ", "โอเค", "งงเลย"],
        "thanks": ["ขอบคุณครับ", "ty", "ขอบใจนะ"],
        "party_join": ["เข้าแล้วครับ", "มาเก็บเวลกัน", "ฝากตัวด้วยนะ"],
        "party_decline": ["ขอโทษนะ ตอนนี้ขอเก็บคนเดียวก่อน", "ไม่เป็นไรครับ ขอบคุณ"],
        "party_invite": ["ไปเก็บเวลด้วยกันไหม?", "ปาร์ตี้กันไหมครับ"],
        "levelup": ["เลเวลอัพแล้ว!", "อัพแล้ว เย้", "lv up!!"],
        "death": ["ตายอีกแล้ว...", "เจ็บใจ", "แลคหรอเนี่ย"],
        "flee": ["หนีก่อน!", "เลือดจะหมดแล้ว", "ไม่ไหวๆ"],
        "mvp": ["MVP มาาา หนีเร็ว!!", "บอสโผล่! วิ่งงง", "ใครตีบอสอยู่ ช่วยด้วย"],
        "rare_drop": ["ของแรร์ตก!!!", "เฮ้ย การ์ดตก!", "โชคดีสุดๆ"],
        "refine_fail": ["ตีบวกแตกอีกแล้ว T_T", "+7 แตก... ร้องไห้", "ช่างตีบวกใจร้าย"],
        "bored": ["ง่วงจัง", "เก็บเวลเบื่อเลย", "มีใครไปดันไหม", "เซิร์ฟนี้คนเยอะดีนะ"],
        "selling": ["ขายของก่อน", "กระเป๋าเต็มแล้ว"],
        "whisper_reply": ["ว่าไงครับ", "ครับผม?", "มีอะไรหรอ"],
        "busy": ["เดี๋ยวนะ ตีมอนอยู่", "ขอเก็บเวลแปป"],
        "jobchange": ["เปลี่ยนอาชีพเป็น {job} แล้ว!!", "ได้เป็น {job} แล้ววว", "{job} มาแล้ว เย้"],
        "new_gear": ["ได้ของใหม่แล้ว", "อัพของแล้ว แรงขึ้นเยอะ", "ซื้อของใหม่มา หมดตัวเลย 555"],
        "greet_known": ["หวัดดี {name}!", "อ้าว {name} มาแล้ว", "{name} ไปไหนมา", "คิดถึง {name} เลย"],
        "party_follow": ["ตามไปแล้ว", "รอด้วยย", "มาแล้วๆ"],
        "party_leave": ["ขอออกตี้ก่อนนะ ไว้เจอกันใหม่", "ไปก่อนนะ ขอบคุณที่ชวน"],
        "resurrect": ["{name} ลุกขึ้นมา!", "ชุบให้แล้ว {name}"],
    },
    # idle emotions (ids from enum emotion_type in src/map/clif.hpp):
    # 2 delight, 3 throb, 18 smile, 21 best, 33 ok, 38 cool
    "emotes": [2, 3, 18, 21, 33, 38],
}

BUILTIN = {
    "hardcore": {
        "tick": 0.8,
        "aggression": 0.95,
        "level_margin": 8,
        "flee_hp": 0.15,
        "potion_hp": 0.45,
        "skill_chance": 0.6,
        "chattiness": 0.12,
        "idle_chat_chance": 0.003,
        "accept_party": 0.4,
        "invite_party": 0.004,
        "typing_cps": 12.0,
        "llm_style": "สายฮาร์ดคอร์ เน้นเก็บเวล พิมพ์สั้นห้วน พูดเรื่องเวล/บิวด์/ดันเจี้ยน",
        "schedule": [
            {"from": "00:00", "to": "03:00", "activity": "farm"},
            {"from": "03:00", "to": "08:00", "activity": "rest"},
            {"from": "08:00", "to": "12:30", "activity": "farm"},
            {"from": "12:30", "to": "13:00", "activity": "town"},
            {"from": "13:00", "to": "24:00", "activity": "farm"},
        ],
        "phrases": {
            "bored": ["ใครไปดันบ้าง", "เวลช้าจัง", "ต้องการพรีสปาร์ตี้"],
            "busy": ["ไม่ว่าง เก็บเวล", "ตีมอนอยู่"],
        },
    },
    "merchant": {
        "tick": 1.2,
        "aggression": 0.35,
        "level_margin": 2,
        "flee_hp": 0.4,
        "potion_hp": 0.6,
        "sell_weight": 0.5,
        "chattiness": 0.4,
        "idle_chat_chance": 0.02,
        "accept_party": 0.3,
        "llm_style": "สายพ่อค้า ชอบคุยเรื่องราคาของ ต่อรอง ขายของ พิมพ์สุภาพ",
        "schedule": [
            {"from": "00:00", "to": "08:00", "activity": "rest"},
            {"from": "08:00", "to": "11:00", "activity": "town"},
            {"from": "11:00", "to": "14:00", "activity": "farm"},
            {"from": "14:00", "to": "22:00", "activity": "town"},
            {"from": "22:00", "to": "24:00", "activity": "social"},
        ],
        "phrases": {
            "bored": ["ขายยาแดงราคาถูกครับ", "รับซื้อการ์ดทุกใบ", "S> ของดีราคาเบาๆ ซิบมา", "B> Jellopy ราคาดี"],
            "greet": ["สวัสดีครับ สนใจของอะไรไหม", "ดีครับ ร้านเปิดแล้ว"],
        },
    },
    "chill": {
        "tick": 1.5,
        "tick_jitter": 0.8,
        "aggression": 0.5,
        "level_margin": 3,
        "flee_hp": 0.35,
        "chattiness": 0.5,
        "bot_chat_chance": 0.2,
        "idle_chat_chance": 0.03,
        "drama_chance": 0.006,
        "emote_chance": 0.05,
        "accept_party": 0.85,
        "typing_cps": 6.0,
        "llm_style": "สายชิล เป็นกันเอง ชอบคุยเล่น ใช้ 555 และอีโมจิบ้าง",
        "schedule": [
            {"from": "00:00", "to": "10:00", "activity": "rest"},
            {"from": "10:00", "to": "12:00", "activity": "social"},
            {"from": "12:00", "to": "16:00", "activity": "farm"},
            {"from": "16:00", "to": "19:00", "activity": "social"},
            {"from": "19:00", "to": "22:00", "activity": "farm"},
            {"from": "22:00", "to": "24:00", "activity": "social"},
        ],
        "phrases": {
            "bored": ["วันนี้อากาศดีนะ", "ใครอยู่ปรอนบ้าง", "นั่งเล่นกันไหม", "หิวข้าวแล้ว 555"],
        },
    },
}


def _merge(base, override):
    out = copy.deepcopy(base)
    for key, value in (override or {}).items():
        if key == "phrases":
            phrases = out.setdefault("phrases", {})
            for pkey, plist in value.items():
                phrases[pkey] = list(plist)
        elif isinstance(value, dict) and isinstance(out.get(key), dict):
            out[key] = _merge(out[key], value)
        else:
            out[key] = copy.deepcopy(value)
    return out


class Personality:
    def __init__(self, name, data):
        self.name = name
        self.data = data

    def __getattr__(self, item):
        try:
            return self.__dict__["data"][item]
        except KeyError:
            raise AttributeError(item)

    def phrase(self, kind, rng=random):
        options = self.data["phrases"].get(kind) or DEFAULTS["phrases"].get(kind) or [""]
        return rng.choice(options)

    def next_tick(self, rng=random):
        return max(0.2, self.tick + rng.uniform(-self.tick_jitter / 2, self.tick_jitter))


def load_personalities(config_overrides):
    """Builtins + config overrides + new personalities from config."""
    result = {}
    names = set(BUILTIN) | set(config_overrides or {})
    for name in names:
        data = _merge(DEFAULTS, BUILTIN.get(name, {}))
        data = _merge(data, (config_overrides or {}).get(name, {}))
        result[name] = Personality(name, data)
    return result
