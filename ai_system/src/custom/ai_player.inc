// Ro-X Player AI (phases 1-6)
// Included from src/custom/script.inc (compiled inside src/map/script.cpp).
//
// Script commands:
//   ai_on {<char id>};        -> 1 = running, -1 = in town, -2 = dead, -3 = overweight (90%), -4 = no AI time
//   ai_off {<char id>};       -> 1 = stopped, 0 = was not running
//   ai_status({<char id>})    -> 1 = running, 0 = off
//   ai_maplist({<char id>})   -> number of monster kinds alive on the player's map, fills
//                                @ai_mob_id[], @ai_mob_cnt[], @ai_mob_boss[] (0 normal, 1 boss, 2 MVP)
//   ai_skilltype(<skill id>)  -> 1 = attack skill (target / ground), 2 = self-only buff,
//                                3 = support skill without status (Heal ...), 4 = buff castable on others,
//                                0 = not usable by the AI
//   ai_skillname(<skill id>)  -> skill name (e.g. "Bash")
//
// Settings = permanent character variables (set by npc/ai_menu.txt):
//   AI_RANGE       search radius 3-14 (default 9)       AI_NOBOSS    1 = skip boss / MVP
//   AI_SKILL[0-4]  attack skills in priority order      AI_SKSP      use attack skills only when SP% >= this
//   AI_BUFF[0-5]   self buffs, recast when they end
//   AI_HPPCT / AI_HPITEM   drink AI_HPITEM when HP% < AI_HPPCT (same for AI_SPPCT / AI_SPITEM)
//   AI_LOOT        0 = no loot, 1 = loot everything, 2 = loot only AI_LOOTID[0-19]
//   AI_RT_HP       go home (Butterfly Wing) when HP% < this (0 = off)
//   AI_RT_POT      go home when potions run out: 1 = HP potion, 2 = SP potion, 3 = both
//   AI_RT_WEIGHT   go home when weight% >= this (0 = off)
//   AI_FLYWING     use Fly Wing when no monster was found for this many seconds (0 = off)
//   AI_ROLE        0 = farm, 1 = support (follow a party member, heal and buff the party)
//   AI_LEADER      char id to follow (support)          AI_FOLLOW    follow distance 1-10 (default 3)
//   AI_HEALSK[0]   heal skill (support)                 AI_HEALPCT   heal members below this HP% (default 70)
//   AI_PBUFF[0-5]  party buffs, cast on members that miss them; self-cast party skills
//                  (Magnificat, Gloria, Angelus ...) are recast when any member misses them
//   AI_RES         1 = resurrect dead party members (needs Resurrection)
//   #AI_TIME       account variable: AI time left in seconds, counted down while the AI runs
//                  in farm mode (support mode is free)
//   AI_MM_<map>    per-map monster mode: 0 = attack all, 1 = only listed, 2 = all except listed
//   AI_ML_<map>$   per-map monster list, e.g. ",1002,1063,"
//
// The AI keeps running across map changes. Farm mode pauses in towns and stops on death,
// weight >= 90%, no AI time left or after a Butterfly Wing trip home. Support mode pauses
// while dead / overweight, follows the leader through warp portals and waits when the
// leader is gone. Everything stops on logout.

#include <unordered_map>
#include <unordered_set>
#include <vector>
#include <string>
#include <algorithm>

static const int32 AI_PLAYER_INTERVAL = 300;         // think every 300 ms
static const int32 AI_PLAYER_TARGET_TIMEOUT = 15000; // drop a target we could not hurt in 15 s
static const int32 AI_PLAYER_LOOT_TIMEOUT = 6000;    // give up an item we could not reach in 6 s
static const int32 AI_PLAYER_MAX_SKILL = 5;
static const int32 AI_PLAYER_MAX_BUFF = 6;
static const int32 AI_PLAYER_MAX_LOOT = 20;
static const int32 AI_PLAYER_MAX_PBUFF = 6;
static const t_itemid AI_PLAYER_FLY_WING = 601;
static const t_itemid AI_PLAYER_BUTTERFLY_WING = 602;

struct s_ai_player {
	int16 m;                   // map the AI was started on
	int32 target_id;           // current monster
	t_tick target_tick;        // when the current target was picked
	t_tick next_roam;          // next time we may walk around when no monster is found
	t_tick last_mob_tick;      // last time a monster was found (Fly Wing timer)
	int32 loot_id;             // floor item we are walking to
	t_tick loot_tick;
	std::vector<int32> loot_ignore;     // floor items we failed to pick up
	std::vector<int32> mob_ignore;      // monsters we could not reach / hurt
	t_tick ignore_reset;       // when the ignore lists are cleared
	t_tick last_tick;          // AI time accounting
	int32 time_ms;             // used AI time not yet taken from #AI_TIME
	bool time_warned;          // "10 minutes left" was shown
	t_tick leader_lost;        // support: since when the leader is not on this map (0 = here)
	int16 leader_m, leader_x, leader_y; // support: where the leader was last seen
	bool waiting_notice;       // "waiting for the leader" was shown
	bool town_notice;          // "paused in town" was shown
	int64 reg_mode;            // AI_MM_<map>
	int64 reg_list;            // AI_ML_<map>$
	std::string list_raw;      // cached AI_ML_<map>$ value
	std::unordered_set<int32> list;     // parsed AI_ML_<map>$
};

enum e_ai_player_stop : int32 {
	AI_PLAYER_OK = 0,
	AI_PLAYER_STOP_DEAD,
	AI_PLAYER_STOP_MAP,
	AI_PLAYER_STOP_WEIGHT,
	AI_PLAYER_STOP_TOWN,
	AI_PLAYER_HOME_HP,
	AI_PLAYER_HOME_HPPOT,
	AI_PLAYER_HOME_SPPOT,
	AI_PLAYER_HOME_WEIGHT,
	AI_PLAYER_STOP_NOWING,
	AI_PLAYER_STOP_TIME,
	AI_PLAYER_STOP_LEADER,
	AI_PLAYER_PAUSE,           // do nothing this time, keep the AI running
};

static std::unordered_map<uint32, s_ai_player> ai_player_list; // key: char_id
static int32 ai_player_timer_id = INVALID_TIMER;

/// Registry keys of the settings, created once.
struct s_ai_player_regs {
	int64 range, noboss, skill, sksp, buff, hppct, hpitem, sppct, spitem;
	int64 loot, lootid, rt_hp, rt_pot, rt_weight, flywing;
	int64 role, leader, follow, healsk, healpct, pbuff, res, time;

	s_ai_player_regs() {
		range = add_str("AI_RANGE");     noboss = add_str("AI_NOBOSS");
		skill = add_str("AI_SKILL");     sksp = add_str("AI_SKSP");
		buff = add_str("AI_BUFF");
		hppct = add_str("AI_HPPCT");     hpitem = add_str("AI_HPITEM");
		sppct = add_str("AI_SPPCT");     spitem = add_str("AI_SPITEM");
		loot = add_str("AI_LOOT");       lootid = add_str("AI_LOOTID");
		rt_hp = add_str("AI_RT_HP");     rt_pot = add_str("AI_RT_POT");
		rt_weight = add_str("AI_RT_WEIGHT");
		flywing = add_str("AI_FLYWING");
		role = add_str("AI_ROLE");       leader = add_str("AI_LEADER");
		follow = add_str("AI_FOLLOW");   healsk = add_str("AI_HEALSK");
		healpct = add_str("AI_HEALPCT"); pbuff = add_str("AI_PBUFF");
		res = add_str("AI_RES");         time = add_str("#AI_TIME");
	}
};

static const s_ai_player_regs& ai_player_reg() {
	static s_ai_player_regs regs;
	return regs;
}

static int64 ai_player_get(map_session_data* sd, int64 reg, int32 index = 0) {
	return pc_readglobalreg(sd, index ? reference_uid(reg, index) : reg);
}

static const char* ai_player_stop_message(e_ai_player_stop reason) {
	switch (reason) {
		case AI_PLAYER_STOP_DEAD:   return "[AI] \313\302\330\264\267\323\247\322\271: \265\321\307\305\320\244\303\340\312\325\302\252\325\307\324\265";
		case AI_PLAYER_STOP_MAP:    return "[AI] \313\302\330\264\267\323\247\322\271: \302\351\322\302\341\274\271\267\325\350";
		case AI_PLAYER_STOP_WEIGHT: return "[AI] \313\302\330\264\267\323\247\322\271: \271\351\323\313\271\321\241\340\241\324\271 90%";
		case AI_PLAYER_STOP_TOWN:   return "[AI] \313\302\330\264\267\323\247\322\271: \315\302\331\350\343\271\340\301\327\315\247";
		case AI_PLAYER_HOME_HP:     return "[AI] HP \265\350\323 - \241\305\321\272\340\301\327\315\247\341\305\351\307";
		case AI_PLAYER_HOME_HPPOT:  return "[AI] \302\322 HP \313\301\264 - \241\305\321\272\340\301\327\315\247\341\305\351\307";
		case AI_PLAYER_HOME_SPPOT:  return "[AI] \302\322 SP \313\301\264 - \241\305\321\272\340\301\327\315\247\341\305\351\307";
		case AI_PLAYER_HOME_WEIGHT: return "[AI] \242\315\247\340\265\347\301 - \241\305\321\272\340\301\327\315\247\341\305\351\307";
		case AI_PLAYER_STOP_NOWING: return "[AI] \313\302\330\264\267\323\247\322\271: \265\351\315\247\241\305\321\272\340\301\327\315\247\341\265\350\344\301\350\301\325 Butterfly Wing \313\303\327\315\341\301\276\271\325\351\313\351\322\301\343\252\351";
		case AI_PLAYER_STOP_TIME:   return "[AI] \313\301\264\340\307\305\322\343\252\351\247\322\271 AI - \253\327\351\315\340\307\305\322\340\276\324\350\301\344\264\351\267\325\350 @ai";
		case AI_PLAYER_STOP_LEADER: return "[AI] \313\302\330\264\267\323\247\322\271: \244\271\267\325\350\265\322\301\344\301\350\315\302\331\350\343\271\341\301\276\271\325\351\340\241\324\271 60 \307\324\271\322\267\325";
		default:                    return nullptr;
	}
}

static bool ai_player_is_support(map_session_data* sd) {
	return ai_player_get(sd, ai_player_reg().role) == 1;
}

/// Conditions that block the AI. Farm mode: death / town / weight; support mode only
/// reports them so the caller can pause instead of stopping.
static e_ai_player_stop ai_player_check(map_session_data* sd) {
	if (pc_isdead(sd))
		return AI_PLAYER_STOP_DEAD;
	if (map_getmapflag(sd->m, MF_TOWN) && !ai_player_is_support(sd)) // support may follow in town
		return AI_PLAYER_STOP_TOWN;
	if (sd->max_weight > 0 && (uint64)sd->weight * 100 >= (uint64)sd->max_weight * 90)
		return AI_PLAYER_STOP_WEIGHT;
	return AI_PLAYER_OK;
}

static int32 ai_player_range(map_session_data* sd) {
	int32 range = (int32)ai_player_get(sd, ai_player_reg().range);
	if (range <= 0)
		range = 9;
	return cap_value(range, 3, AREA_SIZE);
}

static int32 ai_player_percent(uint32 value, uint32 max) {
	return max > 0 ? (int32)((uint64)value * 100 / max) : 100;
}

/// Re-read the per-map monster list when the NPC menu changed it.
static void ai_player_refresh_list(map_session_data* sd, s_ai_player& ai) {
	const char* raw = pc_readglobalreg_str(sd, ai.reg_list);
	if (raw == nullptr)
		raw = "";
	if (ai.list_raw == raw)
		return;
	ai.list_raw = raw;
	ai.list.clear();
	int32 value = 0;
	bool digit = false;
	for (const char* p = raw; ; p++) {
		if (*p >= '0' && *p <= '9') {
			value = value * 10 + (*p - '0');
			digit = true;
		} else {
			if (digit)
				ai.list.insert(value);
			value = 0;
			digit = false;
			if (*p == '\0')
				break;
		}
	}
}

/// Can the character walk there? (the server refuses paths longer than max_walk_path)
static bool ai_player_can_walk(map_session_data* sd, int16 x, int16 y) {
	walkpath_data wpd;
	if (!path_search(&wpd, sd->m, sd->x, sd->y, x, y, 0, CELL_CHKNOPASS))
		return false;
	return wpd.path_len <= battle_config.max_walk_path;
}

/// Is this monster a valid target for the player?
static bool ai_player_valid_target(map_session_data* sd, s_ai_player& ai, block_list* bl, bool noboss, int32 mode) {
	if (bl == nullptr || bl->type != BL_MOB || bl->m != sd->m)
		return false;
	mob_data* md = reinterpret_cast<mob_data*>(bl);
	if (status_isdead(*md))
		return false;
	if (md->special_state.ai) // summoned by a player (plants, spheres, ...)
		return false;
	if (std::find(ai.mob_ignore.begin(), ai.mob_ignore.end(), bl->id) != ai.mob_ignore.end())
		return false;
	if (noboss && md->get_bosstype() != BOSSTYPE_NONE)
		return false;
	// Monsters attacking the player are always fought back, whatever the monster list says
	bool attacking_me = md->target_id == sd->id;
	if (!attacking_me && mode == 1 && ai.list.count(md->mob_id) == 0)
		return false;
	if (!attacking_me && mode == 2 && ai.list.count(md->mob_id) > 0)
		return false;
	if (battle_check_target(sd, md, BCT_ENEMY) <= 0)
		return false;
	// Do not steal a monster that is fighting another player
	if (md->target_id != 0 && md->target_id != sd->id) {
		block_list* tbl = map_id2bl(md->target_id);
		if (tbl != nullptr && tbl->type == BL_PC)
			return false;
	}
	return true;
}

static int32 ai_player_search_sub(block_list* bl, va_list ap) {
	map_session_data* sd = va_arg(ap, map_session_data*);
	s_ai_player* ai = va_arg(ap, s_ai_player*);
	int32* best_id = va_arg(ap, int32*);
	int32* best_dist = va_arg(ap, int32*);
	bool noboss = va_arg(ap, int32) != 0;
	int32 mode = va_arg(ap, int32);

	if (!ai_player_valid_target(sd, *ai, bl, noboss, mode))
		return 0;
	int32 dist = distance_bl(sd, bl);
	if (*best_id != 0 && dist >= *best_dist)
		return 0;
	if (!ai_player_can_walk(sd, bl->x, bl->y))
		return 0;
	*best_id = bl->id;
	*best_dist = dist;
	return 1;
}

/// Should the AI pick up this floor item?
static bool ai_player_want_item(map_session_data* sd, s_ai_player& ai, block_list* bl, int32 loot_mode, t_tick tick) {
	if (bl == nullptr || bl->type != BL_ITEM || bl->m != sd->m)
		return false;
	flooritem_data* fitem = reinterpret_cast<flooritem_data*>(bl);
	if (std::find(ai.loot_ignore.begin(), ai.loot_ignore.end(), bl->id) != ai.loot_ignore.end())
		return false;
	// Still reserved for someone else
	if (fitem->first_get_charid > 0 && fitem->first_get_charid != sd->status.char_id && DIFF_TICK(tick, fitem->first_get_tick) < 0)
		return false;
	if (loot_mode == 2) {
		const s_ai_player_regs& reg = ai_player_reg();
		bool found = false;
		for (int32 i = 0; i < AI_PLAYER_MAX_LOOT && !found; i++) {
			int64 id = ai_player_get(sd, reg.lootid, i);
			if (id > 0 && (t_itemid)id == fitem->item.nameid)
				found = true;
		}
		if (!found)
			return false;
	}
	return true;
}

static int32 ai_player_loot_sub(block_list* bl, va_list ap) {
	map_session_data* sd = va_arg(ap, map_session_data*);
	s_ai_player* ai = va_arg(ap, s_ai_player*);
	int32* best_id = va_arg(ap, int32*);
	int32* best_dist = va_arg(ap, int32*);
	int32 loot_mode = va_arg(ap, int32);
	t_tick tick = va_arg(ap, t_tick);

	if (!ai_player_want_item(sd, *ai, bl, loot_mode, tick))
		return 0;
	int32 dist = distance_bl(sd, bl);
	if (*best_id != 0 && dist >= *best_dist)
		return 0;
	if (!ai_player_can_walk(sd, bl->x, bl->y))
		return 0;
	*best_id = bl->id;
	*best_dist = dist;
	return 1;
}

static void ai_player_ignore_loot(s_ai_player& ai, int32 id) {
	if (ai.loot_ignore.size() >= 50)
		ai.loot_ignore.clear();
	ai.loot_ignore.push_back(id);
	ai.loot_id = 0;
}

/// Walk to / pick up the current loot. Returns true while busy looting.
static bool ai_player_do_loot(map_session_data* sd, s_ai_player& ai, int32 loot_mode, t_tick tick) {
	if (ai.loot_id == 0)
		return false;
	block_list* bl = map_id2bl(ai.loot_id);
	if (!ai_player_want_item(sd, ai, bl, loot_mode, tick)) {
		ai.loot_id = 0;
		return false;
	}
	if (DIFF_TICK(tick, ai.loot_tick) > AI_PLAYER_LOOT_TIMEOUT) {
		ai_player_ignore_loot(ai, ai.loot_id);
		return false;
	}
	if (check_distance_bl(sd, bl, 1)) {
		if (!pc_takeitem(sd, reinterpret_cast<flooritem_data*>(bl)))
			ai_player_ignore_loot(ai, ai.loot_id);
		else
			ai.loot_id = 0;
		return true;
	}
	if (!unit_is_walking(sd) && !unit_walktoxy(sd, bl->x, bl->y, 0))
		ai_player_ignore_loot(ai, ai.loot_id);
	return true;
}

static bool ai_player_skill_ready(map_session_data* sd, uint16 skill_id, uint16 skill_lv) {
	if (skill_lv == 0)
		return false;
	if (sd->scd.find(skill_id) != sd->scd.end()) // cooldown
		return false;
	if ((int32)sd->battle_status.sp < skill_get_sp(skill_id, skill_lv))
		return false;
	return true;
}

/// Recast the first self buff that ran out. Returns true when a skill was used.
static bool ai_player_do_buff(map_session_data* sd) {
	const s_ai_player_regs& reg = ai_player_reg();
	for (int32 i = 0; i < AI_PLAYER_MAX_BUFF; i++) {
		uint16 skill_id = (uint16)ai_player_get(sd, reg.buff, i);
		if (skill_id == 0)
			continue;
		sc_type sc = skill_get_sc(skill_id);
		if (sc <= SC_NONE || sd->sc.getSCE(sc) != nullptr)
			continue;
		uint16 skill_lv = pc_checkskill(sd, skill_id);
		if (!ai_player_skill_ready(sd, skill_id, skill_lv))
			continue;
		if (unit_skilluse_id(sd, sd->id, skill_id, skill_lv))
			return true;
	}
	return false;
}

enum e_ai_player_approach : int32 {
	AI_PLAYER_IN_RANGE = 0,
	AI_PLAYER_WALKING,
	AI_PLAYER_UNREACHABLE,
};

/// Players are normally walked into range by the client, so the AI has to walk the
/// character itself.
static e_ai_player_approach ai_player_approach(map_session_data* sd, block_list* target, int32 range, bool attack) {
	if (battle_check_range(sd, target, range))
		return AI_PLAYER_IN_RANGE;
	if (unit_is_walking(sd) && sd->ud.target_to == target->id)
		return AI_PLAYER_WALKING;
	if (!unit_walktobl(sd, target, range, attack ? 2 : 0))
		return AI_PLAYER_UNREACHABLE;
	return AI_PLAYER_WALKING;
}

/// Use the first ready attack skill on the target. Returns true when a skill was used
/// or when the character is walking into skill range.
static bool ai_player_do_attack_skill(map_session_data* sd, block_list* target, bool& unreachable) {
	const s_ai_player_regs& reg = ai_player_reg();
	if (ai_player_percent(sd->battle_status.sp, sd->battle_status.max_sp) < (int32)ai_player_get(sd, reg.sksp))
		return false;
	for (int32 i = 0; i < AI_PLAYER_MAX_SKILL; i++) {
		uint16 skill_id = (uint16)ai_player_get(sd, reg.skill, i);
		if (skill_id == 0)
			continue;
		uint16 skill_lv = pc_checkskill(sd, skill_id);
		if (!ai_player_skill_ready(sd, skill_id, skill_lv))
			continue;
		switch (ai_player_approach(sd, target, skill_get_range2(sd, skill_id, skill_lv, true), false)) {
			case AI_PLAYER_WALKING: return true;
			case AI_PLAYER_UNREACHABLE: unreachable = true; return false;
			default: break;
		}
		int32 ok;
		if (skill_get_inf(skill_id) & INF_GROUND_SKILL)
			ok = unit_skilluse_pos(sd, target->x, target->y, skill_id, skill_lv);
		else
			ok = unit_skilluse_id(sd, target->id, skill_id, skill_lv);
		if (ok)
			return true;
	}
	return false;
}

/// Drink a potion from the inventory. Returns true when an item was used.
static bool ai_player_use_potion(map_session_data* sd, int64 pct_reg, int64 item_reg, uint32 value, uint32 max) {
	int32 pct = (int32)ai_player_get(sd, pct_reg);
	t_itemid nameid = (t_itemid)ai_player_get(sd, item_reg);
	if (pct <= 0 || nameid == 0 || ai_player_percent(value, max) >= pct)
		return false;
	int16 idx = pc_search_inventory(sd, nameid);
	if (idx < 0)
		return false;
	return pc_useitem(sd, idx) != 0;
}

static bool ai_player_has_item(map_session_data* sd, int64 item_reg) {
	t_itemid nameid = (t_itemid)ai_player_get(sd, item_reg);
	return nameid != 0 && pc_search_inventory(sd, nameid) >= 0;
}

/// Go home with a Butterfly Wing (warp to the save point). Always ends the AI.
/// The wing is used directly instead of through its item script, because the item skill
/// silently fails while the character is still in attack delay.
static e_ai_player_stop ai_player_go_home(map_session_data* sd, e_ai_player_stop reason) {
	int16 idx = pc_search_inventory(sd, AI_PLAYER_BUTTERFLY_WING);
	unit_stop_attack(sd);
	if (idx < 0 || map_getmapflag(sd->m, MF_NORETURN))
		return AI_PLAYER_STOP_NOWING;
	if (pc_delitem(sd, idx, 1, 0, 0, LOG_TYPE_CONSUME) != 0)
		return AI_PLAYER_STOP_NOWING;
	pc_setpos(sd, mapindex_name2id(sd->status.save_point.map), sd->status.save_point.x, sd->status.save_point.y, CLR_TELEPORT);
	return reason;
}

/// Random teleport with a Fly Wing. Returns true when the character was warped.
static bool ai_player_fly_wing(map_session_data* sd) {
	int16 idx = pc_search_inventory(sd, AI_PLAYER_FLY_WING);
	if (idx < 0 || map_getmapflag(sd->m, MF_NOTELEPORT))
		return false;
	unit_stop_attack(sd);
	if (pc_delitem(sd, idx, 1, 0, 0, LOG_TYPE_CONSUME) != 0)
		return false;
	pc_randomwarp(sd, CLR_TELEPORT);
	return true;
}

/// Attack the target: skill first, else walk into range and use normal attacks.
/// Returns false when the target cannot be reached.
static bool ai_player_fight(map_session_data* sd, block_list* target) {
	bool unreachable = false;
	if (ai_player_do_attack_skill(sd, target, unreachable))
		return true;
	if (unreachable)
		return false;
	switch (ai_player_approach(sd, target, status_get_range(sd), true)) {
		case AI_PLAYER_WALKING: return true;
		case AI_PLAYER_UNREACHABLE: return false;
		default: break;
	}
	if (sd->ud.target != target->id || sd->ud.attacktimer == INVALID_TIMER)
		unit_attack(sd, target->id, 1);
	return true;
}

/// Forget the current target and do not pick it again for a while.
static void ai_player_ignore_target(map_session_data* sd, s_ai_player& ai) {
	if (ai.mob_ignore.size() >= 30)
		ai.mob_ignore.clear();
	ai.mob_ignore.push_back(ai.target_id);
	ai.target_id = 0;
	unit_stop_attack(sd);
}

/// The character changed map: forget everything that belonged to the old map.
static void ai_player_enter_map(map_session_data* sd, s_ai_player& ai, t_tick tick) {
	ai.m = sd->m;
	ai.target_id = 0;
	ai.loot_id = 0;
	ai.loot_ignore.clear();
	ai.mob_ignore.clear();
	ai.ignore_reset = tick + 60000;
	ai.next_roam = tick;
	ai.last_mob_tick = tick;
	ai.town_notice = false;
	std::string mapname = map_getmapdata(sd->m)->name;
	ai.reg_mode = add_str(("AI_MM_" + mapname).c_str());
	ai.reg_list = add_str(("AI_ML_" + mapname + "$").c_str());
	ai.list_raw = "\x01"; // force a re-read
	ai.list.clear();
}

static int32 ai_player_warp_sub(block_list* bl, va_list ap) {
	int16 x = (int16)va_arg(ap, int32);
	int16 y = (int16)va_arg(ap, int32);
	npc_data** best = va_arg(ap, npc_data**);
	npc_data* nd = reinterpret_cast<npc_data*>(bl);
	if (nd->subtype != NPCTYPE_WARP || nd->is_invisible)
		return 0;
	if (*best == nullptr || distance_xy(x, y, nd->x, nd->y) < distance_xy(x, y, (*best)->x, (*best)->y))
		*best = nd;
	return 1;
}

/// Walk towards a cell; long distances are walked in steps. Returns false when no step was possible.
static bool ai_player_walk_towards(map_session_data* sd, int16 tx, int16 ty) {
	if (ai_player_can_walk(sd, tx, ty) && unit_walktoxy(sd, tx, ty, 0))
		return true;
	int32 dx = tx - sd->x, dy = ty - sd->y;
	int32 len = std::max(abs(dx), abs(dy));
	for (int32 step = 12; step >= 4 && len > 0; step -= 4) {
		int16 x = (int16)(sd->x + dx * std::min(step, len) / len);
		int16 y = (int16)(sd->y + dy * std::min(step, len) / len);
		if (map_getcell(sd->m, x, y, CELL_CHKPASS) && ai_player_can_walk(sd, x, y) && unit_walktoxy(sd, x, y, 0))
			return true;
	}
	return false;
}

/// Cast a skill on a player (walks into range first). Returns true while busy with it.
static bool ai_player_cast_on(map_session_data* sd, map_session_data* target, uint16 skill_id) {
	uint16 skill_lv = pc_checkskill(sd, skill_id);
	if (!ai_player_skill_ready(sd, skill_id, skill_lv))
		return false;
	if (target != sd) {
		switch (ai_player_approach(sd, target, skill_get_range2(sd, skill_id, skill_lv, true), false)) {
			case AI_PLAYER_WALKING: return true;
			case AI_PLAYER_UNREACHABLE: return false;
			default: break;
		}
	}
	return unit_skilluse_id(sd, target->id, skill_id, skill_lv) != 0;
}

/// Support mode: follow the leader, resurrect / heal / buff party members.
static e_ai_player_stop ai_player_support(map_session_data* sd, s_ai_player& ai, t_tick tick) {
	const s_ai_player_regs& reg = ai_player_reg();

	uint32 leader_id = (uint32)ai_player_get(sd, reg.leader);
	map_session_data* lsd = leader_id != 0 ? map_charid2sd(leader_id) : nullptr;
	if (lsd == sd)
		lsd = nullptr;
	if (lsd != nullptr && lsd->m == sd->m) {
		ai.leader_m = lsd->m;
		ai.leader_x = lsd->x;
		ai.leader_y = lsd->y;
		ai.leader_lost = 0;
		ai.waiting_notice = false;
	} else {
		lsd = nullptr;
		if (ai.leader_lost == 0)
			ai.leader_lost = tick;
	}

	// The leader just left this map: follow through the warp portal it used (if any)
	if (lsd == nullptr && ai.leader_m == sd->m && DIFF_TICK(tick, ai.leader_lost) < 30000) {
		npc_data* warp = nullptr;
		map_foreachinallarea(ai_player_warp_sub, sd->m, ai.leader_x - 7, ai.leader_y - 7, ai.leader_x + 7, ai.leader_y + 7, BL_NPC, (int32)ai.leader_x, (int32)ai.leader_y, &warp);
		if (warp != nullptr) {
			if (!unit_is_walking(sd))
				ai_player_walk_towards(sd, warp->x, warp->y);
			return AI_PLAYER_OK;
		}
	}
	if (lsd == nullptr && !ai.waiting_notice && DIFF_TICK(tick, ai.leader_lost) > 3000) {
		ai.waiting_notice = true;
		clif_displaymessage(sd->fd, "[AI] \303\315\244\271\267\325\350\265\322\301 - \302\321\247\344\301\350\315\302\331\350\343\271\341\301\276\271\325\351 (AI \302\321\247\267\323\247\322\271\315\302\331\350)");
	}

	// Players to look after: leader first, then party members on this map, then ourselves
	std::vector<map_session_data*> members;
	if (lsd != nullptr)
		members.push_back(lsd);
	party_data* p = sd->status.party_id != 0 ? party_search(sd->status.party_id) : nullptr;
	if (p != nullptr) {
		for (int32 i = 0; i < MAX_PARTY; i++) {
			map_session_data* msd = p->data[i].sd;
			if (msd == nullptr || msd == sd || msd == lsd || msd->m != sd->m || !check_distance_bl(sd, msd, AREA_SIZE))
				continue;
			members.push_back(msd);
		}
	}
	members.push_back(sd);

	// Resurrect
	if (ai_player_get(sd, reg.res) != 0 && pc_checkskill(sd, ALL_RESURRECTION) > 0) {
		for (map_session_data* msd : members) {
			if (msd != sd && pc_isdead(msd) && ai_player_cast_on(sd, msd, ALL_RESURRECTION))
				return AI_PLAYER_OK;
		}
	}

	// Heal the member with the lowest HP%
	uint16 heal_id = (uint16)ai_player_get(sd, reg.healsk);
	if (heal_id != 0 && pc_checkskill(sd, heal_id) > 0) {
		int32 heal_pct = (int32)ai_player_get(sd, reg.healpct);
		if (heal_pct <= 0)
			heal_pct = 70;
		map_session_data* worst = nullptr;
		int32 worst_pct = heal_pct;
		for (map_session_data* msd : members) {
			if (pc_isdead(msd))
				continue;
			int32 pct = ai_player_percent(msd->battle_status.hp, msd->battle_status.max_hp);
			if (pct < worst_pct) {
				worst = msd;
				worst_pct = pct;
			}
		}
		if (worst != nullptr && ai_player_cast_on(sd, worst, heal_id))
			return AI_PLAYER_OK;
	}

	// Party buffs
	for (int32 i = 0; i < AI_PLAYER_MAX_PBUFF; i++) {
		uint16 skill_id = (uint16)ai_player_get(sd, reg.pbuff, i);
		if (skill_id == 0)
			continue;
		sc_type sc = skill_get_sc(skill_id);
		if (sc <= SC_NONE)
			continue;
		// Self-cast party skills (Magnificat, Gloria, Angelus ...): one cast covers the party
		bool self_cast = (skill_get_inf(skill_id) & INF_SUPPORT_SKILL) == 0;
		for (map_session_data* msd : members) {
			if (pc_isdead(msd) || msd->sc.getSCE(sc) != nullptr)
				continue;
			if (ai_player_cast_on(sd, self_cast ? sd : msd, skill_id))
				return AI_PLAYER_OK;
			break; // not ready (SP / cooldown / range): try the next skill
		}
	}

	// Follow the leader
	if (lsd != nullptr) {
		int32 dist = (int32)ai_player_get(sd, reg.follow);
		dist = dist > 0 ? cap_value(dist, 1, 10) : 3;
		if (!check_distance_bl(sd, lsd, dist) && (!unit_is_walking(sd) || sd->ud.target_to != lsd->id)) {
			if (!unit_walktobl(sd, lsd, dist, 0) && !unit_is_walking(sd))
				ai_player_walk_towards(sd, lsd->x, lsd->y); // too far for one walk
		}
	}
	return AI_PLAYER_OK;
}

static e_ai_player_stop ai_player_think(map_session_data* sd, s_ai_player& ai, t_tick tick) {
	const s_ai_player_regs& reg = ai_player_reg();

	if (sd->m != ai.m)
		ai_player_enter_map(sd, ai, tick); // keep running on the new map
	e_ai_player_stop reason = ai_player_check(sd);
	if (reason == AI_PLAYER_STOP_TOWN) { // farm mode: wait until the player leaves town
		if (!ai.town_notice) {
			ai.town_notice = true;
			unit_stop_attack(sd);
			clif_displaymessage(sd->fd, "[AI] \276\321\241\343\271\340\301\327\315\247 - \315\315\241\250\322\241\340\301\327\315\247\341\305\351\307 AI \250\320\267\323\247\322\271\265\350\315\340\315\247");
		}
		return AI_PLAYER_PAUSE;
	}
	if (reason != AI_PLAYER_OK)
		return ai_player_is_support(sd) ? AI_PLAYER_PAUSE : reason; // support: wait (resurrection, weight)

	// Busy: talking to an NPC, chat room, trading, casting, sitting, ...
	if (pc_cant_act(sd) || pc_istrading(sd) || pc_issit(sd) || sd->ud.skilltimer != INVALID_TIMER)
		return AI_PLAYER_OK;

	// Go home?
	int32 rt_hp = (int32)ai_player_get(sd, reg.rt_hp);
	if (rt_hp > 0 && ai_player_percent(sd->battle_status.hp, sd->battle_status.max_hp) < rt_hp)
		return ai_player_go_home(sd, AI_PLAYER_HOME_HP);
	int32 rt_pot = (int32)ai_player_get(sd, reg.rt_pot);
	if ((rt_pot & 1) && ai_player_get(sd, reg.hpitem) != 0 && !ai_player_has_item(sd, reg.hpitem))
		return ai_player_go_home(sd, AI_PLAYER_HOME_HPPOT);
	if ((rt_pot & 2) && ai_player_get(sd, reg.spitem) != 0 && !ai_player_has_item(sd, reg.spitem))
		return ai_player_go_home(sd, AI_PLAYER_HOME_SPPOT);
	int32 rt_weight = (int32)ai_player_get(sd, reg.rt_weight);
	if (rt_weight > 0 && ai_player_percent(sd->weight, sd->max_weight) >= rt_weight)
		return ai_player_go_home(sd, AI_PLAYER_HOME_WEIGHT);

	// Potions
	ai_player_use_potion(sd, reg.hppct, reg.hpitem, sd->battle_status.hp, sd->battle_status.max_hp);
	ai_player_use_potion(sd, reg.sppct, reg.spitem, sd->battle_status.sp, sd->battle_status.max_sp);

	// Still in skill / attack delay
	if (DIFF_TICK(sd->ud.canact_tick, tick) > 0)
		return AI_PLAYER_OK;

	// Self buffs
	if (ai_player_do_buff(sd))
		return AI_PLAYER_OK;

	if (ai_player_get(sd, reg.role) == 1)
		return ai_player_support(sd, ai, tick);

	bool noboss = ai_player_get(sd, reg.noboss) != 0;
	int32 range = ai_player_range(sd);
	int32 mode = (int32)pc_readglobalreg(sd, ai.reg_mode);
	if (mode != 0)
		ai_player_refresh_list(sd, ai);
	int32 loot_mode = (int32)ai_player_get(sd, reg.loot);

	if (DIFF_TICK(tick, ai.ignore_reset) >= 0) {
		ai.mob_ignore.clear();
		ai.loot_ignore.clear();
		ai.ignore_reset = tick + 60000;
	}

	// Keep fighting the current target
	if (ai.target_id != 0) {
		block_list* tbl = map_id2bl(ai.target_id);
		if (ai_player_valid_target(sd, ai, tbl, noboss, mode) && check_distance_bl(sd, tbl, range + 5)) {
			if (DIFF_TICK(tick, ai.target_tick) > AI_PLAYER_TARGET_TIMEOUT && status_get_hp(tbl) >= status_get_max_hp(tbl))
				ai_player_ignore_target(sd, ai); // could not reach / hurt it
			else if (!ai_player_fight(sd, tbl))
				ai_player_ignore_target(sd, ai);
			else {
				ai.last_mob_tick = tick;
				return AI_PLAYER_OK;
			}
		} else {
			ai.target_id = 0;
			unit_stop_attack(sd);
		}
	}

	// Loot before looking for the next monster
	if (loot_mode > 0) {
		if (ai.loot_id == 0) {
			int32 best_id = 0, best_dist = 0;
			map_foreachinallrange(ai_player_loot_sub, sd, range, BL_ITEM, sd, &ai, &best_id, &best_dist, loot_mode, tick);
			if (best_id != 0) {
				ai.loot_id = best_id;
				ai.loot_tick = tick;
			}
		}
		if (ai_player_do_loot(sd, ai, loot_mode, tick))
			return AI_PLAYER_OK;
	}

	// Look for the nearest monster
	int32 best_id = 0, best_dist = 0;
	map_foreachinallrange(ai_player_search_sub, sd, range, BL_MOB, sd, &ai, &best_id, &best_dist, (int32)noboss, mode);
	if (best_id != 0) {
		ai.target_id = best_id;
		ai.target_tick = tick;
		ai.last_mob_tick = tick;
		block_list* tbl = map_id2bl(best_id);
		if (tbl != nullptr && !ai_player_fight(sd, tbl))
			ai_player_ignore_target(sd, ai);
		return AI_PLAYER_OK;
	}

	// Nothing around for a while: Fly Wing
	int32 flywing = (int32)ai_player_get(sd, reg.flywing);
	if (flywing > 0 && DIFF_TICK(tick, ai.last_mob_tick) >= (t_tick)flywing * 1000) {
		ai.last_mob_tick = tick;
		if (ai_player_fly_wing(sd))
			return AI_PLAYER_OK;
	}

	// Walk to a random nearby cell
	if (!unit_is_walking(sd) && DIFF_TICK(tick, ai.next_roam) >= 0 && unit_can_move(sd)) {
		ai.next_roam = tick + 1000;
		for (int32 i = 0; i < 10; i++) {
			int16 x = sd->x + rnd_value(-12, 12);
			int16 y = sd->y + rnd_value(-12, 12);
			if ((x != sd->x || y != sd->y) && map_getcell(sd->m, x, y, CELL_CHKPASS) && ai_player_can_walk(sd, x, y) && unit_walktoxy(sd, x, y, 0)) {
				ai.next_roam = tick + 1500;
				break;
			}
		}
	}
	return AI_PLAYER_OK;
}

/// Take the time the AI ran from #AI_TIME. Returns false when no time is left.
static bool ai_player_use_time(map_session_data* sd, s_ai_player& ai, t_tick tick) {
	const s_ai_player_regs& reg = ai_player_reg();
	ai.time_ms += (int32)DIFF_TICK(tick, ai.last_tick);
	ai.last_tick = tick;
	if (ai.time_ms < 1000)
		return true;
	int64 left = pc_readaccountreg(sd, reg.time) - ai.time_ms / 1000;
	ai.time_ms %= 1000;
	if (left < 0)
		left = 0;
	pc_setaccountreg(sd, reg.time, left);
	if (left <= 0)
		return false;
	if (left <= 600 && !ai.time_warned) {
		ai.time_warned = true;
		clif_displaymessage(sd->fd, "[AI] \340\307\305\322\343\252\351\247\322\271 AI \340\313\305\327\315\344\301\350\266\326\247 10 \271\322\267\325");
	}
	return true;
}

static TIMER_FUNC(ai_player_timer) {
	for (auto it = ai_player_list.begin(); it != ai_player_list.end(); ) {
		map_session_data* sd = map_charid2sd(it->first);
		if (sd == nullptr) { // logged out
			it = ai_player_list.erase(it);
			continue;
		}
		e_ai_player_stop reason = AI_PLAYER_STOP_TIME;
		if (ai_player_is_support(sd)) { // support mode is free
			it->second.last_tick = tick;
			it->second.time_ms = 0;
			reason = ai_player_think(sd, it->second, tick);
		} else if (ai_player_use_time(sd, it->second, tick))
			reason = ai_player_think(sd, it->second, tick);
		if (reason == AI_PLAYER_PAUSE)
			reason = AI_PLAYER_OK;
		if (reason != AI_PLAYER_OK) {
			unit_stop_attack(sd);
			const char* msg = ai_player_stop_message(reason);
			if (msg != nullptr)
				clif_displaymessage(sd->fd, msg);
			it = ai_player_list.erase(it);
			continue;
		}
		++it;
	}
	return 0;
}

static void ai_player_start_timer() {
	if (ai_player_timer_id != INVALID_TIMER)
		return;
	add_timer_func_list(ai_player_timer, "ai_player_timer");
	ai_player_timer_id = add_timer_interval(gettick() + AI_PLAYER_INTERVAL, ai_player_timer, 0, 0, AI_PLAYER_INTERVAL);
}

/// ai_on {<char id>};
BUILDIN_FUNC(ai_on) {
	map_session_data* sd;

	if (!script_charid2sd(2, sd)) {
		script_pushint(st, 0);
		return SCRIPT_CMD_FAILURE;
	}
	switch (ai_player_check(sd)) {
		case AI_PLAYER_STOP_TOWN:   script_pushint(st, -1); return SCRIPT_CMD_SUCCESS;
		case AI_PLAYER_STOP_DEAD:   script_pushint(st, -2); return SCRIPT_CMD_SUCCESS;
		case AI_PLAYER_STOP_WEIGHT: script_pushint(st, -3); return SCRIPT_CMD_SUCCESS;
		default: break;
	}
	if (!ai_player_is_support(sd) && pc_readaccountreg(sd, ai_player_reg().time) <= 0) {
		script_pushint(st, -4); // no AI time left (support mode is free)
		return SCRIPT_CMD_SUCCESS;
	}
	t_tick tick = gettick();
	s_ai_player& ai = ai_player_list[sd->status.char_id];
	ai.m = sd->m;
	ai.target_id = 0;
	ai.target_tick = 0;
	ai.next_roam = tick;
	ai.last_mob_tick = tick;
	ai.loot_id = 0;
	ai.loot_tick = 0;
	ai.loot_ignore.clear();
	ai.mob_ignore.clear();
	ai.ignore_reset = tick + 60000;
	ai.last_tick = tick;
	ai.time_ms = 0;
	ai.time_warned = false;
	ai.leader_lost = 0;
	ai.leader_m = -1;
	ai.leader_x = ai.leader_y = 0;
	ai.waiting_notice = false;
	ai_player_enter_map(sd, ai, tick);
	ai_player_start_timer();
	script_pushint(st, 1);
	return SCRIPT_CMD_SUCCESS;
}

/// ai_off {<char id>};
BUILDIN_FUNC(ai_off) {
	map_session_data* sd;

	if (!script_charid2sd(2, sd)) {
		script_pushint(st, 0);
		return SCRIPT_CMD_FAILURE;
	}
	if (ai_player_list.erase(sd->status.char_id) > 0) {
		unit_stop_attack(sd);
		script_pushint(st, 1);
	} else
		script_pushint(st, 0);
	return SCRIPT_CMD_SUCCESS;
}

/// ai_status({<char id>})
BUILDIN_FUNC(ai_status) {
	map_session_data* sd;

	if (!script_charid2sd(2, sd)) {
		script_pushint(st, 0);
		return SCRIPT_CMD_FAILURE;
	}
	script_pushint(st, ai_player_list.count(sd->status.char_id) > 0 ? 1 : 0);
	return SCRIPT_CMD_SUCCESS;
}

struct s_ai_player_mapmob {
	int32 mob_id;
	int32 count;
	int32 boss;
};

static int32 ai_player_maplist_sub(block_list* bl, va_list ap) {
	std::vector<s_ai_player_mapmob>* list = va_arg(ap, std::vector<s_ai_player_mapmob>*);
	mob_data* md = reinterpret_cast<mob_data*>(bl);
	if (status_isdead(*md) || md->special_state.ai)
		return 0;
	for (s_ai_player_mapmob& e : *list) {
		if (e.mob_id == md->mob_id) {
			e.count++;
			return 1;
		}
	}
	e_mob_bosstype bt = md->get_bosstype();
	list->push_back({ md->mob_id, 1, bt == BOSSTYPE_MVP ? 2 : (bt == BOSSTYPE_MINIBOSS ? 1 : 0) });
	return 1;
}

/// ai_maplist({<char id>})
BUILDIN_FUNC(ai_maplist) {
	map_session_data* sd;

	if (!script_charid2sd(2, sd)) {
		script_pushint(st, 0);
		return SCRIPT_CMD_FAILURE;
	}
	std::vector<s_ai_player_mapmob> list;
	map_foreachinmap(ai_player_maplist_sub, sd->m, BL_MOB, &list);
	std::sort(list.begin(), list.end(), [](const s_ai_player_mapmob& a, const s_ai_player_mapmob& b) {
		if (a.boss != b.boss)
			return a.boss < b.boss;
		return a.count > b.count;
	});
	if (list.size() > 40)
		list.resize(40);
	for (size_t i = 0; i < list.size(); i++) {
		pc_setreg(sd, reference_uid(add_str("@ai_mob_id"), i), list[i].mob_id);
		pc_setreg(sd, reference_uid(add_str("@ai_mob_cnt"), i), list[i].count);
		pc_setreg(sd, reference_uid(add_str("@ai_mob_boss"), i), list[i].boss);
	}
	script_pushint(st, (int32)list.size());
	return SCRIPT_CMD_SUCCESS;
}

/// ai_skilltype(<skill id>)
BUILDIN_FUNC(ai_skilltype) {
	uint16 skill_id = (uint16)script_getnum(st, 2);
	if (skill_db.find(skill_id) == nullptr) {
		script_pushint(st, 0);
		return SCRIPT_CMD_SUCCESS;
	}
	int32 inf = skill_get_inf(skill_id);
	bool has_sc = skill_get_sc(skill_id) > SC_NONE;
	if (inf & (INF_ATTACK_SKILL | INF_GROUND_SKILL))
		script_pushint(st, 1);
	else if (inf & INF_SUPPORT_SKILL)
		script_pushint(st, has_sc ? 4 : 3);
	else if ((inf & INF_SELF_SKILL) && has_sc)
		script_pushint(st, 2);
	else
		script_pushint(st, 0);
	return SCRIPT_CMD_SUCCESS;
}

/// ai_skillname(<skill id>)
BUILDIN_FUNC(ai_skillname) {
	uint16 skill_id = (uint16)script_getnum(st, 2);
	const char* name = skill_db.find(skill_id) != nullptr ? skill_get_desc(skill_id) : nullptr;
	script_pushstrcopy(st, name ? name : "Unknown");
	return SCRIPT_CMD_SUCCESS;
}
