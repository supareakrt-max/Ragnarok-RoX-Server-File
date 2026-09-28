"""Career decisions: job changes and learning which farming map pays off."""

import logging
import time

from .world import farm_spots_for

log = logging.getLogger("aibot.career")

EMO_BEST, EMO_DELIGHT = 21, 2


class CareerMixin:
    # ------------------------------------------------------------------
    # job change
    # ------------------------------------------------------------------
    def job_name(self, cls=None):
        cls = self.state.get("class", 0) if cls is None else cls
        return self.world["job_names"].get(str(cls), "Job %s" % cls)

    def pick_next_job(self):
        promo = self.world["promotions"].get(str(self.state.get("class", 0)))
        if not promo:
            return None, None
        forced = self.spec.get("job2") if self.state.get("class", 0) != 0 else self.spec.get("job1")
        if forced:
            return int(forced), promo["jlv"]
        choices = promo["to"].get(self.p.name) or promo["to"].get("hardcore") or []
        if not choices:
            return None, None
        # stable choice per bot, so it does not flip between restarts
        return choices[sum(map(ord, self.name)) % len(choices)], promo["jlv"]

    async def maybe_change_job(self):
        s = self.state
        if not self.cooldown("jobcheck", 30):
            return False
        job, need = self.pick_next_job()
        if job is None or s.get("jlv", 0) < need:
            return False
        if s.get("skpts", 0) > 0 and not self.nothing_to_learn:
            return False  # spend skill points first, like a real player
        old = self.job_name()
        res = await self.cmd("jobchange", job=job)
        if not res.get("ok"):
            log.warning("%s: job change to %s failed: %s", self.name, job, res.get("error"))
            return False
        new = self.job_name(job)
        log.info("%s: job change %s -> %s", self.name, old, new)
        self.brain.stats["job_changes"] += 1
        self.memory.note("เปลี่ยนอาชีพจาก %s เป็น %s ที่ Lv.%s" % (old, new, s.get("blv")))
        self.skill_done = set()
        self.nothing_to_learn = False
        self.gear_checked_blv = None
        self.farm_map = None
        await self.emote(EMO_BEST)
        await self.say(self.p.phrase("jobchange", self.rng).format(job=new))
        return True

    # ------------------------------------------------------------------
    # map learning
    # ------------------------------------------------------------------
    def track_farming(self):
        """Book the progress made since the last tick on the current farm map."""
        s = self.state
        now = time.monotonic()
        prev = self.farm_sample
        self.farm_sample = (now, s["map"], s["blv"], s.get("bexp", 0), s.get("bnext") or 1)
        if not prev or self.activity != "farm" or prev[1] != s["map"] or s["map"] != self.farm_map:
            return
        secs = now - prev[0]
        if secs <= 0 or secs > 30:
            return
        if s["blv"] == prev[2]:
            progress = max(0, s.get("bexp", 0) - prev[3]) / prev[4]
        elif s["blv"] == prev[2] + 1:
            progress = max(0, prev[4] - prev[3]) / prev[4] + s.get("bexp", 0) / (s.get("bnext") or 1)
        else:
            progress = 0
        self.memory.add_farm_time(s["map"], secs, progress)

    def choose_farm_map(self):
        spots = [sp["map"] for sp in farm_spots_for(self.world, self.state["blv"])]
        scored = [(self.memory.map_score(m), m) for m in spots]
        untried = [m for sc, m in scored if sc is None]
        known = sorted(((sc, m) for sc, m in scored if sc is not None), reverse=True)
        explore = self.rng.random() < 0.15
        if untried and (explore or not known):
            choice, why = self.rng.choice(untried), "ลองแมพใหม่"
        elif known and not explore:
            choice, why = known[0][1], "คุ้มที่สุด ~%.0f%%/ชม." % (known[0][0] * 100)
        else:
            choice, why = self.rng.choice(spots), "สุ่ม"
        log.info("%s: farm map %s (%s)", self.name, choice, why)
        self.farm_since = time.monotonic()
        return choice

    def should_rethink_map(self):
        # re-evaluate every 30 minutes of farming
        return self.farm_map is not None and time.monotonic() - self.farm_since > 1800
