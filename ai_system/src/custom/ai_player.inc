// Ro-X Player AI - phase 1 (auto attack)
// Included from src/custom/script.inc (compiled inside src/map/script.cpp).
//
// Script commands:
//   ai_on {<char id>};      -> 1 = running, -1 = in town, -2 = dead, -3 = overweight (90%)
//   ai_off {<char id>};     -> 1 = stopped, 0 = was not running
//   ai_status({<char id>})  -> 1 = running, 0 = off
//
// Settings are read from permanent character variables (set them from the NPC menu):
//   AI_RANGE   search radius in cells (3-14, default 9)
//   AI_NOBOSS  1 = do not attack boss / MVP monsters
//
// The AI stops by itself when the character dies, changes map, enters a town map
// or goes over 90% weight. It is also dropped when the character logs out.

#include <unordered_map>

struct s_ai_player {
	int16 m;             // map the AI was started on
	int32 target_id;     // current monster
	t_tick target_tick;  // when the current target was picked
	t_tick next_roam;    // next time we may walk around when no monster is found
};

enum e_ai_player_stop : int32 {
	AI_PLAYER_OK = 0,
	AI_PLAYER_STOP_DEAD,
	AI_PLAYER_STOP_MAP,
	AI_PLAYER_STOP_WEIGHT,
	AI_PLAYER_STOP_TOWN,
};

static std::unordered_map<uint32, s_ai_player> ai_player_list; // key: char_id
static int32 ai_player_timer_id = INVALID_TIMER;
static const int32 AI_PLAYER_INTERVAL = 300;        // think every 300 ms
static const int32 AI_PLAYER_TARGET_TIMEOUT = 15000; // drop a target we could not hurt in 15 s

static const char* ai_player_stop_message(e_ai_player_stop reason) {
	switch (reason) {
		case AI_PLAYER_STOP_DEAD:   return "[AI] \313\302\330\264\267\323\247\322\271: \265\321\307\305\320\244\303\340\312\325\302\252\325\307\324\265"; // AI stopped: character died
		case AI_PLAYER_STOP_MAP:    return "[AI] \313\302\330\264\267\323\247\322\271: \302\351\322\302\341\274\271\267\325\350"; // AI stopped: map changed
		case AI_PLAYER_STOP_WEIGHT: return "[AI] \313\302\330\264\267\323\247\322\271: \271\351\323\313\271\321\241\340\241\324\271 90%"; // AI stopped: weight over 90%
		case AI_PLAYER_STOP_TOWN:   return "[AI] \313\302\330\264\267\323\247\322\271: \315\302\331\350\343\271\340\301\327\315\247"; // AI stopped: town map
		default:                    return nullptr;
	}
}

static e_ai_player_stop ai_player_check(map_session_data* sd) {
	if (pc_isdead(sd))
		return AI_PLAYER_STOP_DEAD;
	if (map_getmapflag(sd->m, MF_TOWN))
		return AI_PLAYER_STOP_TOWN;
	if (sd->max_weight > 0 && (uint64)sd->weight * 100 >= (uint64)sd->max_weight * 90)
		return AI_PLAYER_STOP_WEIGHT;
	return AI_PLAYER_OK;
}

static int32 ai_player_range(map_session_data* sd) {
	static int64 reg_range = add_str("AI_RANGE");
	int32 range = (int32)pc_readglobalreg(sd, reg_range);
	if (range <= 0)
		range = 9;
	return cap_value(range, 3, AREA_SIZE);
}

/// Is this monster a valid target for the player?
static bool ai_player_valid_target(map_session_data* sd, block_list* bl, bool noboss) {
	if (bl == nullptr || bl->type != BL_MOB || bl->m != sd->m)
		return false;
	mob_data* md = reinterpret_cast<mob_data*>(bl);
	if (status_isdead(*md))
		return false;
	if (md->special_state.ai) // summoned by a player (plants, spheres, ...)
		return false;
	if (noboss && md->get_bosstype() != BOSSTYPE_NONE)
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
	int32* best_id = va_arg(ap, int32*);
	int32* best_dist = va_arg(ap, int32*);
	bool noboss = va_arg(ap, int32) != 0;

	if (!ai_player_valid_target(sd, bl, noboss))
		return 0;
	int32 dist = distance_bl(sd, bl);
	if (*best_id != 0 && dist >= *best_dist)
		return 0;
	if (!path_search(nullptr, sd->m, sd->x, sd->y, bl->x, bl->y, 0, CELL_CHKNOPASS))
		return 0;
	*best_id = bl->id;
	*best_dist = dist;
	return 1;
}

/// Walk to a random nearby cell when there is nothing to attack.
static void ai_player_roam(map_session_data* sd, s_ai_player& ai, t_tick tick) {
	if (unit_is_walking(sd) || DIFF_TICK(tick, ai.next_roam) < 0 || !unit_can_move(sd))
		return;
	for (int32 i = 0; i < 10; i++) {
		int16 x = sd->x + rnd_value(-8, 8);
		int16 y = sd->y + rnd_value(-8, 8);
		if ((x != sd->x || y != sd->y) && map_getcell(sd->m, x, y, CELL_CHKPASS) && unit_walktoxy(sd, x, y, 0)) {
			ai.next_roam = tick + 1500;
			return;
		}
	}
	ai.next_roam = tick + 1000;
}

static e_ai_player_stop ai_player_think(map_session_data* sd, s_ai_player& ai, t_tick tick) {
	if (sd->m != ai.m)
		return AI_PLAYER_STOP_MAP;
	e_ai_player_stop reason = ai_player_check(sd);
	if (reason != AI_PLAYER_OK)
		return reason;

	// Busy: talking to an NPC, chat room, trading, casting, sitting, ...
	if (pc_cant_act(sd) || pc_istrading(sd) || pc_issit(sd) || sd->ud.skilltimer != INVALID_TIMER)
		return AI_PLAYER_OK;

	bool noboss = pc_readglobalreg(sd, add_str("AI_NOBOSS")) != 0;
	int32 range = ai_player_range(sd);

	// Keep hitting the current target
	if (ai.target_id != 0) {
		block_list* tbl = map_id2bl(ai.target_id);
		bool keep = ai_player_valid_target(sd, tbl, noboss) && check_distance_bl(sd, tbl, range + 5);
		if (keep && DIFF_TICK(tick, ai.target_tick) > AI_PLAYER_TARGET_TIMEOUT && status_get_hp(tbl) >= status_get_max_hp(tbl))
			keep = false; // could not reach / hurt it
		if (keep) {
			if (sd->ud.target != ai.target_id || (sd->ud.attacktimer == INVALID_TIMER && !unit_is_walking(sd)))
				unit_attack(sd, ai.target_id, 1);
			return AI_PLAYER_OK;
		}
		ai.target_id = 0;
		unit_stop_attack(sd);
	}

	// Look for the nearest monster
	int32 best_id = 0, best_dist = 0;
	map_foreachinallrange(ai_player_search_sub, sd, range, BL_MOB, sd, &best_id, &best_dist, (int32)noboss);
	if (best_id != 0) {
		ai.target_id = best_id;
		ai.target_tick = tick;
		unit_attack(sd, best_id, 1);
		return AI_PLAYER_OK;
	}

	ai_player_roam(sd, ai, tick);
	return AI_PLAYER_OK;
}

static TIMER_FUNC(ai_player_timer) {
	for (auto it = ai_player_list.begin(); it != ai_player_list.end(); ) {
		map_session_data* sd = map_charid2sd(it->first);
		if (sd == nullptr) { // logged out
			it = ai_player_list.erase(it);
			continue;
		}
		e_ai_player_stop reason = ai_player_think(sd, it->second, tick);
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
	s_ai_player& ai = ai_player_list[sd->status.char_id];
	ai.m = sd->m;
	ai.target_id = 0;
	ai.target_tick = 0;
	ai.next_roam = gettick();
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
