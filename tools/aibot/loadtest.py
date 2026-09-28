#!/usr/bin/env python3
"""Phase 4 load test: spawn many bots that walk and scan at the same time and
measure how the map-server copes.

    python create_bots.py --count 200 --prefix LoadBot --out load.sql   # once
    python loadtest.py --count 200 --map prt_fild08 --duration 180 --pid <map-server pid>

Reports command latency (p50/p95/p99), state push jitter, stuck events and
map-server CPU / RAM (Linux /proc, or psutil on Windows if installed).
"""

import argparse
import asyncio
import json
import os
import random
import sys
import time

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from aibot.bridge import Bridge, BridgeError  # noqa: E402
from aibot.mapcache import MapCache  # noqa: E402


class ProcSampler:
    def __init__(self, pid):
        self.pid = pid
        self.samples = []
        self.psutil = None
        if pid and not os.path.exists("/proc/%d/stat" % pid):
            try:
                import psutil  # type: ignore
                self.psutil = psutil.Process(pid)
            except Exception:
                print("! cannot sample pid %s (no /proc and no psutil)" % pid)
                self.pid = None

    def _read(self):
        if self.psutil:
            t = self.psutil.cpu_times()
            return t.user + t.system, self.psutil.memory_info().rss
        with open("/proc/%d/stat" % self.pid) as fp:
            parts = fp.read().rsplit(")", 1)[1].split()
        ticks = os.sysconf("SC_CLK_TCK")
        cpu = (int(parts[11]) + int(parts[12])) / ticks
        rss = int(parts[21]) * os.sysconf("SC_PAGE_SIZE")
        return cpu, rss

    async def run(self, interval=2.0):
        if not self.pid:
            return
        last_cpu, _ = self._read()
        last_t = time.monotonic()
        while True:
            await asyncio.sleep(interval)
            cpu, rss = self._read()
            now = time.monotonic()
            self.samples.append(((cpu - last_cpu) / (now - last_t) * 100, rss / 1024 / 1024))
            last_cpu, last_t = cpu, now

    def summary(self):
        if not self.samples:
            return {}
        cpu = [s[0] for s in self.samples]
        rss = [s[1] for s in self.samples]
        return {"cpu_avg_pct": round(sum(cpu) / len(cpu), 1), "cpu_max_pct": round(max(cpu), 1),
                "rss_start_mb": round(rss[0], 1), "rss_max_mb": round(max(rss), 1), "rss_end_mb": round(rss[-1], 1)}


async def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--host", default="127.0.0.1")
    ap.add_argument("--port", type=int, default=7100)
    ap.add_argument("--token", default="")
    ap.add_argument("--prefix", default="LoadBot")
    ap.add_argument("--count", type=int, default=50)
    ap.add_argument("--map", default="prt_fild08")
    ap.add_argument("--duration", type=float, default=120)
    ap.add_argument("--interval", type=float, default=1.0, help="seconds between actions per bot")
    ap.add_argument("--rate", type=float, default=500, help="max commands per second (global)")
    ap.add_argument("--spawn-rate", type=float, default=10, help="logins per second")
    ap.add_argument("--map-cache", nargs="*", default=["../../db/map_cache.dat", "../../db/re/map_cache.dat"])
    ap.add_argument("--pid", type=int, help="map-server pid for CPU/RAM sampling")
    ap.add_argument("--keep", action="store_true", help="leave bots online afterwards")
    ap.add_argument("--json", help="write the report to this file")
    args = ap.parse_args()

    here = os.path.dirname(os.path.abspath(__file__))
    cache = MapCache()
    for p in args.map_cache:
        cache.load(p if os.path.isabs(p) else os.path.join(here, p))
    grid = cache.get(args.map)
    if grid is None:
        print("! map %s not in map cache, walking blind" % args.map)

    bridge = Bridge(args.host, args.port, args.token, max_commands_per_sec=args.rate, timeout=10)
    states = {}
    spawned = {}
    state_times = []
    stuck = {"events": 0}

    def on_state(ev):
        state_times.append(time.monotonic())
        for s in ev["bots"]:
            states[s["id"]] = s

    def on_spawned(ev):
        spawned[ev["bot"]] = time.monotonic()
        states[ev["bot"]] = ev["state"]

    bridge.on("state", on_state)
    bridge.on("spawned", on_spawned)
    await bridge.connect()

    sampler = ProcSampler(args.pid)
    sampler_task = asyncio.create_task(sampler.run())

    names = ["%s%04d" % (args.prefix, i + 1) for i in range(args.count)]
    ids = {}
    login_start = {}
    t0 = time.monotonic()
    for name in names:
        res = await bridge.call("login", name=name)
        if res.get("id"):
            ids[name] = res["id"]
            login_start[res["id"]] = time.monotonic()
        if not res.get("ok") and res.get("error") != "already_bot":
            print("! login %s: %s" % (name, res.get("error")))
        await asyncio.sleep(1.0 / args.spawn_rate)

    deadline = time.monotonic() + 30
    while len(spawned) < len(ids) and time.monotonic() < deadline:
        await asyncio.sleep(0.5)
    spawn_secs = [spawned[i] - login_start[i] for i in spawned if i in login_start]
    print("spawned %d/%d bots in %.1fs" % (len(spawned), len(names), time.monotonic() - t0))

    for cid in list(spawned):
        await bridge.call("warp", bot=cid, map=args.map, x=0, y=0)

    last_pos = {}
    stuck_counts = {}

    async def drive(cid):
        rng = random.Random(cid)
        await asyncio.sleep(rng.uniform(0, args.interval))
        while time.monotonic() < end:
            s = states.get(cid)
            try:
                if s and s.get("onmap") and not s.get("dead"):
                    pos = (s["x"], s["y"])
                    if last_pos.get(cid) == pos and s.get("walking"):
                        stuck_counts[cid] = stuck_counts.get(cid, 0) + 1
                        if stuck_counts[cid] >= 3:
                            stuck["events"] += 1
                            stuck_counts[cid] = 0
                    else:
                        stuck_counts[cid] = 0
                    last_pos[cid] = pos
                    if rng.random() < 0.3:
                        await bridge.call("scan", bot=cid, range=14)
                    if not s.get("walking"):
                        dest = grid.random_walkable_near(s["x"], s["y"], 10, rng) if grid else (s["x"] + rng.randint(-8, 8), s["y"] + rng.randint(-8, 8))
                        if dest:
                            await bridge.call("walk", bot=cid, x=dest[0], y=dest[1])
                elif s and s.get("dead"):
                    await bridge.call("respawn", bot=cid)
            except BridgeError:
                pass
            await asyncio.sleep(args.interval * rng.uniform(0.7, 1.3))

    end = time.monotonic() + args.duration
    print("running for %ds ..." % args.duration)
    await asyncio.gather(*(drive(cid) for cid in spawned))

    gaps = [b - a for a, b in zip(state_times, state_times[1:]) if b - a > 0.05]
    report = {
        "bots_requested": len(names),
        "bots_spawned": len(spawned),
        "spawn_time_avg_s": round(sum(spawn_secs) / len(spawn_secs), 2) if spawn_secs else None,
        "spawn_time_max_s": round(max(spawn_secs), 2) if spawn_secs else None,
        "commands": bridge.stats.summary(),
        "state_push_interval_avg_s": round(sum(gaps) / len(gaps), 3) if gaps else None,
        "state_push_interval_max_s": round(max(gaps), 3) if gaps else None,
        "stuck_events": stuck["events"],
        "map_server": sampler.summary(),
    }
    sampler_task.cancel()

    if not args.keep:
        for cid in spawned:
            try:
                await bridge.call("logout", bot=cid)
            except BridgeError:
                pass

    print(json.dumps(report, indent=2))
    if args.json:
        with open(args.json, "w") as fp:
            json.dump(report, fp, indent=2)
    await bridge.close()


if __name__ == "__main__":
    asyncio.run(main())
