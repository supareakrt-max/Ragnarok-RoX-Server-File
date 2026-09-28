"""Brain: owns the bridge connection, the agents, and routes server events."""

import asyncio
import csv
import logging
import os
import random
import time
from collections import Counter, defaultdict

from .agent import BotAgent
from .bridge import Bridge, BridgeError
from .mapcache import MapCache
from .personality import load_personalities
from .routine import Clock
from .social import LLMClient
from .world import load_world

log = logging.getLogger("aibot.brain")


class Brain:
    def __init__(self, config, base_dir="."):
        self.config = config
        self.base_dir = base_dir
        srv = config.get("server", {})
        self.bridge = Bridge(
            host=srv.get("host", "127.0.0.1"),
            port=int(srv.get("port", 7100)),
            token=srv.get("token", ""),
            max_commands_per_sec=float(config.get("max_commands_per_sec", 300)),
            timeout=float(srv.get("timeout", 5)),
        )
        self.clock = Clock(config.get("time_scale", 1.0), config.get("start_hour"))
        self.world = load_world(config.get("world"))
        self.personalities = load_personalities(config.get("personalities"))
        self.llm = LLMClient(config.get("llm"), base_dir)
        self.mapcache = MapCache()
        for path in config.get("map_cache", []):
            self.mapcache.load(path if os.path.isabs(path) else os.path.join(base_dir, path))

        mem = config.get("memory_dir", "memory")
        self.memory_dir = mem if os.path.isabs(mem) else os.path.join(base_dir, mem)
        self.item_cache = {}
        self.agents = {}
        self.by_id = {}
        self.stats = Counter()
        self.stuck_spots = defaultdict(int)
        self.stuck_file = config.get("stuck_report")
        self.max_chat_responders = int(config.get("max_chat_responders", 2))
        self.spawn_interval = float(config.get("spawn_interval", 1.5))

        for spec in config.get("bots", []):
            pname = spec.get("personality") or random.choice(sorted(self.personalities))
            if pname not in self.personalities:
                log.warning("unknown personality %s for %s, using chill", pname, spec["name"])
                pname = "chill"
            self.agents[spec["name"]] = BotAgent(self, spec, self.personalities[pname])

        b = self.bridge
        b.on("state", self.on_state)
        b.on("spawned", self.on_spawned)
        b.on("map_changed", self.on_map_changed)
        b.on("logout", self.on_logout)
        b.on("login_failed", self.on_login_failed)
        b.on("chat", self.on_chat)
        b.on("whisper", self.on_whisper)
        b.on("party_chat", self.on_party_chat)
        b.on("party_invite", self.on_party_invite)
        b.on("emotion", self.on_emotion)

    def grid(self, mapname):
        return self.mapcache.get(mapname) if mapname else None

    # ------------------------------------------------------------------
    # lifecycle
    # ------------------------------------------------------------------
    async def run(self):
        tasks = [asyncio.create_task(a.run()) for a in self.agents.values()]
        tasks.append(asyncio.create_task(self.report_loop()))
        backoff = 1
        while True:
            try:
                await self.bridge.connect()
                backoff = 1
                await self.sync_and_login()
                await self.bridge.disconnected.wait()
            except (OSError, BridgeError) as exc:
                log.warning("bridge unavailable (%s), retrying in %ss", exc, backoff)
            for agent in self.agents.values():
                agent.spawned.clear()
            await asyncio.sleep(backoff)
            backoff = min(backoff * 2, 30)

    async def sync_and_login(self):
        res = await self.bridge.call("list")
        online = {b["name"]: b for b in res.get("bots", [])}
        for name, agent in self.agents.items():
            if name in online:
                info = online[name]
                self.bind(agent, info["id"])
                if not info.get("pending"):
                    agent.state = info
                    agent.spawned.set()
        for agent in self.agents.values():
            if agent.name not in online:
                await self.login(agent)
                await asyncio.sleep(self.spawn_interval * random.uniform(0.5, 1.5))

    async def login(self, agent):
        res = await self.bridge.call("login", name=agent.name)
        if res.get("id"):
            self.bind(agent, res["id"])
        if not res.get("ok"):
            log.warning("login %s failed: %s", agent.name, res.get("error"))
            if res.get("error") in ("account_in_use", "char_server_offline"):
                asyncio.create_task(self.relogin_later(agent, 60))
        return res

    async def relogin_later(self, agent, delay):
        await asyncio.sleep(delay)
        if not agent.spawned.is_set() and self.bridge.connected.is_set():
            try:
                await self.login(agent)
            except BridgeError:
                pass

    def bind(self, agent, char_id):
        agent.id = char_id
        self.by_id[char_id] = agent

    # ------------------------------------------------------------------
    # events
    # ------------------------------------------------------------------
    def on_state(self, ev):
        for s in ev["bots"]:
            agent = self.by_id.get(s["id"])
            if agent:
                agent.state = s

    def on_spawned(self, ev):
        agent = self.by_id.get(ev["bot"])
        if agent:
            agent.state = ev["state"]
            agent.spawned.set()
            log.info("%s spawned at %s (%s,%s) [%s]", agent.name, ev["state"]["map"], ev["state"]["x"], ev["state"]["y"], agent.p.name)

    def on_map_changed(self, ev):
        agent = self.by_id.get(ev["bot"])
        if agent:
            agent.goal = None
            agent.target = None
            agent.last_pos = None

    def on_logout(self, ev):
        agent = self.by_id.get(ev["bot"])
        if agent:
            log.warning("%s was logged out by the server", agent.name)
            agent.spawned.clear()
            asyncio.create_task(self.relogin_later(agent, 30))

    def on_login_failed(self, ev):
        agent = self.by_id.get(ev["bot"])
        if agent:
            asyncio.create_task(self.relogin_later(agent, 60))

    def on_chat(self, ev):
        self.stats["chat_in"] += 1
        listeners = [self.by_id[b] for b in ev["bots"] if b in self.by_id and self.by_id[b].spawned.is_set()]
        if not listeners:
            return
        text = ev["msg"].lower()
        # everyone remembers, but only a couple of bots may answer one line
        mentioned = [a for a in listeners if a.name.lower() in text]
        others = [a for a in listeners if a not in mentioned]
        random.shuffle(others)
        responders = (mentioned + others)[: self.max_chat_responders]
        for agent in listeners:
            if agent in responders:
                asyncio.create_task(agent.on_chat(ev))
            else:
                agent.chat.add(ev["from"], ev["msg"])
                if not ev.get("from_bot"):
                    agent.memory.chatted(ev["from"], ev["msg"])

    def on_whisper(self, ev):
        agent = self.by_id.get(ev["bot"])
        if agent:
            self.stats["whispers_in"] += 1
            asyncio.create_task(agent.on_whisper(ev))

    def on_party_chat(self, ev):
        for b in ev["bots"]:
            agent = self.by_id.get(b)
            if agent:
                asyncio.create_task(agent.on_party_chat(ev))
                break  # one bot answers per line

    def on_emotion(self, ev):
        if ev.get("from_bot"):
            return
        listeners = [self.by_id[b] for b in ev["bots"] if b in self.by_id and self.by_id[b].spawned.is_set()]
        random.shuffle(listeners)
        for agent in listeners[:2]:
            asyncio.create_task(agent.on_emotion(ev))

    def save_memories(self):
        for agent in self.agents.values():
            agent.memory.save()

    def on_party_invite(self, ev):
        agent = self.by_id.get(ev["bot"])
        if agent:
            agent.on_party_invite(ev)

    # ------------------------------------------------------------------
    # reporting (Phase 4)
    # ------------------------------------------------------------------
    def report_stuck(self, agent, mapname, x, y):
        self.stats["stuck_events"] += 1
        key = (mapname, x // 5 * 5, y // 5 * 5)
        self.stuck_spots[key] += 1
        if self.stuck_file:
            path = self.stuck_file if os.path.isabs(self.stuck_file) else os.path.join(self.base_dir, self.stuck_file)
            new = not os.path.exists(path)
            with open(path, "a", newline="", encoding="utf-8") as fp:
                w = csv.writer(fp)
                if new:
                    w.writerow(["time", "bot", "map", "x", "y", "open_neighbours"])
                grid = self.grid(mapname)
                w.writerow([time.strftime("%Y-%m-%d %H:%M:%S"), agent.name, mapname, x, y, grid.open_cells_around(x, y) if grid else ""])

    async def report_loop(self):
        interval = float(self.config.get("report_interval", 60))
        while True:
            await asyncio.sleep(interval)
            online = [a for a in self.agents.values() if a.spawned.is_set()]
            acts = Counter(a.activity for a in online)
            lat = self.bridge.stats.summary()
            log.info(
                "[report %s] online %d/%d | %s | cmds %s avg %sms p95 %sms | %s",
                self.clock.hhmm(), len(online), len(self.agents),
                ", ".join("%s=%d" % kv for kv in sorted(acts.items(), key=lambda kv: str(kv[0]))),
                lat.get("count"), lat.get("avg_ms"), lat.get("p95_ms"),
                ", ".join("%s=%d" % kv for kv in sorted(self.stats.items())),
            )
            if self.llm.enabled:
                log.info("[report] LLM %s | %s", self.llm.stats, self.llm.summary())
            if self.stuck_spots:
                top = sorted(self.stuck_spots.items(), key=lambda kv: -kv[1])[:5]
                log.info("[report] stuck hotspots: %s", ", ".join("%s(%d,%d)x%d" % (k[0], k[1], k[2], v) for k, v in top))
