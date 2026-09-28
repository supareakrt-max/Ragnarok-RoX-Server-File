"""BotAgent: the decision loop of one bot.

Every tick (personality.tick +/- jitter) the agent looks at the latest state the
server pushed and decides what to do next:

    dead?          -> complain, wait, respawn
    points?        -> allocate stats / skills, change job when ready
    party invite?  -> accept or decline (friends more likely)
    survival       -> potion, heal, flee (teleport / go home)
    party          -> follow / assist / heal a real player partner
    routine        -> farm | town | rest | social
    stuck check    -> random side step, then teleport
    idle social    -> random chat, emotes, drama
"""

import asyncio
import logging
import random
import time

from .bridge import BridgeError
from .career import CareerMixin
from .gear import GearMixin
from .memory import BotMemory
from .party_ai import PartyMixin
from .planner import PlannerMixin
from .routine import Routine
from .social import ChatMemory, system_prompt, template_reply

log = logging.getLogger("aibot.agent")

IT_WEAPON, IT_ARMOR, IT_CARD = 5, 4, 6
EMO_HELP, EMO_CRY, EMO_BEST, EMO_DELIGHT, EMO_THANKS, EMO_MONEY, EMO_SURPRISE = 26, 28, 21, 2, 15, 8, 0


def dist(ax, ay, bx, by):
    return max(abs(ax - bx), abs(ay - by))


class BotAgent(CareerMixin, GearMixin, PartyMixin, PlannerMixin):
    def __init__(self, brain, spec, personality):
        self.brain = brain
        self.bridge = brain.bridge
        self.world = brain.world
        self.spec = spec
        self.name = spec["name"]
        self.p = personality
        self.rng = random.Random(hash(self.name) ^ int(time.time()))
        self.routine = Routine(personality, brain.clock, self.rng, brain.config.get("cycle"))
        self.chat = ChatMemory()
        self.memory = BotMemory(brain.memory_dir, self.name)

        self.id = None
        self.state = {}
        self.spawned = asyncio.Event()
        self.running = True

        self.activity = None
        self.farm_map = None
        self.goal = None
        self.target = None
        self.target_since = 0.0
        self.target_hp = None
        self.ignore = {}
        self.loot_target = None

        self.last_pos = None
        self.stuck_ticks = 0
        self.unstuck_attempts = 0

        self.inventory = []
        self.inventory_time = 0.0
        self.known_items = None
        self.last_potion = 0.0
        self.last_shout = {}
        self.last_reply = 0.0
        self.last_bot_reply = 0.0
        self.prev_level = None
        self.prev_dead = False
        self.dead_since = None
        self.stat_idx = 0
        self.skill_done = set()
        self.town_done = False
        self.rest_spot = None
        self.pending_invite = None
        self.want_invite = None

        # career / learning
        self.nothing_to_learn = False
        self.gear_checked_blv = None
        self.farm_sample = None
        self.farm_since = 0.0
        self.hp_potion = None
        # party
        self.party_members = []
        self.partner = None
        self.partner_seen = 0.0
        self.buff_times = {}
        # social
        self.greeted = {}
        # LLM planner
        self.plan = None
        self.planning = False
        self.next_plan_at = None

    # ------------------------------------------------------------------
    # helpers
    # ------------------------------------------------------------------
    async def cmd(self, name, **params):
        return await self.bridge.call(name, bot=self.id, **params)

    @property
    def hp_ratio(self):
        s = self.state
        return s["hp"] / s["mhp"] if s.get("mhp") else 1.0

    @property
    def sp_ratio(self):
        s = self.state
        return s["sp"] / s["msp"] if s.get("msp") else 1.0

    @property
    def job(self):
        jobs = self.world["jobs"]
        return jobs.get(str(self.state.get("class", 0)), jobs["0"])

    def cooldown(self, key, seconds):
        now = time.monotonic()
        if now - self.last_shout.get(key, 0) < seconds:
            return False
        self.last_shout[key] = now
        return True

    async def say(self, text, delay=True):
        if not text:
            return
        if delay:
            await asyncio.sleep(len(text) / self.p.typing_cps + self.rng.uniform(0.3, 1.5))
        res = await self.cmd("say", msg=text)
        if res.get("ok"):
            self.chat.add(self.name, text)
            self.brain.stats["chat_out"] += 1

    async def emote(self, emo):
        await self.cmd("emotion", type=emo)

    async def walk_to(self, x, y):
        self.goal = (self.state.get("map"), x, y)
        res = await self.cmd("walk", x=x, y=y)
        if not res.get("ok"):
            grid = self.brain.grid(self.state.get("map"))
            alt = grid.random_walkable_near(x, y, 4, self.rng) if grid else None
            if alt:
                res = await self.cmd("walk", x=alt[0], y=alt[1])
                self.goal = (self.state.get("map"), alt[0], alt[1])
            if not res.get("ok"):
                self.goal = None
        return res.get("ok", False)

    def arrived(self, x, y, radius=2):
        return dist(self.state["x"], self.state["y"], x, y) <= radius

    async def refresh_inventory(self, force=False):
        if not force and time.monotonic() - self.inventory_time < 20:
            return
        res = await self.cmd("inventory")
        if not res.get("ok"):
            return
        self.inventory = res["items"]
        self.inventory_time = time.monotonic()
        await self.check_new_items()

    def count_item(self, item_id):
        return sum(i["amount"] for i in self.inventory if i["id"] == item_id)

    # ------------------------------------------------------------------
    # main loop
    # ------------------------------------------------------------------
    async def run(self):
        while self.running:
            await self.bridge.connected.wait()
            await self.spawned.wait()
            try:
                await self.tick()
            except BridgeError as exc:
                log.debug("%s: bridge error %s", self.name, exc)
                await asyncio.sleep(2)
            except Exception:
                log.exception("%s: tick failed", self.name)
                await asyncio.sleep(3)
            await asyncio.sleep(self.p.next_tick(self.rng))

    async def tick(self):
        s = self.state
        if not s or not s.get("onmap"):
            return

        await self.track_progress()

        if s["dead"]:
            await self.handle_dead()
            return

        await self.allocate_points()
        if await self.maybe_change_job():
            return

        if self.pending_invite:
            await self.answer_invite()

        self.track_farming()
        self.memory.autosave()
        self.maybe_plan()

        if await self.survival():
            return

        await self.refresh_party()
        if self.partner:
            if self.activity != "party":
                log.info("%s: %s -> party with %s", self.name, self.activity, self.partner)
                self.activity = "party"
                self.goal = None
                if s.get("sit"):
                    await self.cmd("stand")
            await self.do_party()
            await self.check_stuck()
            return

        activity = self.routine.current()
        if activity != self.activity:
            log.info("%s: %s -> %s (%s)", self.name, self.activity, activity, self.brain.clock.hhmm())
            self.activity = activity
            self.goal = None
            self.target = None
            self.town_done = False
            self.rest_spot = None
            if activity != "rest" and s.get("sit"):
                await self.cmd("stand")

        if activity == "farm" and s["mw"] and s["w"] / s["mw"] >= self.p.sell_weight:
            if self.cooldown("selling", 300):
                await self.say(self.p.phrase("selling", self.rng))
            self.routine.force("town", 900)
            return

        handler = getattr(self, "do_" + activity, self.do_rest)
        await handler()

        await self.check_stuck()
        await self.idle_social()

    # ------------------------------------------------------------------
    # progress events: level up, death, rare drops
    # ------------------------------------------------------------------
    async def track_progress(self):
        s = self.state
        if self.prev_level is not None and s["blv"] > self.prev_level:
            self.brain.stats["levelups"] += 1
            await self.emote(EMO_BEST)
            if self.rng.random() < 0.7:
                await self.say(self.p.phrase("levelup", self.rng))
            self.farm_map = None  # maybe time for a better map
        self.prev_level = s["blv"]

        if s["dead"] and not self.prev_dead:
            self.brain.stats["deaths"] += 1
            if s["map"] == self.farm_map:
                self.memory.add_death(s["map"])
            self.dead_since = time.monotonic()
            self.target = None
            if self.rng.random() < 0.6:
                await self.emote(EMO_CRY)
                await self.say(self.p.phrase("death", self.rng))
        self.prev_dead = s["dead"]

    async def check_new_items(self):
        ids = {i["id"] for i in self.inventory}
        if self.known_items is not None:
            rare = self.world["rare_items"]
            for item in self.inventory:
                if item["id"] in self.known_items:
                    continue
                if item.get("type") == IT_CARD or item["id"] in rare:
                    self.brain.stats["rare_drops"] += 1
                    await self.emote(EMO_SURPRISE)
                    await self.say(self.p.phrase("rare_drop", self.rng))
                    break
        self.known_items = ids

    async def handle_dead(self):
        wait = self.rng.uniform(4, 15)
        if self.dead_since and time.monotonic() - self.dead_since > wait:
            res = await self.cmd("respawn")
            if res.get("ok"):
                self.dead_since = None
                self.farm_map = None
                self.routine.force("rest", self.rng.uniform(60, 180))

    # ------------------------------------------------------------------
    # stat and skill points
    # ------------------------------------------------------------------
    async def allocate_points(self):
        s = self.state
        if s.get("stpts", 0) > 0:
            build = self.world["stat_builds"].get(self.spec.get("build") or self.job["build"], ["str", "agi", "vit", "dex"])
            for _ in range(len(build)):
                stat = build[self.stat_idx % len(build)]
                self.stat_idx += 1
                res = await self.cmd("statup", stat=stat, amount=1)
                if res.get("ok"):
                    break
        if s.get("skpts", 0) > 0:
            learned = False
            for skill, max_lv in self.job["learn"]:
                if skill in self.skill_done:
                    continue
                res = await self.cmd("skillup", skill=skill, amount=1)
                lv = res.get("lv", 0)
                if lv >= max_lv:
                    self.skill_done.add(skill)
                if res.get("ok"):
                    learned = True
                    break
                # maxed, or requirements not met yet: try the next skill
            # nothing learnable with the current job: allow the job change anyway
            self.nothing_to_learn = not learned
        else:
            self.nothing_to_learn = False

    # ------------------------------------------------------------------
    # survival
    # ------------------------------------------------------------------
    async def survival(self):
        now = time.monotonic()
        heal = self.job.get("heal")
        if self.hp_ratio < self.p.potion_hp and now - self.last_potion > 1.0:
            self.last_potion = now
            if heal and self.sp_ratio > 0.15:
                res = await self.cmd("skill", skill=heal)
                if res.get("ok"):
                    return False
            await self.refresh_inventory()
            for pot in self.world["hp_potions"]:
                if self.count_item(pot) > 0:
                    res = await self.cmd("useitem", item=pot)
                    if res.get("ok"):
                        self.inventory_time = 0  # refresh count later
                        break

        if self.sp_ratio < self.p.potion_sp and now - self.last_potion > 1.0 and self.activity == "farm":
            await self.refresh_inventory()
            for pot in self.world["sp_potions"]:
                if self.count_item(pot) > 0:
                    self.last_potion = now
                    await self.cmd("useitem", item=pot)
                    break

        if self.hp_ratio < self.p.flee_hp and self.activity == "farm":
            self.brain.stats["flees"] += 1
            self.target = None
            if self.cooldown("flee", 60) and self.rng.random() < 0.5:
                asyncio.create_task(self.say(self.p.phrase("flee", self.rng), delay=False))
            if self.hp_ratio < self.p.flee_hp / 2:
                # "butterfly wing" home and rest
                await self.go_town()
                self.routine.force("rest", self.rng.uniform(90, 240))
            else:
                await self.cmd("stop")
                await self.cmd("tele")  # "fly wing"
            return True
        return False

    # ------------------------------------------------------------------
    # activities
    # ------------------------------------------------------------------
    async def go_town(self):
        town = self.world["towns"][self.world["home_town"]]
        cx, cy = town["center"]
        await self.cmd("warp", map=self.world["home_town"], x=cx + self.rng.randint(-5, 5), y=cy + self.rng.randint(-5, 5))

    async def do_farm(self):
        s = self.state
        if self.farm_map is None or (self.should_rethink_map() and self.target is None):
            self.farm_map = self.choose_farm_map()
        if s["map"] != self.farm_map:
            await self.cmd("warp", map=self.farm_map, x=0, y=0)
            return

        # regen while nothing attacks us
        if s.get("sit"):
            if self.hp_ratio > 0.9 and self.sp_ratio > 0.5:
                await self.cmd("stand")
            else:
                scan = await self.cmd("scan", range=9)
                if any(m["target"] == s["aid"] for m in scan.get("scan", {}).get("mobs", [])):
                    await self.cmd("stand")
                return

        res = await self.cmd("scan", range=14)
        if not res.get("ok"):
            return
        scan = res["scan"]
        mobs = scan["mobs"]
        now = time.monotonic()
        self.ignore = {k: v for k, v in self.ignore.items() if v > now}

        # MVP nearby: run away (unless hardcore enough)
        mvp = [m for m in mobs if m["boss"] == 2]
        if mvp and not self.p.fight_mvp:
            self.brain.stats["mvp_escapes"] += 1
            if self.cooldown("mvp", 120):
                await self.emote(EMO_HELP)
                asyncio.create_task(self.say(self.p.phrase("mvp", self.rng), delay=False))
            await self.cmd("tele")
            self.target = None
            return

        mine = [m for m in mobs if m["target"] == s["aid"]]
        await self.greet_players(scan["players"])

        # surrounded: teleport out before it goes wrong
        if len(mine) >= 3 and self.hp_ratio < 0.7:
            self.brain.stats["swarm_escapes"] += 1
            await self.cmd("tele")
            self.target = None
            return

        # keep current target if it is still alive and progress is being made
        current = next((m for m in mobs if m["id"] == self.target), None)
        if current:
            if self.target_hp is not None and current["hp"] >= self.target_hp and now - self.target_since > 12:
                # can't reach / can't hurt it: give up for a while
                self.ignore[current["id"]] = now + 30
                self.target = None
                await self.cmd("stop")
            else:
                if current["hp"] < (self.target_hp or current["mhp"] + 1):
                    self.target_since = now
                self.target_hp = current["hp"]
                if s.get("target") != current["id"] and not s.get("casting"):
                    await self.cmd("attack", target=current["id"])
                await self.maybe_use_skill(current)
                return

        self.target = None
        caster = self.job.get("skill_chance", 0) >= 0.8
        if not mine and (self.hp_ratio < 0.45 or (caster and self.sp_ratio < 0.2)):
            await self.cmd("sit")
            return

        # loot before looking for the next fight
        if self.p.loot and not mine and await self.do_loot(scan["items"]):
            return

        candidates = mine or [
            m for m in mobs
            if m["boss"] == 0
            and m["mob_id"] not in self.world["ignore_mobs"]
            and m["id"] not in self.ignore
            and m["lv"] <= s["blv"] + self.p.level_margin
            and m["target"] in (0, s["aid"])
        ]
        if candidates and (mine or self.rng.random() < self.p.aggression):
            candidates.sort(key=lambda m: (m["dist"], m["lv"]))
            pick = self.rng.choice(candidates[:3])
            res = await self.cmd("attack", target=pick["id"])
            if res.get("ok"):
                self.target = pick["id"]
                self.target_since = now
                self.target_hp = pick["hp"]
                self.goal = None
                await self.maybe_use_skill(pick)
                return
            self.ignore[pick["id"]] = now + 20

        await self.maybe_invite(scan["players"])
        await self.roam()

    async def maybe_use_skill(self, mob):
        skills = [sk for sk in (self.job.get("attack") or []) if mob["dist"] <= sk[2]]
        chance = self.job.get("skill_chance", self.p.skill_chance)
        if not skills or self.rng.random() > chance or self.state.get("casting"):
            return
        skill, sp_cost, _ = self.rng.choice(skills)
        if self.state["sp"] < sp_cost * 2:
            return
        res = await self.cmd("skill", skill=skill, target=mob["id"])
        if res.get("ok"):
            self.brain.stats["skills_used"] += 1

    async def do_loot(self, items):
        s = self.state
        mine = [
            i for i in items
            if i["dist"] <= self.p.loot_range and i["owner"] in (0, s["id"]) and i["id"] not in self.ignore
        ]
        if not mine:
            self.loot_target = None
            return False
        item = min(mine, key=lambda i: i["dist"])
        if item["dist"] <= 1:
            res = await self.cmd("pickup", target=item["id"])
            if not res.get("ok"):
                self.ignore[item["id"]] = time.monotonic() + 60
            else:
                self.brain.stats["loot"] += 1
                self.inventory_time = 0
            return True
        if self.loot_target == item["id"] and self.state.get("walking"):
            return True
        self.loot_target = item["id"]
        if not await self.walk_to(item["x"], item["y"]):
            self.ignore[item["id"]] = time.monotonic() + 60
        return True

    async def roam(self):
        s = self.state
        if s.get("walking"):
            return
        grid = self.brain.grid(s["map"])
        step = self.p.roam_step
        dest = grid.random_walkable_near(s["x"], s["y"], step, self.rng) if grid else (
            s["x"] + self.rng.randint(-step, step), s["y"] + self.rng.randint(-step, step))
        if dest:
            await self.walk_to(*dest)

    async def do_town(self):
        s = self.state
        town_name = self.world["home_town"]
        town = self.world["towns"][town_name]
        if s["map"] != town_name:
            await self.go_town()
            return
        if not self.town_done:
            sx, sy = town["shop"]
            if not self.arrived(sx, sy, 4):
                if not s.get("walking"):
                    await self.walk_to(sx, sy)
                return
            await self.shop()
            self.town_done = True
            self.routine.clear_override()
            return
        await self.hang_out(town, sit_chance=0.2)

    async def shop(self):
        keep = list(self.world["hp_potions"]) + list(self.world["sp_potions"]) + list(self.p.keep_items)
        res = await self.cmd("sell", loot=True, keep=keep)
        if res.get("ok") and res.get("zeny_gained"):
            self.brain.stats["zeny_earned"] += res["zeny_gained"]
            await self.emote(EMO_MONEY)
        await self.refresh_inventory(force=True)
        await self.buy_potions()
        await self.refresh_inventory(force=True)
        await self.upgrade_equipment()
        await self.buy_gear()

    async def upgrade_equipment(self):
        blv = self.state["blv"]
        equipped = [i for i in self.inventory if i.get("equipped")]
        for item in self.inventory:
            if item.get("equipped") or not item.get("can_equip") or not item.get("loc"):
                continue
            if item.get("elv", 0) > blv or item.get("type") not in (IT_WEAPON, IT_ARMOR):
                continue
            key = "atk" if item["type"] == IT_WEAPON else "def"
            current = [e for e in equipped if e["equipped"] & item["loc"]]
            best_now = max((e.get(key, 0) for e in current), default=-1)
            if item.get(key, 0) > best_now:
                res = await self.cmd("equip", idx=item["idx"])
                if res.get("ok"):
                    log.info("%s equipped %s", self.name, item.get("name"))
                    self.brain.stats["equips"] += 1
                    await self.refresh_inventory(force=True)
                    return

    async def do_resupply(self):
        """Cycle mode town break: sell + buy first, then sit down and rest."""
        if not self.town_done:
            await self.do_town()
            return
        await self.do_rest()

    async def do_rest(self):
        s = self.state
        town_name = self.world["home_town"]
        town = self.world["towns"][town_name]
        if s["map"] != town_name:
            await self.go_town()
            return
        if self.rest_spot is None:
            sx, sy = self.rng.choice(town["spots"])
            self.rest_spot = (sx + self.rng.randint(-3, 3), sy + self.rng.randint(-3, 3))
        if not self.arrived(*self.rest_spot, radius=2):
            if not s.get("walking"):
                if not await self.walk_to(*self.rest_spot):
                    self.rest_spot = None
            return
        self.goal = None
        if not s.get("sit"):
            await self.cmd("sit")

    async def do_social(self):
        s = self.state
        town_name = self.world["home_town"]
        town = self.world["towns"][town_name]
        if s["map"] != town_name:
            await self.go_town()
            return
        # every bot picks the same hangout for the current game hour so they gather
        hour = int(self.brain.clock.minute_of_day() // 60)
        sx, sy = town["spots"][hour % len(town["spots"])]
        if not self.arrived(sx, sy, 6):
            if not s.get("walking"):
                await self.walk_to(sx + self.rng.randint(-3, 3), sy + self.rng.randint(-3, 3))
            return
        self.goal = None
        await self.hang_out(town, sit_chance=0.4, radius=3)

    async def hang_out(self, town, sit_chance, radius=None):
        s = self.state
        if s.get("sit"):
            if self.rng.random() < 0.02:
                await self.cmd("stand")
            return
        roll = self.rng.random()
        if roll < 0.05 * sit_chance:
            await self.cmd("sit")
        elif roll < 0.12 and not s.get("walking"):
            grid = self.brain.grid(s["map"])
            dest = grid.random_walkable_near(s["x"], s["y"], radius or self.p.wander_radius, self.rng) if grid else None
            if dest:
                await self.walk_to(*dest)

    # ------------------------------------------------------------------
    # stuck detection (Phase 4: pathfinding check)
    # ------------------------------------------------------------------
    async def check_stuck(self):
        s = self.state
        pos = (s["map"], s["x"], s["y"])
        moving_intent = self.goal is not None and not s.get("sit") and not s.get("casting") and self.target is None
        if moving_intent and pos == self.last_pos:
            if self.goal and self.arrived(self.goal[1], self.goal[2], 1):
                self.goal = None
                self.stuck_ticks = 0
            else:
                self.stuck_ticks += 1
        else:
            self.stuck_ticks = 0
            if pos != self.last_pos:
                self.unstuck_attempts = 0
        self.last_pos = pos

        if self.stuck_ticks < 3:
            return
        self.stuck_ticks = 0
        self.unstuck_attempts += 1
        self.brain.report_stuck(self, s["map"], s["x"], s["y"])
        grid = self.brain.grid(s["map"])
        if self.unstuck_attempts >= 3 or grid is None:
            self.brain.stats["stuck_teleports"] += 1
            self.goal = None
            self.unstuck_attempts = 0
            await self.cmd("tele")
            return
        dest = grid.random_walkable_near(s["x"], s["y"], 3 + self.unstuck_attempts * 2, self.rng)
        if dest:
            self.brain.stats["unstuck_steps"] += 1
            await self.cmd("walk", x=dest[0], y=dest[1])

    # ------------------------------------------------------------------
    # social life
    # ------------------------------------------------------------------
    async def idle_social(self):
        r = self.rng.random
        if r() < self.p.emote_chance:
            await self.emote(self.rng.choice(self.p.emotes))
        if r() < self.p.idle_chat_chance and self.cooldown("idle", 120):
            await self.say(self.p.phrase("bored", self.rng))
        if r() < self.p.drama_chance and self.cooldown("drama", 600):
            await self.drama()

    async def drama(self):
        """Random events that make the chat feel alive."""
        self.brain.stats["drama"] += 1
        kind = self.rng.choice(["refine_fail", "refine_fail", "bored", "lag"])
        if kind == "refine_fail":
            await self.emote(EMO_CRY)
            await self.say(self.p.phrase("refine_fail", self.rng))
        elif kind == "lag":
            await self.say(self.rng.choice(["แลคป่ะ?", "เน็ตกระตุกจัง", "วาร์ปเองเฉยเลย"]))
        else:
            await self.say(self.p.phrase("bored", self.rng))

    async def maybe_invite(self, players):
        s = self.state
        if self.want_invite:
            target = self.want_invite
            if s["party"]:
                self.want_invite = None
                res = await self.cmd("party_invite", name=target)
                if res.get("ok"):
                    self.brain.stats["party_invites"] += 1
                    await self.cmd("whisper", to=target, msg=self.p.phrase("party_invite", self.rng))
            return
        if s["party"] or self.rng.random() > self.p.invite_party:
            return
        real = [p for p in players if not p["bot"] and not p["party"] and abs(p["blv"] - s["blv"]) <= 10 and p["dist"] <= 10]
        if not real:
            return
        self.want_invite = self.rng.choice(real)["name"]
        await self.cmd("party_create", name=(self.name[:14] + " Party"))

    async def answer_invite(self):
        inv = self.pending_invite
        if time.monotonic() < inv["answer_at"]:
            return
        self.pending_invite = None
        chance = self.p.accept_party * (0.5 if inv.get("from_bot") else 1.0)
        if not inv.get("from_bot"):
            chance = min(0.98, chance + 0.08 * self.memory.friendship(inv["from"]))
        accept = self.rng.random() < chance
        res = await self.cmd("party_reply", accept=accept)
        if not res.get("ok"):
            return
        if accept:
            self.brain.stats["party_joins"] += 1
            await asyncio.sleep(self.rng.uniform(1, 3))
            await self.cmd("party_chat", msg=self.p.phrase("party_join", self.rng))
        elif not inv.get("from_bot"):
            await self.cmd("whisper", to=inv["from"], msg=self.p.phrase("party_decline", self.rng))

    # ------------------------------------------------------------------
    # incoming events (called by Brain)
    # ------------------------------------------------------------------
    def on_party_invite(self, ev):
        ev = dict(ev)
        ev["answer_at"] = time.monotonic() + self.rng.uniform(2, 6)
        self.pending_invite = ev

    def context_line(self, speaker=None):
        s = self.state
        line = "%s Lv.%s/%s อยู่แมพ %s กำลัง%s" % (
            self.job_name(), s.get("blv"), s.get("jlv"), s.get("map"),
            {"farm": "เก็บเวล", "town": "ขายของในเมือง", "resupply": "กลับเมืองมาซื้อของกับพัก", "rest": "นั่งพัก", "social": "เดินเล่นคุยกับเพื่อน",
             "party": "ปาร์ตี้เก็บเวลกับ %s" % self.partner}.get(self.activity, "เดินเล่น"),
        )
        goal = self.plan_goal()
        if goal:
            line += ". เป้าหมายตอนนี้: " + goal
        if speaker:
            line += ". " + self.memory.describe(speaker)
        diary = self.memory.data["log"][-2:]
        if diary:
            line += ". เรื่องล่าสุดของคุณ: " + " / ".join(d["text"] for d in diary)
        return line

    async def compose_reply(self, speaker, text, mentioned, remember=True):
        if remember:
            self.chat.add(speaker, text)
        reply = None
        if self.brain.llm.allowed(self.name):
            reply = await self.brain.llm.reply(system_prompt(self.name, self.p, self.context_line(speaker)), self.chat.as_messages(self.name), bot_name=self.name)
        if not reply:
            reply = template_reply(self.p, text, self.rng, mentioned=mentioned, busy=self.target is not None)
            # greet people it knows by name
            if reply and self.memory.friendship(speaker) >= 3 and self.rng.random() < 0.5:
                reply = self.p.phrase("greet_known", self.rng).format(name=speaker)
        return reply

    async def on_chat(self, ev):
        text = ev["msg"]
        self.chat.add(ev["from"], text)
        if not ev.get("from_bot"):
            self.memory.chatted(ev["from"], text)
        mentioned = self.name.lower() in text.lower()
        if ev.get("from_bot"):
            if time.monotonic() - self.last_bot_reply < 45:
                return
            chance = self.p.bot_chat_chance * (3 if mentioned else 1)
        else:
            chance = 0.9 if mentioned else self.p.chattiness
        if self.rng.random() > chance or time.monotonic() - self.last_reply < 4:
            return
        reply = await self.compose_reply(ev["from"], text, mentioned, remember=False)
        if not reply:
            return
        self.last_reply = time.monotonic()
        if ev.get("from_bot"):
            self.last_bot_reply = self.last_reply
        await self.say(reply)

    async def on_whisper(self, ev):
        if not ev.get("from_bot"):
            self.memory.chatted(ev["from"], ev["msg"])
        if ev.get("from_bot") and self.rng.random() > 0.3:
            return
        if self.rng.random() > 0.9:
            return  # sometimes "afk"
        reply = await self.compose_reply(ev["from"], ev["msg"], True)
        reply = reply or self.p.phrase("whisper_reply", self.rng)
        await asyncio.sleep(len(reply) / self.p.typing_cps + self.rng.uniform(1, 3))
        await self.cmd("whisper", to=ev["from"], msg=reply)
        self.brain.stats["chat_out"] += 1

    async def greet_players(self, players):
        """Say hi to real players passing by; known players get greeted by name."""
        now = time.monotonic()
        for pl in players:
            if pl.get("bot") or pl["dist"] > 7:
                continue
            new_meeting = self.memory.saw(pl["name"])
            if not new_meeting or now - self.greeted.get(pl["name"], 0) < 1800:
                continue
            self.greeted[pl["name"]] = now
            friend = self.memory.friendship(pl["name"])
            if friend >= 3 and self.rng.random() < 0.7:
                await self.emote(EMO_DELIGHT)
                await self.say(self.p.phrase("greet_known", self.rng).format(name=pl["name"]))
            elif self.rng.random() < self.p.chattiness * 0.3:
                if self.rng.random() < 0.5:
                    await self.emote(self.rng.choice([EMO_DELIGHT, 18, 33]))
                else:
                    await self.say(self.p.phrase("greet", self.rng))
            return  # one greeting per tick is enough

    async def on_emotion(self, ev):
        """Answer emotes from players nearby."""
        if not self.cooldown("emote_back", 8) or self.rng.random() > 0.6:
            return
        answers = {
            2: [2, 18], 18: [18, 2], 3: [3, 2], 15: [33, 18], 21: [21, 2], 1: [1, 22], 0: [0, 1],
            28: [17, 28], 7: [17, 4], 6: [4, 17], 16: [16, 18], 33: [33], 38: [38, 21],
        }
        await asyncio.sleep(self.rng.uniform(0.5, 2.0))
        await self.emote(self.rng.choice(answers.get(ev["type"], [18, 2])))

    async def on_party_chat(self, ev):
        chance = self.p.bot_chat_chance if ev.get("from_bot") else max(self.p.chattiness, 0.4)
        if self.rng.random() > chance:
            return
        reply = await self.compose_reply(ev["from"], ev["msg"], self.name.lower() in ev["msg"].lower())
        if reply:
            await asyncio.sleep(len(reply) / self.p.typing_cps + self.rng.uniform(0.5, 2))
            await self.cmd("party_chat", msg=reply)
