# RO SERVER — สถานะงาน (E:\Project 2\RO_20250716)

อัปเดตล่าสุด: 2026-09-28 — โฟลเดอร์หลัก: Game (client), Server (rAthena), Data Game (ข้อมูลแตก GRF), Tools, Database

## เซิร์ฟ
- rAthena fork (hiphop9/rathena20250614, branch Skylove-ro, commit 0c35263) — PACKETVER 20250716, ENABLE_CASHSHOP_PREVIEW_PATCH, VIP_ENABLE (เปิดอยู่แล้วใน fork)
- เปลี่ยนเป็น Pre-RE แล้ว (`src/config/renewal.hpp` เปิด `#define PRERE`)
- exe 4 ตัวคอมไพล์ด้วย MinGW i686 (static, ใช้ libmysql/pcre8/zlib.dll เดิม) — ตัว Renewal เดิมสำรองที่ `Server\backup_renewal\`; เครื่องผู้ใช้ไม่มีเครื่องมือคอมไพล์ → งานใหม่เลี่ยงการแก้ source (ใช้ db/import + NPC)
- MySQL = USBWebserver ใน `Database\` (port 3307, root/usbw, phpMyAdmin http://127.0.0.1:8080/phpmyadmin/), codepage tis620; Apache 8080 + PHP 5.4.17 (mysqli), docroot `Database\root`
- แก้ Apache crash (ERR_CONNECTION_RESET, child exit 0xC0000005): เครื่องผู้ใช้มี PHP 5.6 อื่นที่ Windows หาเจอก่อน → ก๊อป `Database\php\php5ts.dll` (5.4.17) ไปไว้ `Database\apache2\bin\` (ลบออกได้ถ้ามีปัญหา)
- ช่องกระเป๋า: base 100 + expansion 100 (สูงสุด 200) — ขยายผ่าน `char.inventory_slots` ไม่ต้องคอมไพล์
- ตอนนี้รันแบบ local เท่านั้น (127.0.0.1)
- NPC ที่ Claude ทำ อยู่โฟลเดอร์ `npc/Npc RoX/` (โหลดใน npc/scripts_custom.conf ส่วน "===== Npc RoX =====", ไฟล์ TIS-620) — ตอนนี้ตั้งไว้ที่ prontera ผู้ใช้จะย้ายแมพเองทีหลัง; เพิ่ม NPC ใหม่ต้องรีสตาร์ท Map server

## ระบบ Charm (ต้นแบบจากเซิร์ฟ Hiclass: D:\Ragnarok Server\RO ServerHiclass Juti800.2025) — ทดสอบผ่านครบ
- `Type: Charm` (IT_CHARM = 13) ได้โบนัสจาก Script เมื่ออยู่ในกระเป๋า, ซ้อนในช่องเดียวได้ (stackable), ไคลเอนต์เห็นเป็น Etc
- ฟิลด์ YAML ใหม่: `CharmGroup` (สาย) / `CharmLevel` (ระดับ) — สายเดียวกันให้ผลเฉพาะ N ชิ้นที่ดีที่สุด (`charm_max_per_group: 3` ใน conf/battle/items.conf); ไม่มี CharmGroup = ID ละครั้ง
- เช็กเลเวล/เพศ/อาชีพ (pc_charm_usable), recalc ตอน additem/delitem, และ recalc เมื่อสถานะ VIP เปลี่ยน (chrif_parse_ack_vipActive)
- ไฟล์ที่แก้: common/mmo.hpp, map/script_constants.hpp, map/itemdb.cpp/.hpp, map/clif.cpp, map/status.cpp, map/pc.cpp/.hpp, map/battle.cpp/.hpp, map/chrif.cpp
- ไอเท็ม (db/import/item_db.yml, 84 รายการ): 60000–60002 HotWeek Charm 01–03 (ไม่มีสาย) + Charm สเตตัส 8 สาย × +1..+10 = ID 60100–60179 (Str 60100-09, Agi 60110-19, Vit 60120-29, Int 60130-39, Luk 60140-49, MaxHP% 60150-59, MaxSP% 60160-69, DropRate% 60170-79, Trade NoDrop) + 60200 Charm VIP
- `npc/Npc RoX/charm_system.txt`: ดรอป 0.05% ต่อการฆ่า สุ่ม 1 สาย ระดับ +1 เข้ากระเป๋าทันที (OnNPCKillEvent) + NPC อัพเกรด (ดูหัวข้อ Charm Upgrade) + ร้าน Forge Scroll
- Charm VIP (60200): ALL Stats +10 เฉพาะตอนไอดีเป็น VIP (`vip_status`), rental 30 วัน (ไม่เกิน VIP ที่เหลือ), ทิ้ง/เทรด/ขาย/ฝากคลัง/เมลไม่ได้; `npc/Npc RoX/charm_vip.txt` NPC "Charm VIP" prontera 153,193 แจกตัวละครละ 1 ชิ้นต่อรอบ VIP (ตัวแปร VIPCharmClaim) — ไอดี GM ระบบไม่นับเป็น VIP
- ไฟล์เก่า npc/custom/charm_system.txt เหลือแค่หมายเหตุว่าย้ายแล้ว (ลบได้)
- exe ที่มี Charm ติดตั้งแล้ว — exe ก่อน Charm สำรอง `Server\backup_before_charm\`
- Hiclass: item 61540 Charm_01 โบนัสอยู่ใน CollectionScript (ระบบ Collection แยก)

## ไคลเอนต์ (Game\) — อัปเดต 2026-09-28
- exe ที่ใช้: `20250604_rox.exe` (20250604 unpacked by Skylove, patch ด้วย WARP0716 รันผ่าน wine: DisableDoram, NoAdvAgencyInParty, NoEquipSwap, HideTraitStatusButton, ShowExpNumbers, Zoom50Percent, HideLicense) — NEMO 4144 ใช้กับ client นี้ไม่ได้; client 0604/0716 packet เหมือนกัน (PACKETVER max threshold 20250402)
- ยังไม่มี patch DataFolderFirst → client ไม่อ่านไฟล์ในโฟลเดอร์ `Game\data\` (ต้องแพ็กเข้า GRF)
- **DATA.INI**: `0=rox_item2.grf  1=rox_fix2.grf  2=private.grf  3=data.grf` (ตัวเลขน้อย = สำคัญกว่า); `DATA.INI.bak` = ของเดิม 3 บรรทัด
- `rox_item2.grf`: ไอคอน/collection/สไปรต์ดรอปของ Charm (rox_charm_<kind>_<tier>, hotweek_1-3, vip) + Forge Scroll (rox_forge_scroll_<kind>) + `data\luafiles514\lua files\datainfo\lapineddukddakbox.lub` (text, เพิ่มสูตร Ro-X Forge) — `rox_item.grf` เก่าไม่ใช้แล้ว
- `rox_fix2.grf` (สร้างด้วย Python GRF 0x200 writer ใน cloud): `data\msgstringtable.csv` (ไทยจาก thRO), `skillinfoz\skilldescript.lub` (ไทยจาก thRO), ไฟล์หมวกงู 5388 (8 ไฟล์ — ทดสอบผ่าน) — `rox_fix.grf` เก่าลบได้
- client นี้อ่าน `data\msgstringtable.csv` (base64(key),base64(UTF-8 value)) ไม่ใช่ .txt; ไม่ใช้ questid2display แล้ว (ใช้ System\OngoingQuestInfoList_True.lub)
- client โหลด `System\itemInfo_true.lub` ตรงๆ (ไม่ผ่าน SystemEN)

### itemInfo_true.lub (Game\System)
- ตอนนี้เป็นไฟล์ compiled (luac 5.1 32-bit, 9.5MB) เพราะ text 24MB เกินขีดส่งไฟล์; ซอร์ส text อยู่ใน `System\itemInfo_source.zip` (`itemInfo_true_source.lub` = ตัวปัจจุบัน, `itemInfo_true_before_thro.lub` = ก่อนใช้ thRO) — จะแก้ให้แก้ซอร์สแล้วให้ Claude compile
- รวม 28,030 ไอเทม: แทนชื่อ+คำอธิบายด้วย thRO (`E:\Project 2\ThaiRo\System\itemInfo_new.lub`, UTF-8 → cp874) 14,970 ตัว + เพิ่มไอเทมเฉพาะ thRO 3,477 ตัว; คง resource name / slot / ClassNum ของเรา; ตัดอักษรเกาหลี 1,748 ตัว, ― → -, ※ → *
- `main()` เพิ่มบรรทัด "รหัสไอเทม: <ID>" ท้ายคำอธิบายทุกไอเทม (ทดสอบผ่าน)
- บล็อก Ro-X (BEGIN/END) ต่อท้ายหลัง main_server: ClassNum fix (5412→414, 5054→180), Charm 84 ตัว, Forge Scroll 72 ตัว (ไทย)
- สำรอง: `itemInfo_true.lub.bak` (ต้นฉบับ EN), `.en.bak` (EN + Ro-X)
- ไอเทมที่เหลือ 9,583 ตัว (ไม่มีใน thRO) ผู้ใช้จะจัดการเอง; thRO เป็น Renewal ตัวเลขบางชิ้นอาจไม่ตรง Pre-RE

### คำแปลไทยจาก thRO (ทำแล้ว 2026-09-28)
- MsgStringTable: ไทย 2,732/4,238 (เรียงตามลำดับ ID ตรงกับ msgstringtable.txt ของ thRO, คงอังกฤษ 46 บรรทัดที่ %d/%s ไม่ตรง) — **ยังไม่ได้ยืนยันว่าแสดงไทยถูก (UTF-8) ถ้าเพี้ยนให้เปลี่ยนเป็น cp874**
- คำอธิบายสกิล: ไทย 1,270/1,469 — ทดสอบผ่าน (ชื่อสกิลยังอังกฤษ; ตัดบรรทัดกลางคำไทย = ข้อจำกัด client)
- System\OngoingQuestInfoList_True.lub เควส 6,756/10,999, achievement_list.lub 357/362, RecommendedQuestInfoList_True.lub 1/15 — สำรองเป็น `.en.bak`
- ไฟล์ thRO อื่นที่ยังใช้ได้: mapnametable.txt, stateiconinfo.lub, navi_*_th.lub, jobname.lub, emotionlist.lub, pettalktable.xml, cardprefix/postfix, spopup.lub, mapInfo.lub

### ไฟล์รูปไอเทมที่หาย
- `Data Game\datawg` (WARPGATE) + `Data Game\thairo data` แตกไว้แล้ว; filelist: `filelist_datawg.txt`, `filelist_thairo_data.txt` (สร้างด้วย make_filelist_new.bat)
- เทียบแล้ว: แก้ได้ครบ 335 ไอเทม (1,219 ไฟล์ ~50MB, ส่วนใหญ่จาก thRO), ยังขาดบางส่วน 284 (ของเซิร์ฟต่างประเทศ เช่น Russia Ribbon/Brazil Hat/Fanta, Costume) , 147 ไอเทมไม่มี itemInfo
- **ค้าง:** ผู้ใช้รัน `Data Game\collect_fix.bat` → ได้ `rox_fix_files.zip` (ไฟล์ชื่อเลข 00000.. ตาม `rox_fix_list.txt`) → Claude แพ็กเข้า rox_fix2.grf (ต้องคง msgstringtable/skilldescript/หมวกงูไว้)
- `Data Game\rox_item_checklist.txt` = รายการ ID ให้ผู้ใช้ @item เช็กทีละชิ้น

## Charm Upgrade (แทนระบบ 3→1 เดิม) — ทดสอบผ่าน
- NPC "ช่างอัพเกรด Charm" prontera 150,193 เปิดหน้าต่าง Laphine ด้วย `laphine_synthesis <scroll id>;` (เลือก Charm → ถ้าไม่มี scroll ให้ซื้อ)
- Forge Scroll 72 ตัว ID 60300–60379 (id = 60300 + สาย×10 + (lv-1)), Etc, Weight 1 — ใน db/import/item_db.yml
- สูตรใน db/import/laphine_synthesis.yml + item_group_db.yml (ใช้ชื่อ IG_RT_* ที่ไม่ได้ใช้ 72 ชื่อ เพื่อไม่ต้องคอมไพล์) — ต้องการ Charm ×(3,3,3,2×6) + scroll ×1; สำเร็จ 100/90/80/70/60/50/40/30/25%; ล้มเหลวคืน Charm 1 ชิ้น
- NPC "ร้าน Forge Scroll" prontera 147,193 ขายราคา 5k,10k,25k,50k,100k,200k,500k,1M,2M (ซื้อ 1–100)
- HotWeek ใช้ `bonus2 bAddClass,Class_All,N;` (SP_ATK_RATE ใช้ไม่ได้ใน pre-re)
- itemdb.hpp / script_constants.hpp กลับเป็นของเดิมแล้ว (มี .bak)

## ปรับ EXP/Drop แบบ realtime — ทดสอบผ่าน
- `npc/Npc RoX/rate_control.txt` (TIS-620, โหลดใน scripts_custom.conf): ตาราง rox_rate / rox_rate_state, เช็กทุก 3 วิ, setbattleflag base/job exp + item_rate_* ×mult/100 แล้ว reload, ประกาศทั้งเซิร์ฟ, หมดเวลาคืนค่าเอง, รีสตาร์ทแล้วทำต่อ
- RO_Manager แท็บ "อัตรา EXP / Drop" (RateTab.cs) — ผู้ใช้เปลี่ยนเป็นตัวใหม่แล้ว; `RO_Manager_old.exe` = ตัวก่อนเพิ่มแท็บ

## งานค้าง (อัปเดต 2026-09-28)
> **พักไว้ก่อน (ผู้ใช้สั่ง 2026-09-28):** งาน GRF / ไคลเอนต์ทั้งหมด (ข้อ 1, 2, 4, 5, 6) — จะทำผ่าน chat ภายหลัง; session cloud ทำเฉพาะงานฝั่ง Server
> **กติกา:** ทำงานเสร็จทุกครั้งให้อัปเดตไฟล์นี้ (`RO_Manager_and_server_setup.md` ที่ root ของ repo) แล้ว commit ไปพร้อมกัน

1. รัน collect_fix.bat → แพ็กรูป 335 ไอเทมเข้า rox_fix2.grf → ผู้ใช้เช็กตาม checklist
2. ยืนยัน MsgStringTable ไทยแสดงถูก (UTF-8 vs cp874)
3. อัตราดรอป Charm (0.05% น่าจะต่ำไป) — รอตัดสินใจ
4. ตัดคำไทยในคำอธิบาย (เว้นวรรคระหว่างคำ) — เสนอไว้ ยังไม่สั่ง
5. ไฟล์ thRO อื่น (mapname, stateicon, navi ฯลฯ), ไอเทม thRO 6 ตัวที่ server มีแต่ client ไม่มี
6. แยก GRF ตาม split_map.tsv (run_split.bat ยังไม่รัน), พื้นหลัง Laphine ธีม Ro-X, patch exe เพิ่ม (~14 อย่าง รวม DataFolderFirst)
7. ลบไฟล์ไม่ใช้: rox_item.grf, rox_fix.grf, System\itemInfo_rox.lua, SystemEN\itemInfo.lua, Game\data\MsgStringTable.csv (ทดสอบ), .bak ต่างๆ
- เครื่องมือใน cloud (หายเมื่อ session ใหม่): lua32/luac32 (Lua 5.1 32-bit), GRF writer — สร้างใหม่ได้จาก lua5.1 source + `gcc -m32`

## RO_Manager.exe (Server\RO_Manager.exe) — ทดสอบผ่านครบ
- C# WinForms .NET 4.x x86, ต่อ DB ผ่าน libmysql.dll (P/Invoke), ซอร์สที่ `Tools\RO_Manager_src\` (build.txt มีคำสั่ง, build ด้วย mono mcs ใน cloud ได้)
- แท็บ: เซิร์ฟเวอร์ (start/stop แบบ Ctrl+C เซฟข้อมูล, log สด, auto-restart), ผู้เล่นออนไลน์, จัดการไอดี (แก้ในตารางได้ ✎), ค้นหาตัวละคร (แก้ในตาราง, ของในตัว/รถเข็น/คลัง), ค้นหาไอเท็ม, ส่งเมล/แจกของ (RODEX), อัตรา EXP / Drop (ดูหัวข้อด้านล่าง)
- ธีมมืด (ค่าเริ่มต้น) + แบนเนอร์/พื้นหลัง Ro-X ฝังใน exe, ไอคอน Ro-X
- ตั้งค่าใน `RO_Manager.ini`, log การส่งเมล `RO_Manager_mail.log`

## Setup.exe (Game) แปลไทย
- แปล dialog ตัวเลือก/ระบบ (ฟอนต์ Tahoma), ปุ่ม ตกลง/ยกเลิก/รีเซ็ต (TIS-620), `System\LuaFiles514\MsgString.lub` แปลไทย
- ต้นฉบับ: `Game\Setup_original_kr.exe`, `MsgString_original_kr.lub`

## หน้าเว็บ Ro-X (ดีไซน์) — ผู้ใช้บอก "ไว้แค่นี้ก่อน"
- Design artifact "Ro-X Website" (https://claude.ai/artifact/T4UgrkDBfDdAKWmrfkiNcH) มี 2 บอร์ด: Main.dc.html (Desktop 1440) และ Mobile.dc.html (390)
- โทน: ครีม #FBF6EF, ชมพูซากุระ #F8ECEE/#E7A1B3/#A8445E, ทอง #E0B45A/#F3D48A, น้ำเงินเข้ม #1F2847/#33508C; ฟอนต์ Cinzel + Noto Serif Thai + IBM Plex Sans Thai
- ช่องที่ต้องเติม: [EXP] [Drop] [สถานะ] [จำนวน] [HH:MM] [ขนาด] [วันที่] [หัวข้อกิจกรรม] ลิงก์ Discord/Facebook

## เว็บใช้งานจริง (Database\root) — เมนู ดาวน์โหลด / เติมเงิน / สมัครไอดี (`rox_nav()` ใน config.php)
- ไฟล์ร่วม: `rox\config.php`, `rox\style.css`, `rox\hero.jpg`, `rox\hero_m.jpg`; PHP 5.4-compatible, mysqli prepared statements; timezone Asia/Bangkok
- config.php อ่านค่า DB, default_codepage, use_MD5_passwords จาก `Server\conf\login_athena.conf` (+ import inter_athena) เอง
- **ผู้ใช้แก้ config.php เองแล้ว (admin_pass) — ก่อน commit ไฟล์นี้ต้อง stage ของในเครื่องมา merge ทุกครั้ง**
- register.php: ตรวจไอดี/รหัส/อีเมล/เพศ, captcha บวกเลข, CSRF, จำกัด 3 ไอดี/IP/24 ชม. (ตาราง rox_web_register)
- download.php: ไฟล์ใน `Database\root\files\` ขึ้นปุ่มเอง; config `mirrors`, `runtime_url`, `client_version`, `client_size`, `client_exe`
- เติมเงิน topup.php: (1) โอน/พร้อมเพย์ + สลิป + GM อนุมัติ (ทดสอบผ่าน) (2) ซองอั่งเปา TrueMoney อัตโนมัติ (ทดสอบกับ mock ผ่าน, ยังไม่ทดสอบของจริง) — ตาราง rox_topup / rox_voucher; admin_topup.php (local only); VIP สะสมทุก 300 บาท = 30 วัน; ส่งเข้าเกมด้วย `npc/Npc RoX/topup_delivery.txt` (#CASHPOINTS, vip_time)
- ผู้ใช้ต้องใส่: tmn_phone, บัญชีธนาคาร/พร้อมเพย์, qr.png แล้วทดสอบซองจริง

## ก่อนเปิดออนไลน์ (ผู้ใช้จะให้ทำตอนนั้น — ยังไม่ต้องทำ)
1. phpMyAdmin/Apache 8080: บล็อกเฉพาะ phpMyAdmin + เปลี่ยนรหัส MySQL root (usbw) หรือสร้าง user แยกสำหรับเว็บ
2. MySQL 3307: bind 127.0.0.1, เปลี่ยนรหัส, แก้ inter_athena.conf ให้ตรง
3. เปลี่ยน inter-server account s1/p1 (char_athena, map_athena, ตาราง login)
4. ไอดี test1–test9 (Admin 99) ลบ/ลดสิทธิ์
5. use_MD5_passwords: yes (หน้าสมัคร/เติมเงินรองรับแล้ว)
6. เปิด port เฉพาะ 6900/6121/5121 (+8080 เว็บ) ห้ามเปิด 3307; แก้ char_ip/map_ip เป็น IP จริง
7. ตัวเลือกเสริม: packet keys เฉพาะเซิร์ฟ, client_hash_check, ddos_* ใน packet_athena.conf
8. ปุ่ม "สำรองฐานข้อมูล" / "ตรวจความปลอดภัย" / "ตั้งช่องกระเป๋า" ใน RO_Manager
9. หน้า GM เติมเงิน: คง admin_local_only = true; เว็บออนไลน์ควรมี HTTPS
10. ลบรายการทดสอบใน rox_topup / rox_voucher และรูปใน Database\slips\
11. ตรวจ/แปลง NPC แปลไทยอื่นๆ (UTF-8 → TIS-620), ย้าย NPC Ro-X ไปแมพที่ต้องการ
