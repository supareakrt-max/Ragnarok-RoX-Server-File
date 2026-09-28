"""Daily Routine Engine: maps the (optionally accelerated) clock to an activity
per bot, using the personality schedule with a per-bot random offset so bots
don't all switch at the same minute."""

import datetime
import random
import time

ACTIVITIES = ("farm", "town", "rest", "social", "resupply")


def _minutes(hhmm):
    h, m = hhmm.split(":")
    return int(h) * 60 + int(m)


class Clock:
    """Game clock. time_scale=24 means one full day passes every real hour."""

    def __init__(self, time_scale=1.0, start_hour=None):
        self.time_scale = float(time_scale)
        self.real_start = time.time()
        now = datetime.datetime.now()
        base = now.hour * 60 + now.minute + now.second / 60.0
        self.start_minutes = float(start_hour) * 60 if start_hour is not None else base

    def minute_of_day(self):
        elapsed_min = (time.time() - self.real_start) / 60.0 * self.time_scale
        return (self.start_minutes + elapsed_min) % 1440

    def hhmm(self):
        m = int(self.minute_of_day())
        return "%02d:%02d" % (m // 60, m % 60)


class Routine:
    def __init__(self, personality, clock, rng=None, cycle=None):
        self.rng = rng or random.Random()
        self.clock = clock
        # cycle mode: ignore the clock, farm for a long stretch, then go back to
        # town to sell/buy and rest for a few minutes, and repeat
        cycle = personality.data.get("cycle") or cycle
        self.cycle = cycle if cycle and cycle.get("enabled", True) else None
        if self.cycle:
            self.farm_range = [float(v) for v in self.cycle.get("farm_minutes", [60, 120])]
            self.town_range = [float(v) for v in self.cycle.get("town_minutes", [5, 10])]
            self.phase = "farm"
            # the first stretch is random so bots don't all walk back to town together
            self.phase_until = time.monotonic() + self.rng.uniform(0.2, 1.0) * self._length(self.farm_range)
        self.offset = self.rng.uniform(-1, 1) * personality.schedule_offset_minutes
        self.blocks = [
            (_minutes(b["from"]), _minutes(b["to"]), b["activity"]) for b in personality.schedule
        ]
        self.override = None
        self.override_until = 0.0

    def force(self, activity, seconds):
        """Temporarily override the schedule (e.g. go to town to sell)."""
        self.override = activity
        self.override_until = time.monotonic() + seconds

    def clear_override(self):
        self.override = None

    def _length(self, rng_minutes):
        lo, hi = rng_minutes[0], rng_minutes[-1]
        return self.rng.uniform(min(lo, hi), max(lo, hi)) * 60

    def current(self):
        if self.override and time.monotonic() < self.override_until:
            return self.override
        self.override = None
        if self.cycle:
            now = time.monotonic()
            if now >= self.phase_until:
                self.phase = "resupply" if self.phase == "farm" else "farm"
                self.phase_until = now + self._length(self.town_range if self.phase == "resupply" else self.farm_range)
            return self.phase
        minute = (self.clock.minute_of_day() + self.offset) % 1440
        for start, end, activity in self.blocks:
            if start <= minute < end:
                return activity
        return "rest"
