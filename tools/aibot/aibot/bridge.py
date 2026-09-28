"""TCP client for the map-server AI bot bridge (src/map/aibot.cpp).

Protocol: one JSON object per line, UTF-8.
  request : {"rid": 1, "cmd": "walk", "bot": 150001, "x": 100, "y": 120}
  response: {"re": 1, "ok": true, ...}
  event   : {"ev": "state", "bots": [...]}
"""

import asyncio
import json
import logging
import time
from collections import defaultdict

log = logging.getLogger("aibot.bridge")


class BridgeError(Exception):
    pass


class TokenBucket:
    """Global command rate limiter so a swarm of bots cannot flood the map-server."""

    def __init__(self, rate_per_sec, burst=None):
        self.rate = float(rate_per_sec)
        self.capacity = float(burst or rate_per_sec)
        self.tokens = self.capacity
        self.last = time.monotonic()

    async def take(self):
        if self.rate <= 0:
            return
        while True:
            now = time.monotonic()
            self.tokens = min(self.capacity, self.tokens + (now - self.last) * self.rate)
            self.last = now
            if self.tokens >= 1:
                self.tokens -= 1
                return
            await asyncio.sleep((1 - self.tokens) / self.rate)


class LatencyStats:
    def __init__(self):
        self.samples = []
        self.errors = defaultdict(int)
        self.timeouts = 0

    def add(self, ms):
        self.samples.append(ms)
        if len(self.samples) > 20000:
            self.samples = self.samples[-10000:]

    def summary(self):
        if not self.samples:
            return {"count": 0}
        s = sorted(self.samples)
        pick = lambda p: s[min(len(s) - 1, int(len(s) * p))]
        return {
            "count": len(s),
            "avg_ms": round(sum(s) / len(s), 2),
            "p50_ms": round(pick(0.50), 2),
            "p95_ms": round(pick(0.95), 2),
            "p99_ms": round(pick(0.99), 2),
            "max_ms": round(s[-1], 2),
            "timeouts": self.timeouts,
            "errors": dict(self.errors),
        }


class Bridge:
    def __init__(self, host="127.0.0.1", port=7100, token="", max_commands_per_sec=300, timeout=5.0):
        self.host = host
        self.port = port
        self.token = token
        self.timeout = timeout
        self.limiter = TokenBucket(max_commands_per_sec, burst=max_commands_per_sec)
        self.stats = LatencyStats()
        self._reader = None
        self._writer = None
        self._rid = 0
        self._pending = {}
        self._handlers = defaultdict(list)
        self._read_task = None
        self._write_lock = asyncio.Lock()
        self.connected = asyncio.Event()
        self.disconnected = asyncio.Event()

    # -- events ---------------------------------------------------------
    def on(self, event, handler):
        """Register a handler (sync or async) for an event name, or '*' for all."""
        self._handlers[event].append(handler)

    async def _dispatch(self, msg):
        for handler in self._handlers.get(msg["ev"], []) + self._handlers.get("*", []):
            try:
                result = handler(msg)
                if asyncio.iscoroutine(result):
                    await result
            except Exception:
                log.exception("event handler failed for %s", msg.get("ev"))

    # -- connection -----------------------------------------------------
    async def connect(self):
        self._reader, self._writer = await asyncio.open_connection(self.host, self.port, limit=4 * 1024 * 1024)
        self.disconnected.clear()
        self._read_task = asyncio.create_task(self._read_loop())
        if self.token:
            res = await self.call("auth", token=self.token)
            if not res.get("ok"):
                raise BridgeError("authentication failed: %s" % res.get("error"))
        self.connected.set()
        log.info("connected to bridge %s:%s", self.host, self.port)

    async def close(self):
        if self._writer:
            self._writer.close()
            try:
                await self._writer.wait_closed()
            except Exception:
                pass

    async def _read_loop(self):
        try:
            while True:
                line = await self._reader.readline()
                if not line:
                    break
                try:
                    msg = json.loads(line.decode("utf-8", "replace"))
                except ValueError:
                    log.warning("bad line from server: %r", line[:200])
                    continue
                if "re" in msg:
                    fut = self._pending.pop(msg["re"], None)
                    if fut and not fut.done():
                        fut.set_result(msg)
                elif "ev" in msg:
                    asyncio.create_task(self._dispatch(msg))
        except (ConnectionError, asyncio.IncompleteReadError):
            pass
        finally:
            self.connected.clear()
            self.disconnected.set()
            for fut in self._pending.values():
                if not fut.done():
                    fut.set_exception(BridgeError("disconnected"))
            self._pending.clear()
            log.warning("bridge connection closed")

    # -- commands -------------------------------------------------------
    async def call(self, cmd, timeout=None, **params):
        """Send a command and wait for its response dict ({"ok": bool, ...})."""
        if self._writer is None or self.disconnected.is_set():
            raise BridgeError("not connected")
        if cmd != "auth":
            await self.limiter.take()
        self._rid += 1
        rid = self._rid
        fut = asyncio.get_running_loop().create_future()
        self._pending[rid] = fut
        params.update(rid=rid, cmd=cmd)
        data = (json.dumps(params, ensure_ascii=False) + "\n").encode("utf-8")
        start = time.perf_counter()
        async with self._write_lock:
            self._writer.write(data)
            await self._writer.drain()
        try:
            res = await asyncio.wait_for(fut, timeout or self.timeout)
        except asyncio.TimeoutError:
            self._pending.pop(rid, None)
            self.stats.timeouts += 1
            raise BridgeError("timeout waiting for %s" % cmd)
        self.stats.add((time.perf_counter() - start) * 1000)
        if not res.get("ok"):
            self.stats.errors[res.get("error", "unknown")] += 1
        return res
