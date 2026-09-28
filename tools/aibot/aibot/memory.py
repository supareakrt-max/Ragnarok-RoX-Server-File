"""Long-term memory of one bot, saved to tools/aibot/memory/<name>.json.

It survives restarts of the brain and holds:
  maps     farming results per map (seconds, level progress, deaths) used to
           learn which map is worth it
  players  people the bot met: how often, chats, parties, last words
  log      short diary of milestones (job changes, rare drops...)
"""

import json
import logging
import os
import re
import time

log = logging.getLogger("aibot.memory")


def _safe_name(name):
    return re.sub(r"[^\w\-]", "_", name, flags=re.UNICODE)[:40] or "bot"


class BotMemory:
    def __init__(self, directory, name):
        self.path = os.path.join(directory, _safe_name(name) + ".json")
        self.data = {"maps": {}, "players": {}, "log": []}
        self.dirty = False
        self.last_save = time.monotonic()
        try:
            with open(self.path, encoding="utf-8") as fp:
                loaded = json.load(fp)
            for key in self.data:
                if isinstance(loaded.get(key), type(self.data[key])):
                    self.data[key] = loaded[key]
        except (OSError, ValueError):
            pass

    # -- persistence -----------------------------------------------------
    def save(self, force=False):
        if not self.dirty and not force:
            return
        try:
            os.makedirs(os.path.dirname(self.path), exist_ok=True)
            tmp = self.path + ".tmp"
            with open(tmp, "w", encoding="utf-8") as fp:
                json.dump(self.data, fp, ensure_ascii=False, indent=1)
            os.replace(tmp, self.path)
            self.dirty = False
            self.last_save = time.monotonic()
        except OSError as exc:
            log.warning("cannot save memory %s: %s", self.path, exc)

    def autosave(self, every=60):
        if self.dirty and time.monotonic() - self.last_save >= every:
            self.save()

    # -- maps --------------------------------------------------------------
    def map_stats(self, mapname):
        return self.data["maps"].setdefault(mapname, {"secs": 0.0, "prog": 0.0, "deaths": 0})

    def add_farm_time(self, mapname, secs, progress):
        st = self.map_stats(mapname)
        st["secs"] = round(st["secs"] + secs, 1)
        st["prog"] = round(st["prog"] + progress, 5)
        self.dirty = True

    def add_death(self, mapname):
        self.map_stats(mapname)["deaths"] += 1
        self.dirty = True

    def map_score(self, mapname):
        """Level progress per hour, reduced by how often the bot dies there.
        None when the map has not been tried long enough."""
        st = self.data["maps"].get(mapname)
        if not st or st["secs"] < 600:
            return None
        hours = st["secs"] / 3600.0
        rate = st["prog"] / hours
        deaths_per_hour = st["deaths"] / hours
        return rate * max(0.2, 1.0 - 0.25 * deaths_per_hour)

    # -- players -----------------------------------------------------------
    def player(self, name):
        key = name.lower()
        p = self.data["players"].get(key)
        if p is None:
            p = {"name": name, "met": 0, "chats": 0, "parties": 0, "last_seen": 0, "last_msg": "", "mood": 0}
            self.data["players"][key] = p
        return p

    def known(self, name):
        return name.lower() in self.data["players"]

    def saw(self, name):
        p = self.player(name)
        now = int(time.time())
        new_meeting = now - p["last_seen"] > 1800
        if new_meeting:
            p["met"] += 1
        p["last_seen"] = now
        self.dirty = True
        return new_meeting

    def chatted(self, name, text):
        p = self.player(name)
        p["chats"] += 1
        p["last_msg"] = text[:80]
        p["last_seen"] = int(time.time())
        p["mood"] = min(10, p["mood"] + 1)
        self.dirty = True

    def partied(self, name):
        p = self.player(name)
        p["parties"] += 1
        p["mood"] = min(10, p["mood"] + 3)
        self.dirty = True

    def friendship(self, name):
        p = self.data["players"].get(name.lower())
        if not p:
            return 0
        return p["mood"] + p["parties"] * 2 + min(p["met"], 10) * 0.5

    def describe(self, name):
        """One line about a player for the LLM prompt."""
        p = self.data["players"].get(name.lower())
        if not p or (p["met"] <= 1 and p["chats"] <= 1):
            return "%s เป็นคนที่เพิ่งเจอครั้งแรก" % name
        parts = ["รู้จัก %s มาแล้ว (เจอ %d ครั้ง คุยกัน %d ครั้ง" % (name, p["met"], p["chats"])]
        if p["parties"]:
            parts.append(" เคยปาร์ตี้ด้วย %d ครั้ง" % p["parties"])
        parts.append(")")
        if p["last_msg"]:
            parts.append(" ครั้งก่อนเขาพูดว่า \"%s\"" % p["last_msg"])
        return "".join(parts)

    # -- diary -------------------------------------------------------------
    def note(self, text):
        self.data["log"].append({"t": int(time.time()), "text": text})
        self.data["log"] = self.data["log"][-50:]
        self.dirty = True
