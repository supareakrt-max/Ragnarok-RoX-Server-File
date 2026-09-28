"""Static game knowledge used by the brain. Every value can be overridden in
config.json under "world" (same keys)."""

import copy

DEFAULT_WORLD = {
    # Town used by each bot to sell, buy and hang out. "spots" are walkable
    # cells picked at random for idling / chatting.
    "towns": {
        "prontera": {"center": [155, 187], "shop": [134, 221], "spots": [[155, 187], [146, 90], [116, 72], [156, 230]]},
    },
    "home_town": "prontera",
    # Farming maps by base level. The bot picks a random entry whose range fits.
    "farm_spots": [
        {"map": "prt_fild08", "min": 1, "max": 12},
        {"map": "prt_fild01", "min": 5, "max": 15},
        {"map": "pay_fild01", "min": 8, "max": 20},
        {"map": "prt_fild05", "min": 12, "max": 25},
        {"map": "prt_sewb1", "min": 18, "max": 32},
        {"map": "pay_dun00", "min": 25, "max": 40},
        {"map": "moc_fild12", "min": 25, "max": 40},
        {"map": "pay_dun01", "min": 35, "max": 50},
        {"map": "orcsdun01", "min": 40, "max": 60},
        {"map": "gef_fild10", "min": 40, "max": 60},
        {"map": "mjo_dun01", "min": 50, "max": 70},
        {"map": "ein_fild06", "min": 60, "max": 85},
        {"map": "gl_cas01", "min": 70, "max": 99},
    ],
    # Plants, mushrooms and eggs give no exp worth the time.
    "ignore_mobs": [1008, 1047, 1078, 1079, 1080, 1081, 1082, 1083, 1084, 1085, 1097],
    "hp_potions": [504, 503, 502, 501],  # best first
    "sp_potions": [505],
    # Items that trigger the "rare drop" celebration when they enter the bag.
    # Cards (type 6) always count.
    "rare_items": [603, 616, 617, 7020, 969],
    # Stat allocation patterns, repeated in order.
    "stat_builds": {
        "melee": ["str", "agi", "str", "agi", "vit", "dex"],
        "tank": ["vit", "str", "vit", "agi", "dex"],
        "caster": ["int", "dex", "int", "dex", "vit"],
        "archer": ["dex", "agi", "dex", "luk"],
    },
    # Per job class (id): stat build, skills to learn in order, combat skills as
    # [name, sp_cost, max_distance], a self heal skill and optional skill_chance.
    "jobs": {
        "0": {"build": "melee", "learn": [["NV_BASIC", 9]], "attack": [], "heal": None},
        "1": {"build": "melee", "learn": [["NV_BASIC", 9], ["SM_SWORD", 1], ["SM_BASH", 10], ["SM_RECOVERY", 5], ["SM_PROVOKE", 5], ["SM_MAGNUM", 10], ["SM_TWOHAND", 10], ["SM_ENDURE", 10]],
               "attack": [["SM_BASH", 8, 2], ["SM_MAGNUM", 15, 1]], "heal": None},
        "2": {"build": "caster", "learn": [["NV_BASIC", 9], ["MG_SRECOVERY", 2], ["MG_COLDBOLT", 10], ["MG_FIREBOLT", 10], ["MG_LIGHTNINGBOLT", 10], ["MG_SIGHT", 1], ["MG_NAPALMBEAT", 3], ["MG_SOULSTRIKE", 10], ["MG_SRECOVERY", 10]],
               "attack": [["MG_COLDBOLT", 12, 9], ["MG_FIREBOLT", 12, 9], ["MG_LIGHTNINGBOLT", 12, 9], ["MG_SOULSTRIKE", 18, 9]], "heal": None, "skill_chance": 0.9},
        "3": {"build": "archer", "learn": [["NV_BASIC", 9], ["AC_OWL", 10], ["AC_VULTURE", 10], ["AC_DOUBLE", 10], ["AC_CONCENTRATION", 10], ["AC_SHOWER", 10]],
               "attack": [["AC_DOUBLE", 12, 9]], "heal": None},
        "4": {"build": "tank", "learn": [["NV_BASIC", 9], ["AL_HEAL", 10], ["AL_DP", 3], ["AL_BLESSING", 10], ["AL_INCAGI", 10], ["AL_DEMONBANE", 10], ["AL_CURE", 1]],
               "attack": [], "heal": "AL_HEAL"},
        "5": {"build": "melee", "learn": [["NV_BASIC", 9], ["MC_INCCARRY", 10], ["MC_DISCOUNT", 10], ["MC_OVERCHARGE", 10], ["MC_PUSHCART", 10], ["MC_MAMMONITE", 10]],
               "attack": [["MC_MAMMONITE", 5, 2]], "heal": None},
        "6": {"build": "melee", "learn": [["NV_BASIC", 9], ["TF_DOUBLE", 10], ["TF_MISS", 10], ["TF_STEAL", 10], ["TF_HIDING", 1], ["TF_POISON", 10]],
               "attack": [["TF_POISON", 12, 2]], "heal": None},
    },
}


def load_world(overrides):
    world = copy.deepcopy(DEFAULT_WORLD)
    for key, value in (overrides or {}).items():
        world[key] = copy.deepcopy(value)
    return world


def farm_spots_for(world, level):
    spots = [s for s in world["farm_spots"] if s["min"] <= level <= s["max"]]
    if not spots:
        spots = sorted(world["farm_spots"], key=lambda s: abs((s["min"] + s["max"]) / 2 - level))[:1]
    return spots
