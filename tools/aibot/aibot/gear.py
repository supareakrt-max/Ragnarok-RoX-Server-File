"""Shopping brain: better potions as the bot levels, and gear upgrades from NPC shops."""

import logging

log = logging.getLogger("aibot.gear")

IT_ARMOR, IT_WEAPON = 4, 5
LOC_ACCESSORY = 8 | 128
LOC_SHIELD = 32
LOC_BOTH_HANDS = 2 | 32
LOC_AMMO = 32768
EMO_MONEY = 8


class GearMixin:
    async def item_info(self, ids):
        missing = [i for i in ids if i not in self.brain.item_cache]
        if missing:
            res = await self.cmd("iteminfo", items=missing)
            for it in res.get("items", []):
                # "usable" depends on the bot (class/level/sex): keep it per bot
                self.brain.item_cache[it["id"]] = it
        return [self.brain.item_cache[i] for i in ids if i in self.brain.item_cache]

    async def usable_items(self, ids):
        """iteminfo filtered for this bot (asks the server; class/level/sex dependent)."""
        res = await self.cmd("iteminfo", items=list(ids))
        return res.get("items", [])

    def potion_for_level(self):
        blv = self.state.get("blv", 1)
        best = self.p.potion_item
        for min_lv, item in self.world["potion_tiers"]:
            if blv >= min_lv:
                best = item
        return best

    async def buy_potions(self):
        zeny = self.state.get("zeny", 0)
        tiers = [item for min_lv, item in self.world["potion_tiers"] if self.state.get("blv", 1) >= min_lv]
        infos = {i["id"]: i for i in await self.item_info(tiers or [self.p.potion_item])}
        stock = self.p.potion_stock
        # best tier we can stock without spending more than 40% of our money
        choice = None
        for item in reversed(tiers or [self.p.potion_item]):
            price = infos.get(item, {}).get("buy", 0)
            if price and price * stock <= zeny * 0.4:
                choice = item
                break
        if choice is None:
            choice = self.p.potion_item
        self.hp_potion = choice
        have = self.count_item(choice)
        if have < stock:
            res = await self.cmd("buy", item=choice, amount=stock - have)
            if res.get("ok"):
                self.brain.stats["potions_bought"] += stock - have
        if self.p.sp_potion_item and self.count_item(self.p.sp_potion_item) < 10:
            await self.cmd("buy", item=self.p.sp_potion_item, amount=10)

    async def buy_gear(self):
        """Buy at most two upgrades per town visit, only once per base level."""
        s = self.state
        if self.gear_checked_blv == s["blv"]:
            return
        self.gear_checked_blv = s["blv"]
        await self.refresh_inventory(force=True)
        equipped = [i for i in self.inventory if i.get("equipped")]
        weapon = next((i for i in equipped if i["equipped"] & 2), None)
        two_handed = bool(weapon and (weapon["equipped"] & LOC_BOTH_HANDS) == LOC_BOTH_HANDS)
        weapon_sub = None
        if weapon:
            info = await self.item_info([weapon["id"]])
            weapon_sub = info[0].get("subtype") if info else None

        offers = await self.usable_items(self.world["shop_items"])
        budget = s.get("zeny", 0) * 0.5
        best_by_slot = {}
        for it in offers:
            if not it.get("usable") or it["type"] not in (IT_WEAPON, IT_ARMOR) or it["elv"] > s["blv"]:
                continue
            loc = it["loc"]
            if not loc or loc & (LOC_ACCESSORY | LOC_AMMO) or it["buy"] <= 0 or it["buy"] > budget:
                continue
            if it["type"] == IT_WEAPON:
                if weapon_sub is not None and it.get("subtype") != weapon_sub:
                    continue  # stay with the weapon family of the class (bows, rods, ...)
                key, stat = "weapon", "atk"
            else:
                if loc & LOC_SHIELD and two_handed:
                    continue
                key, stat = "armor-%d" % loc, "def"
            current = max((e.get(stat, 0) for e in equipped if e["equipped"] & loc), default=-1)
            gain = it.get(stat, 0) - current
            if gain <= 0:
                continue
            # value for money
            score = gain / (1 + it["buy"] / 1000.0)
            if key not in best_by_slot or score > best_by_slot[key][0]:
                best_by_slot[key] = (score, it)

        bought = 0
        for score, it in sorted(best_by_slot.values(), key=lambda x: -x[0])[:2]:
            if it["buy"] > self.state.get("zeny", 0) * 0.5:
                continue
            res = await self.cmd("buy", item=it["id"], amount=1)
            if not res.get("ok"):
                continue
            await self.refresh_inventory(force=True)
            new = next((i for i in self.inventory if i["id"] == it["id"] and not i.get("equipped")), None)
            if new and (await self.cmd("equip", idx=new["idx"])).get("ok"):
                bought += 1
                self.brain.stats["gear_bought"] += 1
                log.info("%s bought and equipped %s (%s z)", self.name, it["name"], it["buy"])
                self.memory.note("ซื้อ %s มาใส่ (%s z)" % (it["name"], it["buy"]))
        if bought:
            await self.emote(EMO_MONEY)
            if self.rng.random() < 0.6:
                await self.say(self.p.phrase("new_gear", self.rng))
            await self.refresh_inventory(force=True)
