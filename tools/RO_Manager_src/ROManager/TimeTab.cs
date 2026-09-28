// Ro-X: "เวลา / กลางวัน-กลางคืน" tab — PC clock vs. map-server clock and the
// automatic @day / @night schedule run by npc/Npc RoX/daynight.txt.
using System;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ROManager;

internal partial class MainForm
{
	private const string DayNightTableSql = "CREATE TABLE IF NOT EXISTS `rox_daynight` (`id` TINYINT NOT NULL PRIMARY KEY,`enabled` TINYINT NOT NULL DEFAULT 1,`day_min` INT NOT NULL DEFAULT 420,`night_min` INT NOT NULL DEFAULT 1080,`announce` TINYINT NOT NULL DEFAULT 0,`day_msg` VARCHAR(200) NOT NULL DEFAULT '',`night_msg` VARCHAR(200) NOT NULL DEFAULT '',`cmd` TINYINT NOT NULL DEFAULT 0,`updated` DATETIME NULL) ENGINE=MyISAM";
	private const string DayNightStateSql = "CREATE TABLE IF NOT EXISTS `rox_daynight_state` (`id` TINYINT NOT NULL PRIMARY KEY,`is_night` TINYINT NOT NULL DEFAULT 0,`server_ts` INT UNSIGNED NOT NULL DEFAULT 0,`server_time` VARCHAR(32) NOT NULL DEFAULT '',`hold` TINYINT NOT NULL DEFAULT -1,`updated` DATETIME NULL) ENGINE=MyISAM";
	private const string DefaultDayMsg = "[Ro-X] รุ่งเช้าแล้ว ออกผจญภัยกันเถอะ!";
	private const string DefaultNightMsg = "[Ro-X] ค่ำแล้ว ระวังตัวด้วยนะ";

	private static readonly string[] ThaiDays = { "วันอาทิตย์", "วันจันทร์", "วันอังคาร", "วันพุธ", "วันพฤหัสบดี", "วันศุกร์", "วันเสาร์" };
	private static readonly string[] ThaiMonths = { "มกราคม", "กุมภาพันธ์", "มีนาคม", "เมษายน", "พฤษภาคม", "มิถุนายน", "กรกฎาคม", "สิงหาคม", "กันยายน", "ตุลาคม", "พฤศจิกายน", "ธันวาคม" };

	private static string ThaiDate(DateTime d)
	{
		return ThaiDays[(int)d.DayOfWeek] + "ที่ " + d.Day + " " + ThaiMonths[d.Month - 1] + " " + (d.Year + 543);
	}

	private TabPage timePage;
	private Label lblPcClock;
	private Label lblPcDate;
	private Label lblSrvClock;
	private Label lblSrvSync;
	private Label lblDnState;
	private CheckBox chkDnEnabled;
	private DateTimePicker dtDay;
	private DateTimePicker dtNight;
	private CheckBox chkDnAnnounce;
	private TextBox txtDayMsg;
	private TextBox txtNightMsg;
	private ToolStripStatusLabel stClock;
	private Timer clockTimer;
	private int clockTick;
	private bool dnLoaded;

	private TabPage BuildTimeTab()
	{
		timePage = new TabPage("เวลา / กลางวัน-กลางคืน");
		TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(8) };
		root.RowStyles.Add(new RowStyle(SizeType.Absolute, 150f));
		root.RowStyles.Add(new RowStyle(SizeType.Absolute, 300f));
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		timePage.Controls.Add(root);

		// --- clocks -------------------------------------------------------
		GroupBox gClock = new GroupBox { Text = "เวลาเซิร์ฟเวอร์ (ใช้นาฬิกาเครื่อง PC ที่รันเซิร์ฟ)", Dock = DockStyle.Fill };
		TableLayoutPanel tClock = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
		tClock.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
		tClock.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
		FlowLayoutPanel fPc = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
		fPc.Controls.Add(new Label { AutoSize = true, ForeColor = Theme.Muted, Text = "เครื่อง PC" });
		lblPcClock = new Label { AutoSize = true, Font = new Font("Tahoma", 26f, FontStyle.Bold), ForeColor = Theme.Accent, Text = "--:--:--" };
		lblPcDate = new Label { AutoSize = true, ForeColor = Theme.Text, Text = string.Empty };
		fPc.Controls.AddRange(new Control[] { lblPcClock, lblPcDate });
		FlowLayoutPanel fSrv = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
		fSrv.Controls.Add(new Label { AutoSize = true, ForeColor = Theme.Muted, Text = "Map server (รายงานผ่าน NPC ทุก 5 วินาที)" });
		lblSrvClock = new Label { AutoSize = true, Font = new Font("Tahoma", 18f, FontStyle.Bold), ForeColor = Theme.Text, Text = "--:--:--" };
		lblSrvSync = new Label { AutoSize = true, MaximumSize = new Size(560, 0), ForeColor = Theme.Muted, Text = "ยังไม่ได้รับเวลาจาก Map server" };
		fSrv.Controls.AddRange(new Control[] { lblSrvClock, lblSrvSync });
		tClock.Controls.Add(fPc, 0, 0);
		tClock.Controls.Add(fSrv, 1, 0);
		gClock.Controls.Add(tClock);
		root.Controls.Add(gClock, 0, 0);

		// --- day / night --------------------------------------------------
		GroupBox gDn = new GroupBox { Text = "กลางวัน / กลางคืนอัตโนมัติ (@day / @night)", Dock = DockStyle.Fill };
		TableLayoutPanel t = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 7 };
		t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170f));
		t.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 120f));
		t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		for (int i = 0; i < 6; i++)
			t.RowStyles.Add(new RowStyle(SizeType.Absolute, 34f));
		t.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));

		lblDnState = new Label { AutoSize = true, Font = new Font("Tahoma", 12f, FontStyle.Bold), ForeColor = Theme.Muted, Text = "ตอนนี้ในเกม: -" };
		t.Controls.Add(lblDnState, 0, 0);
		t.SetColumnSpan(lblDnState, 3);

		chkDnEnabled = new CheckBox { Text = "เปิดระบบอัตโนมัติ", AutoSize = true, Checked = true, Anchor = AnchorStyles.Left };
		t.Controls.Add(chkDnEnabled, 0, 1);
		t.SetColumnSpan(chkDnEnabled, 3);

		dtDay = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Width = 90, Value = DateTime.Today.AddHours(7) };
		dtNight = new DateTimePicker { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm", ShowUpDown = true, Width = 90, Value = DateTime.Today.AddHours(18) };
		t.Controls.Add(new Label { Text = "เริ่มกลางวัน (@day)", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
		t.Controls.Add(dtDay, 1, 2);
		t.Controls.Add(new Label { Text = "เริ่มกลางคืน (@night)", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
		t.Controls.Add(dtNight, 1, 3);
		t.Controls.Add(new Label { Text = "ค่าเริ่มต้น 07:00 / 18:00 • ข้ามเที่ยงคืนได้", AutoSize = true, ForeColor = Theme.Muted, Anchor = AnchorStyles.Left }, 2, 2);

		chkDnAnnounce = new CheckBox { Text = "ประกาศเพิ่มเอง", AutoSize = true, Anchor = AnchorStyles.Left };
		txtDayMsg = new TextBox { Dock = DockStyle.Fill, Text = DefaultDayMsg };
		txtNightMsg = new TextBox { Dock = DockStyle.Fill, Text = DefaultNightMsg };
		t.Controls.Add(chkDnAnnounce, 0, 4);
		t.Controls.Add(new Label { Text = "ข้อความกลางวัน", AutoSize = true, Anchor = AnchorStyles.Left }, 1, 4);
		t.Controls.Add(txtDayMsg, 2, 4);
		t.Controls.Add(new Label { Text = "(เกมประกาศของมันเองอยู่แล้ว)", AutoSize = true, ForeColor = Theme.Muted, Anchor = AnchorStyles.Left }, 0, 5);
		t.Controls.Add(new Label { Text = "ข้อความกลางคืน", AutoSize = true, Anchor = AnchorStyles.Left }, 1, 5);
		t.Controls.Add(txtNightMsg, 2, 5);
		chkDnAnnounce.CheckedChanged += delegate { txtDayMsg.Enabled = txtNightMsg.Enabled = chkDnAnnounce.Checked; };
		txtDayMsg.Enabled = txtNightMsg.Enabled = false;

		FlowLayoutPanel fBtn = new FlowLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(0, 6, 0, 0) };
		Button bSave = Btn("บันทึก", delegate { SaveDayNight(); }, 110);
		bSave.Height = 36;
		bSave.BackColor = Theme.BtnGo;
		Button bDay = Btn("กลางวันเลย (@day)", delegate { DayNightNow(1); }, 150);
		bDay.Height = 36;
		Button bNight = Btn("กลางคืนเลย (@night)", delegate { DayNightNow(2); }, 160);
		bNight.Height = 36;
		fBtn.Controls.AddRange(new Control[] { bSave, bDay, bNight, new Label
		{
			AutoSize = true,
			ForeColor = Theme.Muted,
			Margin = new Padding(12, 12, 3, 3),
			Text = "กดสั่งเองแล้วค่าจะอยู่ถึงรอบเวลาถัดไป • มีผลภายใน ~5 วินาที"
		} });
		t.Controls.Add(fBtn, 0, 6);
		t.SetColumnSpan(fBtn, 3);
		gDn.Controls.Add(t);
		root.Controls.Add(gDn, 0, 1);

		// --- help ---------------------------------------------------------
		GroupBox gHelp = new GroupBox { Text = "หมายเหตุ", Dock = DockStyle.Fill };
		gHelp.Controls.Add(new Label
		{
			Dock = DockStyle.Fill,
			ForeColor = Theme.Muted,
			Text = "• ระบบทำงานโดย NPC  npc/Npc RoX/daynight.txt  (ต้องมีบรรทัดนี้ใน npc/scripts_custom.conf)\n"
				+ "• เซิร์ฟใช้เวลาเครื่องเดียวกับ PC เสมอ — ถ้าเวลาไม่ตรง ให้ตั้งเวลา Windows (Settings > Time && language > ตั้งเวลาอัตโนมัติ)\n"
				+ "• ต้องปิดรอบกลางวัน/กลางคืนของเกมเอง: day_duration: 0 และ night_duration: 0 ใน conf/battle/misc.conf (ค่าปัจจุบันเป็น 0 แล้ว)\n"
				+ "• เปิดเซิร์ฟตอนไหนก็ได้ NPC จะตั้งกลางวัน/กลางคืนให้ตรงเวลาทันที"
		});
		root.Controls.Add(gHelp, 0, 2);

		clockTimer = new Timer { Interval = 1000 };
		clockTimer.Tick += delegate { ClockTick(); };
		clockTimer.Start();
		return timePage;
	}

	private void InitTimeTab()
	{
		StatusStrip strip = Controls.OfType<StatusStrip>().FirstOrDefault();
		if (strip != null)
		{
			stClock = new ToolStripStatusLabel("เวลา --:--:--") { ForeColor = Theme.Accent };
			strip.Items.Insert(0, stClock);
		}
		tabs.SelectedIndexChanged += delegate
		{
			if (tabs.SelectedTab == timePage)
				LoadDayNight(force: !dnLoaded);
		};
		ClockTick();
	}

	private static long UnixNow()
	{
		return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
	}

	private void ClockTick()
	{
		DateTime now = DateTime.Now;
		lblPcClock.Text = now.ToString("HH:mm:ss");
		lblPcDate.Text = ThaiDate(now) + "  (" + TimeZoneInfo.Local.DisplayName + ")";
		if (stClock != null)
			stClock.Text = "เวลา " + now.ToString("HH:mm:ss") + (lblDnState.Tag is string s ? "  " + s : string.Empty);

		clockTick++;
		if (clockTick % 5 == 0 && db.Connected)
			LoadDayNight(force: false);
	}

	private void EnsureDayNightTables()
	{
		db.Exec(DayNightTableSql);
		db.Exec(DayNightStateSql);
		db.Exec("INSERT IGNORE INTO `rox_daynight` (`id`,`enabled`,`day_min`,`night_min`,`announce`,`day_msg`,`night_msg`,`cmd`,`updated`) VALUES (1,1,420,1080,0,'" + Db.Esc(DefaultDayMsg) + "','" + Db.Esc(DefaultNightMsg) + "',0,NOW())");
	}

	private void LoadDayNight(bool force)
	{
		if (!db.Connected)
			return;
		try
		{
			EnsureDayNightTables();
			if (force || !dnLoaded)
			{
				DataTable c = db.Query("SELECT `enabled`,`day_min`,`night_min`,`announce`,`day_msg`,`night_msg` FROM `rox_daynight` WHERE `id`=1");
				if (c.Rows.Count > 0)
				{
					DataRow r = c.Rows[0];
					chkDnEnabled.Checked = r[0].ToString() == "1";
					dtDay.Value = DateTime.Today.AddMinutes(int.Parse(r[1].ToString()));
					dtNight.Value = DateTime.Today.AddMinutes(int.Parse(r[2].ToString()));
					chkDnAnnounce.Checked = r[3].ToString() == "1";
					if (r[4].ToString().Length > 0) txtDayMsg.Text = r[4].ToString();
					if (r[5].ToString().Length > 0) txtNightMsg.Text = r[5].ToString();
					dnLoaded = true;
				}
			}

			DataTable s = db.Query("SELECT `is_night`,`server_ts`,`server_time`,`hold`,UNIX_TIMESTAMP(`updated`),UNIX_TIMESTAMP() FROM `rox_daynight_state` WHERE `id`=1");
			if (s.Rows.Count == 0)
			{
				lblSrvClock.Text = "--:--:--";
				lblSrvSync.Text = "ยังไม่ได้รับเวลาจาก Map server — เปิด Map server และเช็คว่าโหลด daynight.txt แล้ว";
				lblSrvSync.ForeColor = Theme.Warn;
				lblDnState.Text = "ตอนนี้ในเกม: -";
				lblDnState.Tag = null;
				return;
			}
			DataRow st = s.Rows[0];
			bool night = st[0].ToString() == "1";
			long srvTs = long.Parse(st[1].ToString());
			int hold = int.Parse(st[3].ToString());
			long updTs = long.Parse(st[4].ToString());
			long dbNow = long.Parse(st[5].ToString());
			long age = dbNow - updTs;

			if (age > 20 || !map.Running)
			{
				lblSrvClock.Text = "--:--:--";
				lblSrvSync.Text = "Map server ไม่ได้รายงานเวลามา " + age + " วินาที (ปิดอยู่หรือยังไม่โหลด daynight.txt)";
				lblSrvSync.ForeColor = Theme.Warn;
			}
			else
			{
				// map-server clock "now" = its last report + time since that report
				long srvNow = srvTs + age;
				long diff = srvNow - UnixNow();
				DateTime srvLocal = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(srvNow).ToLocalTime();
				lblSrvClock.Text = srvLocal.ToString("HH:mm:ss");
				if (Math.Abs(diff) <= 5)
				{
					lblSrvSync.Text = "ตรงกับเครื่อง PC (ต่างกันไม่เกิน 5 วินาที)";
					lblSrvSync.ForeColor = Theme.Good;
				}
				else
				{
					lblSrvSync.Text = "ต่างจาก PC " + Math.Abs(diff) + " วินาที — Map server อาจค้าง หรือ MySQL อยู่คนละเครื่อง";
					lblSrvSync.ForeColor = Theme.Warn;
				}
			}

			string mode = hold >= 0 ? "สั่งเอง ค้างถึงรอบถัดไป" : (chkDnEnabled.Checked ? "ตามตารางเวลา" : "ระบบอัตโนมัติปิดอยู่");
			lblDnState.Text = "ตอนนี้ในเกม: " + (night ? "กลางคืน" : "กลางวัน") + "   (" + mode + ")";
			lblDnState.ForeColor = night ? Color.FromArgb(140, 160, 255) : Theme.Accent;
			lblDnState.Tag = night ? "กลางคืน" : "กลางวัน";
		}
		catch (Exception ex)
		{
			lblSrvSync.Text = "อ่านสถานะไม่ได้: " + ex.Message;
			lblSrvSync.ForeColor = Theme.Bad;
		}
	}

	private void SaveDayNight()
	{
		if (!NeedDb())
			return;
		int dmin = dtDay.Value.Hour * 60 + dtDay.Value.Minute;
		int nmin = dtNight.Value.Hour * 60 + dtNight.Value.Minute;
		if (dmin == nmin)
		{
			MessageBox.Show(this, "เวลากลางวันกับกลางคืนต้องไม่เท่ากัน", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}
		try
		{
			EnsureDayNightTables();
			db.Exec("UPDATE `rox_daynight` SET `enabled`=" + (chkDnEnabled.Checked ? 1 : 0) + ",`day_min`=" + dmin + ",`night_min`=" + nmin
				+ ",`announce`=" + (chkDnAnnounce.Checked ? 1 : 0) + ",`day_msg`='" + Db.Esc(txtDayMsg.Text.Trim()) + "',`night_msg`='" + Db.Esc(txtNightMsg.Text.Trim())
				+ "',`updated`=NOW() WHERE `id`=1");
			dnLoaded = true;
			MessageBox.Show(this, "บันทึกแล้ว — กลางวัน " + dtDay.Value.ToString("HH:mm") + " / กลางคืน " + dtNight.Value.ToString("HH:mm") + (chkDnEnabled.Checked ? string.Empty : "\n(ปิดระบบอัตโนมัติอยู่)") + "\nมีผลภายใน ~5 วินาที", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	/// 1 = day, 2 = night
	private void DayNightNow(int cmd)
	{
		if (!NeedDb())
			return;
		if (!map.Running)
		{
			MessageBox.Show(this, "Map server ยังไม่ได้เปิด", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		try
		{
			EnsureDayNightTables();
			db.Exec("UPDATE `rox_daynight` SET `cmd`=" + cmd + " WHERE `id`=1");
			stMsgSafe(cmd == 1 ? "สั่งกลางวันแล้ว (มีผลภายใน ~5 วินาที)" : "สั่งกลางคืนแล้ว (มีผลภายใน ~5 วินาที)");
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void stMsgSafe(string text)
	{
		if (stMsg != null)
			stMsg.Text = text;
	}
}
