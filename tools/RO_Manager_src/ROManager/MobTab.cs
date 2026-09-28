// Ro-X: "มอนสเตอร์" tab — monsters in a map (live count / spawn / kill), the
// permanent spawn lines in npc/*/mobs, monster.conf settings and boss / MVP
// on a schedule. Live actions go through npc/Npc RoX/mob_control.txt, which
// polls the rox_mobcmd table every 3 seconds.
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ROManager;

internal partial class MainForm
{
	private const string MobCmdSql = "CREATE TABLE IF NOT EXISTS `rox_mobcmd` (`id` INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,`cmd` VARCHAR(16) NOT NULL,`map` VARCHAR(16) NOT NULL DEFAULT '',`mob_id` INT NOT NULL DEFAULT 0,`amount` INT NOT NULL DEFAULT 1,`x` INT NOT NULL DEFAULT 0,`y` INT NOT NULL DEFAULT 0,`flag` VARCHAR(48) NOT NULL DEFAULT '',`value` INT NOT NULL DEFAULT 0,`msg` VARCHAR(200) NOT NULL DEFAULT '',`status` TINYINT NOT NULL DEFAULT 0,`result` VARCHAR(255) NOT NULL DEFAULT '',`created` DATETIME NULL,`done` DATETIME NULL) ENGINE=MyISAM";
	private const string MobCountSql = "CREATE TABLE IF NOT EXISTS `rox_mobcount` (`map` VARCHAR(16) NOT NULL,`mob_id` INT NOT NULL,`name` VARCHAR(48) NOT NULL DEFAULT '',`cnt` INT NOT NULL DEFAULT 0,`updated` DATETIME NULL,PRIMARY KEY (`map`,`mob_id`)) ENGINE=MyISAM";
	private const string MobFlagSql = "CREATE TABLE IF NOT EXISTS `rox_mobflag` (`flag` VARCHAR(48) NOT NULL PRIMARY KEY,`value` INT NOT NULL DEFAULT 0,`updated` DATETIME NULL) ENGINE=MyISAM";
	private const string MobTimerSql = "CREATE TABLE IF NOT EXISTS `rox_mobtimer` (`id` INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,`enabled` TINYINT NOT NULL DEFAULT 1,`map` VARCHAR(16) NOT NULL,`x` INT NOT NULL DEFAULT 0,`y` INT NOT NULL DEFAULT 0,`mob_id` INT NOT NULL,`amount` INT NOT NULL DEFAULT 1,`minute` INT NOT NULL DEFAULT 0,`days` VARCHAR(16) NOT NULL DEFAULT '0123456',`skip_alive` TINYINT NOT NULL DEFAULT 1,`msg` VARCHAR(200) NOT NULL DEFAULT '',`last_run` VARCHAR(16) NOT NULL DEFAULT '') ENGINE=MyISAM";
	private const string CustomSpawnFile = "npc/Npc RoX/mob_spawn_custom.txt";
	private const string OffMark = "//RoXoff ";

	// flag, description, applies without restart
	private static readonly string[][] MobFlags = {
		new[] { "mob_count_rate", "จำนวนมอนที่เกิดในแมพ (%) 200 = มอนเยอะขึ้น 2 เท่า", "0" },
		new[] { "mob_spawn_delay", "เวลาเกิดใหม่ของมอนทั่วไป (%) 50 = เกิดเร็วขึ้นครึ่งหนึ่ง", "1" },
		new[] { "boss_spawn_delay", "เวลาเกิดใหม่ของบอส / MVP (%)", "1" },
		new[] { "plant_spawn_delay", "เวลาเกิดใหม่ของต้นไม้ / เห็ด (%)", "1" },
		new[] { "monster_hp_rate", "HP มอนทั่วไป (%)", "0" },
		new[] { "mvp_hp_rate", "HP บอส / MVP (%)", "0" },
		new[] { "mob_skill_rate", "โอกาสที่มอนใช้สกิล (%) 0 = มอนไม่ใช้สกิล", "0" },
		new[] { "mob_skill_delay", "ดีเลย์สกิลของมอน (%)", "0" },
		new[] { "monster_active_enable", "มอนตีก่อน 1 = ปกติ, 0 = มอนทุกตัวไม่ตีก่อน", "0" },
		new[] { "view_range_rate", "ระยะที่มอนมองเห็นผู้เล่น (%)", "0" },
		new[] { "chase_range_rate", "ระยะที่มอนไล่ตาม (%)", "0" },
		new[] { "show_mob_info", "แสดงบนชื่อมอน 0 = ไม่แสดง, 1 = HP, 2 = HP%, 4 = เลเวล (บวกกันได้ เช่น 3)", "1" },
		new[] { "mvp_tomb_enabled", "ป้ายหลุมศพ MVP 1 = เปิด, 0 = ปิด", "1" },
		new[] { "ksprotection", "กันแย่งมอน (มิลลิวินาที) 0 = ปิด, 5000 = 5 วินาที", "1" },
		new[] { "no_spawn_on_player", "ไม่ให้มอนเกิดใกล้ผู้เล่น 0 = ปิด, 1-99 = ระยะช่อง", "1" },
		new[] { "mob_remove_damaged", "ลบมอนที่โดนตีแล้วในแมพที่ไม่มีคน 1 = ใช่, 0 = ไม่", "1" },
		new[] { "monster_loot_type", "มอนเก็บของ 0 = เก็บจนเต็ม, 1 = เก็บแล้วหยุด", "1" },
	};

	private static readonly Encoding Latin1 = Encoding.GetEncoding(28591); // keeps TIS-620 bytes as they are
	private static readonly string[] ThaiDayShort = { "อา", "จ", "อ", "พ", "พฤ", "ศ", "ส" };
	private static readonly Regex SpawnLine = new Regex(
		"^(?<off>//RoXoff )?(?<map>[^,\\t/ ]+),(?<x>\\d+),(?<y>\\d+)(?<area>,\\d+,\\d+)?\\t(?<type>monster|boss_monster|miniboss_monster)\\t(?<name>[^\\t]+)\\t(?<id>\\d+),(?<amt>\\d+)(?:,(?<d1>\\d+))?(?:,(?<d2>\\d+))?(?<rest>.*)$",
		RegexOptions.Compiled);

	private TabPage mobPage;
	private readonly Dictionary<int, string> mobNames = new Dictionary<int, string>();
	private readonly List<string> mobChoices = new List<string>();
	private readonly SortedSet<string> spawnMaps = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
	private bool mobLoaded;
	private Label lblMobNpc;
	// live
	private ComboBox cmbLiveMap, cmbLiveMob;
	private NumericUpDown numLiveAmt, numLiveX, numLiveY;
	private TextBox txtLiveMsg;
	private DataGridView gridLive;
	private Label lblLiveResult;
	// spawn files
	private ComboBox cmbSpawnMap, cmbSpawnMob;
	private NumericUpDown numSpawnAmt, numSpawnD1, numSpawnD2;
	private DataGridView gridSpawn;
	private DataTable spawnTable;
	// monster.conf
	private DataGridView gridMobConf;
	private DataTable mobConfTable;
	// timers
	private DateTimePicker dtTimer;
	private readonly CheckBox[] chkTimerDays = new CheckBox[7];
	private ComboBox cmbTimerMap, cmbTimerMob;
	private NumericUpDown numTimerAmt, numTimerX, numTimerY;
	private CheckBox chkTimerSkip;
	private TextBox txtTimerMsg;
	private DataGridView gridTimer;

	// ------------------------------------------------------------------
	// UI
	// ------------------------------------------------------------------
	private TabPage BuildMobTab()
	{
		mobPage = new TabPage("มอนสเตอร์");
		TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(4) };
		root.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		lblMobNpc = new Label { Dock = DockStyle.Fill, ForeColor = Theme.Muted, Text = "NPC คุมมอน: ยังไม่ได้เช็ค", TextAlign = ContentAlignment.MiddleLeft };
		root.Controls.Add(lblMobNpc, 0, 0);
		TabControl inner = new ThemedTab { Dock = DockStyle.Fill, SizeMode = TabSizeMode.Fixed, ItemSize = new Size(200, 26) };
		inner.TabPages.Add(BuildMobLivePage());
		inner.TabPages.Add(BuildMobSpawnPage());
		inner.TabPages.Add(BuildMobConfPage());
		inner.TabPages.Add(BuildMobTimerPage());
		root.Controls.Add(inner, 0, 1);
		mobPage.Controls.Add(root);
		return mobPage;
	}

	private void InitMobTab()
	{
		tabs.SelectedIndexChanged += delegate
		{
			if (tabs.SelectedTab == mobPage)
				MobTabOpened();
		};
	}

	private static NumericUpDown Num(int min, int max, int value, int width = 70)
	{
		return new NumericUpDown { Minimum = min, Maximum = max, Value = value, Width = width, Margin = new Padding(3, 5, 3, 3) };
	}

	private static Label Lbl(string text, bool muted = false)
	{
		return new Label { Text = text, AutoSize = true, Margin = new Padding(6, 9, 2, 3), ForeColor = muted ? Theme.Muted : Theme.Text };
	}

	/// <summary>Editable combo box that filters its list while typing (the built-in
	/// AutoComplete crashes under Wine, so this does it by hand).</summary>
	private static ComboBox MobCombo(int width = 230)
	{
		ComboBox c = new ComboBox { Width = width, DropDownStyle = ComboBoxStyle.DropDown, MaxDropDownItems = 15, Margin = new Padding(3, 5, 3, 3), Tag = new string[0] };
		c.TextUpdate += delegate
		{
			string text = c.Text;
			string[] all = (string[])c.Tag;
			object[] match = all.Where(x => x.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0).Take(200).Cast<object>().ToArray();
			c.BeginUpdate();
			c.Items.Clear();
			c.Items.AddRange(match);
			c.EndUpdate();
			// opening the list selects the whole text: open it first, then put the text and caret back
			if (match.Length > 0 && text.Length > 0 && !c.DroppedDown)
			{
				c.DroppedDown = true;
				Cursor.Current = Cursors.Default;
			}
			c.Text = text;
			c.SelectionStart = text.Length;
			c.SelectionLength = 0;
		};
		return c;
	}

	private static void SetChoices(ComboBox c, IEnumerable<string> items)
	{
		string[] all = items.ToArray();
		c.Tag = all;
		c.Items.Clear();
		c.Items.AddRange(all.Take(300).Cast<object>().ToArray());
	}

	private TabPage BuildMobLivePage()
	{
		TabPage p = new TabPage("มอนในแมพตอนนี้");
		TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(4) };
		t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
		t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
		t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
		t.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
		t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

		FlowLayoutPanel f1 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
		cmbLiveMap = MobCombo(160);
		f1.Controls.Add(Lbl("แมพ"));
		f1.Controls.Add(cmbLiveMap);
		f1.Controls.Add(Btn("นับมอน", async delegate { await LiveCount(); }, 100));
		f1.Controls.Add(Btn("ฆ่าตัวที่เลือก", async delegate { await LiveKill(selectedOnly: true); }, 120));
		Button bKillAll = Btn("ฆ่าทั้งแมพ", async delegate { await LiveKill(selectedOnly: false); }, 110);
		bKillAll.BackColor = Theme.BtnStop;
		f1.Controls.Add(bKillAll);
		t.Controls.Add(f1, 0, 0);

		FlowLayoutPanel f2 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
		cmbLiveMob = MobCombo();
		numLiveAmt = Num(1, 500, 1);
		numLiveX = Num(0, 1000, 0, 60);
		numLiveY = Num(0, 1000, 0, 60);
		f2.Controls.AddRange(new Control[] { Lbl("มอน"), cmbLiveMob, Lbl("จำนวน"), numLiveAmt, Lbl("X"), numLiveX, Lbl("Y"), numLiveY, Lbl("(0,0 = สุ่มทั้งแมพ)", true) });
		t.Controls.Add(f2, 0, 1);

		FlowLayoutPanel f3 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
		txtLiveMsg = new TextBox { Width = 380, Margin = new Padding(3, 7, 3, 3) };
		Button bSpawn = Btn("เสกมอน", async delegate { await LiveSpawn(); }, 110);
		bSpawn.BackColor = Theme.BtnGo;
		f3.Controls.AddRange(new Control[] { Lbl("ประกาศ"), txtLiveMsg, bSpawn, Lbl("(เว้นว่าง = ไม่ประกาศ)", true) });
		t.Controls.Add(f3, 0, 2);

		lblLiveResult = new Label { Dock = DockStyle.Fill, ForeColor = Theme.Muted, TextAlign = ContentAlignment.MiddleLeft,
			Text = "มอนประจำแมพจะเกิดตอนมีคนอยู่ในแมพเท่านั้น (dynamic_mobs) แมพที่ไม่มีคนเลยนับได้ 0 เป็นปกติ" };
		t.Controls.Add(lblLiveResult, 0, 3);
		gridLive = Grid();
		t.Controls.Add(gridLive, 0, 4);
		p.Controls.Add(t);
		return p;
	}

	private TabPage BuildMobSpawnPage()
	{
		TabPage p = new TabPage("จุดเกิดประจำแมพ (ถาวร)");
		TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 4, Padding = new Padding(4) };
		t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
		t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
		t.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
		t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

		FlowLayoutPanel f1 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
		cmbSpawnMap = MobCombo(160);
		cmbSpawnMap.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; LoadSpawns(); } };
		Button bSave = Btn("บันทึก", delegate { SaveSpawns(); }, 100);
		bSave.BackColor = Theme.BtnGo;
		f1.Controls.AddRange(new Control[] { Lbl("แมพ"), cmbSpawnMap, Btn("โหลด", delegate { LoadSpawns(); }, 80),
			Btn("ปิด / เปิด", delegate { ToggleSpawn(); }, 100), Btn("ลบ", delegate { DeleteSpawn(); }, 70), bSave });
		t.Controls.Add(f1, 0, 0);

		FlowLayoutPanel f2 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
		cmbSpawnMob = MobCombo();
		numSpawnAmt = Num(1, 500, 10);
		numSpawnD1 = Num(0, 604800, 0, 80);
		numSpawnD2 = Num(0, 604800, 0, 80);
		f2.Controls.AddRange(new Control[] { Lbl("เพิ่มมอน"), cmbSpawnMob, Lbl("จำนวน"), numSpawnAmt, Lbl("เกิดใหม่ (วินาที)"), numSpawnD1,
			Lbl("+ สุ่ม"), numSpawnD2, Btn("เพิ่ม", delegate { AddSpawn(); }, 70) });
		t.Controls.Add(f2, 0, 1);

		t.Controls.Add(new Label { Dock = DockStyle.Fill, ForeColor = Theme.Muted, TextAlign = ContentAlignment.MiddleLeft,
			Text = "แก้ \"จำนวน\" / \"เกิดใหม่\" ในตารางได้เลย แล้วกดบันทึก • มีผลหลังรีสตาร์ท Map server (หรือพิมพ์ @reloadscript ในเกม) • เกิดใหม่ 0 = ค่าปกติของเกม" }, 0, 2);
		gridSpawn = Grid();
		gridSpawn.ReadOnly = false;
		gridSpawn.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
		t.Controls.Add(gridSpawn, 0, 3);
		p.Controls.Add(t);
		return p;
	}

	private TabPage BuildMobConfPage()
	{
		TabPage p = new TabPage("ตั้งค่า monster.conf");
		TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(4) };
		t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
		t.RowStyles.Add(new RowStyle(SizeType.Absolute, 26f));
		t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		FlowLayoutPanel f1 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
		Button bSave = Btn("บันทึก + ใช้เลย", delegate { SaveMobConf(); }, 140);
		bSave.BackColor = Theme.BtnGo;
		f1.Controls.AddRange(new Control[] { Btn("โหลด", delegate { LoadMobConf(); }, 80), bSave });
		t.Controls.Add(f1, 0, 0);
		t.Controls.Add(new Label { Dock = DockStyle.Fill, ForeColor = Theme.Muted, TextAlign = ContentAlignment.MiddleLeft,
			Text = "แก้ช่อง \"ค่าที่ตั้ง\" (เว้นว่าง = ใช้ค่าเดิม) • บันทึกลง conf/import/battle_conf.txt • ค่าที่ \"มีผล\" = ทันที จะเปลี่ยนในเซิร์ฟภายใน 3 วินาที" }, 0, 1);
		gridMobConf = Grid();
		gridMobConf.ReadOnly = false;
		gridMobConf.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
		t.Controls.Add(gridMobConf, 0, 2);
		p.Controls.Add(t);
		return p;
	}

	private TabPage BuildMobTimerPage()
	{
		TabPage p = new TabPage("บอส / MVP ตามเวลา");
		TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, Padding = new Padding(4) };
		t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
		t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
		t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
		t.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
		t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

		FlowLayoutPanel f1 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
		dtTimer = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Width = 80, Margin = new Padding(3, 5, 3, 3), Value = DateTime.Today.AddHours(20) };
		f1.Controls.Add(Lbl("เวลา"));
		f1.Controls.Add(dtTimer);
		f1.Controls.Add(Lbl("วัน"));
		for (int i = 0; i < 7; i++)
		{
			chkTimerDays[i] = new CheckBox { Text = ThaiDayShort[i], AutoSize = true, Checked = true, Margin = new Padding(2, 8, 2, 3) };
			f1.Controls.Add(chkTimerDays[i]);
		}
		t.Controls.Add(f1, 0, 0);

		FlowLayoutPanel f2 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
		cmbTimerMap = MobCombo(150);
		cmbTimerMob = MobCombo();
		numTimerAmt = Num(1, 100, 1, 55);
		numTimerX = Num(0, 1000, 0, 60);
		numTimerY = Num(0, 1000, 0, 60);
		f2.Controls.AddRange(new Control[] { Lbl("แมพ"), cmbTimerMap, Lbl("มอน"), cmbTimerMob, Lbl("จำนวน"), numTimerAmt, Lbl("X"), numTimerX, Lbl("Y"), numTimerY });
		t.Controls.Add(f2, 0, 1);

		FlowLayoutPanel f3 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
		txtTimerMsg = new TextBox { Width = 360, Margin = new Padding(3, 7, 3, 3), Text = "[Ro-X] บอสปรากฏตัวแล้ว!" };
		chkTimerSkip = new CheckBox { Text = "ไม่เสกซ้ำถ้าตัวเดิมยังไม่ตาย", AutoSize = true, Checked = true, Margin = new Padding(8, 8, 3, 3) };
		Button bAdd = Btn("เพิ่ม", delegate { AddTimer(); }, 80);
		bAdd.BackColor = Theme.BtnGo;
		f3.Controls.AddRange(new Control[] { Lbl("ประกาศ"), txtTimerMsg, chkTimerSkip, bAdd });
		t.Controls.Add(f3, 0, 2);

		FlowLayoutPanel f4 = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
		f4.Controls.AddRange(new Control[] { Btn("โหลด", delegate { LoadTimers(); }, 80), Btn("เปิด / ปิด", delegate { ToggleTimer(); }, 100),
			Btn("ลบ", delegate { DeleteTimer(); }, 70), Btn("เสกทดสอบตอนนี้", async delegate { await TestTimer(); }, 140),
			Lbl("ใช้เวลาเครื่อง PC ที่รันเซิร์ฟ • แมพที่ไม่มีคนก็เสกได้", true) });
		t.Controls.Add(f4, 0, 3);
		gridTimer = Grid();
		t.Controls.Add(gridTimer, 0, 4);
		p.Controls.Add(t);
		return p;
	}

	// ------------------------------------------------------------------
	// data: monster names and spawn maps
	// ------------------------------------------------------------------
	private void MobTabOpened()
	{
		if (!mobLoaded && serverDir != null)
		{
			mobLoaded = true;
			try
			{
				LoadMobNames();
				ScanSpawnMaps();
				// the live commands need npc/Npc RoX/mob_control.txt loaded by the map server
				if (File.Exists(Path.Combine(serverDir, "npc", "Npc RoX", "mob_control.txt")) && EnsureScriptRegistered("npc/Npc RoX/mob_control.txt"))
					tool.Add("[Tool] เพิ่ม npc/Npc RoX/mob_control.txt ใน scripts_custom.conf แล้ว (มีผลตอนเปิด Map server ครั้งหน้า)");
			}
			catch (Exception ex)
			{
				tool.Add("[Tool][Error] โหลดข้อมูลมอนไม่ได้: " + ex.Message);
			}
			foreach (ComboBox c in new[] { cmbLiveMob, cmbSpawnMob, cmbTimerMob })
				SetChoices(c, mobChoices);
			foreach (ComboBox c in new[] { cmbLiveMap, cmbSpawnMap, cmbTimerMap })
				SetChoices(c, spawnMaps);
			LoadMobConf();
			if (db.Connected)
				LoadTimers();
		}
		CheckMobNpc();
	}

	private void LoadMobNames()
	{
		mobNames.Clear();
		mobChoices.Clear();
		Regex rId = new Regex("^\\s*-\\s*Id:\\s*(\\d+)");
		Regex rName = new Regex("^\\s*Name:\\s*(.+?)\\s*$");
		foreach (string dir in new[] { Path.Combine("db", preRe ? "pre-re" : "re"), Path.Combine("db", "import") })
		{
			string path = Path.Combine(serverDir, dir, "mob_db.yml");
			if (!File.Exists(path))
				continue;
			int id = -1;
			foreach (string line in File.ReadLines(path, Encoding.UTF8))
			{
				Match m = rId.Match(line);
				if (m.Success)
				{
					id = int.Parse(m.Groups[1].Value);
					continue;
				}
				m = rName.Match(line);
				if (m.Success && id > 0)
				{
					mobNames[id] = m.Groups[1].Value.Trim('"', '\'');
					id = -1;
				}
			}
		}
		foreach (KeyValuePair<int, string> kv in mobNames.OrderBy(k => k.Key))
			mobChoices.Add(kv.Key + " " + kv.Value);
	}

	private string MobName(int id)
	{
		return mobNames.TryGetValue(id, out string n) ? n : "?";
	}

	private IEnumerable<string> SpawnFiles()
	{
		string dir = Path.Combine(serverDir, "npc", preRe ? "pre-re" : "re", "mobs");
		if (Directory.Exists(dir))
			foreach (string f in Directory.GetFiles(dir, "*.txt", SearchOption.AllDirectories))
				yield return f;
		string custom = Path.Combine(serverDir, CustomSpawnFile.Replace('/', Path.DirectorySeparatorChar));
		if (File.Exists(custom))
			yield return custom;
	}

	private void ScanSpawnMaps()
	{
		spawnMaps.Clear();
		foreach (string f in SpawnFiles())
			foreach (string line in File.ReadAllLines(f, Latin1))
			{
				Match m = SpawnLine.Match(line.TrimEnd('\r'));
				if (m.Success)
					spawnMaps.Add(m.Groups["map"].Value);
			}
	}

	private static int ParseMob(ComboBox c)
	{
		Match m = Regex.Match(c.Text.Trim(), "^(\\d+)");
		return m.Success ? int.Parse(m.Groups[1].Value) : 0;
	}

	private static string MapText(ComboBox c)
	{
		return c.Text.Trim().ToLowerInvariant();
	}

	// ------------------------------------------------------------------
	// commands to the NPC
	// ------------------------------------------------------------------
	private void EnsureMobTables()
	{
		db.Exec(MobCmdSql);
		db.Exec(MobCountSql);
		db.Exec(MobFlagSql);
		db.Exec(MobTimerSql);
	}

	private bool CheckMobNpc()
	{
		if (!db.Connected)
		{
			lblMobNpc.Text = "NPC คุมมอน: ยังไม่ได้ต่อฐานข้อมูล";
			lblMobNpc.ForeColor = Theme.Muted;
			return false;
		}
		try
		{
			EnsureMobTables();
			object hb = db.Scalar("SELECT `value` FROM `rox_mobflag` WHERE `flag`='_heartbeat'");
			long ts = hb == null || hb is DBNull ? 0 : Convert.ToInt64(hb);
			bool ok = ts > 0 && Math.Abs(UnixNow() - ts) < 150;
			lblMobNpc.Text = ok
				? "NPC คุมมอน: พร้อม (npc/Npc RoX/mob_control.txt)"
				: "NPC คุมมอน: ไม่ตอบ — เปิด Map server และต้องมีบรรทัด npc: npc/Npc RoX/mob_control.txt ใน npc/scripts_custom.conf";
			lblMobNpc.ForeColor = ok ? Theme.Good : Theme.Warn;
			return ok;
		}
		catch (Exception ex)
		{
			lblMobNpc.Text = "NPC คุมมอน: " + ex.Message;
			lblMobNpc.ForeColor = Theme.Bad;
			return false;
		}
	}

	/// <summary>Queue a command for mob_control.txt and wait for its result (null = no answer).</summary>
	private async Task<string> MobCmd(string cmd, string map, int mob = 0, int amount = 1, int x = 0, int y = 0, string flag = "", int value = 0, string msg = "")
	{
		if (!NeedDb())
			return null;
		EnsureMobTables();
		db.Exec("INSERT INTO `rox_mobcmd` (`cmd`,`map`,`mob_id`,`amount`,`x`,`y`,`flag`,`value`,`msg`,`created`) VALUES ('"
			+ Db.Esc(cmd) + "','" + Db.Esc(map) + "'," + mob + "," + amount + "," + x + "," + y + ",'" + Db.Esc(flag) + "'," + value + ",'" + Db.Esc(msg) + "',NOW())");
		long id = Convert.ToInt64(db.Scalar("SELECT MAX(`id`) FROM `rox_mobcmd`"));
		for (int i = 0; i < 40; i++)
		{
			await Task.Delay(250);
			DataTable r = db.Query("SELECT `status`,`result` FROM `rox_mobcmd` WHERE `id`=" + id);
			if (r.Rows.Count > 0 && Convert.ToInt32(r.Rows[0]["status"]) != 0)
				return Convert.ToString(r.Rows[0]["result"]);
		}
		CheckMobNpc();
		return null;
	}

	private void ShowLive(string text, bool ok)
	{
		lblLiveResult.Text = text;
		lblLiveResult.ForeColor = ok ? Theme.Good : Theme.Bad;
	}

	private static string ExplainError(string res)
	{
		if (res == null)
			return "Map server ไม่ตอบภายใน 10 วินาที (เปิดเซิร์ฟอยู่ไหม / มี mob_control.txt ไหม)";
		if (res.StartsWith("error no map"))
			return "ไม่มีแมพนี้ในเซิร์ฟ";
		if (res.StartsWith("error no monster"))
			return "ไม่มีมอน ID นี้";
		return res;
	}

	private async Task LiveCount()
	{
		string map = MapText(cmbLiveMap);
		if (map.Length == 0)
			return;
		string res = await MobCmd("count", map);
		if (res == null || !res.StartsWith("ok"))
		{
			ShowLive(ExplainError(res), false);
			return;
		}
		DataTable t = db.Query("SELECT `mob_id` AS `ID`, `name` AS `mname`, `cnt` AS `mcount` FROM `rox_mobcount` WHERE `map`='" + Db.Esc(map) + "' ORDER BY `cnt` DESC");
		t.Columns["mname"].ColumnName = "ชื่อ";
		t.Columns["mcount"].ColumnName = "มีชีวิตอยู่";
		Bind(gridLive, t);
		ShowLive(map + ": มอนมีชีวิต " + res.Substring(3) + " ตัว (" + DateTime.Now.ToString("HH:mm:ss") + ")", true);
	}

	private async Task LiveKill(bool selectedOnly)
	{
		string map = MapText(cmbLiveMap);
		if (map.Length == 0)
			return;
		int mob = 0;
		if (selectedOnly)
		{
			if (gridLive.CurrentRow == null)
			{
				ShowLive("กด \"นับมอน\" แล้วเลือกมอนในตารางก่อน", false);
				return;
			}
			mob = Convert.ToInt32(gridLive.CurrentRow.Cells["ID"].Value);
		}
		string what = selectedOnly ? MobName(mob) + " ทุกตัว" : "มอนทุกตัว";
		if (MessageBox.Show(this, "ฆ่า " + what + " ในแมพ " + map + "?\n\nมอนประจำแมพจะเกิดใหม่ตามเวลาปกติ", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
			return;
		string res = await MobCmd("kill", map, mob);
		if (res == null || !res.StartsWith("ok"))
		{
			ShowLive(ExplainError(res), false);
			return;
		}
		tool.Add("[Tool] ฆ่า " + what + " ใน " + map + " (" + res.Substring(3) + " ตัว)");
		await LiveCount();
		ShowLive("ฆ่า " + what + " ใน " + map + " แล้ว " + res.Substring(3) + " ตัว", true);
	}

	private async Task LiveSpawn()
	{
		string map = MapText(cmbLiveMap);
		int mob = ParseMob(cmbLiveMob);
		if (map.Length == 0 || mob <= 0)
		{
			ShowLive("เลือกแมพและมอนก่อน", false);
			return;
		}
		string res = await MobCmd("spawn", map, mob, (int)numLiveAmt.Value, (int)numLiveX.Value, (int)numLiveY.Value, msg: txtLiveMsg.Text.Trim());
		if (res == null || !res.StartsWith("ok"))
		{
			ShowLive(ExplainError(res), false);
			return;
		}
		tool.Add("[Tool] เสก " + res.Substring(3) + " ที่ " + map);
		await LiveCount();
		ShowLive("เสก " + res.Substring(3) + " ที่ " + map + " แล้ว", true);
	}

	// ------------------------------------------------------------------
	// permanent spawn lines
	// ------------------------------------------------------------------
	private string RelPath(string full)
	{
		return full.Substring(serverDir.Length).TrimStart('\\', '/').Replace('\\', '/');
	}

	private void LoadSpawns()
	{
		if (serverDir == null)
			return;
		string map = MapText(cmbSpawnMap);
		if (map.Length == 0)
			return;
		spawnTable = new DataTable();
		spawnTable.Columns.Add("สถานะ", typeof(string));
		spawnTable.Columns.Add("ID", typeof(int));
		spawnTable.Columns.Add("ชื่อ", typeof(string));
		spawnTable.Columns.Add("จำนวน", typeof(int));
		spawnTable.Columns.Add("เกิดใหม่ (วินาที)", typeof(int));
		spawnTable.Columns.Add("+ สุ่ม (วินาที)", typeof(int));
		spawnTable.Columns.Add("ประเภท", typeof(string));
		spawnTable.Columns.Add("ไฟล์", typeof(string));
		spawnTable.Columns.Add("_line", typeof(int));
		spawnTable.Columns.Add("_raw", typeof(string));
		foreach (string f in SpawnFiles())
		{
			string[] lines = File.ReadAllLines(f, Latin1);
			for (int i = 0; i < lines.Length; i++)
			{
				string raw = lines[i].TrimEnd('\r');
				Match m = SpawnLine.Match(raw);
				if (!m.Success || !m.Groups["map"].Value.Equals(map, StringComparison.OrdinalIgnoreCase))
					continue;
				int id = int.Parse(m.Groups["id"].Value);
				spawnTable.Rows.Add(m.Groups["off"].Success ? "ปิด" : "เปิด", id, MobName(id), int.Parse(m.Groups["amt"].Value),
					m.Groups["d1"].Success ? int.Parse(m.Groups["d1"].Value) / 1000 : 0,
					m.Groups["d2"].Success ? int.Parse(m.Groups["d2"].Value) / 1000 : 0,
					m.Groups["type"].Value, RelPath(f), i, raw);
			}
		}
		spawnTable.AcceptChanges();
		gridSpawn.DataSource = spawnTable;
		foreach (DataGridViewColumn c in gridSpawn.Columns)
		{
			c.ReadOnly = !(c.Name == "จำนวน" || c.Name.StartsWith("เกิดใหม่") || c.Name.StartsWith("+ สุ่ม"));
			if (!c.ReadOnly)
				c.HeaderCell.Style.BackColor = Theme.EditHead;
			c.Visible = !c.Name.StartsWith("_");
		}
		gridSpawn.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
		if (spawnTable.Rows.Count == 0)
			MessageBox.Show(this, "ไม่พบจุดเกิดมอนของแมพ " + map + "\n\nเพิ่มใหม่ได้จากแถว \"เพิ่มมอน\"", Text);
	}

	private void AddSpawn()
	{
		if (spawnTable == null)
		{
			MessageBox.Show(this, "เลือกแมพแล้วกดโหลดก่อน", Text);
			return;
		}
		int mob = ParseMob(cmbSpawnMob);
		if (!mobNames.ContainsKey(mob))
		{
			MessageBox.Show(this, "เลือกมอนจากรายการก่อน", Text);
			return;
		}
		spawnTable.Rows.Add("ใหม่", mob, MobName(mob), (int)numSpawnAmt.Value, (int)numSpawnD1.Value, (int)numSpawnD2.Value,
			"monster", CustomSpawnFile, -1, string.Empty);
	}

	private void ToggleSpawn()
	{
		if (gridSpawn.CurrentRow?.DataBoundItem is DataRowView v)
		{
			string st = Convert.ToString(v["สถานะ"]);
			if (st == "ใหม่")
				return;
			v["สถานะ"] = st == "ปิด" ? "เปิด" : "ปิด";
		}
	}

	private void DeleteSpawn()
	{
		if (!(gridSpawn.CurrentRow?.DataBoundItem is DataRowView v))
			return;
		if (Convert.ToString(v["สถานะ"]) == "ใหม่")
		{
			v.Row.Delete();
			return;
		}
		if (Convert.ToString(v["ไฟล์"]) != CustomSpawnFile)
		{
			MessageBox.Show(this, "จุดเกิดนี้มาจากไฟล์ของเกม ลบไม่ได้ ให้กด \"ปิด / เปิด\" แทน (ปิดแล้วเปิดคืนได้)", Text);
			return;
		}
		v["สถานะ"] = "ลบ";
	}

	private static string BuildSpawnLine(Match m, DataRow r)
	{
		int amt = Convert.ToInt32(r["จำนวน"]);
		int d1 = Convert.ToInt32(r["เกิดใหม่ (วินาที)"]) * 1000;
		int d2 = Convert.ToInt32(r["+ สุ่ม (วินาที)"]) * 1000;
		string rest = m.Groups["rest"].Value;
		string delays = (d1 > 0 || d2 > 0 || rest.Length > 0 || m.Groups["d1"].Success) ? "," + d1 + "," + d2 : string.Empty;
		return (Convert.ToString(r["สถานะ"]) == "ปิด" ? OffMark : string.Empty)
			+ m.Groups["map"].Value + "," + m.Groups["x"].Value + "," + m.Groups["y"].Value + m.Groups["area"].Value
			+ "\t" + m.Groups["type"].Value + "\t" + m.Groups["name"].Value + "\t" + m.Groups["id"].Value + "," + amt + delays + rest;
	}

	private void SaveSpawns()
	{
		if (spawnTable == null || serverDir == null)
			return;
		gridSpawn.EndEdit();
		string map = MapText(cmbSpawnMap);
		int changed = 0;
		try
		{
			// edits, grouped per file
			foreach (IGrouping<string, DataRow> g in spawnTable.Rows.Cast<DataRow>()
				.Where(r => r.RowState != DataRowState.Deleted && Convert.ToInt32(r["_line"]) >= 0)
				.GroupBy(r => Convert.ToString(r["ไฟล์"])))
			{
				string path = Path.Combine(serverDir, g.Key.Replace('/', Path.DirectorySeparatorChar));
				string text = File.ReadAllText(path, Latin1);
				string[] lines = text.Split('\n');
				List<int> remove = new List<int>();
				foreach (DataRow r in g)
				{
					int i = Convert.ToInt32(r["_line"]);
					string raw = Convert.ToString(r["_raw"]);
					if (i >= lines.Length || lines[i].TrimEnd('\r') != raw)
						throw new Exception("ไฟล์ " + g.Key + " ถูกแก้จากที่อื่น กดโหลดใหม่ก่อน");
					if (Convert.ToString(r["สถานะ"]) == "ลบ")
					{
						remove.Add(i);
						changed++;
						continue;
					}
					string line = BuildSpawnLine(SpawnLine.Match(raw), r);
					if (line != raw)
					{
						lines[i] = line + (lines[i].EndsWith("\r") ? "\r" : string.Empty);
						changed++;
					}
				}
				string outText = string.Join("\n", lines.Where((l, i) => !remove.Contains(i)));
				if (outText != text)
				{
					string bak = path + ".bak_rox";
					if (!File.Exists(bak))
						File.Copy(path, bak);
					File.WriteAllText(path, outText, Latin1);
				}
			}
			// new lines -> custom spawn file
			List<DataRow> added = spawnTable.Rows.Cast<DataRow>().Where(r => r.RowState != DataRowState.Deleted && Convert.ToInt32(r["_line"]) < 0).ToList();
			if (added.Count > 0)
			{
				string path = Path.Combine(serverDir, CustomSpawnFile.Replace('/', Path.DirectorySeparatorChar));
				StringBuilder sb = new StringBuilder();
				if (!File.Exists(path))
					sb.Append("//===== Ro-X: monster spawns added from RO_Manager (tab \"monsters\") =====\r\n");
				foreach (DataRow r in added)
				{
					int id = Convert.ToInt32(r["ID"]);
					int d1 = Convert.ToInt32(r["เกิดใหม่ (วินาที)"]) * 1000;
					int d2 = Convert.ToInt32(r["+ สุ่ม (วินาที)"]) * 1000;
					sb.Append(map + ",0,0\tmonster\t" + Regex.Replace(MobName(id), "[\\t\\r\\n]", " ") + "\t" + id + "," + Convert.ToInt32(r["จำนวน"])
						+ (d1 > 0 || d2 > 0 ? "," + d1 + "," + d2 : string.Empty) + "\r\n");
					changed++;
				}
				File.AppendAllText(path, sb.ToString(), Latin1);
				EnsureCustomSpawnRegistered();
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}
		if (changed > 0)
		{
			tool.Add("[Tool] แก้จุดเกิดมอนแมพ " + map + " (" + changed + " รายการ)");
			MessageBox.Show(this, "บันทึกแล้ว " + changed + " รายการ\n\nมีผลหลังรีสตาร์ท Map server หรือพิมพ์ @reloadscript ในเกม", Text);
		}
		ScanSpawnMaps();
		LoadSpawns();
	}

	private void EnsureCustomSpawnRegistered()
	{
		EnsureScriptRegistered(CustomSpawnFile);
	}

	/// <summary>Adds "npc: file" to npc/scripts_custom.conf when missing. True when it was added.</summary>
	private bool EnsureScriptRegistered(string file)
	{
		string conf = Path.Combine(serverDir, "npc", "scripts_custom.conf");
		string line = "npc: " + file;
		if (!File.Exists(conf))
			return false;
		string text = File.ReadAllText(conf, Latin1);
		if (text.IndexOf(line, StringComparison.OrdinalIgnoreCase) >= 0)
			return false; // already there (or commented out on purpose)
		string nl = text.Contains("\r\n") ? "\r\n" : "\n";
		File.AppendAllText(conf, (text.Length == 0 || text.EndsWith("\n") ? string.Empty : nl) + line + nl, Latin1);
		return true;
	}

	// ------------------------------------------------------------------
	// monster.conf
	// ------------------------------------------------------------------
	private static Dictionary<string, string> ReadConfValues(string path)
	{
		Dictionary<string, string> d = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (!File.Exists(path))
			return d;
		foreach (string line in File.ReadAllLines(path, Latin1))
		{
			Match m = Regex.Match(line, "^\\s*([A-Za-z0-9_]+)\\s*:\\s*([^/\\s]+)");
			if (m.Success)
				d[m.Groups[1].Value] = m.Groups[2].Value;
		}
		return d;
	}

	private static string ConfNum(string v)
	{
		if (v == null)
			return string.Empty;
		switch (v.ToLowerInvariant())
		{
			case "yes": case "on": return "1";
			case "no": case "off": return "0";
			default: return v;
		}
	}

	private void LoadMobConf()
	{
		if (serverDir == null)
			return;
		Dictionary<string, string> def = ReadConfValues(Path.Combine(serverDir, "conf", "battle", "monster.conf"));
		Dictionary<string, string> imp = ReadConfValues(Path.Combine(serverDir, "conf", "import", "battle_conf.txt"));
		Dictionary<string, string> live = new Dictionary<string, string>();
		if (db.Connected)
		{
			try
			{
				EnsureMobTables();
				foreach (DataRow r in db.Query("SELECT `flag`,`value` FROM `rox_mobflag`").Rows)
					live[Convert.ToString(r["flag"])] = Convert.ToString(r["value"]);
			}
			catch { }
		}
		mobConfTable = new DataTable();
		mobConfTable.Columns.Add("ค่า", typeof(string));
		mobConfTable.Columns.Add("ความหมาย", typeof(string));
		mobConfTable.Columns.Add("ค่าเดิม", typeof(string));
		mobConfTable.Columns.Add("ค่าที่ตั้ง", typeof(string));
		mobConfTable.Columns.Add("ในเซิร์ฟตอนนี้", typeof(string));
		mobConfTable.Columns.Add("มีผล", typeof(string));
		foreach (string[] f in MobFlags)
		{
			def.TryGetValue(f[0], out string d);
			imp.TryGetValue(f[0], out string i);
			live.TryGetValue(f[0], out string l);
			mobConfTable.Rows.Add(f[0], f[1], ConfNum(d), ConfNum(i), l ?? "-", f[2] == "1" ? "ทันที" : "หลังรีสตาร์ท");
		}
		mobConfTable.AcceptChanges();
		gridMobConf.DataSource = mobConfTable;
		foreach (DataGridViewColumn c in gridMobConf.Columns)
		{
			c.ReadOnly = c.Name != "ค่าที่ตั้ง";
			if (!c.ReadOnly)
				c.HeaderCell.Style.BackColor = Theme.EditHead;
		}
		gridMobConf.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.AllCells);
	}

	private async void SaveMobConf()
	{
		if (mobConfTable == null || serverDir == null)
			return;
		gridMobConf.EndEdit();
		Dictionary<string, string> want = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		foreach (DataRow r in mobConfTable.Rows)
		{
			string v = Convert.ToString(r["ค่าที่ตั้ง"]).Trim();
			if (v.Length > 0 && !Regex.IsMatch(v, "^-?\\d+$"))
			{
				MessageBox.Show(this, r["ค่า"] + ": ใส่ได้เฉพาะตัวเลข", Text);
				return;
			}
			if (v == Convert.ToString(r["ค่าเดิม"]))
				v = string.Empty; // same as the default: no need to override
			want[Convert.ToString(r["ค่า"])] = v;
		}
		string path = Path.Combine(serverDir, "conf", "import", "battle_conf.txt");
		List<string> lines = File.Exists(path) ? File.ReadAllLines(path, Latin1).ToList() : new List<string>();
		HashSet<string> done = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<string> output = new List<string>();
		foreach (string line in lines)
		{
			Match m = Regex.Match(line, "^\\s*([A-Za-z0-9_]+)\\s*:");
			if (m.Success && want.TryGetValue(m.Groups[1].Value, out string v))
			{
				if (v.Length > 0 && done.Add(m.Groups[1].Value))
					output.Add(m.Groups[1].Value + ": " + v);
				continue; // replaced or removed
			}
			output.Add(line);
		}
		foreach (KeyValuePair<string, string> kv in want)
			if (kv.Value.Length > 0 && !done.Contains(kv.Key))
				output.Add(kv.Key + ": " + kv.Value);
		try
		{
			File.WriteAllText(path, string.Join("\r\n", output) + "\r\n", Latin1);
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}
		tool.Add("[Tool] บันทึกค่ามอนลง conf/import/battle_conf.txt");

		// push the new values to the running server too
		int pushed = 0;
		bool npc = db.Connected && CheckMobNpc();
		if (npc)
		{
			foreach (DataRow r in mobConfTable.Rows)
			{
				string flag = Convert.ToString(r["ค่า"]);
				string v = want[flag].Length > 0 ? want[flag] : Convert.ToString(r["ค่าเดิม"]);
				if (v.Length == 0 || v == Convert.ToString(r["ในเซิร์ฟตอนนี้"]))
					continue;
				string res = await MobCmd("flag", string.Empty, flag: flag, value: int.Parse(v));
				if (res != null && res.StartsWith("ok"))
					pushed++;
			}
		}
		LoadMobConf();
		MessageBox.Show(this, "บันทึกแล้ว\n\n"
			+ (npc ? "ส่งค่าเข้าเซิร์ฟ " + pushed + " ค่า • ค่าที่ \"หลังรีสตาร์ท\" จะมีผลเต็มที่เมื่อรีสตาร์ท Map server" : "Map server ไม่ได้เปิดอยู่ ค่าจะใช้ตอนเปิดเซิร์ฟครั้งหน้า"), Text);
	}

	// ------------------------------------------------------------------
	// boss / MVP timers
	// ------------------------------------------------------------------
	private static string DaysText(string days)
	{
		if (days.Length >= 7 && "0123456".All(days.Contains))
			return "ทุกวัน";
		return string.Join(" ", days.Where(char.IsDigit).Select(c => ThaiDayShort[c - '0']));
	}

	private void LoadTimers()
	{
		if (!db.Connected)
			return;
		try
		{
			EnsureMobTables();
			DataTable src = db.Query("SELECT `id`,`enabled`,`minute`,`days`,`map`,`mob_id`,`amount`,`x`,`y`,`skip_alive`,`msg`,`last_run` FROM `rox_mobtimer` ORDER BY `minute`,`id`");
			DataTable t = new DataTable();
			t.Columns.Add("ID", typeof(long));
			t.Columns.Add("สถานะ", typeof(string));
			t.Columns.Add("เวลา", typeof(string));
			t.Columns.Add("วัน", typeof(string));
			t.Columns.Add("แมพ", typeof(string));
			t.Columns.Add("มอน", typeof(string));
			t.Columns.Add("จำนวน", typeof(int));
			t.Columns.Add("X,Y", typeof(string));
			t.Columns.Add("ไม่เสกซ้ำ", typeof(string));
			t.Columns.Add("ประกาศ", typeof(string));
			t.Columns.Add("เสกล่าสุด", typeof(string));
			t.Columns.Add("_mob", typeof(int));
			foreach (DataRow r in src.Rows)
			{
				int min = Convert.ToInt32(r["minute"]);
				int mob = Convert.ToInt32(r["mob_id"]);
				t.Rows.Add(Convert.ToInt64(r["id"]), Convert.ToInt32(r["enabled"]) == 1 ? "เปิด" : "ปิด", (min / 60).ToString("00") + ":" + (min % 60).ToString("00"),
					DaysText(Convert.ToString(r["days"])), Convert.ToString(r["map"]), mob + " " + MobName(mob), Convert.ToInt32(r["amount"]),
					r["x"] + "," + r["y"], Convert.ToInt32(r["skip_alive"]) == 1 ? "ใช่" : "", Convert.ToString(r["msg"]), Convert.ToString(r["last_run"]), mob);
			}
			Bind(gridTimer, t);
			if (gridTimer.Columns.Contains("_mob"))
				gridTimer.Columns["_mob"].Visible = false;
		}
		catch (Exception ex)
		{
			tool.Add("[Tool][Error] โหลดตารางบอสไม่ได้: " + ex.Message);
		}
	}

	private void AddTimer()
	{
		if (!NeedDb())
			return;
		string map = MapText(cmbTimerMap);
		int mob = ParseMob(cmbTimerMob);
		string days = string.Concat(Enumerable.Range(0, 7).Where(i => chkTimerDays[i].Checked).Select(i => i.ToString()));
		if (map.Length == 0 || !mobNames.ContainsKey(mob) || days.Length == 0)
		{
			MessageBox.Show(this, "เลือกแมพ มอน และอย่างน้อย 1 วัน", Text);
			return;
		}
		int minute = dtTimer.Value.Hour * 60 + dtTimer.Value.Minute;
		EnsureMobTables();
		db.Exec("INSERT INTO `rox_mobtimer` (`enabled`,`map`,`x`,`y`,`mob_id`,`amount`,`minute`,`days`,`skip_alive`,`msg`) VALUES (1,'" + Db.Esc(map) + "',"
			+ (int)numTimerX.Value + "," + (int)numTimerY.Value + "," + mob + "," + (int)numTimerAmt.Value + "," + minute + ",'" + days + "',"
			+ (chkTimerSkip.Checked ? 1 : 0) + ",'" + Db.Esc(txtTimerMsg.Text.Trim()) + "')");
		tool.Add("[Tool] ตั้งบอส " + MobName(mob) + " ที่ " + map + " เวลา " + dtTimer.Value.ToString("HH:mm") + " (" + DaysText(days) + ")");
		LoadTimers();
	}

	private long SelectedTimer()
	{
		if (gridTimer.CurrentRow == null)
		{
			MessageBox.Show(this, "เลือกรายการในตารางก่อน", Text);
			return -1;
		}
		return Convert.ToInt64(gridTimer.CurrentRow.Cells["ID"].Value);
	}

	private void ToggleTimer()
	{
		long id = SelectedTimer();
		if (id < 0 || !NeedDb())
			return;
		db.Exec("UPDATE `rox_mobtimer` SET `enabled`=1-`enabled` WHERE `id`=" + id);
		LoadTimers();
	}

	private void DeleteTimer()
	{
		long id = SelectedTimer();
		if (id < 0 || !NeedDb())
			return;
		if (MessageBox.Show(this, "ลบรายการบอสนี้?", Text, MessageBoxButtons.YesNo) != DialogResult.Yes)
			return;
		db.Exec("DELETE FROM `rox_mobtimer` WHERE `id`=" + id);
		LoadTimers();
	}

	private async Task TestTimer()
	{
		long id = SelectedTimer();
		if (id < 0 || !NeedDb())
			return;
		DataTable r = db.Query("SELECT `map`,`x`,`y`,`mob_id`,`amount`,`msg` FROM `rox_mobtimer` WHERE `id`=" + id);
		if (r.Rows.Count == 0)
			return;
		DataRow row = r.Rows[0];
		string res = await MobCmd("spawn", Convert.ToString(row["map"]), Convert.ToInt32(row["mob_id"]), Convert.ToInt32(row["amount"]),
			Convert.ToInt32(row["x"]), Convert.ToInt32(row["y"]), msg: Convert.ToString(row["msg"]));
		if (res != null && res.StartsWith("ok"))
		{
			tool.Add("[Tool] เสกทดสอบ " + res.Substring(3) + " ที่ " + row["map"]);
			MessageBox.Show(this, "เสกแล้ว: " + res.Substring(3) + " ที่ " + row["map"], Text);
		}
		else
			MessageBox.Show(this, ExplainError(res), Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
	}
}
