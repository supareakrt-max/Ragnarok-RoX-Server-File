# AI Bot Bridge Protocol

The map-server (`src/map/aibot.cpp`) listens on a TCP port (default
`127.0.0.1:7100`, see `conf/aibot.conf`). An external program, the "brain",
connects and controls bot characters. Bots are real characters from the `char`
table that are logged in **without a game client**, the same way `@autotrade`
keeps characters online, so all normal player mechanics work for them.

* One JSON object per line (`\n`), UTF-8.
* The bridge converts names and chat between UTF-8 and the client codepage
  (`client_encoding`, default `cp874` for Thai clients).
* Requests carry a `rid`, and the matching response echoes it as `re`.
* Server events carry `ev` and have no `re`.
* If `token` is set in the config, send `{"cmd":"auth","token":"..."}` first.

```
-> {"rid":1,"cmd":"login","name":"PBank"}
<- {"re":1,"ok":true,"pending":true,"id":150001,"aid":2000001,"name":"PBank"}
<- {"ev":"spawned","bot":150001,"state":{...}}
-> {"rid":2,"cmd":"walk","bot":150001,"x":160,"y":190}
<- {"re":2,"ok":true,"x":160,"y":190}
```

Every failed command returns `{"ok":false,"error":"<code>"}`.

## Commands

`bot` is always the bot's **char_id**. Block ids (`target`) come from `scan`.

| cmd | params | notes |
|---|---|---|
| `auth` | `token` | required only when a token is configured |
| `ping` | | returns `tick`, `bots` |
| `login` | `name` or `char_id` | loads the character with no client; `spawned` event follows |
| `logout` | `bot` | saves and removes the bot |
| `list` | | every bot with its state (`pending` while loading) |
| `status` | `bot` | `state` object (see below) |
| `walk` | `bot`, `x`, `y` | if the target is unreachable it walks part of the way (`partial:true`); `no_path` when blocked |
| `stop` | `bot` | stops walking and attacking |
| `attack` | `bot`, `target`, `continuous`=true | the server chases the target for bots |
| `skill` | `bot`, `skill` (id or name, e.g. `"SM_BASH"`), `lv`?, `target`? or `x`,`y` | level is capped at the learned level; target defaults to self |
| `useitem` | `bot`, `item` (item id) | |
| `say` | `bot`, `msg` | public chat, heard by players and other bots |
| `whisper` | `bot`, `to`, `msg` | |
| `party_chat` | `bot`, `msg` | |
| `emotion` | `bot`, `type` | ids from `enum emotion_type` in `src/map/clif.hpp` |
| `sit` / `stand` | `bot` | |
| `pickup` | `bot`, `target` (floor item id) | `too_far` (with `x`,`y`) when more than 2 cells away |
| `warp` | `bot`, `map`, `x`, `y` | `x`=`y`=0 picks a random cell |
| `tele` | `bot` | random teleport on the current map (fly wing) |
| `respawn` | `bot` | only when dead, goes to the save point |
| `scan` | `bot`, `range`=AREA_SIZE, `npcs`=false | `mobs`, `players`, `items`, `npcs` around the bot |
| `inventory` | `bot` | items with `idx`, `id`, `name`, `type`, `loc`, `equipped`, `elv`, `atk`, `def`, `sell`, `buy`, `can_equip` |
| `equip` | `bot`, `idx`, `unequip`=false | |
| `sell` | `bot`, `items`=[ids] or `loot`=true, `keep`=[ids] | direct sale at item_db sell price (loot = etc, cards and unequipped gear) |
| `buy` | `bot`, `item`, `amount` | direct purchase at item_db buy price |
| `statup` | `bot`, `stat` (`str`..`luk`), `amount` | |
| `skillup` | `bot`, `skill`, `amount` | returns the new `lv` |
| `party_create` | `bot`, `name`, `share_exp`, `share_item` | |
| `party_invite` | `bot`, `name` | invites a player or another bot |
| `party_reply` | `bot`, `accept` | answers a pending invite |
| `party_leave` | `bot` | |

### State object

```json
{"id":150001,"aid":2000001,"name":"PBank","map":"prt_fild08","x":120,"y":88,
 "hp":180,"mhp":249,"sp":20,"msp":46,"blv":18,"jlv":9,"bexp":320,"bnext":1260,
 "class":1,"zeny":4542,"w":2700,"mw":28300,"dead":false,"sit":false,"onmap":true,
 "target":110015577,"walking":false,"casting":false,"party":0,"invite":0,
 "stpts":12,"skpts":0}
```

In `scan` results, `target` on a mob is the block id (the account id for players)
of whatever that mob is attacking. Compare it with the bot's `aid` to find the
monsters attacking it. `owner` on an item is the char_id with loot priority
(0 = free).

## Events

| ev | fields |
|---|---|
| `hello` | `auth_required`, `bots`, `state_interval` |
| `state` | `bots`: list of state objects, every `state_interval` ms, up to 50 per line |
| `spawned` | `bot`, `state` |
| `map_changed` | `bot`, `map`, `x`, `y` |
| `logout` | `bot` (kicked or disconnected by the server) |
| `login_failed` | `bot`, `name` |
| `chat` | `from`, `from_id`, `from_cid`, `from_bot`, `map`, `x`, `y`, `msg`, `bots` (bots within `chat_range`) |
| `whisper` | `bot`, `from`, `from_bot`, `msg` |
| `party_chat` | `party`, `from`, `from_bot`, `msg`, `bots` |
| `party_invite` | `bot`, `from`, `from_cid`, `from_bot`, `party` |

## Server-side hooks

| file | change |
|---|---|
| `pc.hpp` | `state.aibot` flag |
| `pc.cpp` (`pc_setpos`) | finishes the map load (LoadEndAck) for bots after warps |
| `clif.cpp` | forwards public chat and whispers to the brain, and skips login/loadmap NPC events for bots |
| `unit.cpp` | chases the attack target on the server (a normal client does this itself) |
| `party.cpp` | lets bots receive invites and forwards invites and party chat |
| `chrif.cpp` | removes the bot when its account gets kicked |
| `map.cpp` | init/final |
