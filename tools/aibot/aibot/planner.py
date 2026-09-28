"""Gemini planner: every 45-75 minutes the LLM decides what the bot does next
(goal, activity, farm map, how long). The local brain still does the moving,
fighting and shopping; when the LLM is off or out of quota nothing changes."""

import asyncio
import json
import logging
import re
import time

from .world import farm_spots_for

log = logging.getLogger("aibot.planner")

PLAN_ACTIVITIES = ("farm", "town", "social", "rest")
ACTIVITY_TH = {"farm": "เก็บเวล", "town": "เข้าเมืองขาย/ซื้อของ", "social": "เดินเล่นคุยกับคนในเมือง",
               "rest": "นั่งพัก", "resupply": "กลับเมืองซื้อของกับพัก"}


def planner_bots(config):
    """Names of the bots the LLM plans for (config "planner")."""
    cfg = config.get("planner") or {}
    if not cfg.get("enabled"):
        return set()
    names = cfg.get("bots") or [b["name"] for b in config.get("bots", [])][: int(cfg.get("max_bots", 10))]
    return {n.lower() for n in names}


def parse_plan(text):
    if not text:
        return None
    m = re.search(r"\{.*\}", text, re.S)
    if not m:
        return None
    try:
        data = json.loads(m.group(0))
    except ValueError:
        return None
    return data if isinstance(data, dict) else None


class PlannerMixin:
    def planner_on(self):
        return self.name.lower() in self.brain.planner_bots and self.brain.llm.allowed(self.name)

    def plan_active(self):
        return self.plan is not None and time.monotonic() < self.plan["until"]

    def plan_goal(self):
        return self.plan["goal"] if self.plan_active() else None

    def maybe_plan(self):
        """Called every tick; starts a background LLM call when a new plan is due."""
        if self.planning or not self.planner_on():
            return
        now = time.monotonic()
        if self.next_plan_at is None:
            # first plan a few minutes after login, spread over the bots
            self.next_plan_at = now + self.rng.uniform(*self.brain.planner_first_delay)
        if now < self.next_plan_at or self.partner:
            return
        self.planning = True
        self.next_plan_at = now + 300  # retry in 5 minutes if this one fails
        asyncio.create_task(self._make_plan())

    def _plan_prompt(self, maps):
        s = self.state
        job, need = self.pick_next_job()
        lines = [
            "อาชีพ %s Base Lv.%s Job Lv.%s" % (self.job_name(), s.get("blv"), s.get("jlv")),
            "อาชีพถัดไป: %s (ต้อง Job Lv.%s)" % (self.job_name(job), need) if job is not None else "ยังไม่มีอาชีพถัดไป",
            "เงิน %s z, ยาแดง/ยาฟื้น %s ขวด, กระเป๋าหนัก %d%%" % (
                s.get("zeny", 0), self.count_item(self.hp_potion or self.p.potion_item),
                100 * s.get("w", 0) // max(1, s.get("mw", 1))),
            "อยู่แมพ %s, ตามตารางปกติตอนนี้จะ%s" % (s.get("map"), ACTIVITY_TH.get(self.routine.natural(), "พัก")),
        ]
        lines.append("แมพเก็บเวลที่เหมาะกับเลเวล:")
        for m in maps:
            score = self.memory.map_score(m)
            deaths = self.memory.data["maps"].get(m, {}).get("deaths", 0)
            lines.append("- %s: %s, ตายไป %d ครั้ง" % (
                m, "ได้ ~%.0f%% ของเลเวลต่อชม." % (score * 100) if score is not None else "ยังไม่เคยลอง", deaths))
        friends = sorted(self.memory.data["players"].items(), key=lambda kv: -self.memory.friendship(kv[0]))[:3]
        if friends:
            lines.append("คนที่รู้จัก: " + ", ".join(v.get("name", k) for k, v in friends))
        diary = self.memory.data["log"][-4:]
        if diary:
            lines.append("เรื่องล่าสุด: " + " / ".join(d["text"] for d in diary))
        if self.plan:
            lines.append("เป้าหมายรอบที่แล้ว: " + self.plan["goal"])
        lines.append(
            'ตอบเป็น JSON อย่างเดียว: {"goal": "เป้าหมายสั้นๆ ไม่เกิน 12 คำ", '
            '"activity": "farm|town|social|rest", "map": "ชื่อแมพจากรายการถ้า activity เป็น farm ไม่งั้น \\"\\"", '
            '"minutes": 20-120, "say": "ประโยคสั้นๆ ที่อยากพิมพ์ในแชทตอนนี้ หรือ \\"\\""}'
        )
        return "\n".join(lines)

    async def _make_plan(self):
        try:
            maps = [sp["map"] for sp in farm_spots_for(self.world, self.state.get("blv", 1))]
            system = (
                "คุณคือความคิดของผู้เล่นเกม Ragnarok Online ชื่อ %s นิสัย: %s. "
                "วางแผนว่าจะทำอะไรต่อจากนี้ให้เหมือนคนเล่นจริง: อยากเก่งขึ้น เปลี่ยนอาชีพ หาเงิน "
                "หรือพักคุยกับเพื่อนบ้าง ตามนิสัยของตัวเอง. ตอบ JSON อย่างเดียว ไม่มีคำอธิบาย."
            ) % (self.name, self.p.llm_style)
            text = await self.brain.llm.complete(
                system, [{"role": "user", "content": self._plan_prompt(maps)}],
                bot_name=self.name, max_tokens=self.brain.planner_max_tokens, max_wait=90)
            plan = parse_plan(text)
            if not plan:
                log.info("%s: planner got no usable plan (%r)", self.name, (text or "")[:80])
                self.brain.stats["plans_failed"] += 1
                return
            await self._apply_plan(plan, maps)
        except Exception:
            log.exception("%s: planner failed", self.name)
        finally:
            self.planning = False

    async def _apply_plan(self, plan, maps):
        activity = str(plan.get("activity", "")).strip().lower()
        if activity not in PLAN_ACTIVITIES:
            activity = "farm"
        try:
            minutes = max(20, min(120, int(plan.get("minutes", 60))))
        except (TypeError, ValueError):
            minutes = 60
        goal = str(plan.get("goal") or ACTIVITY_TH[activity]).strip()[:80]
        pmap = str(plan.get("map") or "").strip()
        if pmap not in maps:
            pmap = None

        now = time.monotonic()
        self.plan = {"goal": goal, "activity": activity, "map": pmap, "until": now + minutes * 60}
        self.routine.set_plan(activity, minutes * 60)
        if activity == "farm" and pmap and pmap != self.farm_map:
            self.farm_map = None  # choose_farm_map picks the planned map
        lo, hi = self.brain.planner_interval
        self.next_plan_at = now + max(minutes, self.rng.uniform(lo, hi)) * 60
        self.brain.stats["plans"] += 1
        log.info("%s: plan [%s %s %d min] %s", self.name, activity, pmap or "-", minutes, goal)
        self.memory.note("ตั้งเป้า: %s" % goal)

        say = str(plan.get("say") or "").strip().splitlines()
        if say and say[0] and self.rng.random() < 0.6:
            await self.say(say[0][:100])
