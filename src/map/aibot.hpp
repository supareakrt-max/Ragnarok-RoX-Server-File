// Copyright (c) rAthena Dev Teams - Licensed under GNU GPL
// For more information, see LICENCE in the main folder

// AI Bot bridge
// -------------
// Lets an external process (the "brain", see tools/aibot) log in real characters
// without a game client and drive them through a line based JSON protocol over TCP.
// Bots are normal map_session_data objects bound to the null session (fd 0),
// the same way @autotrade keeps characters online, so every regular player
// mechanic (walking, combat, skills, items, party, chat) works for them.
//
// See doc/aibot_protocol.md for the protocol reference.

#ifndef AIBOT_HPP
#define AIBOT_HPP

#include <vector>

#include <common/cbasetypes.hpp>

class map_session_data;

void do_init_aibot();
void do_final_aibot();

/// Called by pc_setpos when a bot changed its position through a map reload.
/// Bots have no client to send the LoadEndAck, so the server does it for them.
void aibot_request_loadend( map_session_data& sd );

/// Chat hooks (Phase 1: chat capture)
void aibot_on_public_chat( map_session_data& sd, const char* message );
void aibot_on_whisper( map_session_data& bot, const char* from_name, const char* message );
void aibot_on_party_chat( int32 party_id, uint32 from_account_id, const char* message, const std::vector<uint32>& bot_char_ids );
void aibot_on_party_invite( map_session_data& bot, map_session_data& inviter );
void aibot_on_emotion( map_session_data& sd, int32 type );

#endif /* AIBOT_HPP */
