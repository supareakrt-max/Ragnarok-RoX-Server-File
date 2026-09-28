// Ro-X: "AI Bot" tab — runs the bot brain (tools\aibot\run_brain.py), imports
// bots.sql, creates new bot characters and lists / deletes them.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ROManager;

internal partial class MainForm
{
	private const long BotAccountMin = 3000000;
	private const long BotAccountMax = 3999999;
	private const int BridgePort = 7100;

	private TabPage botPage;
	private Process botProc;
	private bool botUserStopped;
	private readonly ConcurrentQueue<string> botLogQ = new ConcurrentQueue<string>();
	private RichTextBox botLog;
	private Label lblBotState;
	private Label lblBotPython;
	private CheckBox chkBotAuto;
	private DataGridView gridBots;
	private NumericUpDown numBotCount;
	private NumericUpDown numBotMinLv;
	private NumericUpDown numBotMaxLv;
	private readonly List<KeyValuePair<CheckBox, string>> botJobChecks = new List<KeyValuePair<CheckBox, string>>();
	private readonly List<KeyValuePair<CheckBox, string>> botPersChecks = new List<KeyValuePair<CheckBox, string>>();
	private Timer botTimer;
	private int botTick;

	private string BotDir => serverDir == null ? null : Path.Combine(serverDir, "tools", "aibot");

	private bool BotRunning
	{
		get
		{
			try { return botProc != null && !botProc.HasExited; }
			catch { return false; }
		}
	}

	// ------------------------------------------------------------------
	// UI
	// ------------------------------------------------------------------
	private TabPage BuildBotTab()
	{
		botPage = new TabPage("AI Bot");
		TableLayoutPanel root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Padding = new Padding(8) };
		root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55f));
		root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45f));
		root.RowStyles.Add(new RowStyle(SizeType.Absolute, 250f));
		root.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		botPage.Controls.Add(root);

		// --- run / stop ---------------------------------------------------
		GroupBox gRun = new GroupBox { Text = "สมองกลบอท (run_brain.py)", Dock = DockStyle.Fill };
		FlowLayoutPanel fRun = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.TopDown, WrapContents = false };
		lblBotState = new Label { AutoSize = true, Font = new Font("Tahoma", 12f, FontStyle.Bold), ForeColor = Theme.Muted, Text = "หยุดอยู่", Margin = new Padding(3, 6, 3, 6) };
		FlowLayoutPanel fBtns = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
		Button bStart = Btn("เริ่มบอท", delegate { botUserStopped = false; Guard(StartBrain); }, 120);
		bStart.Height = 36;
		bStart.BackColor = Theme.BtnGo;
		Button bStop = Btn("หยุดบอท", delegate { botUserStopped = true; StopBrain(); }, 120);
		bStop.Height = 36;
		bStop.BackColor = Theme.BtnStop;
		Button bFolder = Btn("เปิดโฟลเดอร์บอท", delegate
		{
			if (BotDir != null && Directory.Exists(BotDir))
				Process.Start("explorer.exe", "\"" + BotDir + "\"");
		}, 140);
		bFolder.Height = 36;
		Button bConfig = Btn("แก้ config.json", delegate
		{
			string p = BotDir == null ? null : Path.Combine(BotDir, "config.json");
			if (p != null && File.Exists(p))
				Process.Start("notepad.exe", "\"" + p + "\"");
		}, 130);
		bConfig.Height = 36;
		fBtns.Controls.AddRange(new Control[] { bStart, bStop, bFolder, bConfig });
		chkBotAuto = new CheckBox { AutoSize = true, Text = "เริ่มบอทอัตโนมัติเมื่อ Map server พร้อม (พอร์ต " + BridgePort + ")", Margin = new Padding(3, 8, 3, 3) };
		chkBotAuto.CheckedChanged += delegate
		{
			if (ini == null) return;
			ini["BotAutoStart"] = chkBotAuto.Checked ? "1" : "0";
			Ini.Save(iniPath, ini);
			if (chkBotAuto.Checked) botUserStopped = false;
		};
		FlowLayoutPanel fPy = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
		lblBotPython = new Label { AutoSize = true, ForeColor = Theme.Muted, Text = "Python: -", Margin = new Padding(3, 9, 3, 3) };
		Button bPy = Btn("เลือก python.exe...", delegate { PickPython(); }, 140);
		fPy.Controls.AddRange(new Control[] { lblBotPython, bPy });
		Label note = new Label
		{
			AutoSize = true,
			MaximumSize = new Size(560, 0),
			ForeColor = Theme.Muted,
			Text = "ปิดบอทแล้วตัวละครบอทจะยืนนิ่งอยู่ในเกม เปิดใหม่แล้วทำงานต่อเอง\nต้องใช้ map-server.exe ที่มี AI bot bridge และติดตั้ง Python 3"
		};
		fRun.Controls.AddRange(new Control[] { lblBotState, fBtns, chkBotAuto, fPy, note });
		gRun.Controls.Add(fRun);
		root.Controls.Add(gRun, 0, 0);

		// --- create / import ------------------------------------------------
		GroupBox gNew = new GroupBox { Text = "สร้างไอดีบอท", Dock = DockStyle.Fill };
		TableLayoutPanel tNew = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 6 };
		tNew.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90f));
		tNew.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		foreach (float h in new[] { 32f, 32f, 54f, 32f, 38f })
			tNew.RowStyles.Add(new RowStyle(SizeType.Absolute, h));
		tNew.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		numBotCount = new NumericUpDown { Minimum = 1, Maximum = 200, Value = 10, Width = 70 };
		numBotMinLv = new NumericUpDown { Minimum = 1, Maximum = 99, Value = 1, Width = 60 };
		numBotMaxLv = new NumericUpDown { Minimum = 1, Maximum = 99, Value = 30, Width = 60 };
		FlowLayoutPanel fLv = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
		fLv.Controls.AddRange(new Control[] { numBotMinLv, new Label { Text = "ถึง", AutoSize = true, Margin = new Padding(3, 6, 3, 3) }, numBotMaxLv });
		FlowLayoutPanel fJobs = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = true, Margin = new Padding(0) };
		foreach (KeyValuePair<string, string> j in new[] {
			new KeyValuePair<string, string>("Swordman", "1"), new KeyValuePair<string, string>("Mage", "2"),
			new KeyValuePair<string, string>("Archer", "3"), new KeyValuePair<string, string>("Acolyte", "4"),
			new KeyValuePair<string, string>("Merchant", "5"), new KeyValuePair<string, string>("Thief", "6") })
		{
			CheckBox c = new CheckBox { Text = j.Key, AutoSize = true, Checked = true, Margin = new Padding(0, 4, 4, 0) };
			botJobChecks.Add(new KeyValuePair<CheckBox, string>(c, j.Value));
			fJobs.Controls.Add(c);
		}
		FlowLayoutPanel fPers = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
		foreach (KeyValuePair<string, string> p in new[] {
			new KeyValuePair<string, string>("hardcore (สายฟาร์ม)", "hardcore"),
			new KeyValuePair<string, string>("merchant (สายพ่อค้า)", "merchant"),
			new KeyValuePair<string, string>("chill (สายชิล)", "chill") })
		{
			CheckBox c = new CheckBox { Text = p.Key, AutoSize = true, Checked = true, Margin = new Padding(0, 4, 6, 0) };
			botPersChecks.Add(new KeyValuePair<CheckBox, string>(c, p.Value));
			fPers.Controls.Add(c);
		}
		tNew.Controls.Add(new Label { Text = "จำนวน", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 0);
		tNew.Controls.Add(numBotCount, 1, 0);
		tNew.Controls.Add(new Label { Text = "เลเวล", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 1);
		tNew.Controls.Add(fLv, 1, 1);
		tNew.Controls.Add(new Label { Text = "อาชีพ", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 2);
		tNew.Controls.Add(fJobs, 1, 2);
		tNew.Controls.Add(new Label { Text = "นิสัย", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 3);
		tNew.Controls.Add(fPers, 1, 3);
		FlowLayoutPanel fNewBtns = new FlowLayoutPanel { AutoSize = true, WrapContents = false, Margin = new Padding(0) };
		Button bCreate = Btn("สร้างบอทใหม่", delegate { Guard(CreateBots); }, 130);
		bCreate.BackColor = Theme.BtnSend;
		Button bImport = Btn("นำเข้า bots.sql...", delegate { Guard(ImportBotsSqlDialog); }, 140);
		fNewBtns.Controls.AddRange(new Control[] { bCreate, bImport });
		tNew.Controls.Add(fNewBtns, 0, 4);
		tNew.SetColumnSpan(fNewBtns, 2);
		tNew.Controls.Add(new Label
		{
			AutoSize = true,
			ForeColor = Theme.Muted,
			MaximumSize = new Size(470, 0),
			Text = "บอทเกิดที่ prontera 155,187 • ไอดีบอทใช้เลข 3000000 ขึ้นไป • สร้าง/นำเข้าแล้วระบบเพิ่มชื่อลง config.json ให้เอง (มีผลเมื่อเริ่มบอทใหม่)"
		}, 0, 5);
		tNew.SetColumnSpan(tNew.GetControlFromPosition(0, 5), 2);
		gNew.Controls.Add(tNew);
		root.Controls.Add(gNew, 1, 0);

		// --- bot list -------------------------------------------------------
		GroupBox gList = new GroupBox { Text = "ตัวละครบอท", Dock = DockStyle.Fill };
		TableLayoutPanel tList = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
		tList.RowStyles.Add(new RowStyle(SizeType.Absolute, 40f));
		tList.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
		FlowLayoutPanel fList = new FlowLayoutPanel { Dock = DockStyle.Fill, WrapContents = false };
		fList.Controls.Add(Btn("รีเฟรช", delegate { LoadBots(); }, 90));
		Button bDel = Btn("ลบบอทที่เลือก", delegate { Guard(DeleteSelectedBots); }, 130);
		bDel.BackColor = Theme.BtnStop;
		fList.Controls.Add(bDel);
		gridBots = Grid();
		gridBots.MultiSelect = true;
		tList.Controls.Add(fList, 0, 0);
		tList.Controls.Add(gridBots, 0, 1);
		gList.Controls.Add(tList);
		root.Controls.Add(gList, 0, 1);

		// --- log ------------------------------------------------------------
		GroupBox gLog = new GroupBox { Text = "Log บอท", Dock = DockStyle.Fill };
		botLog = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true, Font = MonoFont, BorderStyle = BorderStyle.None, WordWrap = false, BackColor = Theme.Surface, ForeColor = Theme.Text };
		gLog.Controls.Add(botLog);
		root.Controls.Add(gLog, 1, 1);

		botTimer = new Timer { Interval = 1000 };
		botTimer.Tick += delegate { BotTick(); };
		botTimer.Start();
		return botPage;
	}

	/// Runs a button action and shows errors as a message instead of crashing.
	private void Guard(Action action)
	{
		try
		{
			action();
		}
		catch (Exception ex)
		{
			Cursor = Cursors.Default;
			BotLog("[Tool][Error] " + ex.Message);
			MessageBox.Show(this, "เกิดข้อผิดพลาด:\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void InitBotTab()
	{
		chkBotAuto.Checked = ini != null && ini.TryGetValue("BotAutoStart", out string v) && v == "1";
		lblBotPython.Text = "Python: " + (FindPython() ?? "ไม่พบ (ติดตั้งจาก python.org)");
		tabs.SelectedIndexChanged += delegate
		{
			if (tabs.SelectedTab == botPage)
				LoadBots();
		};
		FormClosed += delegate { StopBrain(); };
	}

	// ------------------------------------------------------------------
	// brain process
	// ------------------------------------------------------------------
	private void BotLog(string line)
	{
		botLogQ.Enqueue(DateTime.Now.ToString("HH:mm:ss") + " " + line);
	}

	private void BotTick()
	{
		botTick++;
		FlushBotLog();

		bool running = BotRunning;
		lblBotState.Text = running ? "กำลังทำงาน (PID " + SafePid() + ")" : (botUserStopped ? "หยุดอยู่ (กดหยุดเอง)" : "หยุดอยู่");
		lblBotState.ForeColor = running ? Theme.Good : Theme.Muted;

		if (!running && chkBotAuto.Checked && !botUserStopped && botTick % 5 == 0 && map.Running && PortOpen(BridgePort))
		{
			BotLog("[Tool] Map server พร้อมแล้ว เริ่มบอทอัตโนมัติ");
			StartBrain();
		}
	}

	private int SafePid()
	{
		try { return botProc.Id; }
		catch { return 0; }
	}

	private void FlushBotLog()
	{
		if (botLogQ.IsEmpty || botLog == null)
			return;
		botLog.SuspendLayout();
		int n = 0;
		while (n++ < 500 && botLogQ.TryDequeue(out string line))
		{
			botLog.SelectionStart = botLog.TextLength;
			botLog.SelectionLength = 0;
			botLog.SelectionColor = line.Contains("ERROR") || line.Contains("[Error]") || line.Contains("Traceback") ? Theme.Bad
				: line.Contains("WARNING") || line.Contains("[Warning]") ? Theme.Warn
				: line.Contains("spawned") || line.Contains("[report") ? Theme.Good
				: Theme.Text;
			botLog.AppendText(line + "\n");
		}
		if (botLog.Lines.Length > 4000)
		{
			botLog.ReadOnly = false;
			botLog.Select(0, botLog.GetFirstCharIndexFromLine(1000));
			botLog.SelectedText = string.Empty;
			botLog.ReadOnly = true;
			botLog.SelectionStart = botLog.TextLength;
		}
		botLog.ScrollToCaret();
		botLog.ResumeLayout();
	}

	private static bool PortOpen(int port)
	{
		try
		{
			using (TcpClient c = new TcpClient())
			{
				IAsyncResult r = c.BeginConnect("127.0.0.1", port, null, null);
				bool ok = r.AsyncWaitHandle.WaitOne(250) && c.Connected;
				return ok;
			}
		}
		catch
		{
			return false;
		}
	}

	private string FindPython()
	{
		if (ini != null && ini.TryGetValue("PythonPath", out string p) && File.Exists(p))
			return p;
		List<string> found = new List<string>();
		foreach (string baseDir in new[] {
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "Python"),
			Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
			Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
			@"C:\" })
		{
			try
			{
				if (!Directory.Exists(baseDir))
					continue;
				foreach (string d in Directory.GetDirectories(baseDir, "Python3*"))
				{
					string exe = Path.Combine(d, "python.exe");
					if (File.Exists(exe))
						found.Add(exe);
				}
			}
			catch
			{
			}
		}
		if (found.Count > 0)
			return found.OrderByDescending(f => PyVersion(Path.GetFileName(Path.GetDirectoryName(f)))).First();
		foreach (string dir in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty).Split(';'))
		{
			try
			{
				string exe = Path.Combine(dir.Trim(), "python.exe");
				if (File.Exists(exe) && exe.IndexOf("WindowsApps", StringComparison.OrdinalIgnoreCase) < 0)
					return exe;
			}
			catch
			{
			}
		}
		return null;
	}

	private static int PyVersion(string dirName)
	{
		Match m = Regex.Match(dirName, @"Python3(\d+)");
		return m.Success ? int.Parse(m.Groups[1].Value) : 0;
	}

	private void PickPython()
	{
		using (OpenFileDialog d = new OpenFileDialog { Filter = "python.exe|python.exe", Title = "เลือก python.exe" })
		{
			if (d.ShowDialog(this) != DialogResult.OK)
				return;
			ini["PythonPath"] = d.FileName;
			Ini.Save(iniPath, ini);
			lblBotPython.Text = "Python: " + d.FileName;
		}
	}

	private ProcessStartInfo PythonStart(string args)
	{
		string py = FindPython();
		if (py == null)
			return null;
		ProcessStartInfo psi = new ProcessStartInfo(py, args)
		{
			WorkingDirectory = BotDir,
			UseShellExecute = false,
			CreateNoWindow = true,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			StandardOutputEncoding = Encoding.UTF8,
			StandardErrorEncoding = Encoding.UTF8
		};
		psi.EnvironmentVariables["PYTHONIOENCODING"] = "utf-8";
		psi.EnvironmentVariables["PYTHONUNBUFFERED"] = "1";
		return psi;
	}

	private bool CheckBotFiles(bool needConfig)
	{
		if (BotDir == null || !File.Exists(Path.Combine(BotDir, "run_brain.py")))
		{
			MessageBox.Show(this, "ไม่พบโฟลเดอร์บอท:\n" + (BotDir ?? "(ยังไม่ได้ตั้งโฟลเดอร์เซิร์ฟ)") + "\n\nแตกไฟล์ tools\\aibot ลงในโฟลเดอร์ Server ก่อน", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return false;
		}
		if (FindPython() == null)
		{
			MessageBox.Show(this, "ไม่พบ Python\n\nติดตั้งจาก https://www.python.org/downloads/ แล้วติ๊ก \"Add python.exe to PATH\"\nหรือกด \"เลือก python.exe...\"", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return false;
		}
		if (needConfig && !File.Exists(Path.Combine(BotDir, "config.json")))
		{
			MessageBox.Show(this, "ยังไม่มี config.json ในโฟลเดอร์บอท\n\nสร้างบอทหรือนำเข้า bots.sql ก่อน (ระบบจะสร้าง config.json ให้)", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
			return false;
		}
		return true;
	}

	private void StartBrain()
	{
		if (BotRunning)
		{
			BotLog("[Tool] บอททำงานอยู่แล้ว");
			return;
		}
		if (!CheckBotFiles(needConfig: true))
		{
			botUserStopped = true;
			return;
		}
		ProcessStartInfo psi = PythonStart("-u run_brain.py -c config.json");
		Process p = new Process { StartInfo = psi, EnableRaisingEvents = true };
		DataReceivedEventHandler h = delegate(object s, DataReceivedEventArgs e)
		{
			if (e.Data != null)
				botLogQ.Enqueue(e.Data);
		};
		p.OutputDataReceived += h;
		p.ErrorDataReceived += h;
		p.Exited += delegate
		{
			int code = 0;
			try { code = p.ExitCode; } catch { }
			BotLog("[Tool] สมองกลบอทปิดแล้ว (exit code " + code + ")");
		};
		try
		{
			p.Start();
			p.BeginOutputReadLine();
			p.BeginErrorReadLine();
			botProc = p;
			BotLog("[Tool] เริ่มสมองกลบอท (PID " + p.Id + ") ด้วย " + psi.FileName);
		}
		catch (Exception ex)
		{
			BotLog("[Tool][Error] เริ่มบอทไม่ได้: " + ex.Message);
		}
	}

	private void StopBrain()
	{
		if (!BotRunning)
			return;
		try
		{
			botProc.Kill();
			botProc.WaitForExit(3000);
			BotLog("[Tool] หยุดสมองกลบอทแล้ว (ตัวละครบอทยืนนิ่งในเกม)");
		}
		catch (Exception ex)
		{
			BotLog("[Tool][Error] หยุดบอทไม่ได้: " + ex.Message);
		}
	}

	/// Runs a short python helper and returns (exit code, stdout+stderr).
	private KeyValuePair<int, string> RunPy(string args, int timeoutMs = 60000)
	{
		ProcessStartInfo psi = PythonStart(args);
		if (psi == null)
			return new KeyValuePair<int, string>(-1, "python not found");
		Cursor = Cursors.WaitCursor;
		try
		{
			using (Process p = Process.Start(psi))
			{
				string err = null;
				p.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e) { if (e.Data != null) err += e.Data + "\n"; };
				p.BeginErrorReadLine();
				string outText = p.StandardOutput.ReadToEnd();
				if (!p.WaitForExit(timeoutMs))
				{
					try { p.Kill(); } catch { }
					return new KeyValuePair<int, string>(-1, "timeout");
				}
				p.WaitForExit();
				return new KeyValuePair<int, string>(p.ExitCode, outText + err);
			}
		}
		catch (Exception ex)
		{
			return new KeyValuePair<int, string>(-1, ex.Message);
		}
		finally
		{
			Cursor = Cursors.Default;
		}
	}

	// ------------------------------------------------------------------
	// bots.sql import / create
	// ------------------------------------------------------------------
	private void ImportBotsSqlDialog()
	{
		if (!NeedDb() || !CheckBotFiles(needConfig: false))
			return;
		using (OpenFileDialog d = new OpenFileDialog { Filter = "SQL (*.sql)|*.sql", Title = "เลือกไฟล์ bots.sql", InitialDirectory = BotDir })
		{
			if (d.ShowDialog(this) != DialogResult.OK)
				return;
			ImportBotsSql(d.FileName, confirm: true);
		}
	}

	private static List<string> SplitSql(string path)
	{
		List<string> stmts = new List<string>();
		StringBuilder cur = new StringBuilder();
		foreach (string raw in File.ReadAllLines(path, Encoding.UTF8))
		{
			string line = raw.Trim();
			if (line.Length == 0 || line.StartsWith("--"))
				continue;
			cur.Append(line).Append(' ');
			if (line.EndsWith(";"))
			{
				stmts.Add(cur.ToString().Trim().TrimEnd(';'));
				cur.Clear();
			}
		}
		if (cur.Length > 0)
			stmts.Add(cur.ToString().Trim());
		return stmts;
	}

	private static readonly Regex SqlLoginIds = new Regex(@"INSERT INTO `login` \([^)]*\) VALUES \((\d+),", RegexOptions.IgnoreCase);
	private static readonly Regex SqlCharRow = new Regex(@"INSERT INTO `char` \([^)]*\) VALUES \((\d+),(\d+),\d+,'((?:[^'\\]|\\.)*)'", RegexOptions.IgnoreCase);

	private bool ImportBotsSql(string path, bool confirm)
	{
		List<string> stmts;
		try
		{
			stmts = SplitSql(path);
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, "อ่านไฟล์ไม่ได้: " + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			return false;
		}
		List<string> accIds = stmts.Select(s => SqlLoginIds.Match(s)).Where(m => m.Success).Select(m => m.Groups[1].Value).ToList();
		List<Match> chars = stmts.Select(s => SqlCharRow.Match(s)).Where(m => m.Success).ToList();
		if (chars.Count == 0)
		{
			MessageBox.Show(this, "ไฟล์นี้ไม่มีตัวละครบอท (INSERT INTO `char`)", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return false;
		}

		// what already exists?
		List<string> takenAcc = new List<string>();
		List<string> takenNames = new List<string>();
		try
		{
			if (accIds.Count > 0)
				foreach (DataRow r in db.Query("SELECT `account_id` FROM `login` WHERE `account_id` IN (" + string.Join(",", accIds) + ")").Rows)
					takenAcc.Add(r[0].ToString());
			string names = string.Join(",", chars.Select(m => "'" + Db.Esc(Regex.Unescape(m.Groups[3].Value)) + "'"));
			string cids = string.Join(",", chars.Select(m => m.Groups[1].Value));
			foreach (DataRow r in db.Query("SELECT `name` FROM `char` WHERE `name` IN (" + names + ") OR `char_id` IN (" + cids + ")").Rows)
				takenNames.Add(r[0].ToString());
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			return false;
		}

		bool allExist = takenNames.Count >= chars.Count && takenAcc.Count >= accIds.Count;
		if (allExist)
		{
			SyncBotConfig(path);
			MessageBox.Show(this, "บอทในไฟล์นี้อยู่ในฐานข้อมูลครบแล้ว (" + chars.Count + " ตัว)\nอัปเดตรายชื่อใน config.json ให้แล้ว", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
			LoadBots();
			return true;
		}
		if (takenNames.Count > 0 || takenAcc.Count > 0)
		{
			MessageBox.Show(this, "นำเข้าไม่ได้: ไอดีหรือชื่อซ้ำกับที่มีอยู่แล้ว\n\nชื่อ/ตัวละครซ้ำ: " + string.Join(", ", takenNames.Take(15)) + (takenAcc.Count > 0 ? "\naccount_id ซ้ำ: " + string.Join(", ", takenAcc.Take(15)) : string.Empty) + "\n\nใช้ปุ่ม \"สร้างบอทใหม่\" แทน (ระบบเลือกเลขไอดีที่ว่างให้เอง)", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return false;
		}
		if (confirm && MessageBox.Show(this, "นำเข้าบอท " + chars.Count + " ตัวจาก\n" + path + " ?", Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
			return false;

		int done = 0;
		try
		{
			foreach (string s in stmts)
			{
				db.Exec(s);
				done++;
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, "นำเข้าไม่สำเร็จที่คำสั่งที่ " + (done + 1) + "/" + stmts.Count + ":\n" + ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			LoadBots();
			return false;
		}
		BotLog("[Tool] นำเข้าบอท " + chars.Count + " ตัวจาก " + Path.GetFileName(path));
		SyncBotConfig(path);
		LoadBots();
		return true;
	}

	private void SyncBotConfig(string sqlPath)
	{
		KeyValuePair<int, string> r = RunPy("manage.py sync --sql \"" + sqlPath + "\"");
		if (r.Key == 0)
		{
			Match m = Regex.Match(r.Value, "\"added\": \\[([^\\]]*)\\]");
			int added = m.Success && m.Groups[1].Value.Trim().Length > 0 ? m.Groups[1].Value.Split(',').Length : 0;
			BotLog("[Tool] config.json: เพิ่มชื่อบอท " + added + " ตัว" + (BotRunning && added > 0 ? " — กดหยุดแล้วเริ่มบอทใหม่เพื่อให้บอทใหม่ออนไลน์" : string.Empty));
		}
		else
		{
			BotLog("[Tool][Error] อัปเดต config.json ไม่ได้: " + r.Value.Trim());
		}
	}

	private void CreateBots()
	{
		if (!NeedDb() || !CheckBotFiles(needConfig: false))
			return;
		string jobs = string.Join(",", botJobChecks.Where(k => k.Key.Checked).Select(k => k.Value));
		string pers = string.Join(",", botPersChecks.Where(k => k.Key.Checked).Select(k => k.Value));
		if (jobs.Length == 0 || pers.Length == 0)
		{
			MessageBox.Show(this, "เลือกอาชีพและนิสัยอย่างน้อยอย่างละ 1", Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
			return;
		}
		int minLv = (int)Math.Min(numBotMinLv.Value, numBotMaxLv.Value);
		int maxLv = (int)Math.Max(numBotMinLv.Value, numBotMaxLv.Value);
		int count = (int)numBotCount.Value;

		long nextAcc, nextChar;
		try
		{
			nextAcc = Math.Max(BotAccountMin, Convert.ToInt64(db.Scalar("SELECT IFNULL(MAX(`account_id`),0)+1 FROM `login` WHERE `account_id` BETWEEN " + BotAccountMin + " AND " + BotAccountMax)));
			nextChar = Math.Max(BotAccountMin, Convert.ToInt64(db.Scalar("SELECT IFNULL(MAX(`char_id`),0)+1 FROM `char` WHERE `char_id` BETWEEN " + BotAccountMin + " AND " + BotAccountMax)));
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
			return;
		}

		string outDir = Path.Combine(BotDir, "generated");
		Directory.CreateDirectory(outDir);
		string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
		string sql = Path.Combine(outDir, "bots_" + stamp + ".sql");
		string json = Path.Combine(outDir, "bots_" + stamp + ".json");

		// retry with another seed if a generated name is already taken
		for (int attempt = 0; attempt < 5; attempt++)
		{
			KeyValuePair<int, string> r = RunPy("create_bots.py --count " + count + " --start-account-id " + nextAcc + " --start-char-id " + nextChar
				+ " --min-level " + minLv + " --max-level " + maxLv + " --jobs " + jobs + " --personalities " + pers
				+ " --seed " + (Environment.TickCount + attempt * 7919) + " --out \"" + sql + "\" --config-out \"" + json + "\"");
			if (r.Key != 0 || !File.Exists(sql))
			{
				MessageBox.Show(this, "สร้างไฟล์บอทไม่สำเร็จ:\n" + r.Value, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
				return;
			}
			List<string> names = SplitSql(sql).Select(s => SqlCharRow.Match(s)).Where(m => m.Success).Select(m => "'" + Db.Esc(Regex.Unescape(m.Groups[3].Value)) + "'").ToList();
			object taken = db.Scalar("SELECT COUNT(*) FROM `char` WHERE `name` IN (" + string.Join(",", names) + ")");
			if (Convert.ToInt32(taken) == 0)
			{
				if (ImportBotsSql(sql, confirm: false))
					MessageBox.Show(this, "สร้างบอทใหม่ " + count + " ตัวแล้ว\n\nไฟล์: " + sql + (BotRunning ? "\n\nกด \"หยุดบอท\" แล้ว \"เริ่มบอท\" เพื่อให้บอทใหม่ออนไลน์" : string.Empty), Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
				return;
			}
		}
		MessageBox.Show(this, "สุ่มชื่อบอทแล้วซ้ำกับตัวละครที่มีอยู่หลายครั้ง ลองลดจำนวนแล้วสร้างใหม่", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
	}

	// ------------------------------------------------------------------
	// bot list
	// ------------------------------------------------------------------
	private HashSet<string> ConfigBotNames()
	{
		HashSet<string> set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			string p = Path.Combine(BotDir, "config.json");
			if (!File.Exists(p))
				return set;
			string text = File.ReadAllText(p, Encoding.UTF8);
			int i = text.IndexOf("\"bots\"", StringComparison.Ordinal);
			if (i < 0)
				return set;
			foreach (Match m in Regex.Matches(text.Substring(i), "\"name\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\""))
				set.Add(Regex.Unescape(m.Groups[1].Value));
		}
		catch
		{
		}
		return set;
	}

	private void LoadBots()
	{
		if (!db.Connected || gridBots == null)
			return;
		try
		{
			// ASCII aliases only: Db reads column names as ANSI, Thai headers are set below
			DataTable t = db.Query("SELECT c.`char_id` AS `ID`, c.`name` AS `bname`, c.`class` AS `bjob`, c.`base_level` AS `Base Lv`, c.`job_level` AS `Job Lv`, c.`zeny` AS `Zeny`, c.`last_map` AS `bmap`, IF(c.`online`=1,'ON','') AS `bonline`, c.`account_id` FROM `char` c WHERE c.`account_id` BETWEEN " + BotAccountMin + " AND " + BotAccountMax + " ORDER BY c.`char_id`");
			t.Columns["bname"].ColumnName = "ชื่อ";
			t.Columns["bmap"].ColumnName = "แมพ";
			t.Columns["bonline"].ColumnName = "ออนไลน์";
			HashSet<string> cfg = ConfigBotNames();
			t.Columns.Add("อาชีพ", typeof(string)).SetOrdinal(2);
			t.Columns.Add("ใน config", typeof(string));
			foreach (DataRow r in t.Rows)
			{
				long cls;
				r["อาชีพ"] = long.TryParse(r["bjob"].ToString(), out cls) ? Jobs.Name(cls) : r["bjob"].ToString();
				r["ใน config"] = cfg.Contains(r["ชื่อ"].ToString()) ? "มี" : "-";
			}
			gridBots.DataSource = t;
			foreach (string hidden in new[] { "account_id", "bjob" })
				if (gridBots.Columns.Contains(hidden))
					gridBots.Columns[hidden].Visible = false;
			int online = t.Rows.Cast<DataRow>().Count(r => r["ออนไลน์"].ToString() == "ON");
			botPage.Text = "AI Bot (" + online + "/" + t.Rows.Count + ")";
		}
		catch (Exception ex)
		{
			BotLog("[Tool][Error] โหลดรายชื่อบอทไม่ได้: " + ex.Message);
		}
	}

	private void DeleteSelectedBots()
	{
		if (!NeedDb() || gridBots.SelectedRows.Count == 0)
			return;
		List<DataGridViewRow> rows = gridBots.SelectedRows.Cast<DataGridViewRow>().ToList();
		if (rows.Any(r => r.Cells["ออนไลน์"].Value?.ToString() == "ON"))
		{
			MessageBox.Show(this, "มีบอทที่ยังออนไลน์อยู่\n\nกด \"หยุดบอท\" แล้วรีสตาร์ท Map server (หรือรอให้บอทออฟไลน์) ก่อนลบ", Text, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			return;
		}
		string names = string.Join(", ", rows.Select(r => r.Cells["ชื่อ"].Value?.ToString()));
		if (MessageBox.Show(this, "ลบบอท " + rows.Count + " ตัว (ตัวละคร + ไอดี + ของ) ถาวร?\n\n" + names, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
			return;
		try
		{
			foreach (DataGridViewRow r in rows)
			{
				long cid = Convert.ToInt64(r.Cells["ID"].Value);
				long aid = Convert.ToInt64(r.Cells["account_id"].Value);
				if (aid < BotAccountMin || aid > BotAccountMax)
					continue; // never touch real players
				foreach (string table in new[] { "inventory", "cart_inventory", "skill", "char_reg_num", "char_reg_str", "sc_data", "hotkey", "quest", "achievement" })
				{
					try { db.Exec("DELETE FROM `" + table + "` WHERE `char_id`=" + cid); }
					catch { }
				}
				db.Exec("DELETE FROM `char` WHERE `char_id`=" + cid + " AND `account_id`=" + aid);
				if (Convert.ToInt32(db.Scalar("SELECT COUNT(*) FROM `char` WHERE `account_id`=" + aid)) == 0)
					db.Exec("DELETE FROM `login` WHERE `account_id`=" + aid);
			}
		}
		catch (Exception ex)
		{
			MessageBox.Show(this, ex.Message, Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
		RunPy("manage.py remove --names \"" + string.Join(",", rows.Select(r => r.Cells["ชื่อ"].Value?.ToString())) + "\"");
		BotLog("[Tool] ลบบอท: " + names);
		LoadBots();
	}
}
