"""Party behaviour: when a real player is in the bot's party, the bot follows
them, fights the same monsters and (healers) keeps the party alive."""

import logging
import time

log = logging.getLogger("aibot.party")


def cheb(ax, ay, bx, by):
    return max(abs(ax - bx), abs(ay - by))


class PartyMixin:
    async def refresh_party(self):
        s = self.state
        if not s.get("party"):
            if self.partner:
                log.info("%s: party with %s ended", self.name, self.partner)
            self.party_members = []
            self.partner = None
            return
        if not self.cooldown("partyinfo", 4):
            return
        res = await self.cmd("party_info")
        if not res.get("ok"):
            return
        me = s["id"]
        self.party_members = [m for m in res["members"] if m.get("cid") != me]
        humans = [m for m in self.party_members if m.get("online") and not m.get("bot")]
        if not humans and self.brain.config.get("party_with_bots"):
            # bot-only parties travel together behind their leader
            humans = [m for m in self.party_members if m.get("online") and m.get("leader")]
        # prefer the leader, then whoever the bot knows best
        humans.sort(key=lambda m: (not m.get("leader"), -self.memory.friendship(m["name"])))
        partner = humans[0]["name"] if humans else None
        if partner and partner != self.partner:
            log.info("%s: now partying with %s", self.name, partner)
            self.memory.partied(partner)
            self.memory.note("ปาร์ตี้กับ %s" % partner)
        if partner:
            self.partner_seen = time.monotonic()
        self.partner = partner or self.partner

    def partner_member(self):
        return next((m for m in self.party_members if m["name"] == self.partner), None)

    async def party_support(self):
        """Heal / resurrect / buff party members near the bot. Returns True when it cast something."""
        s = self.state
        heal = self.job.get("heal")
        res_skill = self.job.get("resurrect")
        buffs = self.job.get("party_buffs") or []
        if not (heal or res_skill or buffs) or s.get("casting"):
            return False
        near = [m for m in self.party_members if m.get("map") == s["map"] and "x" in m and cheb(s["x"], s["y"], m["x"], m["y"]) <= 9]

        if res_skill and self.sp_ratio > 0.3:
            for m in near:
                if m.get("dead"):
                    res = await self.cmd("skill", skill=res_skill, target=m["id"])
                    if res.get("ok"):
                        self.brain.stats["resurrects"] += 1
                        await self.cmd("party_chat", msg=self.p.phrase("resurrect", self.rng).format(name=m["name"]))
                        return True

        if heal and self.sp_ratio > 0.1:
            hurt = [m for m in near if not m.get("dead") and m.get("mhp") and m["hp"] / m["mhp"] < 0.65]
            if hurt:
                m = min(hurt, key=lambda x: x["hp"] / x["mhp"])
                res = await self.cmd("skill", skill=heal, target=m["id"])
                if res.get("ok"):
                    self.brain.stats["party_heals"] += 1
                    return True

        if buffs and self.sp_ratio > 0.35:
            now = time.monotonic()
            for m in near + [{"id": s["aid"], "cid": s["id"], "name": self.name}]:
                if m.get("dead"):
                    continue
                for skill, every in buffs:
                    key = (m.get("cid"), skill)
                    if now - self.buff_times.get(key, 0) < every:
                        continue
                    res = await self.cmd("skill", skill=skill, target=m["id"])
                    self.buff_times[key] = now  # also when it failed (not learned yet)
                    if res.get("ok"):
                        self.brain.stats["party_buffs"] += 1
                        return True
        return False

    async def do_party(self):
        s = self.state
        p = self.partner_member()
        now = time.monotonic()

        if p is None or not p.get("online") or "x" not in p:
            if now - self.partner_seen > 300:
                await self.cmd("party_chat", msg=self.p.phrase("party_leave", self.rng))
                await self.cmd("party_leave")
                self.memory.note("ออกจากปาร์ตี้ของ %s (หายไปนาน)" % self.partner)
                self.partner = None
            return

        self.partner_seen = now
        if await self.party_support():
            return

        if p["map"] != s["map"]:
            if self.cooldown("follow_warp", 15):
                await self.cmd("warp", map=p["map"], x=p["x"] + self.rng.randint(-2, 2), y=p["y"] + self.rng.randint(-2, 2))
                if self.rng.random() < 0.4:
                    await self.cmd("party_chat", msg=self.p.phrase("party_follow", self.rng))
            return

        support_only = bool(self.job.get("heal")) and not self.job.get("attack") and self.job.get("resurrect")
        dist = cheb(s["x"], s["y"], p["x"], p["y"])

        # assist: the partner's target, or whatever is hitting the partner / us
        if not support_only:
            target = p.get("target") or 0
            if not target:
                scan = await self.cmd("scan", range=10)
                mobs = scan.get("scan", {}).get("mobs", []) if scan.get("ok") else []
                attackers = [m for m in mobs if m["target"] in (p["id"], s["aid"]) and m["boss"] != 2]
                if attackers:
                    target = min(attackers, key=lambda m: m["dist"])["id"]
            if target and s.get("target") != target:
                res = await self.cmd("attack", target=target)
                if res.get("ok"):
                    self.target = target
                    return
            if s.get("target"):
                return  # busy fighting next to the partner

        if dist > 4 and not s.get("walking"):
            await self.walk_to(p["x"] + self.rng.randint(-2, 2), p["y"] + self.rng.randint(-2, 2))
