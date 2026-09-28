"""Reader for rAthena map_cache.dat so the brain can pick walkable cells
(wandering, un-stuck moves) without asking the server."""

import logging
import os
import random
import struct
import zlib

log = logging.getLogger("aibot.mapcache")

WALKABLE = (0, 3)  # gat types: 0 = walkable ground, 3 = walkable water


class MapGrid:
    def __init__(self, name, xs, ys, cells):
        self.name = name
        self.xs = xs
        self.ys = ys
        self.cells = cells

    def walkable(self, x, y):
        if x <= 0 or y <= 0 or x >= self.xs - 1 or y >= self.ys - 1:
            return False
        return self.cells[x + y * self.xs] in WALKABLE

    def random_walkable_near(self, x, y, radius, rng=random, tries=40):
        for _ in range(tries):
            nx = x + rng.randint(-radius, radius)
            ny = y + rng.randint(-radius, radius)
            if (nx, ny) != (x, y) and self.walkable(nx, ny):
                return nx, ny
        return None

    def open_cells_around(self, x, y):
        """Number of walkable neighbours (0-8). Low values mean corners / narrow spots."""
        return sum(
            1
            for dx in (-1, 0, 1)
            for dy in (-1, 0, 1)
            if (dx or dy) and self.walkable(x + dx, y + dy)
        )


class MapCache:
    def __init__(self):
        self.maps = {}

    def load(self, path):
        if not os.path.exists(path):
            log.warning("map cache %s not found", path)
            return
        with open(path, "rb") as fp:
            data = fp.read()
        _, count = struct.unpack_from("<IH", data, 0)
        off = 8  # header is padded to 8 bytes
        for _ in range(count):
            name = data[off:off + 12].split(b"\0")[0].decode("latin1")
            xs, ys, length = struct.unpack_from("<hhi", data, off + 12)
            off += 20
            self.maps[name] = (xs, ys, off, length, data)
            off += length
        log.info("map cache %s: %d maps", path, count)

    def get(self, name):
        entry = self.maps.get(name)
        if entry is None:
            return None
        if isinstance(entry, MapGrid):
            return entry
        xs, ys, off, length, data = entry
        grid = MapGrid(name, xs, ys, zlib.decompress(data[off:off + length]))
        self.maps[name] = grid
        return grid
