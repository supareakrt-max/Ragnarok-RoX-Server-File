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
               "attack": [], "heal": "AL_HEAL", "party_buffs": [["AL_BLESSING", 180], ["AL_INCAGI", 180]]},
        "5": {"build": "melee", "learn": [["NV_BASIC", 9], ["MC_INCCARRY", 10], ["MC_DISCOUNT", 10], ["MC_OVERCHARGE", 10], ["MC_PUSHCART", 10], ["MC_MAMMONITE", 10]],
               "attack": [["MC_MAMMONITE", 5, 2]], "heal": None},
        "6": {"build": "melee", "learn": [["NV_BASIC", 9], ["TF_DOUBLE", 10], ["TF_MISS", 10], ["TF_STEAL", 10], ["TF_HIDING", 1], ["TF_POISON", 10]],
               "attack": [["TF_POISON", 12, 2]], "heal": None},
        # --- second classes -------------------------------------------------
        "7": {"build": "melee", "learn": [["KN_TWOHANDQUICKEN", 1], ["KN_AUTOCOUNTER", 5], ["KN_BOWLINGBASH", 10], ["KN_SPEARMASTERY", 10], ["KN_TWOHANDQUICKEN", 10], ["KN_RIDING", 1]],
               "attack": [["KN_BOWLINGBASH", 13, 2], ["SM_BASH", 8, 2], ["SM_MAGNUM", 15, 1]], "heal": None},
        "14": {"build": "tank", "learn": [["CR_TRUST", 10], ["CR_HOLYCROSS", 10], ["CR_GRANDCROSS", 5]],
               "attack": [["CR_HOLYCROSS", 20, 2], ["SM_BASH", 8, 2]], "heal": None},
        "9": {"build": "caster", "learn": [["WZ_EARTHSPIKE", 5], ["WZ_HEAVENDRIVE", 5], ["WZ_JUPITEL", 10], ["WZ_FROSTNOVA", 10], ["WZ_SIGHTRASHER", 10]],
               "attack": [["WZ_JUPITEL", 40, 9], ["WZ_EARTHSPIKE", 25, 9], ["MG_COLDBOLT", 12, 9], ["MG_FIREBOLT", 12, 9], ["MG_LIGHTNINGBOLT", 12, 9]],
               "heal": None, "skill_chance": 0.9},
        "16": {"build": "caster", "learn": [["SA_ADVANCEDBOOK", 5], ["SA_CASTCANCEL", 1], ["SA_FREECAST", 10], ["SA_AUTOSPELL", 5]],
               "attack": [["MG_COLDBOLT", 12, 9], ["MG_FIREBOLT", 12, 9], ["MG_LIGHTNINGBOLT", 12, 9], ["MG_SOULSTRIKE", 18, 9]],
               "heal": None, "skill_chance": 0.85},
        "11": {"build": "archer", "learn": [["HT_BEASTBANE", 10], ["HT_STEELCROW", 5]],
               "attack": [["AC_DOUBLE", 12, 9]], "heal": None, "skill_chance": 0.6},
        "8": {"build": "tank", "learn": [["PR_MACEMASTERY", 10], ["PR_IMPOSITIO", 5], ["PR_KYRIE", 10], ["PR_STRECOVERY", 1], ["ALL_RESURRECTION", 4], ["PR_MAGNIFICAT", 5], ["PR_GLORIA", 5]],
               "attack": [], "heal": "AL_HEAL", "resurrect": "ALL_RESURRECTION",
               "party_buffs": [["AL_BLESSING", 180], ["AL_INCAGI", 180], ["PR_KYRIE", 90], ["PR_IMPOSITIO", 55]]},
        "15": {"build": "melee", "learn": [["MO_IRONHAND", 10], ["MO_CALLSPIRITS", 5], ["MO_TRIPLEATTACK", 10], ["MO_SPIRITSRECOVERY", 5]],
               "attack": [], "heal": "AL_HEAL", "self_buffs": [["AL_BLESSING", 200], ["AL_INCAGI", 200]]},
        "10": {"build": "melee", "learn": [["BS_SKINTEMPER", 5], ["BS_HILTBINDING", 1], ["BS_ADRENALINE", 5], ["BS_WEAPONPERFECT", 3]],
               "attack": [["MC_MAMMONITE", 5, 2]], "heal": None, "self_buffs": [["BS_ADRENALINE", 100]]},
        "18": {"build": "melee", "learn": [["AM_AXEMASTERY", 10], ["AM_LEARNINGPOTION", 10]],
               "attack": [["MC_MAMMONITE", 5, 2]], "heal": None},
        "12": {"build": "melee", "learn": [["AS_RIGHT", 5], ["AS_LEFT", 5], ["AS_ENCHANTPOISON", 10], ["AS_CLOAKING", 2]],
               "attack": [["TF_POISON", 12, 2]], "heal": None},
        "17": {"build": "melee", "learn": [["RG_SNATCHER", 10], ["RG_STEALCOIN", 4], ["RG_BACKSTAP", 10], ["RG_TUNNELDRIVE", 5]],
               "attack": [["TF_POISON", 12, 2]], "heal": None},
    },
    "job_names": {
        "0": "Novice", "1": "Swordman", "2": "Mage", "3": "Archer", "4": "Acolyte", "5": "Merchant", "6": "Thief",
        "7": "Knight", "8": "Priest", "9": "Wizard", "10": "Blacksmith", "11": "Hunter", "12": "Assassin",
        "14": "Crusader", "15": "Monk", "16": "Sage", "17": "Rogue", "18": "Alchemist",
    },
    # Job change: from class -> required job level and the choices per personality
    # (first entry preferred; a bot spec may force "job2": <class id>).
    "promotions": {
        "0": {"jlv": 10, "to": {"hardcore": [1, 6, 3, 2], "merchant": [5], "chill": [4, 2, 3]}},
        "1": {"jlv": 40, "to": {"hardcore": [7], "merchant": [7], "chill": [14]}},
        "2": {"jlv": 40, "to": {"hardcore": [9], "merchant": [16], "chill": [16]}},
        "3": {"jlv": 40, "to": {"hardcore": [11], "merchant": [11], "chill": [11]}},
        "4": {"jlv": 40, "to": {"hardcore": [15], "merchant": [8], "chill": [8]}},
        "5": {"jlv": 40, "to": {"hardcore": [10], "merchant": [18], "chill": [18]}},
        "6": {"jlv": 40, "to": {"hardcore": [12], "merchant": [17], "chill": [17]}},
    },
    # Weapons and armor sold by NPC shops (npc/merchants/shops.txt). The bot asks the
    # server which ones it may use and buys an upgrade when it can afford it.
    "shop_items": [2101, 2103, 2401, 2403, 2405, 2501, 2503, 2505, 2203, 2201, 2205, 2226, 2301, 2303, 2305, 2321, 2328, 2332, 2307,
                   2309, 2312, 2314, 2628, 1101, 1104, 1107, 1201, 1204, 1207, 1601, 1701, 1301, 1351, 1354, 1357, 1360, 1146, 1245,
                   2228, 2105, 2316, 2627, 1210, 1213, 1216, 1219, 1222, 2211, 1122, 1116, 1154, 1407, 1457, 1519, 13003, 2335, 1901,
                   1903, 1905, 1909, 1911, 1907, 1950, 1952, 1954, 1958, 1960, 1956, 1401, 1404, 1451, 1454, 1460, 1463, 1410, 1247,
                   1248, 1249, 13000, 2107, 2230, 1604, 1607, 1610, 2224, 2232, 1151, 1157, 1160, 1250, 1252, 1254, 2218, 1704, 1707,
                   1710, 1713, 1714, 1718, 2208, 2212, 2330, 1110, 1113, 1119, 1123, 1126, 1129, 2220, 1801, 1803, 1805, 1501, 1504,
                   1507, 1510, 1513, 1807, 1811, 1809],
    # Potion bought for the bot's level: [min base level, item id]
    "potion_tiers": [[1, 501], [25, 502], [45, 503], [65, 504]],
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
