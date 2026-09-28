// Copyright (c) rAthena Dev Teams - Licensed under GNU GPL
// For more information, see LICENCE in the main folder

#include "aibot.hpp"

#include <algorithm>
#include <cstdlib>
#include <cstring>
#include <string>
#include <unordered_map>
#include <unordered_set>

#include <nlohmann/json.hpp>

#include <common/malloc.hpp>
#include <common/mapindex.hpp>
#include <common/nullpo.hpp>
#include <common/random.hpp>
#include <common/showmsg.hpp>
#include <common/socket.hpp>
#include <common/sql.hpp>
#include <common/strlib.hpp>
#include <common/timer.hpp>
#include <common/utils.hpp>

#include "battle.hpp"
#include "chrif.hpp"
#include "clif.hpp"
#include "intif.hpp"
#include "itemdb.hpp"
#include "log.hpp"
#include "map.hpp"
#include "mob.hpp"
#include "npc.hpp"
#include "party.hpp"
#include "path.hpp"
#include "pc.hpp"
#include "skill.hpp"
#include "status.hpp"
#include "unit.hpp"

using json = nlohmann::json;

namespace {

enum class e_aibot_encoding : uint8 {
	UTF8,
	CP874,  // Thai clients (TIS-620 + Windows extensions)
	LATIN1, // Raw bytes, mapped 1:1 to U+0000-U+00FF
};

struct s_aibot_config {
	bool enabled = true;
	std::string bind_ip = "127.0.0.1";
	uint16 port = 7100;
	std::string token;
	uint32 state_interval = 500;
	uint32 max_bots = 200;
	bool logout_on_disconnect = false;
	std::string char_table = "char";
	int16 chat_range = 14;
	int16 scan_range_max = 30;
	e_aibot_encoding encoding = e_aibot_encoding::CP874;
	uint32 max_line_length = 65536;
	uint32 max_commands_per_cycle = 200;
	bool debug = false;
} cfg;

struct s_aibot {
	uint32 account_id = 0;
	uint32 char_id = 0;
	std::string name;
	bool ready = false;          // fully spawned on a map at least once
	int32 loadend_tid = INVALID_TIMER;
	int32 attack_target = 0;     // monster the bot keeps attacking (see aibot_chase_timer)
	int16 loadend_tries = 0;
};

struct s_aibot_client {
	bool authed = false;
};

int32 listen_fd = -1;
RecvFunc listen_default_recv = nullptr;
int32 state_tid = INVALID_TIMER;
int32 chase_tid = INVALID_TIMER;
constexpr t_tick CHASE_INTERVAL = 250;
std::unordered_map<int32, s_aibot_client> clients;   // fd -> client
std::unordered_map<uint32, s_aibot> bots;            // char_id -> bot
std::unordered_map<uint32, uint32> bot_by_account;   // account_id -> char_id

constexpr int16 LOADEND_MAX_TRIES = 100; // 100 * 100ms = 10s
constexpr t_tick LOADEND_INTERVAL = 100;
constexpr size_t WRITE_CHUNK = 32 * 1024;

TIMER_FUNC( aibot_loadend_timer );

// ---------------------------------------------------------------------------
// Encoding helpers (client codepage <-> UTF-8 used on the wire)
// ---------------------------------------------------------------------------

void utf8_append( std::string& out, uint32 cp ){
	if( cp < 0x80 ){
		out += static_cast<char>( cp );
	}else if( cp < 0x800 ){
		out += static_cast<char>( 0xC0 | ( cp >> 6 ) );
		out += static_cast<char>( 0x80 | ( cp & 0x3F ) );
	}else if( cp < 0x10000 ){
		out += static_cast<char>( 0xE0 | ( cp >> 12 ) );
		out += static_cast<char>( 0x80 | ( ( cp >> 6 ) & 0x3F ) );
		out += static_cast<char>( 0x80 | ( cp & 0x3F ) );
	}else{
		out += static_cast<char>( 0xF0 | ( cp >> 18 ) );
		out += static_cast<char>( 0x80 | ( ( cp >> 12 ) & 0x3F ) );
		out += static_cast<char>( 0x80 | ( ( cp >> 6 ) & 0x3F ) );
		out += static_cast<char>( 0x80 | ( cp & 0x3F ) );
	}
}

uint32 cp874_to_unicode( uint8 c ){
	if( c < 0x80 )
		return c;
	if( c >= 0xA1 && c <= 0xFB )
		return 0x0E00 + ( c - 0xA0 );
	switch( c ){
		case 0x80: return 0x20AC;
		case 0x85: return 0x2026;
		case 0x91: return 0x2018;
		case 0x92: return 0x2019;
		case 0x93: return 0x201C;
		case 0x94: return 0x201D;
		case 0x95: return 0x2022;
		case 0x96: return 0x2013;
		case 0x97: return 0x2014;
		case 0xA0: return 0x00A0;
	}
	return '?';
}

int32 unicode_to_cp874( uint32 cp ){
	if( cp < 0x80 )
		return static_cast<int32>( cp );
	if( cp >= 0x0E01 && cp <= 0x0E5B )
		return static_cast<int32>( cp - 0x0E00 + 0xA0 );
	switch( cp ){
		case 0x20AC: return 0x80;
		case 0x2026: return 0x85;
		case 0x2018: return 0x91;
		case 0x2019: return 0x92;
		case 0x201C: return 0x93;
		case 0x201D: return 0x94;
		case 0x2022: return 0x95;
		case 0x2013: return 0x96;
		case 0x2014: return 0x97;
		case 0x00A0: return 0xA0;
	}
	return -1;
}

/// Client bytes -> UTF-8
std::string to_utf8( const char* str ){
	if( str == nullptr )
		return {};

	if( cfg.encoding == e_aibot_encoding::UTF8 )
		return str;

	std::string out;
	for( const uint8* p = reinterpret_cast<const uint8*>( str ); *p != 0; ++p ){
		uint32 cp = ( cfg.encoding == e_aibot_encoding::CP874 ) ? cp874_to_unicode( *p ) : *p;
		utf8_append( out, cp );
	}
	return out;
}

/// UTF-8 -> client bytes (unmappable characters become '?')
std::string from_utf8( const std::string& str ){
	if( cfg.encoding == e_aibot_encoding::UTF8 )
		return str;

	std::string out;
	size_t i = 0;
	while( i < str.size() ){
		uint8 c = static_cast<uint8>( str[i] );
		uint32 cp;
		size_t len;

		if( c < 0x80 ){ cp = c; len = 1; }
		else if( ( c >> 5 ) == 0x6 ){ cp = c & 0x1F; len = 2; }
		else if( ( c >> 4 ) == 0xE ){ cp = c & 0x0F; len = 3; }
		else if( ( c >> 3 ) == 0x1E ){ cp = c & 0x07; len = 4; }
		else { out += '?'; i++; continue; }

		if( i + len > str.size() ){
			out += '?';
			break;
		}
		for( size_t j = 1; j < len; j++ )
			cp = ( cp << 6 ) | ( static_cast<uint8>( str[i + j] ) & 0x3F );
		i += len;

		int32 b;
		if( cfg.encoding == e_aibot_encoding::CP874 )
			b = unicode_to_cp874( cp );
		else
			b = cp < 0x100 ? static_cast<int32>( cp ) : -1;

		out += ( b < 0 ) ? '?' : static_cast<char>( b );
	}
	return out;
}

// ---------------------------------------------------------------------------
// Networking
// ---------------------------------------------------------------------------

void send_raw( int32 fd, const std::string& line ){
	if( !session_isActive( fd ) )
		return;

	size_t pos = 0;
	while( pos < line.size() ){
		size_t len = std::min( WRITE_CHUNK, line.size() - pos );
		WFIFOHEAD( fd, len );
		memcpy( WFIFOP( fd, 0 ), line.data() + pos, len );
		WFIFOSET( fd, len );
		pos += len;
	}
}

void send_json( int32 fd, const json& j ){
	send_raw( fd, j.dump( -1, ' ', false, json::error_handler_t::replace ) + "\n" );
}

bool has_listeners(){
	for( const auto& it : clients ){
		if( it.second.authed )
			return true;
	}
	return false;
}

void broadcast( const json& j ){
	if( !has_listeners() )
		return;

	std::string line = j.dump( -1, ' ', false, json::error_handler_t::replace ) + "\n";
	for( const auto& it : clients ){
		if( it.second.authed )
			send_raw( it.first, line );
	}
}

// ---------------------------------------------------------------------------
// Bot helpers
// ---------------------------------------------------------------------------

map_session_data* bot_sd( uint32 char_id ){
	if( bots.find( char_id ) == bots.end() )
		return nullptr;

	map_session_data* sd = map_charid2sd( char_id );

	if( sd == nullptr || !sd->state.aibot )
		return nullptr;

	return sd;
}

void bot_forget( uint32 char_id ){
	auto it = bots.find( char_id );

	if( it == bots.end() )
		return;

	if( it->second.loadend_tid != INVALID_TIMER )
		delete_timer( it->second.loadend_tid, aibot_loadend_timer );

	bot_by_account.erase( it->second.account_id );
	bots.erase( it );
}

void bot_stand( map_session_data* sd ){
	if( pc_issit( sd ) && !pc_isdead( sd ) && pc_setstand( sd, false ) ){
		skill_sit( sd, false );
		clif_standing( *sd );
	}
}

bool bot_can_act( map_session_data* sd, json& res ){
	if( sd->prev == nullptr ){
		res["error"] = "not_on_map";
		return false;
	}
	if( pc_isdead( sd ) ){
		res["error"] = "dead";
		return false;
	}
	return true;
}

const char* bot_map_name( map_session_data* sd ){
	return mapindex_id2name( sd->mapindex );
}

json bot_state( map_session_data* sd ){
	json s;

	s["id"] = sd->status.char_id;
	s["aid"] = sd->status.account_id;
	s["name"] = to_utf8( sd->status.name );
	s["map"] = bot_map_name( sd );
	s["x"] = sd->x;
	s["y"] = sd->y;
	s["hp"] = sd->battle_status.hp;
	s["mhp"] = sd->battle_status.max_hp;
	s["sp"] = sd->battle_status.sp;
	s["msp"] = sd->battle_status.max_sp;
	s["blv"] = sd->status.base_level;
	s["jlv"] = sd->status.job_level;
	s["bexp"] = sd->status.base_exp;
	s["bnext"] = pc_nextbaseexp( sd );
	s["class"] = sd->status.class_;
	s["zeny"] = sd->status.zeny;
	s["w"] = sd->weight;
	s["mw"] = sd->max_weight;
	s["dead"] = pc_isdead( sd );
	s["sit"] = pc_issit( sd ) && !pc_isdead( sd );
	s["onmap"] = sd->prev != nullptr;
	s["target"] = sd->ud.target;
	s["walking"] = sd->ud.walktimer != INVALID_TIMER;
	s["casting"] = sd->ud.skilltimer != INVALID_TIMER;
	s["party"] = sd->status.party_id;
	s["invite"] = sd->party_invite;
	s["stpts"] = sd->status.status_point;
	s["skpts"] = sd->status.skill_point;

	return s;
}

json item_json( map_session_data* sd, int32 idx ){
	const item& it = sd->inventory.u.items_inventory[idx];
	const item_data* id = sd->inventory_data[idx];
	json j;

	j["idx"] = idx;
	j["id"] = static_cast<t_itemid>( it.nameid );
	j["amount"] = static_cast<int32>( it.amount );
	j["equipped"] = static_cast<uint32>( it.equip );
	j["refine"] = static_cast<int32>( it.refine );
	j["identify"] = it.identify != 0;

	if( id != nullptr ){
		j["name"] = to_utf8( id->ename.c_str() );
		j["type"] = id->type;
		j["loc"] = id->equip;
		j["elv"] = id->elv;
		j["atk"] = id->atk;
		j["def"] = id->def;
		j["weight"] = id->weight;
		j["sell"] = id->value_sell;
		j["buy"] = id->value_buy;
		j["can_equip"] = id->equip != 0 && pc_isequip( sd, idx ) != 0;
	}

	return j;
}

// map_foreachinallrange callbacks ------------------------------------------

int32 scan_sub( block_list* bl, va_list ap ){
	json* out = va_arg( ap, json* );
	map_session_data* self = va_arg( ap, map_session_data* );

	if( bl == self )
		return 0;

	switch( bl->type ){
		case BL_MOB: {
			mob_data* md = reinterpret_cast<mob_data*>( bl );

			if( md->status.hp == 0 )
				return 0;

			json m;
			m["id"] = md->id;
			m["mob_id"] = md->mob_id;
			m["name"] = to_utf8( md->name );
			m["x"] = md->x;
			m["y"] = md->y;
			m["lv"] = md->level;
			m["hp"] = md->status.hp;
			m["mhp"] = md->status.max_hp;
			m["boss"] = md->get_bosstype();
			m["target"] = md->target_id;
			m["dist"] = distance_bl( self, bl );
			( *out )["mobs"].push_back( m );
			break;
		}
		case BL_PC: {
			map_session_data* tsd = reinterpret_cast<map_session_data*>( bl );

			if( pc_isinvisible( tsd ) )
				return 0;

			json p;
			p["id"] = tsd->id;
			p["cid"] = tsd->status.char_id;
			p["name"] = to_utf8( tsd->status.name );
			p["x"] = tsd->x;
			p["y"] = tsd->y;
			p["blv"] = tsd->status.base_level;
			p["class"] = tsd->status.class_;
			p["party"] = tsd->status.party_id;
			p["bot"] = tsd->state.aibot != 0;
			p["dead"] = pc_isdead( tsd );
			p["hp"] = tsd->battle_status.hp;
			p["mhp"] = tsd->battle_status.max_hp;
			p["sit"] = pc_issit( tsd ) && !pc_isdead( tsd );
			p["vending"] = tsd->state.vending != 0;
			p["dist"] = distance_bl( self, bl );
			( *out )["players"].push_back( p );
			break;
		}
		case BL_ITEM: {
			flooritem_data* fi = reinterpret_cast<flooritem_data*>( bl );

			json i;
			i["id"] = fi->id;
			i["item_id"] = static_cast<t_itemid>( fi->item.nameid );
			i["amount"] = static_cast<int32>( fi->item.amount );
			i["x"] = fi->x;
			i["y"] = fi->y;
			i["dist"] = distance_bl( self, bl );
			i["owner"] = fi->first_get_charid;
			( *out )["items"].push_back( i );
			break;
		}
		case BL_NPC: {
			npc_data* nd = reinterpret_cast<npc_data*>( bl );

			if( nd->sc.option & OPTION_INVISIBLE )
				return 0;

			json n;
			n["id"] = nd->id;
			n["name"] = to_utf8( nd->exname );
			n["x"] = nd->x;
			n["y"] = nd->y;
			n["dist"] = distance_bl( self, bl );
			( *out )["npcs"].push_back( n );
			break;
		}
		default:
			break;
	}

	return 1;
}

int32 nearby_bots_sub( block_list* bl, va_list ap ){
	std::vector<uint32>* out = va_arg( ap, std::vector<uint32>* );
	int32 exclude = va_arg( ap, int32 );
	map_session_data* tsd = reinterpret_cast<map_session_data*>( bl );

	if( tsd->state.aibot && tsd->id != exclude && bots.find( tsd->status.char_id ) != bots.end() )
		out->push_back( tsd->status.char_id );

	return 0;
}

// ---------------------------------------------------------------------------
// Command handlers
// ---------------------------------------------------------------------------

using cmd_func = void (*)( const json& req, json& res );

void cmd_login( const json& req, json& res ){
	if( !chrif_isconnected() ){
		res["error"] = "char_server_offline";
		return;
	}
	if( bots.size() >= cfg.max_bots ){
		res["error"] = "max_bots_reached";
		return;
	}

	std::string where;
	char esc[NAME_LENGTH * 2 + 1];

	if( req.contains( "char_id" ) ){
		where = "`char_id` = '" + std::to_string( req["char_id"].get<uint32>() ) + "'";
	}else if( req.contains( "name" ) ){
		std::string name = from_utf8( req["name"].get<std::string>() );

		if( name.empty() || name.size() >= NAME_LENGTH ){
			res["error"] = "invalid_name";
			return;
		}
		Sql_EscapeStringLen( mmysql_handle, esc, name.c_str(), name.size() );
		where = std::string( "`name` = '" ) + esc + "'";
	}else{
		res["error"] = "missing_name";
		return;
	}

	if( Sql_Query( mmysql_handle, "SELECT `char_id`, `account_id`, `sex`, `name` FROM `%s` WHERE %s AND `delete_date` = 0 LIMIT 1", cfg.char_table.c_str(), where.c_str() ) != SQL_SUCCESS ){
		Sql_ShowDebug( mmysql_handle );
		res["error"] = "sql_error";
		return;
	}

	if( Sql_NextRow( mmysql_handle ) != SQL_SUCCESS ){
		Sql_FreeResult( mmysql_handle );
		res["error"] = "char_not_found";
		return;
	}

	char* data;
	uint32 char_id, account_id;
	int32 sex;
	std::string name;

	Sql_GetData( mmysql_handle, 0, &data, nullptr ); char_id = strtoul( data, nullptr, 10 );
	Sql_GetData( mmysql_handle, 1, &data, nullptr ); account_id = strtoul( data, nullptr, 10 );
	Sql_GetData( mmysql_handle, 2, &data, nullptr ); sex = ( data[0] == 'F' ) ? SEX_FEMALE : SEX_MALE;
	Sql_GetData( mmysql_handle, 3, &data, nullptr ); name = data;
	Sql_FreeResult( mmysql_handle );

	res["id"] = char_id;
	res["aid"] = account_id;
	res["name"] = to_utf8( name.c_str() );

	if( bots.find( char_id ) != bots.end() ){
		res["error"] = "already_bot";
		return;
	}
	if( map_charid2sd( char_id ) != nullptr || map_id2sd( account_id ) != nullptr || chrif_search( account_id ) != nullptr ){
		res["error"] = "account_in_use";
		return;
	}

	map_session_data* sd;

	CREATE( sd, map_session_data, 1 );
	new ( sd ) map_session_data();
	pc_setnewpc( sd, account_id, char_id, 0, gettick(), sex, 0 );
	sd->state.aibot = 1;

	s_aibot bot;
	bot.account_id = account_id;
	bot.char_id = char_id;
	bot.name = name;
	bots[char_id] = bot;
	bot_by_account[account_id] = char_id;

	chrif_authreq( sd, true );

	bots[char_id].loadend_tid = add_timer( gettick() + LOADEND_INTERVAL, aibot_loadend_timer, account_id, 0 );

	res["ok"] = true;
	res["pending"] = true;
}

void cmd_logout( const json& req, json& res ){
	uint32 char_id = req.value( "bot", 0u );
	map_session_data* sd = bot_sd( char_id );

	if( sd == nullptr ){
		if( bots.find( char_id ) != bots.end() ){
			bot_forget( char_id );
			res["ok"] = true;
			return;
		}
		res["error"] = "unknown_bot";
		return;
	}

	bot_forget( char_id );
	map_quit( sd );
	res["ok"] = true;
}

void cmd_list( const json& req, json& res ){
	res["bots"] = json::array();
	for( const auto& it : bots ){
		map_session_data* sd = bot_sd( it.first );

		if( sd != nullptr && it.second.ready ){
			res["bots"].push_back( bot_state( sd ) );
		}else{
			json p;
			p["id"] = it.first;
			p["name"] = to_utf8( it.second.name.c_str() );
			p["pending"] = true;
			res["bots"].push_back( p );
		}
	}
	res["ok"] = true;
}

void cmd_status( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){
		res["error"] = "unknown_bot";
		return;
	}
	res["state"] = bot_state( sd );
	res["ok"] = true;
}

void cmd_walk( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( !bot_can_act( sd, res ) ) return;

	int16 tx = req.value( "x", (int16)sd->x );
	int16 ty = req.value( "y", (int16)sd->y );

	bot_stand( sd );

	if( unit_walktoxy( sd, tx, ty, 4 ) ){
		res["ok"] = true;
		res["x"] = tx;
		res["y"] = ty;
		return;
	}

	// Target unreachable in one go (too far or blocked): try closer points
	// on the straight line, with some jitter, so the brain can hop there.
	int32 dx = tx - sd->x, dy = ty - sd->y;
	int32 dist = std::max( std::abs( dx ), std::abs( dy ) );

	for( int32 step : { 14, 10, 7, 4, 2 } ){
		if( step >= dist )
			continue;

		for( int32 attempt = 0; attempt < 4; attempt++ ){
			int16 nx = static_cast<int16>( sd->x + dx * step / dist + ( attempt ? rnd_value( -2, 2 ) : 0 ) );
			int16 ny = static_cast<int16>( sd->y + dy * step / dist + ( attempt ? rnd_value( -2, 2 ) : 0 ) );

			if( unit_walktoxy( sd, nx, ny, 4 ) ){
				res["ok"] = true;
				res["partial"] = true;
				res["x"] = nx;
				res["y"] = ny;
				return;
			}
		}
	}

	res["error"] = "no_path";
}

void cmd_stop( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }

	unit_stop_attack( sd );
	unit_stop_walking( sd, USW_FIXPOS );
	res["ok"] = true;
}

void cmd_attack( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( !bot_can_act( sd, res ) ) return;

	int32 target = req.value( "target", 0 );
	block_list* bl = map_id2bl( target );

	if( bl == nullptr || bl->m != sd->m || status_isdead( *bl ) ){
		res["error"] = "invalid_target";
		return;
	}

	bot_stand( sd );
	// unit_attack returns walking stop flags, not success: check the resulting target instead
	bool continuous = req.value( "continuous", true );
	unit_attack( sd, target, continuous ? 1 : 0 );
	if( sd->ud.target != target && sd->ud.target_to != target ){
		res["error"] = "attack_failed";
		return;
	}
	// A client re-sends the attack when it reaches the target; bots have the
	// chase timer do that for them.
	if( continuous )
		bots[sd->status.char_id].attack_target = target;
	res["ok"] = true;
}

void cmd_skill( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( !bot_can_act( sd, res ) ) return;

	uint16 skill_id = 0;

	if( req.contains( "skill" ) && req["skill"].is_string() )
		skill_id = skill_name2id( req["skill"].get<std::string>().c_str() );
	else
		skill_id = req.value( "skill", (uint16)0 );

	if( skill_id == 0 ){
		res["error"] = "unknown_skill";
		return;
	}

	uint16 known = pc_checkskill( sd, skill_id );

	if( known == 0 ){
		res["error"] = "skill_not_learned";
		return;
	}

	uint16 lv = std::min<uint16>( req.value( "lv", known ), known );

	bot_stand( sd );

	int32 ok;
	if( req.contains( "x" ) && req.contains( "y" ) )
		ok = unit_skilluse_pos( sd, req["x"].get<int16>(), req["y"].get<int16>(), skill_id, lv );
	else
		ok = unit_skilluse_id( sd, req.value( "target", sd->id ), skill_id, lv );

	if( !ok ){
		res["error"] = "skill_failed";
		return;
	}
	res["ok"] = true;
	res["skill"] = skill_id;
	res["lv"] = lv;
}

void cmd_useitem( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( !bot_can_act( sd, res ) ) return;

	int16 idx = pc_search_inventory( sd, req.value( "item", (t_itemid)0 ) );

	if( idx < 0 ){
		res["error"] = "no_item";
		return;
	}
	if( !pc_useitem( sd, idx ) ){
		res["error"] = "use_failed";
		return;
	}
	res["ok"] = true;
}

void cmd_say( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( sd->prev == nullptr ){ res["error"] = "not_on_map"; return; }

	std::string msg = from_utf8( req.value( "msg", std::string() ) );

	if( msg.empty() ){
		res["error"] = "empty_message";
		return;
	}

	char output[CHAT_SIZE_MAX + NAME_LENGTH * 2];
	safesnprintf( output, sizeof( output ), "%s : %s", sd->status.name, msg.c_str() );

	clif_GlobalMessage( *sd, output, AREA_CHAT_WOC );
	log_chat( LOG_CHAT_GLOBAL, 0, sd->status.char_id, sd->status.account_id, bot_map_name( sd ), sd->x, sd->y, nullptr, msg.c_str() );

	// Let the other bots hear it too
	aibot_on_public_chat( *sd, msg.c_str() );

	res["ok"] = true;
}

void cmd_whisper( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }

	std::string to = from_utf8( req.value( "to", std::string() ) );
	std::string msg = from_utf8( req.value( "msg", std::string() ) );

	if( to.empty() || msg.empty() || to.size() >= NAME_LENGTH ){
		res["error"] = "invalid_params";
		return;
	}
	if( msg.size() >= CHAT_SIZE_MAX )
		msg.resize( CHAT_SIZE_MAX - 1 );

	map_session_data* dst = map_nick2sd( to.c_str(), false );

	if( dst == nullptr ){
		char target[NAME_LENGTH], text[CHAT_SIZE_MAX];
		safestrncpy( target, to.c_str(), sizeof( target ) );
		safestrncpy( text, msg.c_str(), sizeof( text ) );
		intif_wis_message( sd, target, text, strlen( text ) + 1 );
	}else if( dst->state.aibot ){
		aibot_on_whisper( *dst, sd->status.name, msg.c_str() );
	}else{
		clif_wis_message( dst, sd->status.name, msg.c_str(), msg.size() + 1, 0 );
	}

	log_chat( LOG_CHAT_WHISPER, 0, sd->status.char_id, sd->status.account_id, bot_map_name( sd ), sd->x, sd->y, to.c_str(), msg.c_str() );
	res["ok"] = true;
}

void cmd_party_chat( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( sd->status.party_id == 0 ){ res["error"] = "no_party"; return; }

	std::string msg = from_utf8( req.value( "msg", std::string() ) );
	char output[CHAT_SIZE_MAX + NAME_LENGTH * 2];

	safesnprintf( output, sizeof( output ), "%s : %s", sd->status.name, msg.c_str() );
	party_send_message( sd, output, strlen( output ) + 1 );
	res["ok"] = true;
}

void cmd_emotion( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( sd->prev == nullptr ){ res["error"] = "not_on_map"; return; }

	int32 type = req.value( "type", 0 );

	if( type < 0 || type >= ET_MAX ){
		res["error"] = "invalid_emotion";
		return;
	}
	clif_emotion( *sd, static_cast<emotion_type>( type ) );
	res["ok"] = true;
}

void cmd_sit( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( !bot_can_act( sd, res ) ) return;

	if( !pc_issit( sd ) ){
		pc_setsit( sd );
		skill_sit( sd, true );
		clif_sitting( *sd );
	}
	res["ok"] = true;
}

void cmd_stand( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( !bot_can_act( sd, res ) ) return;

	bot_stand( sd );
	res["ok"] = true;
}

void cmd_pickup( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( !bot_can_act( sd, res ) ) return;

	block_list* bl = map_id2bl( req.value( "target", 0 ) );

	if( bl == nullptr || bl->type != BL_ITEM || bl->m != sd->m ){
		res["error"] = "invalid_item";
		return;
	}
	if( !check_distance_bl( sd, bl, 2 ) ){
		res["error"] = "too_far";
		res["x"] = bl->x;
		res["y"] = bl->y;
		return;
	}

	bot_stand( sd );
	if( !pc_takeitem( sd, reinterpret_cast<flooritem_data*>( bl ) ) ){
		res["error"] = "pickup_failed";
		return;
	}
	res["ok"] = true;
}

void cmd_warp( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( sd->prev == nullptr ){ res["error"] = "not_on_map"; return; }

	std::string mapname = req.value( "map", std::string( bot_map_name( sd ) ) );
	uint16 mapindex = mapindex_name2id( mapname.c_str() );

	if( mapindex == 0 ){
		res["error"] = "invalid_map";
		return;
	}

	bot_stand( sd );
	e_setpos r = pc_setpos( sd, mapindex, req.value( "x", 0 ), req.value( "y", 0 ), CLR_TELEPORT );

	if( r != SETPOS_OK ){
		res["error"] = "warp_failed";
		res["code"] = r;
		return;
	}
	res["ok"] = true;
}

void cmd_tele( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( !bot_can_act( sd, res ) ) return;

	bot_stand( sd );
	if( pc_randomwarp( sd, CLR_TELEPORT ) != 0 ){
		res["error"] = "tele_failed";
		return;
	}
	res["ok"] = true;
}

void cmd_respawn( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( !pc_isdead( sd ) ){
		res["error"] = "not_dead";
		return;
	}
	pc_respawn( sd, CLR_OUTSIGHT );
	res["ok"] = true;
}

void cmd_scan( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( sd->prev == nullptr ){ res["error"] = "not_on_map"; return; }

	int16 range = std::clamp<int16>( req.value( "range", (int16)AREA_SIZE ), 1, cfg.scan_range_max );
	int32 type = BL_MOB | BL_PC | BL_ITEM;

	if( req.value( "npcs", false ) )
		type |= BL_NPC;

	json out;
	out["mobs"] = json::array();
	out["players"] = json::array();
	out["items"] = json::array();
	out["npcs"] = json::array();

	map_foreachinallrange( scan_sub, sd, range, type, &out, sd );

	res["scan"] = out;
	res["x"] = sd->x;
	res["y"] = sd->y;
	res["map"] = bot_map_name( sd );
	res["ok"] = true;
}

void cmd_inventory( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }

	res["items"] = json::array();
	for( int32 i = 0; i < MAX_INVENTORY; i++ ){
		if( sd->inventory.u.items_inventory[i].nameid == 0 || sd->inventory.u.items_inventory[i].amount <= 0 )
			continue;
		res["items"].push_back( item_json( sd, i ) );
	}
	res["zeny"] = sd->status.zeny;
	res["w"] = sd->weight;
	res["mw"] = sd->max_weight;
	res["ok"] = true;
}

void cmd_equip( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( !bot_can_act( sd, res ) ) return;

	int32 idx = req.value( "idx", -1 );

	if( idx < 0 || idx >= MAX_INVENTORY || sd->inventory_data[idx] == nullptr ){
		res["error"] = "invalid_index";
		return;
	}

	bool ok;
	if( req.value( "unequip", false ) )
		ok = pc_unequipitem( sd, idx, 1 );
	else
		ok = pc_equipitem( sd, idx, sd->inventory_data[idx]->equip );

	if( !ok ){
		res["error"] = "equip_failed";
		return;
	}
	res["ok"] = true;
}

/// Simplified NPC shop: sells directly at item_db sell price.
void cmd_sell( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( !bot_can_act( sd, res ) ) return;

	std::unordered_set<t_itemid> only, keep;
	bool all_loot = req.value( "loot", false );

	if( req.contains( "items" ) )
		for( const auto& v : req["items"] ) only.insert( v.get<t_itemid>() );
	if( req.contains( "keep" ) )
		for( const auto& v : req["keep"] ) keep.insert( v.get<t_itemid>() );

	if( only.empty() && !all_loot ){
		res["error"] = "nothing_selected";
		return;
	}

	int64 total = 0;
	int32 count = 0;

	for( int32 i = 0; i < MAX_INVENTORY; i++ ){
		item& it = sd->inventory.u.items_inventory[i];
		item_data* id = sd->inventory_data[i];

		if( it.nameid == 0 || it.amount <= 0 || id == nullptr || it.equip != 0 || it.favorite )
			continue;
		if( keep.count( it.nameid ) )
			continue;
		if( !only.empty() ){
			if( !only.count( it.nameid ) )
				continue;
		}else if( id->type != IT_ETC && id->type != IT_CARD && !( id->type == IT_WEAPON || id->type == IT_ARMOR ) ){
			continue; // loot mode: etc, cards and spare equipment only
		}
		if( !itemdb_cansell( &it, pc_get_group_level( sd ) ) )
			continue;

		int64 value = static_cast<int64>( id->value_sell ) * it.amount;

		if( static_cast<int64>( sd->status.zeny ) + total + value > MAX_ZENY )
			break;

		total += value;
		count += it.amount;
		pc_delitem( sd, i, it.amount, 0, 6, LOG_TYPE_NPC );
	}

	if( total > 0 )
		pc_getzeny( sd, static_cast<int32>( total ), LOG_TYPE_NPC );

	res["ok"] = true;
	res["zeny_gained"] = total;
	res["sold"] = count;
}

/// Simplified NPC shop: buys directly at item_db buy price.
void cmd_buy( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( !bot_can_act( sd, res ) ) return;

	t_itemid nameid = req.value( "item", (t_itemid)0 );
	int32 amount = std::clamp( req.value( "amount", 1 ), 1, MAX_AMOUNT );
	std::shared_ptr<item_data> id = item_db.find( nameid );

	if( id == nullptr ){
		res["error"] = "invalid_item";
		return;
	}

	int64 cost = static_cast<int64>( id->value_buy ) * amount;

	if( cost > sd->status.zeny ){
		res["error"] = "not_enough_zeny";
		return;
	}
	if( !pc_inventoryblank( sd ) && pc_search_inventory( sd, nameid ) < 0 ){
		res["error"] = "inventory_full";
		return;
	}
	if( sd->weight + id->weight * amount > sd->max_weight ){
		res["error"] = "overweight";
		return;
	}

	item it = {};
	it.nameid = nameid;
	it.identify = 1;

	if( pc_payzeny( sd, static_cast<int32>( cost ), LOG_TYPE_NPC ) != 0 ){
		res["error"] = "not_enough_zeny";
		return;
	}
	if( pc_additem( sd, &it, amount, LOG_TYPE_NPC ) != ADDITEM_SUCCESS ){
		pc_getzeny( sd, static_cast<int32>( cost ), LOG_TYPE_NPC );
		res["error"] = "add_failed";
		return;
	}
	res["ok"] = true;
	res["cost"] = cost;
}

void cmd_statup( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }

	static const std::unordered_map<std::string, int32> stats = {
		{ "str", SP_STR }, { "agi", SP_AGI }, { "vit", SP_VIT },
		{ "int", SP_INT }, { "dex", SP_DEX }, { "luk", SP_LUK },
	};

	auto it = stats.find( req.value( "stat", std::string() ) );

	if( it == stats.end() ){
		res["error"] = "invalid_stat";
		return;
	}

	int32 n = std::clamp( req.value( "amount", 1 ), 1, 100 );
	int32 done = 0;

	while( done < n && pc_statusup( sd, it->second, 1 ) )
		done++;

	res["ok"] = done > 0;
	res["raised"] = done;
	if( done == 0 )
		res["error"] = "no_points";
}

void cmd_skillup( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }

	uint16 skill_id;

	if( req.contains( "skill" ) && req["skill"].is_string() )
		skill_id = skill_name2id( req["skill"].get<std::string>().c_str() );
	else
		skill_id = req.value( "skill", (uint16)0 );

	uint16 before = pc_checkskill( sd, skill_id );
	int32 n = std::clamp( req.value( "amount", 1 ), 1, 20 );

	for( int32 i = 0; i < n; i++ )
		pc_skillup( sd, skill_id );

	res["lv"] = pc_checkskill( sd, skill_id );
	res["ok"] = res["lv"].get<uint16>() > before;
	if( !res["ok"].get<bool>() )
		res["error"] = "skillup_failed";
}

void cmd_party_create( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( sd->status.party_id != 0 ){ res["error"] = "already_in_party"; return; }

	std::string name = from_utf8( req.value( "name", std::string() ) );

	if( name.empty() || name.size() >= NAME_LENGTH ){
		res["error"] = "invalid_name";
		return;
	}

	char buf[NAME_LENGTH];
	safestrncpy( buf, name.c_str(), sizeof( buf ) );
	party_create( *sd, buf, req.value( "share_exp", false ) ? 1 : 0, req.value( "share_item", false ) ? 1 : 0 );
	res["ok"] = true;
	res["pending"] = true;
}

void cmd_party_invite( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }

	std::string name = from_utf8( req.value( "name", std::string() ) );
	map_session_data* tsd = map_nick2sd( name.c_str(), false );

	if( tsd == nullptr ){
		res["error"] = "target_not_found";
		return;
	}
	if( !party_invite( *sd, tsd ) ){
		res["error"] = "invite_failed";
		return;
	}
	res["ok"] = true;
}

void cmd_party_reply( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( sd->party_invite <= 0 ){ res["error"] = "no_invite"; return; }

	party_reply_invite( *sd, sd->party_invite, req.value( "accept", true ) ? 1 : 0 );
	res["ok"] = true;
}

void cmd_jobchange( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( !bot_can_act( sd, res ) ) return;

	int32 job = req.value( "job", -1 );

	if( !pcdb_checkid( job ) ){
		res["error"] = "invalid_job";
		return;
	}
	if( !pc_jobchange( sd, job, 0 ) ){
		res["error"] = "jobchange_failed";
		return;
	}
	res["ok"] = true;
	res["class"] = sd->status.class_;
	res["jlv"] = sd->status.job_level;
	res["skpts"] = sd->status.skill_point;
}

json player_json( map_session_data* tsd ){
	json p;
	p["id"] = tsd->id;
	p["cid"] = tsd->status.char_id;
	p["name"] = to_utf8( tsd->status.name );
	p["map"] = mapindex_id2name( tsd->mapindex );
	p["x"] = tsd->x;
	p["y"] = tsd->y;
	p["hp"] = tsd->battle_status.hp;
	p["mhp"] = tsd->battle_status.max_hp;
	p["sp"] = tsd->battle_status.sp;
	p["msp"] = tsd->battle_status.max_sp;
	p["blv"] = tsd->status.base_level;
	p["class"] = tsd->status.class_;
	p["dead"] = pc_isdead( tsd );
	p["bot"] = tsd->state.aibot != 0;
	p["target"] = tsd->ud.target;
	p["party"] = tsd->status.party_id;
	return p;
}

/// Members of the bot's party, with live position/HP for the ones on this map-server.
void cmd_party_info( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( sd->status.party_id == 0 ){ res["error"] = "no_party"; return; }

	party_data* p = party_search( sd->status.party_id );

	if( p == nullptr ){ res["error"] = "no_party"; return; }

	res["party"] = p->party.party_id;
	res["name"] = to_utf8( p->party.name );
	res["members"] = json::array();
	for( int32 i = 0; i < MAX_PARTY; i++ ){
		const party_member& m = p->party.member[i];

		if( m.account_id == 0 )
			continue;

		json j;
		map_session_data* tsd = p->data[i].sd;

		if( tsd != nullptr )
			j = player_json( tsd );
		else{
			j["cid"] = m.char_id;
			j["name"] = to_utf8( m.name );
			j["map"] = m.map;
			j["class"] = m.class_;
			j["blv"] = m.lv;
		}
		j["online"] = m.online != 0;
		j["leader"] = m.leader != 0;
		res["members"].push_back( j );
	}
	res["ok"] = true;
}

/// Where is a player (on this map-server)?
void cmd_find( const json& req, json& res ){
	std::string name = from_utf8( req.value( "name", std::string() ) );
	map_session_data* tsd = name.empty() ? nullptr : map_nick2sd( name.c_str(), false );

	if( tsd == nullptr || tsd->prev == nullptr || pc_isinvisible( tsd ) ){
		res["error"] = "not_found";
		return;
	}
	res["player"] = player_json( tsd );
	res["ok"] = true;
}

/// Same rule as pc_job_can_use_item() in pc.cpp (static there).
bool bot_job_can_use( map_session_data* sd, const item_data* item ){
	uint64 job = 1ULL << ( sd->class_ & MAPID_BASEMASK );
	size_t index = ( sd->class_ & JOBL_2_1 ) ? 1 : ( ( sd->class_ & JOBL_2_2 ) ? 2 : 0 );

	return ( item->class_base[index] & job ) != 0;
}

/// Item details before buying: price, slot, level and whether the bot may use it.
void cmd_iteminfo( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }

	res["items"] = json::array();
	if( req.contains( "items" ) ){
		for( const auto& v : req["items"] ){
			t_itemid nameid = v.get<t_itemid>();
			std::shared_ptr<item_data> id = item_db.find( nameid );

			if( id == nullptr )
				continue;

			json j;
			j["id"] = nameid;
			j["name"] = to_utf8( id->ename.c_str() );
			j["type"] = id->type;
			j["subtype"] = id->subtype;
			j["loc"] = id->equip;
			j["elv"] = id->elv;
			j["atk"] = id->atk;
			j["def"] = id->def;
			j["weight"] = id->weight;
			j["buy"] = id->value_buy;
			j["sell"] = id->value_sell;
			// level, gender, upper/baby tier (pc_charm_usable) and the job itself
			j["usable"] = pc_charm_usable( sd, id.get() ) && bot_job_can_use( sd, id.get() );
			res["items"].push_back( j );
		}
	}
	res["ok"] = true;
}

void cmd_party_leave( const json& req, json& res ){
	map_session_data* sd = bot_sd( req.value( "bot", 0u ) );

	if( sd == nullptr ){ res["error"] = "unknown_bot"; return; }
	if( sd->status.party_id == 0 ){ res["error"] = "no_party"; return; }

	party_leave( *sd );
	res["ok"] = true;
}

void cmd_ping( const json& req, json& res ){
	res["ok"] = true;
	res["tick"] = gettick();
	res["bots"] = bots.size();
}

const std::unordered_map<std::string, cmd_func> commands = {
	{ "ping", cmd_ping },
	{ "login", cmd_login },
	{ "logout", cmd_logout },
	{ "list", cmd_list },
	{ "status", cmd_status },
	{ "walk", cmd_walk },
	{ "stop", cmd_stop },
	{ "attack", cmd_attack },
	{ "skill", cmd_skill },
	{ "useitem", cmd_useitem },
	{ "say", cmd_say },
	{ "whisper", cmd_whisper },
	{ "party_chat", cmd_party_chat },
	{ "emotion", cmd_emotion },
	{ "sit", cmd_sit },
	{ "stand", cmd_stand },
	{ "pickup", cmd_pickup },
	{ "warp", cmd_warp },
	{ "tele", cmd_tele },
	{ "respawn", cmd_respawn },
	{ "scan", cmd_scan },
	{ "inventory", cmd_inventory },
	{ "equip", cmd_equip },
	{ "sell", cmd_sell },
	{ "buy", cmd_buy },
	{ "statup", cmd_statup },
	{ "skillup", cmd_skillup },
	{ "party_create", cmd_party_create },
	{ "party_invite", cmd_party_invite },
	{ "party_reply", cmd_party_reply },
	{ "party_leave", cmd_party_leave },
	{ "party_info", cmd_party_info },
	{ "jobchange", cmd_jobchange },
	{ "find", cmd_find },
	{ "iteminfo", cmd_iteminfo },
};

void handle_line( int32 fd, const char* data, size_t len ){
	json req = json::parse( data, data + len, nullptr, false );
	json res;

	if( req.is_discarded() || !req.is_object() ){
		res["ok"] = false;
		res["error"] = "invalid_json";
		send_json( fd, res );
		return;
	}

	if( req.contains( "rid" ) )
		res["re"] = req["rid"];

	std::string cmd = req.value( "cmd", std::string() );
	s_aibot_client& client = clients[fd];

	if( cmd == "auth" ){
		client.authed = cfg.token.empty() || req.value( "token", std::string() ) == cfg.token;
		res["ok"] = client.authed;
		if( !client.authed )
			res["error"] = "bad_token";
		send_json( fd, res );
		return;
	}

	if( !client.authed ){
		res["ok"] = false;
		res["error"] = "not_authed";
		send_json( fd, res );
		return;
	}

	auto it = commands.find( cmd );

	// Any new action replaces a pending chase/attack
	static const std::unordered_set<std::string> actions = {
		"walk", "stop", "attack", "skill", "sit", "pickup", "warp", "tele", "respawn", "logout",
	};
	if( actions.count( cmd ) && req.contains( "bot" ) && req["bot"].is_number_unsigned() ){
		auto bit = bots.find( req["bot"].get<uint32>() );
		if( bit != bots.end() )
			bit->second.attack_target = 0;
	}

	if( it == commands.end() ){
		res["ok"] = false;
		res["error"] = "unknown_command";
		send_json( fd, res );
		return;
	}

	res["ok"] = false;
	try{
		it->second( req, res );
	}catch( const json::exception& e ){
		res["ok"] = false;
		res["error"] = "bad_params";
		res["detail"] = e.what();
	}

	if( cfg.debug )
		ShowDebug( "aibot: %s -> %s\n", cmd.c_str(), res.value( "error", std::string( "ok" ) ).c_str() );

	send_json( fd, res );
}

void client_disconnected( int32 fd ){
	clients.erase( fd );
	ShowInfo( "AI bot bridge: brain disconnected (fd %d).\n", fd );

	if( cfg.logout_on_disconnect && !has_listeners() ){
		std::vector<uint32> ids;
		for( const auto& it : bots )
			ids.push_back( it.first );

		for( uint32 char_id : ids ){
			map_session_data* sd = bot_sd( char_id );
			bot_forget( char_id );
			if( sd != nullptr )
				map_quit( sd );
		}
	}
}

int32 aibot_parse( int32 fd ){
	if( session[fd]->flag.eof ){
		client_disconnected( fd );
		do_close( fd );
		return 0;
	}

	uint32 handled = 0;

	while( RFIFOREST( fd ) > 0 && handled < cfg.max_commands_per_cycle ){
		const char* buf = RFIFOCP( fd, 0 );
		size_t rest = RFIFOREST( fd );
		const char* nl = static_cast<const char*>( memchr( buf, '\n', rest ) );

		if( nl == nullptr ){
			if( rest > cfg.max_line_length ){
				ShowWarning( "AI bot bridge: line too long from fd %d, closing.\n", fd );
				set_eof( fd );
			}
			break;
		}

		size_t len = nl - buf;
		size_t trimmed = len;

		if( trimmed > 0 && buf[trimmed - 1] == '\r' )
			trimmed--;
		if( trimmed > 0 )
			handle_line( fd, buf, trimmed );

		// The handler may have closed the session
		if( session[fd] == nullptr )
			return 0;

		RFIFOSKIP( fd, len + 1 );
		handled++;
	}

	return 0;
}

int32 aibot_accept( int32 fd ){
	int32 newfd = listen_default_recv( fd );

	if( newfd <= 0 || session[newfd] == nullptr )
		return 0;

	session[newfd]->func_parse = aibot_parse;
	session[newfd]->flag.server = 1; // no stall timeout, large buffers
	session[newfd]->rdata_tick = 0;

	s_aibot_client client;
	client.authed = cfg.token.empty();
	clients[newfd] = client;

	ShowStatus( "AI bot bridge: brain connected from %s (fd %d).\n", ip2str( session[newfd]->client_addr, nullptr ), newfd );

	json hello;
	hello["ev"] = "hello";
	hello["auth_required"] = !cfg.token.empty();
	hello["bots"] = bots.size();
	hello["state_interval"] = cfg.state_interval;
	send_json( newfd, hello );

	return 0;
}

// ---------------------------------------------------------------------------
// Timers
// ---------------------------------------------------------------------------

TIMER_FUNC( aibot_loadend_timer ){
	auto ait = bot_by_account.find( static_cast<uint32>( id ) );

	if( ait == bot_by_account.end() )
		return 0;

	s_aibot& bot = bots[ait->second];

	if( bot.loadend_tid != tid )
		return 0;

	bot.loadend_tid = INVALID_TIMER;

	map_session_data* sd = map_id2sd( id );

	if( sd != nullptr && sd->state.aibot && sd->state.active && sd->state.pc_loaded ){
		if( sd->prev == nullptr )
			clif_parse_LoadEndAck( 0, sd );

		if( sd->prev != nullptr ){
			bot.loadend_tries = 0;

			if( !bot.ready ){
				bot.ready = true;
				ShowStatus( "AI bot '" CL_WHITE "%s" CL_RESET "' spawned on %s (%d,%d).\n", sd->status.name, bot_map_name( sd ), sd->x, sd->y );

				json ev;
				ev["ev"] = "spawned";
				ev["bot"] = bot.char_id;
				ev["state"] = bot_state( sd );
				broadcast( ev );
			}else{
				json ev;
				ev["ev"] = "map_changed";
				ev["bot"] = bot.char_id;
				ev["map"] = bot_map_name( sd );
				ev["x"] = sd->x;
				ev["y"] = sd->y;
				broadcast( ev );
			}
			return 0;
		}
	}

	if( ++bot.loadend_tries >= LOADEND_MAX_TRIES ){
		ShowWarning( "AI bot bridge: bot '%s' (CID %u) failed to load.\n", bot.name.c_str(), bot.char_id );

		json ev;
		ev["ev"] = "login_failed";
		ev["bot"] = bot.char_id;
		ev["name"] = to_utf8( bot.name.c_str() );
		broadcast( ev );

		uint32 char_id = bot.char_id;
		bot_forget( char_id );
		if( sd != nullptr && sd->state.aibot )
			map_quit( sd );
		return 0;
	}

	bot.loadend_tid = add_timer( tick + LOADEND_INTERVAL, aibot_loadend_timer, id, 0 );
	return 0;
}

/// Keeps bots attacking their target: walks into range and restarts the attack
/// whenever it stopped (what the game client does for real players).
TIMER_FUNC( aibot_chase_timer ){
	for( auto& it : bots ){
		s_aibot& bot = it.second;

		if( bot.attack_target == 0 )
			continue;

		map_session_data* sd = bot_sd( it.first );
		block_list* bl = map_id2bl( bot.attack_target );

		if( sd == nullptr || sd->prev == nullptr || pc_isdead( sd ) || bl == nullptr || bl->prev == nullptr
			|| bl->m != sd->m || status_isdead( *bl ) || !check_distance_bl( sd, bl, cfg.scan_range_max ) ){
			bot.attack_target = 0;
			continue;
		}

		unit_data& ud = sd->ud;

		if( ud.target == bot.attack_target || ud.skilltimer != INVALID_TIMER || pc_issit( sd ) )
			continue; // already attacking / busy
		if( unit_is_walking( sd ) && ud.target_to == bot.attack_target )
			continue; // still chasing

		int32 range = status_get_range( sd );

		if( battle_check_range( sd, bl, range ) )
			unit_attack( sd, bot.attack_target, 1 );
		else if( !unit_walktobl( sd, bl, range, 2 ) )
			bot.attack_target = 0; // unreachable, let the brain pick another one
	}

	return 0;
}

TIMER_FUNC( aibot_state_timer ){
	if( bots.empty() )
		return 0;

	std::vector<uint32> gone;
	json batch = json::array();
	bool listeners = has_listeners();

	for( const auto& it : bots ){
		map_session_data* sd = map_charid2sd( it.first );

		if( sd == nullptr ){
			// Pending logins are not on the id db yet
			if( it.second.ready )
				gone.push_back( it.first );
			continue;
		}
		if( !it.second.ready || !listeners )
			continue;

		batch.push_back( bot_state( sd ) );

		if( batch.size() >= 50 ){
			json ev;
			ev["ev"] = "state";
			ev["bots"] = std::move( batch );
			broadcast( ev );
			batch = json::array();
		}
	}

	if( !batch.empty() ){
		json ev;
		ev["ev"] = "state";
		ev["bots"] = std::move( batch );
		broadcast( ev );
	}

	for( uint32 char_id : gone ){
		json ev;
		ev["ev"] = "logout";
		ev["bot"] = char_id;
		broadcast( ev );
		bot_forget( char_id );
	}

	return 0;
}

// ---------------------------------------------------------------------------
// Config
// ---------------------------------------------------------------------------

bool config_bool( const char* v ){
	return !strcmpi( v, "yes" ) || !strcmpi( v, "on" ) || !strcmpi( v, "true" ) || atoi( v ) != 0;
}

void read_config( const char* path, bool required ){
	FILE* fp = fopen( path, "r" );

	if( fp == nullptr ){
		if( required )
			ShowWarning( "AI bot bridge: config file '%s' not found, using defaults.\n", path );
		return;
	}

	char line[1024], w1[1024], w2[1024];

	while( fgets( line, sizeof( line ), fp ) ){
		if( line[0] == '/' && line[1] == '/' )
			continue;
		if( sscanf( line, "%1023[^:]: %1023[^\r\n]", w1, w2 ) != 2 )
			continue;

		trim( w1 );
		trim( w2 );

		if( !strcmpi( w1, "enabled" ) ) cfg.enabled = config_bool( w2 );
		else if( !strcmpi( w1, "bind_ip" ) ) cfg.bind_ip = w2;
		else if( !strcmpi( w1, "port" ) ) cfg.port = static_cast<uint16>( atoi( w2 ) );
		else if( !strcmpi( w1, "token" ) ) cfg.token = w2;
		else if( !strcmpi( w1, "state_interval" ) ) cfg.state_interval = std::max( 100, atoi( w2 ) );
		else if( !strcmpi( w1, "max_bots" ) ) cfg.max_bots = std::max( 0, atoi( w2 ) );
		else if( !strcmpi( w1, "logout_on_disconnect" ) ) cfg.logout_on_disconnect = config_bool( w2 );
		else if( !strcmpi( w1, "char_table" ) ) cfg.char_table = w2;
		else if( !strcmpi( w1, "chat_range" ) ) cfg.chat_range = static_cast<int16>( std::max( 1, atoi( w2 ) ) );
		else if( !strcmpi( w1, "scan_range_max" ) ) cfg.scan_range_max = static_cast<int16>( std::max( 1, atoi( w2 ) ) );
		else if( !strcmpi( w1, "max_commands_per_cycle" ) ) cfg.max_commands_per_cycle = std::max( 1, atoi( w2 ) );
		else if( !strcmpi( w1, "debug" ) ) cfg.debug = config_bool( w2 );
		else if( !strcmpi( w1, "client_encoding" ) ){
			if( !strcmpi( w2, "utf8" ) || !strcmpi( w2, "utf-8" ) ) cfg.encoding = e_aibot_encoding::UTF8;
			else if( !strcmpi( w2, "latin1" ) ) cfg.encoding = e_aibot_encoding::LATIN1;
			else cfg.encoding = e_aibot_encoding::CP874;
		}
		else if( !strcmpi( w1, "import" ) ) read_config( w2, false );
		else ShowWarning( "AI bot bridge: unknown setting '%s' in %s\n", w1, path );
	}

	fclose( fp );
}

} // namespace

// ---------------------------------------------------------------------------
// Public API
// ---------------------------------------------------------------------------

void aibot_request_loadend( map_session_data& sd ){
	auto ait = bot_by_account.find( sd.status.account_id );

	if( ait == bot_by_account.end() )
		return;

	s_aibot& bot = bots[ait->second];

	if( bot.loadend_tid != INVALID_TIMER )
		return; // already scheduled

	bot.loadend_tries = 0;
	bot.loadend_tid = add_timer( gettick() + 1, aibot_loadend_timer, sd.status.account_id, 0 );
}

void aibot_on_public_chat( map_session_data& sd, const char* message ){
	if( bots.empty() || !has_listeners() || sd.prev == nullptr )
		return;

	std::vector<uint32> nearby;
	map_foreachinallrange( nearby_bots_sub, &sd, cfg.chat_range, BL_PC, &nearby, sd.id );

	if( nearby.empty() )
		return;

	json ev;
	ev["ev"] = "chat";
	ev["from"] = to_utf8( sd.status.name );
	ev["from_id"] = sd.id;
	ev["from_cid"] = sd.status.char_id;
	ev["from_bot"] = sd.state.aibot != 0;
	ev["map"] = mapindex_id2name( sd.mapindex );
	ev["x"] = sd.x;
	ev["y"] = sd.y;
	ev["msg"] = to_utf8( message );
	ev["bots"] = nearby;
	broadcast( ev );
}

void aibot_on_whisper( map_session_data& bot, const char* from_name, const char* message ){
	if( bots.find( bot.status.char_id ) == bots.end() )
		return;

	map_session_data* from = map_nick2sd( from_name, false );

	json ev;
	ev["ev"] = "whisper";
	ev["bot"] = bot.status.char_id;
	ev["from"] = to_utf8( from_name );
	ev["from_bot"] = from != nullptr && from->state.aibot;
	ev["msg"] = to_utf8( message );
	broadcast( ev );
}

void aibot_on_party_chat( int32 party_id, uint32 from_account_id, const char* message, const std::vector<uint32>& bot_char_ids ){
	if( bot_char_ids.empty() || !has_listeners() )
		return;

	// Party messages come as "<name> : <text>"
	std::string raw = message;
	std::string from, text = raw;
	size_t sep = raw.find( " : " );

	if( sep != std::string::npos ){
		from = raw.substr( 0, sep );
		text = raw.substr( sep + 3 );
	}

	map_session_data* fsd = map_id2sd( from_account_id );

	json ev;
	ev["ev"] = "party_chat";
	ev["party"] = party_id;
	ev["from"] = to_utf8( from.c_str() );
	ev["from_bot"] = fsd != nullptr && fsd->state.aibot;
	ev["msg"] = to_utf8( text.c_str() );
	ev["bots"] = json::array();
	for( uint32 cid : bot_char_ids ){
		if( fsd == nullptr || fsd->status.char_id != cid )
			ev["bots"].push_back( cid );
	}
	if( !ev["bots"].empty() )
		broadcast( ev );
}

void aibot_on_emotion( map_session_data& sd, int32 type ){
	if( bots.empty() || !has_listeners() || sd.prev == nullptr )
		return;

	std::vector<uint32> nearby;
	map_foreachinallrange( nearby_bots_sub, &sd, cfg.chat_range, BL_PC, &nearby, sd.id );

	if( nearby.empty() )
		return;

	json ev;
	ev["ev"] = "emotion";
	ev["from"] = to_utf8( sd.status.name );
	ev["from_id"] = sd.id;
	ev["from_bot"] = sd.state.aibot != 0;
	ev["type"] = type;
	ev["bots"] = nearby;
	broadcast( ev );
}

void aibot_on_party_invite( map_session_data& bot, map_session_data& inviter ){
	json ev;
	ev["ev"] = "party_invite";
	ev["bot"] = bot.status.char_id;
	ev["from"] = to_utf8( inviter.status.name );
	ev["from_cid"] = inviter.status.char_id;
	ev["from_bot"] = inviter.state.aibot != 0;
	ev["party"] = bot.party_invite;
	broadcast( ev );
}

void do_init_aibot(){
	read_config( "conf/aibot.conf", true );

	add_timer_func_list( aibot_loadend_timer, "aibot_loadend_timer" );
	add_timer_func_list( aibot_state_timer, "aibot_state_timer" );
	add_timer_func_list( aibot_chase_timer, "aibot_chase_timer" );

	if( !cfg.enabled ){
		ShowInfo( "AI bot bridge is " CL_RED "disabled" CL_RESET " (conf/aibot.conf).\n" );
		return;
	}

	listen_fd = make_listen_bind( str2ip( cfg.bind_ip.c_str() ), cfg.port );

	if( listen_fd <= 0 ){
		ShowError( "AI bot bridge: could not listen on %s:%u.\n", cfg.bind_ip.c_str(), cfg.port );
		listen_fd = -1;
		return;
	}

	listen_default_recv = session[listen_fd]->func_recv;
	session[listen_fd]->func_recv = aibot_accept;

	state_tid = add_timer_interval( gettick() + cfg.state_interval, aibot_state_timer, 0, 0, cfg.state_interval );
	chase_tid = add_timer_interval( gettick() + CHASE_INTERVAL, aibot_chase_timer, 0, 0, CHASE_INTERVAL );

	ShowStatus( "AI bot bridge listening on '" CL_WHITE "%s:%u" CL_RESET "'%s.\n", cfg.bind_ip.c_str(), cfg.port, cfg.token.empty() ? " (no token!)" : "" );
}

void do_final_aibot(){
	if( state_tid != INVALID_TIMER ){
		delete_timer( state_tid, aibot_state_timer );
		state_tid = INVALID_TIMER;
	}
	if( chase_tid != INVALID_TIMER ){
		delete_timer( chase_tid, aibot_chase_timer );
		chase_tid = INVALID_TIMER;
	}

	for( auto& it : bots ){
		if( it.second.loadend_tid != INVALID_TIMER )
			delete_timer( it.second.loadend_tid, aibot_loadend_timer );
	}
	bots.clear();
	bot_by_account.clear();

	for( const auto& it : clients ){
		if( session_isValid( it.first ) )
			do_close( it.first );
	}
	clients.clear();

	if( listen_fd > 0 && session_isValid( listen_fd ) )
		do_close( listen_fd );
	listen_fd = -1;
}
