# รายงานสรุปงาน: ระบบ AI Bot + RO_Manager — Ro-X Server

> เอกสารส่งต่อสำหรับแชท / session ใหม่ อ่านไฟล์นี้ก่อนเริ่มทำงานต่อ

- **Repo:** `supareakrt-max/Ragnarok-RoX-Server-File`
- **Branch งาน:** `claude/clever-mccarthy-3bwyk6` (ยังไม่ได้เปิด PR, ยังไม่ merge เข้า `main`)
- **อัปเดตล่าสุด:** 2026-09-28 (commit `753e590`)
- **Branch อื่นที่เกี่ยวข้อง:** `claude/new-session-qj95nh` = งานระบบ `@ai` และ Charm จากอีก session รวมเข้ามาแล้วใน commit `3c91e04`

---

## 1. ภาพรวม

```
┌──────────────────┐  TCP 127.0.0.1:7100 (JSON ทีละบรรทัด)  ┌────────────────────────┐
│ map-server.exe   │ ◄────────── คำสั่ง ─────────────────── │ สมองกล Python           │
│ src/map/aibot.cpp│ ─── สถานะ / แชท / อีโมท / event ─────► │ tools/aibot/run_brain.py│
└──────────────────┘                                        └────────────────────────┘
        ▲                                                             ▲
        │ ตัวละครบอท = ตัวละครจริงในตาราง char (account_id 3000000+) │
        │ ล็อกอินโดยไม่มีไคลเอนต์ (กลไกแบบ @autotrade)               │ เริ่ม/หยุดจาก
        └──────────────── RO_Manager.exe (แท็บ AI Bot / เวลา) ────────┘
```

---

## 2. เครื่องของผู้ใช้ (สำคัญ)

| เรื่อง | ค่า |
|---|---|
| OS | Windows, โฟลเดอร์หลัก `E:\Project 2\RO_20250716\` (มี `Server`, `Database`, `Game`, `Tools`) |
| เซิร์ฟ | rAthena fork, **Pre-Renewal** (`#define PRERE`), PACKETVER 20250716 |
| exe | **Win32 (x86) MinGW i686** ใช้ `libmysql.dll` / `pcre8.dll` / `zlib.dll` แบบ 32-bit **ผู้ใช้ไม่มีเครื่องมือคอมไพล์** Claude ต้องคอมไพล์ให้ |
| DB | USBWebserver MySQL port **3307**, user `root` / pass `usbw`, database `rathena_main`, codepage tis620 |
| phpMyAdmin | http://127.0.0.1:8080/phpmyadmin/ |
| Python | 3.13 64-bit ที่ `C:\Users\TNZ\AppData\Local\Programs\Python\Python313\python.exe` (ตอนแรกเจอปัญหา alias ของ Microsoft Store แก้ใน `start_bots.bat` แล้ว) |
| ไคลเอนต์ | ภาษาไทย cp874, ใช้ `client_encoding: cp874` |
| บอท | ผู้ใช้ตั้งชื่อบอทเป็นภาษาไทยเอง 10 ตัว (เช่น `น้องตองซ่า`, `แม่ค้ามือทอง`), เปิด Gemini แล้ว (`"llm": {"enabled": true}`) |
| `src/custom/` | **ไม่อยู่ใน git** (gitignored) ในเครื่องผู้ใช้มีแค่ไฟล์ของ `@ai` (ดู `ai_system/src/custom/`) |

---

## 3. สิ่งที่ทำแล้ว (เรียงตาม commit)

| Commit | งาน |
|---|---|
| `5b9671f` | Phase 1–4: bridge ใน map-server + สมองกล Python + create_bots + loadtest |
| `294ff1c` | LLM ฟรี: Gemini (หลัก) + Groq (สำรอง) มีโควต้าต่อวันและสลับเจ้าอัตโนมัติ |
| `3c91e04` | รวมงาน `@ai` + Charm ดรอป 0.20% + ระบบไล่ตีมอนฝั่งเซิร์ฟ + สคริปต์คอมไพล์ MinGW |
| `0aca18e` | RO_Manager: แท็บ AI Bot, แท็บเวลา/กลางวัน-กลางคืน, NPC `daynight.txt` |
| `7afd0cc`, `fafb4bc` | อ่าน `config.json` ที่บันทึกเป็นไทย ANSI (cp874) หรือ UTF-8 มี BOM ได้ |
| `17298d5`, `753e590` | บอทฉลาดขึ้น: เปลี่ยนอาชีพ, เรียนรู้แมพ, ความจำ, ปาร์ตี้, ซื้ออุปกรณ์ |

### 3.1 ฝั่งเซิร์ฟ (C++)

- **`src/map/aibot.cpp/.hpp`** (ใหม่): bridge TCP ตั้งค่าใน `conf/aibot.conf`
  - **คำสั่ง:** `login logout list status walk stop attack skill useitem say whisper party_chat emotion sit stand pickup warp tele respawn scan inventory equip sell buy statup skillup party_create party_invite party_reply party_leave party_info jobchange find iteminfo ping auth`
  - **event:** `hello state spawned map_changed logout login_failed chat whisper party_chat party_invite emotion`
  - ตัวจับเวลาไล่ตี (`aibot_chase_timer`, 250ms): บอทไม่มีไคลเอนต์คอยสั่งตีซ้ำ ฝั่งเซิร์ฟเลยทำแทน
  - `iteminfo` ส่ง `usable` = เช็คเลเวล / เพศ / ชั้นอาชีพ (`pc_charm_usable`) + เช็คอาชีพจริง (`bot_job_can_use` ลอกมาจาก `pc_job_can_use_item` ที่เป็น static)
- **Hook ในไฟล์เดิม:**

| ไฟล์ | ทำอะไร |
|---|---|
| `pc.hpp` | flag `state.aibot` |
| `pc.cpp` | `pc_setpos` → โหลดแมพให้บอทเสร็จเอง |
| `clif.cpp` | ส่งแชท/ซิบ/อีโมทต่อให้สมองกล, ข้าม NPC event ตอนล็อกอินสำหรับบอท |
| `unit.cpp` | เซิร์ฟเดินไล่เป้าหมายแทนไคลเอนต์ |
| `party.cpp` | ให้บอทรับคำเชิญได้ และส่งแชทปาร์ตี้ต่อ |
| `chrif.cpp` | เอาบอทออกเมื่อบัญชีถูกเตะ |
| `map.cpp` | เรียกเริ่มต้น/ปิดโมดูล |

- **Build files:** `CMakeLists.txt`, `Makefile.in` และ `.vcxproj` เพิ่ม `aibot.cpp` + path nlohmann/json
- **NPC:**
  - `npc/Npc RoX/daynight.txt`: @day 07:00 / @night 18:00 ตามนาฬิกาเครื่อง ตั้งค่าได้ผ่านตาราง `rox_daynight` และ `rox_daynight_state`
  - `ai_menu.txt`: ระบบ `@ai`
  - `charm_system.txt`: แก้เฉพาะ `.rate = 20` ยังเป็น 8 สาย
  - ลงทะเบียนทั้งหมดใน `npc/scripts_custom.conf`

### 3.2 สมองกล (`tools/aibot/`, Python stdlib ล้วน)

| ไฟล์ | หน้าที่ |
|---|---|
| `run_brain.py` | จุดเริ่ม (`-c config.json`) |
| `aibot/bridge.py` | client TCP + จำกัดคำสั่งต่อวินาที + วัด latency |
| `aibot/brain.py` | คุมบอททุกตัว ล็อกอิน กระจาย event และรายงาน |
| `aibot/agent.py` | ลูปตัดสินใจของบอท: ฟาร์ม, ตี, เก็บของ, ขาย, พัก, สังสรรค์, หลบมุม, ทักคน, ตอบอีโมท |
| `aibot/career.py` | เปลี่ยนอาชีพ (Job Lv 10 / 40) และเลือกแมพจากสถิติ |
| `aibot/gear.py` | ซื้อยาตามเลเวลและซื้ออุปกรณ์จากรายการร้าน NPC (งบไม่เกินครึ่งของเงิน สูงสุด 4 ชิ้นต่อรอบ) |
| `aibot/party_ai.py` | เดินตามเพื่อน ช่วยตี ฮีล บัฟ ชุบชีวิต (`party_with_bots` = บอทเดินตามหัวหน้าที่เป็นบอทได้) |
| `aibot/memory.py` | ความจำถาวร `memory/<ชื่อ>.json`: สถิติแมพ, ผู้เล่นที่รู้จัก, บันทึก |
| `aibot/personality.py` | นิสัย hardcore / merchant / chill + ประโยคภาษาไทย |
| `aibot/routine.py` | ตารางเวลาประจำวัน + `time_scale` สำหรับเร่งเวลา |
| `aibot/social.py`, `aibot/llm.py` | แชท: ประโยคสำเร็จรูป / Gemini / Groq / Claude / OpenAI-compatible |
| `aibot/world.py` | ข้อมูลเกม: แมพฟาร์มตามเลเวล, อาชีพ 0–18 (สกิล/บิลด์/บัฟ), เส้นทางเปลี่ยนอาชีพ, รายการร้าน 120 ชิ้น, ระดับยา |
| `aibot/mapcache.py` | อ่าน `db/map_cache.dat` เพื่อหาช่องที่เดินได้ |
| `manage.py` | ซิงค์รายชื่อบอทใน `config.json` กับ `bots.sql` (RO_Manager เรียกใช้) |
| `create_bots.py` | สร้าง SQL บัญชีและตัวละครบอท |
| `loadtest.py` | load test |
| `test_llm.py` | ทดสอบ API key |

### 3.3 RO_Manager (`tools/RO_Manager_src/`)

- แกะจาก exe ของผู้ใช้ด้วย ILSpy (source เดิมของผู้ใช้อยู่ที่ `Tools\RO_Manager_src\` ในเครื่องเขา **ไม่มีของใหม่**)
- **คอมไพล์:** `cd tools/RO_Manager_src && dotnet build -c Release -o out` ได้ net45 x86
- **ไฟล์ใหม่:**
  - `BotTab.cs`: เริ่ม/หยุดบอท, เริ่มอัตโนมัติ, นำเข้า bots.sql, สร้างบอทใหม่, รายชื่อ/ลบบอท
  - `TimeTab.cs`: นาฬิกา PC เทียบกับ Map server, ตั้ง @day/@night, ปุ่มสั่งทันที
  - `AnonTypes.cs`
- **ข้อควรรู้เวลาแก้:**
  - `MainForm.cs` เป็นโค้ดที่ ILSpy แกะออกมา (async กลายเป็น struct) อย่าเขียนใหม่ทั้งไฟล์ ให้แก้เป็นจุดเล็กๆ
  - รูปพื้นหลังต้องใส่ `WithCulture="false"` (ชื่อไฟล์ `.bg.jpg` จะถูกเข้าใจว่าเป็นภาษาบัลแกเรีย)
  - `Db.Query` อ่านชื่อคอลัมน์เป็น ANSI → **ห้ามตั้งชื่อคอลัมน์ภาษาไทยใน SQL** ให้ตั้งชื่อในโค้ด C# ทีหลัง
  - คอลัมน์ตัวเลขใน DataTable เป็นชนิด Int64 จะเขียนข้อความทับไม่ได้ ต้องสร้างคอลัมน์ใหม่

---

## 4. วิธีคอมไพล์ / ทดสอบ (สำหรับ Claude บนคลาวด์)

```bash
# เตรียม (ครั้งแรกของ session)
apt-get install -y default-libmysqlclient-dev mariadb-server g++-mingw-w64-i686 rar
mkdir -p src/custom && cp ai_system/src/custom/* src/custom/
for f in atcommand.inc atcommand_def.inc battle_config_init.inc battle_config_struct.inc defines_post.hpp defines_pre.hpp; do
  [ -f src/custom/$f ] || echo "// stub" > src/custom/$f; done

# Windows exe (ส่งให้ผู้ใช้)
SRV=map tools/mingw/build.sh          # -> build-mingw/map-server.exe (ส่วนใหญ่ต้องการแค่ตัวนี้)

# Linux build สำหรับทดสอบ
mkdir -p /tmp/build && cd /tmp/build && cmake <repo> && make -j4 map-server login-server char-server
```

- **ทดสอบ exe Windows จริง:** ใช้ Wine
  - exe ต้องวางไว้ที่ root ของ Server เพราะ rAthena บน Windows จะเปลี่ยน working dir ไปที่โฟลเดอร์ของ exe เอง
  - ใช้คู่กับ `charm_update/login-server.exe` และ `charm_update/char-server.exe` (ตัวเดิมของผู้ใช้) ได้ ทดสอบแล้ว
  - **ห้ามใช้ char-server ของ Linux คู่กับ map-server ของ Windows** จะขึ้น `Data size mismatch`
- **ทดสอบ RO_Manager:** Wine + `wine-mono` + Xvfb
  - ฟอนต์ไทย: เปลี่ยนชื่อ family ของ Garuda เป็น Tahoma
  - Python ของ Windows แบบ embeddable ต้องลบไฟล์ `._pth` ก่อน
- **ระวังตอนใช้ `pkill -f`:** ถ้าข้อความในคำสั่ง shell มีชื่อ process อยู่ด้วย มันจะฆ่า shell ตัวเองไปด้วย ให้ใช้ `pkill -x` หรือ `wineserver -k` แทน

---

## 5. ไฟล์ที่ส่งให้ผู้ใช้แล้ว

| ไฟล์ | เนื้อหา |
|---|---|
| `RoX_AIBot.rar` | ชุดแรก: source + สมองกล + รายงาน |
| `RoX_Build_2026-09-28.rar` | map-server.exe (bridge + @ai + Charm 0.20%) |
| `RoX_Bots.rar` + `start_bots.bat` | บอท 10 ตัว, bat ที่หา Python เองได้ |
| `RoX_Manager_2026-09-28.rar` | RO_Manager ตัวใหม่ + `daynight.txt` + `manage.py` |
| `RoX_Fix_config.rar` / `RoX_Fix_config2.rar` | แก้ปัญหา config.json ภาษาไทย |
| **`RoX_SmartBot_2026-09-28.rar`** | **ล่าสุด:** map-server.exe (มี jobchange/party_info/find/iteminfo) + สมองกลฉลาด |

ผู้ใช้ติดตั้งและรันบอทได้จริงแล้ว (log ขึ้น spawned ครบ, เห็นบอทนั่งในปรอนเทรา) ส่วนชุด SmartBot ยังไม่ได้รับผลตอบกลับ

---

## 6. ผลทดสอบสำคัญ

- **Load test (Linux):** 100 บอท CPU 1.7% / +15 MB, 500 บอท CPU 4.8% / +105 MB, latency p95 < 0.7 ms
- **exe Windows (Wine):** NPC 12,934 ตัวโหลดผ่าน, 100 บอทไม่มี timeout
- **SmartBot:**
  - Swordman → Knight เอง
  - ซื้อ Ring Pommel Saber / Shield / Chain Mail / Boots เอง
  - เดินตามปาร์ตี้ไปถึงดันเจี้ยน
  - เลือกแมพจากสถิติ
- **ยังไม่ได้ทดสอบกับไคลเอนต์จริง:** รับแชท/ซิบจากผู้เล่นจริง, ฮีล/บัฟ/ชุบชีวิตเพื่อน, ทักคนเดินผ่าน, ตอบอีโมท, ปุ่มลบบอทใน RO_Manager

---

## 7. งานค้าง / ไอเดียต่อ

1. **Charm สายใหม่** (โจมบอส / โจมเวทบอส / ดูด HP-SP): รอผู้ใช้ส่ง `db/import/item_db.yml`, `laphine_synthesis.yml`, `item_group_db.yml` (โฟลเดอร์ `db/import` ไม่อยู่ใน git) แล้วค่อยเปิด `charm_system.txt` แบบ 10 สาย (อยู่ใน branch `claude/new-session-qj95nh`)
2. **ผู้ใช้บอกว่า "เดี๋ยวค่อยเพิ่มระบบ run_brain ใน RO_Manager"** ตอนนี้แท็บ AI Bot เริ่ม/หยุดบอทได้แล้ว ต้องถามผู้ใช้ว่าอยากได้อะไรเพิ่ม เช่น ดูความจำ/สถิติแมพของบอท, แก้นิสัยในหน้าจอ, ตั้ง Gemini key จากโปรแกรม
3. บอทที่อยู่ในปาร์ตี้ยังไม่เข้าเมืองขายของ
4. ยังไม่รองรับอาชีพทรานส์ (รีเกิด) และ Bard/Dancer
5. merchant ยังไม่เปิดร้าน Vending จริง
6. ระบบ `@ai`: เพิ่มแมพห้ามใช้ AI, ส่วนลด VIP, ออฟไลน์ฟาร์ม (ดู `ai_system/README.md`)
7. ก่อนเปิดเซิร์ฟออนไลน์: ตั้ง `token` ใน `conf/aibot.conf` และเรื่องความปลอดภัยอื่นๆ ตามเอกสาร `RO_Manager_and_server_setup.md` (อยู่ใน branch `claude/new-session-qj95nh`)

---

## 8. เอกสารอื่นใน repo

- `doc/aibot_protocol.md`: โปรโตคอล bridge
- `tools/aibot/README.md`: คู่มือสมองกลภาษาไทย (ติดตั้ง, Gemini, checklist)
- `tools/mingw/README.md`: คอมไพล์ Win32 ด้วย MinGW
- `tools/RO_Manager_src/README.md`: source ของ RO_Manager
- `ai_system/README.md`: ระบบ `@ai`
