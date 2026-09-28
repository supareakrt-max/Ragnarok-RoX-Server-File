# RO_Manager source

Recovered from `RO_Manager.exe` (decompiled with ILSpy) and extended with:

- `ROManager/BotTab.cs` — "AI Bot" tab: start/stop `tools/aibot/run_brain.py`,
  import `bots.sql`, create new bot characters, list / delete bots.
- `ROManager/TimeTab.cs` — "เวลา / กลางวัน-กลางคืน" tab: PC clock vs. map-server
  clock and the automatic @day / @night schedule (`npc/Npc RoX/daynight.txt`).
- `ROManager/MobTab.cs` — "มอนสเตอร์" tab: live count / spawn / kill per map, the
  permanent spawn lines in `npc/<pre-re|re>/mobs` (+ `npc/Npc RoX/mob_spawn_custom.txt`),
  monster.conf overrides in `conf/import/battle_conf.txt` and boss / MVP on a schedule.
  Live actions and timers run through `npc/Npc RoX/mob_control.txt` (tables `rox_mob*`).
- `ROManager/AccountDelete.cs` — "ลบไอดี" button on the account tab.
- `ROManager/AnonTypes.cs` — stand-ins for anonymous types the decompiler could not emit.

`MainForm.cs` is decompiler output (async methods appear as state machine
structs); it is kept as-is apart from small fixes needed to compile.

## Build (Windows or Linux, .NET SDK 8+)

```
cd tools/RO_Manager_src
dotnet build -c Release -o out
```

Produces `out/RO_Manager.exe` (.NET Framework 4.5, x86 — same as the original,
needs `libmysql.dll` from the Server folder at runtime).
