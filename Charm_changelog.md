# ระบบ Charm — บันทึกการแก้ไขทั้งหมด

อัปเดตล่าสุด: 2026-09-28
ต้นแบบจากเซิร์ฟ Hiclass (`D:\Ragnarok Server\RO ServerHiclass Juti800.2025`)

---

## 1. ภาพรวม

- Charm = ไอเทมที่**ให้โบนัสแค่อยู่ในกระเป๋า** ไม่ต้องสวมใส่
- ซ้อนในช่องเดียวได้ (stackable), ไคลเอนต์เห็นเป็น Etc
- แบ่งเป็น "สาย" (CharmGroup) + "ระดับ" (CharmLevel) — สายเดียวกันให้ผลเฉพาะ **3 ชิ้นที่ดีที่สุด**
- ได้จาก: ดรอปมอน (+1) → ตีบวกขึ้นระดับด้วย Forge Scroll (สูงสุด +10)

---

## 2. แก้ source (คอมไพล์แล้ว — ทดสอบผ่าน)

| ไฟล์ | แก้อะไร |
|---|---|
| `src/common/mmo.hpp` | เพิ่ม item type `IT_CHARM = 13` |
| `src/map/itemdb.hpp` | เพิ่มฟิลด์ `charm_group`, `charm_level` ใน item_data |
| `src/map/itemdb.cpp` | อ่านฟิลด์ YAML ใหม่ `CharmGroup`, `CharmLevel`; ชื่อ type "Charm" |
| `src/map/clif.cpp` | ส่งให้ไคลเอนต์เห็น Charm เป็น `IT_ETC` (ไคลเอนต์ไม่รู้จัก type 13) |
| `src/map/status.cpp` | ตอนคำนวณสเตตัส: รันสคริปต์โบนัสของ Charm ในกระเป๋า — ไม่มีสาย = ID ละครั้ง / มีสาย = เรียงระดับสูงสุด เอาแค่ N ชิ้นแรก |
| `src/map/pc.cpp` / `pc.hpp` | `pc_charm_usable()` เช็กเลเวล/เพศ/อาชีพ; คำนวณสเตตัสใหม่ตอนได้/เสีย Charm (additem/delitem) |
| `src/map/battle.cpp` / `battle.hpp` | ค่า config `charm_max_per_group` (ค่าเริ่ม 3, 1–100) |
| `src/map/chrif.cpp` | คำนวณสเตตัสใหม่เมื่อสถานะ VIP เปลี่ยนระหว่างเล่น (สำหรับ Charm VIP) |
| `conf/battle/items.conf` | `charm_max_per_group: 3` |

- exe ก่อนมี Charm สำรองที่ `Server\backup_before_charm\`
- `itemdb.hpp.bak` / `script_constants.hpp.bak` = ไฟล์สำรอง (ลบได้)

---

## 3. ไอเทม (`db/import/item_db.yml`)

| ID | ไอเทม | โบนัส |
|---|---|---|
| 60000–60002 | HotWeek Charm 01–03 (ไม่มีสาย) | `bonus2 bAddClass,Class_All,N` (pre-re ใช้ bAtkRate ไม่ได้) |
| 60100–60109 | Charm Str +1..+10 | Str |
| 60110–60119 | Charm Agi +1..+10 | Agi |
| 60120–60129 | Charm Vit +1..+10 | Vit |
| 60130–60139 | Charm Int +1..+10 | Int |
| 60140–60149 | Charm Luk +1..+10 | Luk |
| 60150–60159 | Charm MaxHP% +1..+10 | MaxHP % |
| 60160–60169 | Charm MaxSP% +1..+10 | MaxSP % |
| 60170–60179 | Charm DropRate% +1..+10 | อัตราดรอป % |
| 60200 | Charm VIP | ALL Stats +10 เฉพาะตอนไอดีเป็น VIP |
| 60300–60379 | Forge Scroll (8 สาย × 9 ระดับ) | ใช้ตีบวก, Etc, Weight 1 |

- Charm สาย: Trade NoDrop
- สูตร ID: Charm = `60100 + สาย×10 + (ระดับ-1)`, Scroll = `60300 + สาย×10 + (ระดับ-1)`
- ลำดับสาย: 0 Str, 1 Agi, 2 Vit, 3 Int, 4 Luk, 5 MaxHP, 6 MaxSP, 7 Drop

---

## 4. NPC (`npc/Npc RoX/`, ไฟล์ TIS-620, ตอนนี้อยู่ prontera)

### `charm_system.txt`
- **ดรอป (CharmDrop):** ฆ่ามอนทุกตัวมีโอกาส **0.20%** (เดิม 0.05%, เปลี่ยน 2026-09-28) ได้ Charm +1 สุ่ม 1 สาย เข้ากระเป๋าทันที — ค่า `.rate = 20` (ต่อ 10000)
- **ช่างอัพเกรด Charm** (prontera 150,193): เลือก Charm → ถ้าไม่มี Scroll ให้ซื้อ → เปิดหน้าต่าง Laphine (`laphine_synthesis`) → ใส่ Charm + Scroll แล้วกด Make
- **ร้าน Forge Scroll** (prontera 147,193): ขาย Scroll ตามสาย/ระดับ ซื้อได้ 1–100 ใบ

### `charm_vip.txt`
- NPC "Charm VIP" (prontera 153,193) แจก Charm VIP ตัวละครละ 1 ชิ้นต่อรอบ VIP (ตัวแปร `VIPCharmClaim`)
- rental 30 วัน (ไม่เกิน VIP ที่เหลือ), ทิ้ง/เทรด/ขาย/ฝากคลัง/ส่งเมลไม่ได้
- ไอดี GM ระบบไม่นับเป็น VIP

ไฟล์เก่า `npc/custom/charm_system.txt` เหลือแค่หมายเหตุว่าย้ายแล้ว (ลบได้)

---

## 5. ตีบวก Charm (Laphine Synthesis — ทดสอบผ่าน)

| ระดับ | ใช้ Charm | สำเร็จ | ราคา Scroll |
|---|---|---|---|
| +1 → +2 | 3 | 100% | 5,000 |
| +2 → +3 | 3 | 90% | 10,000 |
| +3 → +4 | 3 | 80% | 25,000 |
| +4 → +5 | 2 | 70% | 50,000 |
| +5 → +6 | 2 | 60% | 100,000 |
| +6 → +7 | 2 | 50% | 200,000 |
| +7 → +8 | 2 | 40% | 500,000 |
| +8 → +9 | 2 | 30% | 1,000,000 |
| +9 → +10 | 2 | 25% | 2,000,000 |

- ใช้ Scroll 1 ใบต่อครั้ง (ใช้ตอนกด Make เท่านั้น)
- ล้มเหลว: ได้ Charm ระดับเดิมคืน 1 ชิ้น
- สูตรอยู่ `db/import/laphine_synthesis.yml`, % สำเร็จอยู่ `db/import/item_group_db.yml` (ใช้ชื่อ `IG_RT_*` ที่ pre-re ไม่ได้ใช้ 72 ชื่อ เพื่อไม่ต้องคอมไพล์)
- **แก้ % ต้องแก้ใน item_group_db.yml** (ตัวเลขใน NPC เป็นแค่ตัวแสดงผล)

---

## 6. ฝั่งไคลเอนต์ (ทำแล้ว)

- `System\itemInfo_true.lub` บล็อก Ro-X: ชื่อ/คำอธิบายไทย Charm 84 ตัว + Forge Scroll 72 ตัว
- `rox_item2.grf`: ไอคอน/collection/สไปรต์ดรอป Charm (`rox_charm_<kind>_<tier>`, hotweek_1-3, vip) + Forge Scroll + `lapineddukddakbox.lub` (สูตร Ro-X Forge)

---

## 7. กำลังทำ — Charm สายบอส + ดูด HP/SP (เริ่ม 2026-09-28)

| ID | ไอเทม | โบนัส |
|---|---|---|
| 60180–60188 | Charm โจมบอส +1..+9 | `bonus2 bAddRace,RC_Boss,1..9` |
| 60189 | Charm โจมบอส +10 | บอส +10% + ATK 3% (`bonus2 bAddClass,Class_All,3`) |
| 60190–60198 | Charm โจมเวทบอส +1..+9 | `bonus2 bMagicAddRace,RC_Boss,1..9` |
| 60199 | Charm โจมเวทบอส +10 | บอส +10% + `bonus bMatkRate,3` |
| 60210 | Charm ดูด HP (lv10, ไม่มีสาย) | โอกาส 10% ดูด 10% + MaxHP 2% + MaxSP 2% |
| 60211 | Charm ดูด SP (lv10, ไม่มีสาย) | โอกาส 10% ดูด 10% + MaxHP 2% + MaxSP 2% |
| 60380–60399 | Forge Scroll โจมบอส / โจมเวทบอส | |

- ✅ แก้ `charm_system.txt` แล้ว: ดรอปสุ่ม 10 สาย, ช่างอัพเกรด + ร้าน Scroll รองรับสาย "โจมบอส", "โจมเวทบอส"
- ⏳ **รอผู้ใช้ส่ง** `db/import/item_db.yml`, `laphine_synthesis.yml`, `item_group_db.yml` (โฟลเดอร์ db/import ไม่อยู่ใน git) เพื่อเพิ่มไอเทม + สูตร + IG_RT_* ที่ยังว่างอีก 18 ชื่อ
- ⚠ **ห้ามเอา charm_system.txt ตัวใหม่ไปใช้**จนกว่าจะเพิ่มไอเทมเสร็จ (ดรอปโดนสายใหม่จะ error)
- ⏳ ยังไม่กำหนดที่มาของ Charm ดูด HP/SP (ตอนนี้ได้จาก `@item` เท่านั้น)
- ⏳ ไคลเอนต์: itemInfo + ไอคอนของ Charm ใหม่ (ทำผ่าน chat ภายหลัง)

---

## 8. ประวัติการแก้ไข

| วันที่ | แก้อะไร |
|---|---|
| (ก่อนหน้า) | สร้างระบบ Charm ใน source, ไอเทม 84 ตัว, ดรอป 0.05%, Charm VIP |
| (ก่อนหน้า) | เปลี่ยนระบบอัพเกรดจาก 3→1 เป็น Laphine + Forge Scroll 72 ตัว |
| 2026-09-28 | อัตราดรอป 0.05% → **0.20%** |
| 2026-09-28 | เริ่มสายโจมบอส / โจมเวทบอส + Charm ดูด HP/SP (ฝั่ง NPC เสร็จ, รอ db/import) |
