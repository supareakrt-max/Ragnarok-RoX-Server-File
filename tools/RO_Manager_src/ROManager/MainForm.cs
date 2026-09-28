using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.Layout;

namespace ROManager;

internal partial class MainForm : Form
{
	private class MailItem
	{
		public long Id;

		public int Amount;

		public int Refine;
	}

	private enum EK
	{
		Text,
		Int,
		Sex,
		Group,
		Job,
		Pass,
		Map
	}

	private class EditCol
	{
		public string Db;

		public EK Kind;

		public long Min;

		public long Max = long.MaxValue;

		public int MaxBytes = 23;

		public EditCol(string db, EK kind)
		{
			Db = db;
			Kind = kind;
		}
	}

	private class EditSpec
	{
		public string Table;

		public string KeyCol;

		public string KeyDb;

		public Dictionary<string, EditCol> Cols = new Dictionary<string, EditCol>();

		public Func<DataGridViewRow, string> LockReason;
	}

	private class Opt
	{
		public object Key { get; set; }

		public string Name { get; set; }
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	internal struct _003CWaitMySql_003Ec__async0 : IAsyncStateMachine
	{
		internal int _003Ci_003E__1;

		internal int seconds;

		internal MainForm _0024this;

		internal AsyncTaskMethodBuilder<bool> _0024builder;

		internal int _0024PC;

		private TaskAwaiter<bool> _0024awaiter0;

		private TaskAwaiter _0024awaiter1;

		public void MoveNext()
		{
			uint num = (uint)_0024PC;
			_0024PC = -1;
			bool result;
			try
			{
				switch (num)
				{
				default:
					return;
				case 0u:
					_003Ci_003E__1 = 0;
					goto IL_00f3;
				case 1u:
					if (_0024awaiter0.GetResult())
					{
						result = true;
						break;
					}
					_0024awaiter1 = Task.Delay(500).GetAwaiter();
					if (!_0024awaiter1.IsCompleted)
					{
						_0024PC = 2;
						_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter1, ref this);
						return;
					}
					goto case 2u;
				case 2u:
					{
						_0024awaiter1.GetResult();
						_003Ci_003E__1++;
						goto IL_00f3;
					}
					IL_00f3:
					if (_003Ci_003E__1 < seconds * 2)
					{
						MainForm _0024self = _0024this;
						_0024awaiter0 = Task.Run(() => _0024self.MySqlUp()).GetAwaiter();
						if (!_0024awaiter0.IsCompleted)
						{
							_0024PC = 1;
							_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter0, ref this);
							return;
						}
						goto case 1u;
					}
					result = false;
					break;
				}
			}
			catch (Exception exception)
			{
				_0024PC = -1;
				_0024builder.SetException(exception);
				return;
			}
			_0024PC = -1;
			_0024builder.SetResult(result);
		}

		[DebuggerHidden]
		public void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			_0024builder.SetStateMachine(stateMachine);
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	internal struct _003CStopOne_003Ec__async1 : IAsyncStateMachine
	{
		internal ServerProc s;

		internal MainForm _0024this;

		internal AsyncVoidMethodBuilder _0024builder;

		internal int _0024PC;

		private TaskAwaiter _0024awaiter0;

		public void MoveNext()
		{
			uint num = (uint)_0024PC;
			_0024PC = -1;
			try
			{
				switch (num)
				{
				default:
					return;
				case 0u:
					_0024awaiter0 = s.Stop().GetAwaiter();
					if (!_0024awaiter0.IsCompleted)
					{
						_0024PC = 1;
						_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter0, ref this);
						return;
					}
					break;
				case 1u:
					break;
				}
				_0024awaiter0.GetResult();
				_0024this.RefreshStatus();
			}
			catch (Exception exception)
			{
				_0024PC = -1;
				_0024builder.SetException(exception);
				return;
			}
			_0024PC = -1;
			_0024builder.SetResult();
		}

		[DebuggerHidden]
		public void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			_0024builder.SetStateMachine(stateMachine);
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	internal struct _003CStartAll_003Ec__async2 : IAsyncStateMachine
	{
		internal ServerProc[] _0024locvar0;

		internal int _0024locvar1;

		internal ServerProc _003Cs_003E__1;

		internal MainForm _0024this;

		internal AsyncTaskMethodBuilder _0024builder;

		internal int _0024PC;

		private TaskAwaiter<bool> _0024awaiter0;

		private TaskAwaiter _0024awaiter1;

		public void MoveNext()
		{
			uint num = (uint)_0024PC;
			_0024PC = -1;
			bool flag = false;
			try
			{
				switch (num)
				{
				default:
					return;
				case 0u:
					if (!_0024this.busy)
					{
						_0024this.busy = true;
						num = 4294967293u;
						break;
					}
					goto end_IL_0010;
				case 1u:
				case 2u:
					break;
				}
				try
				{
					switch (num)
					{
					default:
						_0024this.logTabs.SelectedIndex = 4;
						if (!_0024this.MySqlUp())
						{
							_0024this.StartMySql();
							_0024this.tool.Add("[Tool] รอ MySQL ...");
							_0024awaiter0 = _0024this.WaitMySql(40).GetAwaiter();
							if (!_0024awaiter0.IsCompleted)
							{
								_0024PC = 1;
								flag = true;
								_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter0, ref this);
								return;
							}
							goto case 1u;
						}
						goto IL_010e;
					case 1u:
						if (!_0024awaiter0.GetResult())
						{
							_0024this.tool.Add("[Tool][Error] MySQL ไม\u0e48ตอบสนอง หย\u0e38ดการเป\u0e34ดเซ\u0e34ร\u0e4cฟ");
							break;
						}
						goto IL_010e;
					case 2u:
						{
							_0024awaiter1.GetResult();
							goto IL_0217;
						}
						IL_0225:
						if (_0024locvar1 < _0024locvar0.Length)
						{
							_003Cs_003E__1 = _0024locvar0[_0024locvar1];
							if (_003Cs_003E__1 != _0024this.web || _0024this.chkWeb.Checked)
							{
								_003Cs_003E__1.AutoRestart = _0024this.chkAutoRestart.Checked;
								if (!_003Cs_003E__1.Running)
								{
									_003Cs_003E__1.Start();
									_0024awaiter1 = Task.Delay(1500).GetAwaiter();
									if (!_0024awaiter1.IsCompleted)
									{
										_0024PC = 2;
										flag = true;
										_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter1, ref this);
										return;
									}
									goto case 2u;
								}
							}
							goto IL_0217;
						}
						_0024this.ShowLog(_0024this.map);
						_0024this.tool.Add("[Tool] เป\u0e34ดเซ\u0e34ร\u0e4cฟครบแล\u0e49ว");
						break;
						IL_0217:
						_0024locvar1++;
						goto IL_0225;
						IL_010e:
						_0024this.tool.Add("[Tool] MySQL พร\u0e49อม");
						_0024this.TryConnectDb(log: true);
						_0024locvar0 = _0024this.servers;
						_0024locvar1 = 0;
						goto IL_0225;
					}
				}
				finally
				{
					if (!flag)
					{
						_0024this.busy = false;
						_0024this.RefreshStatus();
					}
				}
				end_IL_0010:;
			}
			catch (Exception exception)
			{
				_0024PC = -1;
				_0024builder.SetException(exception);
				return;
			}
			_0024PC = -1;
			_0024builder.SetResult();
		}

		[DebuggerHidden]
		public void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			_0024builder.SetStateMachine(stateMachine);
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	internal struct _003CStopAll_003Ec__async3 : IAsyncStateMachine
	{
		internal MainForm _0024this;

		internal AsyncTaskMethodBuilder _0024builder;

		internal int _0024PC;

		private TaskAwaiter _0024awaiter0;

		public void MoveNext()
		{
			uint num = (uint)_0024PC;
			_0024PC = -1;
			bool flag = false;
			try
			{
				switch (num)
				{
				default:
					return;
				case 0u:
					if (!_0024this.busy)
					{
						_0024this.busy = true;
						num = 4294967293u;
						break;
					}
					goto end_IL_0010;
				case 1u:
				case 2u:
				case 3u:
					break;
				}
				try
				{
					switch (num)
					{
					default:
						_0024this.logTabs.SelectedIndex = 4;
						_0024this.tool.Add("[Tool] กำล\u0e31งป\u0e34ดเซ\u0e34ร\u0e4cฟ (เซฟข\u0e49อม\u0e39ลก\u0e48อนป\u0e34ด) ...");
						_0024awaiter0 = Task.WhenAll(_0024this.web.Stop(), _0024this.map.Stop()).GetAwaiter();
						if (!_0024awaiter0.IsCompleted)
						{
							_0024PC = 1;
							flag = true;
							_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter0, ref this);
							return;
						}
						goto case 1u;
					case 1u:
						_0024awaiter0.GetResult();
						_0024awaiter0 = _0024this.chr.Stop().GetAwaiter();
						if (!_0024awaiter0.IsCompleted)
						{
							_0024PC = 2;
							flag = true;
							_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter0, ref this);
							return;
						}
						goto case 2u;
					case 2u:
						_0024awaiter0.GetResult();
						_0024awaiter0 = _0024this.login.Stop().GetAwaiter();
						if (!_0024awaiter0.IsCompleted)
						{
							_0024PC = 3;
							flag = true;
							_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter0, ref this);
							return;
						}
						break;
					case 3u:
						break;
					}
					_0024awaiter0.GetResult();
					_0024this.tool.Add("[Tool] ป\u0e34ดเซ\u0e34ร\u0e4cฟครบแล\u0e49ว (MySQL ย\u0e31งเป\u0e34ดอย\u0e39\u0e48)");
				}
				finally
				{
					if (!flag)
					{
						_0024this.busy = false;
						_0024this.RefreshStatus();
					}
				}
				end_IL_0010:;
			}
			catch (Exception exception)
			{
				_0024PC = -1;
				_0024builder.SetException(exception);
				return;
			}
			_0024PC = -1;
			_0024builder.SetResult();
		}

		[DebuggerHidden]
		public void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			_0024builder.SetStateMachine(stateMachine);
		}
	}

	[CompilerGenerated]
	private sealed class _003COnCrashed_003Ec__AnonStorey10
	{
		[StructLayout(LayoutKind.Auto)]
		internal struct _003COnCrashed_003Ec__asyncF : IAsyncStateMachine
		{
			internal _003COnCrashed_003Ec__AnonStorey10 _003C_003Ef__ref_002416;

			internal AsyncVoidMethodBuilder _0024builder;

			internal int _0024PC;

			private TaskAwaiter _0024awaiter0;

			public void MoveNext()
			{
				uint num = (uint)_0024PC;
				_0024PC = -1;
				try
				{
					switch (num)
					{
					default:
						return;
					case 0u:
						_003C_003Ef__ref_002416.s.Add("[Tool][Error] " + _003C_003Ef__ref_002416.s.Title + " หย\u0e38ดทำงานเอง (exit code " + _003C_003Ef__ref_002416.code + ")");
						if (!_003C_003Ef__ref_002416._0024this.chkAutoRestart.Checked)
						{
							break;
						}
						_003C_003Ef__ref_002416.s.Add("[Tool] จะเป\u0e34ดใหม\u0e48ใน 5 ว\u0e34นาท\u0e35 ...");
						_0024awaiter0 = Task.Delay(5000).GetAwaiter();
						if (!_0024awaiter0.IsCompleted)
						{
							_0024PC = 1;
							_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter0, ref this);
							return;
						}
						goto case 1u;
					case 1u:
						_0024awaiter0.GetResult();
						if (!_003C_003Ef__ref_002416.s.Running)
						{
							_003C_003Ef__ref_002416.s.Start();
						}
						break;
					}
					_003C_003Ef__ref_002416._0024this.RefreshStatus();
				}
				catch (Exception exception)
				{
					_0024PC = -1;
					_0024builder.SetException(exception);
					return;
				}
				_0024PC = -1;
				_0024builder.SetResult();
			}

			[DebuggerHidden]
			public void SetStateMachine(IAsyncStateMachine stateMachine)
			{
				_0024builder.SetStateMachine(stateMachine);
			}
		}

		internal ServerProc s;

		internal int code;

		internal MainForm _0024this;

		[AsyncStateMachine(typeof(_003COnCrashed_003Ec__asyncF))]
		internal void _003C_003Em__0()
		{
			_003COnCrashed_003Ec__asyncF stateMachine = default(_003COnCrashed_003Ec__asyncF);
			stateMachine._003C_003Ef__ref_002416 = this;
			stateMachine._0024builder = AsyncVoidMethodBuilder.Create();
			stateMachine._0024builder.Start(ref stateMachine);
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	internal struct _003CRefreshStatus_003Ec__async4 : IAsyncStateMachine
	{
		internal _003C_003E__AnonType1<bool, _003C_003E__AnonType0<string, bool, bool>[]> _003Cr_003E__1;

		internal _003C_003E__AnonType0<string, bool, bool>[] _0024locvar0;

		internal int _0024locvar1;

		internal MainForm _0024this;

		internal AsyncVoidMethodBuilder _0024builder;

		internal int _0024PC;

		private TaskAwaiter<_003C_003E__AnonType1<bool, _003C_003E__AnonType0<string, bool, bool>[]>> _0024awaiter0;

		public void MoveNext()
		{
			uint num = (uint)_0024PC;
			_0024PC = -1;
			bool flag = false;
			try
			{
				switch (num)
				{
				default:
					return;
				case 0u:
					if (_0024this.statusBusy || _0024this.serverDir == null)
					{
						return;
					}
					_0024this.statusBusy = true;
					num = 4294967293u;
					break;
				case 1u:
					break;
				}
				try
				{
					switch (num)
					{
					default:
						MainForm _0024self = _0024this;
						_0024awaiter0 = Task.Run(() => new _003C_003E__AnonType1<bool, _003C_003E__AnonType0<string, bool, bool>[]>(
							_0024self.MySqlUp(),
							_0024self.servers.Select((ServerProc s) => new _003C_003E__AnonType0<string, bool, bool>(
								s.Title,
								s.OwnRunning,
								s.External().Length > 0)).ToArray())).GetAwaiter();
						if (!_0024awaiter0.IsCompleted)
						{
							_0024PC = 1;
							flag = true;
							_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter0, ref this);
							return;
						}
						break;
					case 1u:
						break;
					}
					_003Cr_003E__1 = _0024awaiter0.GetResult();
					_0024this.SetStatus("MySQL", _003Cr_003E__1.my, external: false);
					_0024locvar0 = _003Cr_003E__1.st;
					for (_0024locvar1 = 0; _0024locvar1 < _0024locvar0.Length; _0024locvar1++)
					{
						var anon = _0024locvar0[_0024locvar1];
						_0024this.SetStatus(anon.Title, anon.own || anon.ext, anon.ext && !anon.own);
					}
					if (!_003Cr_003E__1.my && _0024this.db.Connected)
					{
						_0024this.db.Close();
					}
					if (_003Cr_003E__1.my && !_0024this.db.Connected)
					{
						_0024this.TryConnectDb(log: false);
					}
					_0024this.UpdateDbLabel();
				}
				catch
				{
				}
				finally
				{
					if (!flag)
					{
						_0024this.statusBusy = false;
					}
				}
			}
			catch (Exception exception)
			{
				_0024PC = -1;
				_0024builder.SetException(exception);
				return;
			}
			_0024PC = -1;
			_0024builder.SetResult();
		}

		[DebuggerHidden]
		public void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			_0024builder.SetStateMachine(stateMachine);
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	internal struct _003CBuildServerTab_003Ec__async6 : IAsyncStateMachine
	{
		internal MainForm _0024this;

		internal AsyncVoidMethodBuilder _0024builder;

		internal int _0024PC;

		private TaskAwaiter _0024awaiter0;

		public void MoveNext()
		{
			uint num = (uint)_0024PC;
			_0024PC = -1;
			try
			{
				switch (num)
				{
				default:
					return;
				case 0u:
					_0024awaiter0 = _0024this.StartAll().GetAwaiter();
					if (!_0024awaiter0.IsCompleted)
					{
						_0024PC = 1;
						_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter0, ref this);
						return;
					}
					break;
				case 1u:
					break;
				}
				_0024awaiter0.GetResult();
			}
			catch (Exception exception)
			{
				_0024PC = -1;
				_0024builder.SetException(exception);
			}
		}

		[DebuggerHidden]
		public void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			_0024builder.SetStateMachine(stateMachine);
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	internal struct _003CBuildServerTab_003Ec__async8 : IAsyncStateMachine
	{
		internal MainForm _0024this;

		internal AsyncVoidMethodBuilder _0024builder;

		internal int _0024PC;

		private TaskAwaiter _0024awaiter0;

		public void MoveNext()
		{
			uint num = (uint)_0024PC;
			_0024PC = -1;
			try
			{
				switch (num)
				{
				default:
					return;
				case 0u:
					_0024awaiter0 = _0024this.StopAll().GetAwaiter();
					if (!_0024awaiter0.IsCompleted)
					{
						_0024PC = 1;
						_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter0, ref this);
						return;
					}
					break;
				case 1u:
					break;
				}
				_0024awaiter0.GetResult();
			}
			catch (Exception exception)
			{
				_0024PC = -1;
				_0024builder.SetException(exception);
			}
		}

		[DebuggerHidden]
		public void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			_0024builder.SetStateMachine(stateMachine);
		}
	}

	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	internal struct _003CBuildServerTab_003Ec__asyncA : IAsyncStateMachine
	{
		internal MainForm _0024this;

		internal AsyncVoidMethodBuilder _0024builder;

		internal int _0024PC;

		private TaskAwaiter _0024awaiter0;

		public void MoveNext()
		{
			uint num = (uint)_0024PC;
			_0024PC = -1;
			try
			{
				switch (num)
				{
				default:
					return;
				case 0u:
					_0024awaiter0 = _0024this.StopAll().GetAwaiter();
					if (!_0024awaiter0.IsCompleted)
					{
						_0024PC = 1;
						_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter0, ref this);
						return;
					}
					goto case 1u;
				case 1u:
					_0024awaiter0.GetResult();
					_0024awaiter0 = _0024this.StartAll().GetAwaiter();
					if (!_0024awaiter0.IsCompleted)
					{
						_0024PC = 2;
						_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter0, ref this);
						return;
					}
					break;
				case 2u:
					break;
				}
				_0024awaiter0.GetResult();
			}
			catch (Exception exception)
			{
				_0024PC = -1;
				_0024builder.SetException(exception);
				return;
			}
			_0024PC = -1;
			_0024builder.SetResult();
		}

		[DebuggerHidden]
		public void SetStateMachine(IAsyncStateMachine stateMachine)
		{
			_0024builder.SetStateMachine(stateMachine);
		}
	}

	private string root;

	private string serverDir;

	private string dbDir;

	private string iniPath;

	private Dictionary<string, string> ini;

	private bool preRe = true;

	private readonly Db db = new Db();

	private readonly ItemDb items = new ItemDb();

	private List<KeyValuePair<int, string>> groups = new List<KeyValuePair<int, string>>();

	private readonly ServerProc login = new ServerProc("Login", "login-server.exe");

	private readonly ServerProc chr = new ServerProc("Char", "char-server.exe");

	private readonly ServerProc map = new ServerProc("Map", "map-server.exe");

	private readonly ServerProc web = new ServerProc("Web", "web-server.exe");

	private readonly ServerProc tool = new ServerProc("Tool", string.Empty);

	private ServerProc[] servers;

	private readonly Dictionary<ServerProc, RichTextBox> logBoxes = new Dictionary<ServerProc, RichTextBox>();

	private readonly Dictionary<string, Label> statusLabels = new Dictionary<string, Label>();

	private bool busy;

	private bool statusBusy;

	private static readonly Font UiFont = new Font("Tahoma", 9f);

	private static readonly Font MonoFont = new Font("Consolas", 9f);

	private TabControl tabs;

	private TabControl logTabs;

	private CheckBox chkAutoRestart;

	private CheckBox chkWeb;

	private CheckBox chkAutoOnline;

	private DataGridView gridOnline;

	private DataGridView gridAcc;

	private DataGridView gridAccChars;

	private DataGridView gridChars;

	private DataGridView gridInv;

	private DataGridView gridCart;

	private DataGridView gridStor;

	private DataGridView gridItemOwners;

	private TextBox txtAccSearch;

	private TextBox txtCharSearch;

	private TextBox txtItemSearch;

	private Label lblOnline;

	private Label lblItemInfo;

	private ToolStripStatusLabel stDb;

	private ToolStripStatusLabel stCounts;

	private ToolStripStatusLabel stMode;

	private ToolStripStatusLabel stPath;

	private Timer uiTimer;

	private Timer statusTimer;

	private Timer onlineTimer;

	private long selectedAccountId = -1L;

	private static readonly Dictionary<string, string> Heads = new Dictionary<string, string>
	{
		{ "h0_", "ช\u0e37\u0e48อ" },
		{ "h1_", "ไอด\u0e35" },
		{ "h2_", "อาช\u0e35พ" },
		{ "h3_", "แผนท\u0e35\u0e48" },
		{ "h4_", "เพศ" },
		{ "h5_", "ล\u0e47อกอ\u0e34น (คร\u0e31\u0e49ง)" },
		{ "h6_", "ล\u0e47อกอ\u0e34นล\u0e48าส\u0e38ด" },
		{ "h7_", "IP ล\u0e48าส\u0e38ด" },
		{ "h8_", "ต\u0e31วละคร" },
		{ "h9_", "ช\u0e48อง" },
		{ "h10_", "สถานะ" },
		{ "h11_", "รห\u0e31สผ\u0e48าน" },
		{ "h12_", "อ\u0e35เมล" }
	};

	private const string CharCols = "c.char_id, c.name AS `h0_`, l.userid AS `h1_`, c.class AS `h2_`, c.base_level AS `Base Lv`, c.job_level AS `Job Lv`, c.zeny AS `Zeny`, c.str AS `STR`, c.agi AS `AGI`, c.vit AS `VIT`, c.`int` AS `INT`, c.dex AS `DEX`, c.luk AS `LUK`, c.last_map AS `h3_`, IF(c.online=1,'ON','') AS `h10_`, c.account_id ";

	private long lastCharItems = -1L;

	private const int MailMaxItems = 5;

	private const int MailMaxInbox = 30;

	private const int MaxZeny = 1000000000;

	private const int MaxStack = 30000;

	private readonly List<MailItem> mailItems = new List<MailItem>();

	private RadioButton rbNames;

	private RadioButton rbOnline;

	private RadioButton rbAllChars;

	private RadioButton rbPerAccount;

	private TextBox txtMailNames;

	private TextBox txtMailSender;

	private TextBox txtMailTitle;

	private TextBox txtMailBody;

	private TextBox txtMailItem;

	private NumericUpDown numMailZeny;

	private NumericUpDown numItemAmount;

	private NumericUpDown numItemRefine;

	private CheckBox chkNoGm;

	private ListView lvMailItems;

	private Label lblMailBytes;

	private DataGridView gridMailHist;

	private bool showPass;

	private object editOld;

	private ToolStripStatusLabel stMsg;

	private NumericUpDown numRateExp;

	private NumericUpDown numRateDrop;

	private NumericUpDown numRateHours;

	private TextBox txtRateMsg;

	private CheckBox chkRateAutoMsg;

	private Label lblRateNow;

	private Label lblRateBase;

	private DataGridView gridRateHist;

	private Timer rateTimer;

	private const string RateTableSql = "CREATE TABLE IF NOT EXISTS `rox_rate` (`id` INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,`exp_mult` INT NOT NULL DEFAULT 100,`drop_mult` INT NOT NULL DEFAULT 100,`minutes` INT NOT NULL DEFAULT 0,`msg` VARCHAR(200) NOT NULL DEFAULT '',`status` TINYINT NOT NULL DEFAULT 0,`until_ts` INT UNSIGNED NOT NULL DEFAULT 0,`created` DATETIME NULL,`applied` DATETIME NULL) ENGINE=MyISAM";

	private const string RateStateSql = "CREATE TABLE IF NOT EXISTS `rox_rate_state` (`id` TINYINT NOT NULL PRIMARY KEY,`exp_mult` INT NOT NULL DEFAULT 100,`drop_mult` INT NOT NULL DEFAULT 100,`base_exp` INT NOT NULL DEFAULT 0,`job_exp` INT NOT NULL DEFAULT 0,`drop_common` INT NOT NULL DEFAULT 0,`def_exp` INT NOT NULL DEFAULT 0,`def_drop` INT NOT NULL DEFAULT 0,`until_ts` INT UNSIGNED NOT NULL DEFAULT 0,`updated` DATETIME NULL) ENGINE=MyISAM";

	public MainForm()
	{
		//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f2: Expected O, but got Unknown
		//IL_0228: Unknown result type (might be due to invalid IL or missing references)
		//IL_022f: Expected O, but got Unknown
		//IL_0265: Unknown result type (might be due to invalid IL or missing references)
		//IL_026c: Expected O, but got Unknown
		((Control)this).Text = "RO Server Manager";
		((Control)this).Font = UiFont;
		((Form)this).Size = new Size(1120, 860);
		((Control)this).MinimumSize = new Size(920, 700);
		((Form)this).StartPosition = (FormStartPosition)1;
		try
		{
			((Form)this).Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
		}
		catch
		{
		}
		try
		{
			Dictionary<string, string> dictionary = Ini.Load(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "RO_Manager.ini"));
			Theme.Dark = !dictionary.TryGetValue("Theme", out var value) || !value.Equals("light", StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
		}
		servers = new ServerProc[4] { login, chr, map, web };
		ServerProc[] array = servers;
		foreach (ServerProc serverProc in array)
		{
			serverProc.Crashed += OnCrashed;
		}
		BuildUi();
		LoadPaths();
		InitBotTab();
		InitTimeTab();
		InitMobTab();
		Timer val = new Timer();
		val.Interval = 200;
		uiTimer = val;
		uiTimer.Tick += delegate
		{
			FlushLogs();
		};
		uiTimer.Start();
		val = new Timer();
		val.Interval = 2000;
		statusTimer = val;
		statusTimer.Tick += delegate
		{
			RefreshStatus();
		};
		statusTimer.Start();
		val = new Timer();
		val.Interval = 10000;
		onlineTimer = val;
		onlineTimer.Tick += delegate
		{
			if (chkAutoOnline.Checked && tabs.SelectedIndex == 1)
			{
				LoadOnline(silent: true);
			}
			UpdateCounts();
		};
		onlineTimer.Start();
		((Form)this).Shown += delegate
		{
			RefreshStatus();
			TryConnectDb(log: false);
		};
		Theme.Apply((Control)(object)this);
	}

	private void BuildUi()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0016: Expected O, but got Unknown
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Expected O, but got Unknown
		//IL_003d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0047: Expected O, but got Unknown
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0057: Expected O, but got Unknown
		//IL_005d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Expected O, but got Unknown
		StatusStrip val = new StatusStrip();
		stDb = new ToolStripStatusLabel("DB: -");
		ToolStripStatusLabel val2 = new ToolStripStatusLabel(string.Empty);
		val2.Spring = true;
		((ToolStripItem)val2).TextAlign = (ContentAlignment)16;
		stCounts = val2;
		stMode = new ToolStripStatusLabel(string.Empty);
		stPath = new ToolStripStatusLabel(string.Empty);
		stMsg = new ToolStripStatusLabel(string.Empty);
		((ToolStrip)val).Items.AddRange((ToolStripItem[])(object)new ToolStripItem[5]
		{
			(ToolStripItem)stDb,
			(ToolStripItem)stMsg,
			(ToolStripItem)stCounts,
			(ToolStripItem)stMode,
			(ToolStripItem)stPath
		});
		((Control)this).Controls.Add((Control)(object)val);
		ThemedTab themedTab = new ThemedTab();
		((Control)themedTab).Dock = (DockStyle)5;
		((TabControl)themedTab).Padding = new Point(14, 5);
		tabs = (TabControl)(object)themedTab;
		tabs.TabPages.Add(BuildServerTab());
		tabs.TabPages.Add(BuildOnlineTab());
		tabs.TabPages.Add(BuildAccountTab());
		tabs.TabPages.Add(BuildCharTab());
		tabs.TabPages.Add(BuildItemTab());
		tabs.TabPages.Add(BuildMailTab());
		tabs.TabPages.Add(BuildRateTab());
		tabs.TabPages.Add(BuildBotTab());
		tabs.TabPages.Add(BuildTimeTab());
		tabs.TabPages.Add(BuildMobTab());
		tabs.SelectedIndexChanged += delegate
		{
			if (tabs.SelectedIndex == 1)
			{
				LoadOnline(silent: true);
			}
			if (tabs.SelectedIndex == 2 && gridAcc.DataSource == null)
			{
				SearchAccounts();
			}
			if (tabs.SelectedIndex == 5)
			{
				LoadMailHistory();
			}
			if (tabs.SelectedIndex == 6)
			{
				LoadRate();
			}
		};
		((Control)this).Controls.Add((Control)(object)tabs);
		((Control)this).Controls.Add((Control)(object)new BannerPanel());
		((Control)val).SendToBack();
		((Control)tabs).BringToFront();
	}

	private static Button Btn(string text, EventHandler click, int w = 110)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		Button val = new Button();
		((Control)val).Text = text;
		((Control)val).Width = w;
		((Control)val).Height = 30;
		((Control)val).Margin = new Padding(3);
		Button val2 = val;
		((Control)val2).Click += click;
		return val2;
	}

	private static DataGridView Grid()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Expected O, but got Unknown
		DataGridView val = new DataGridView();
		((Control)val).Dock = (DockStyle)5;
		val.ReadOnly = true;
		val.AllowUserToAddRows = false;
		val.AllowUserToDeleteRows = false;
		val.AllowUserToResizeRows = false;
		val.SelectionMode = (DataGridViewSelectionMode)1;
		val.MultiSelect = false;
		val.RowHeadersVisible = false;
		val.AutoSizeColumnsMode = (DataGridViewAutoSizeColumnsMode)10;
		val.BackgroundColor = SystemColors.Window;
		val.BorderStyle = (BorderStyle)0;
		DataGridView val2 = val;
		val2.AlternatingRowsDefaultCellStyle.BackColor = Theme.GridAlt;
		val2.DataError += (DataGridViewDataErrorEventHandler)delegate(object s, DataGridViewDataErrorEventArgs e)
		{
			e.ThrowException = false;
		};
		return val2;
	}

	private TabPage BuildServerTab()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_0034: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Expected O, but got Unknown
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Expected O, but got Unknown
		//IL_007c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0089: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Expected O, but got Unknown
		//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Expected O, but got Unknown
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Expected O, but got Unknown
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Expected O, but got Unknown
		//IL_0121: Unknown result type (might be due to invalid IL or missing references)
		//IL_0127: Expected O, but got Unknown
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_016c: Expected O, but got Unknown
		//IL_017a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0184: Expected O, but got Unknown
		//IL_0192: Unknown result type (might be due to invalid IL or missing references)
		//IL_019c: Expected O, but got Unknown
		//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b4: Expected O, but got Unknown
		//IL_028b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0292: Expected O, but got Unknown
		//IL_02b1: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_03a7: Expected O, but got Unknown
		//IL_03cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_03dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e4: Expected O, but got Unknown
		//IL_03ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0500: Expected O, but got Unknown
		//IL_0527: Unknown result type (might be due to invalid IL or missing references)
		//IL_05ca: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d1: Expected O, but got Unknown
		//IL_05d1: Unknown result type (might be due to invalid IL or missing references)
		//IL_05d8: Expected O, but got Unknown
		//IL_062e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0635: Expected O, but got Unknown
		TabPage val = new TabPage("เซ\u0e34ร\u0e4cฟเวอร\u0e4c");
		TableLayoutPanel val2 = new TableLayoutPanel();
		((Control)val2).Dock = (DockStyle)5;
		val2.RowCount = 2;
		val2.ColumnCount = 1;
		TableLayoutPanel val3 = val2;
		val3.RowStyles.Add(new RowStyle((SizeType)1, 236f));
		val3.RowStyles.Add(new RowStyle((SizeType)2, 100f));
		((Control)val).Controls.Add((Control)(object)val3);
		ArtTable artTable = new ArtTable();
		((Control)artTable).Dock = (DockStyle)5;
		((TableLayoutPanel)artTable).ColumnCount = 2;
		((Control)artTable).Padding = new Padding(6);
		((Control)artTable).Margin = new Padding(0);
		ArtTable artTable2 = artTable;
		((TableLayoutPanel)artTable2).ColumnStyles.Add(new ColumnStyle((SizeType)1, 470f));
		((TableLayoutPanel)artTable2).ColumnStyles.Add(new ColumnStyle((SizeType)2, 100f));
		val3.Controls.Add((Control)(object)artTable2, 0, 0);
		GroupBox val4 = new GroupBox();
		((Control)val4).Text = "สถานะ";
		((Control)val4).Dock = (DockStyle)5;
		((Control)val4).BackColor = Theme.Glass(215);
		((Control)val4).Margin = new Padding(6);
		((Control)val4).Font = new Font(UiFont, (FontStyle)1);
		GroupBox val5 = val4;
		val2 = new TableLayoutPanel();
		((Control)val2).Dock = (DockStyle)5;
		val2.ColumnCount = 4;
		val2.RowCount = 5;
		((Control)val2).BackColor = Color.Transparent;
		((Control)val2).Font = UiFont;
		TableLayoutPanel val6 = val2;
		val6.ColumnStyles.Add(new ColumnStyle((SizeType)1, 90f));
		val6.ColumnStyles.Add(new ColumnStyle((SizeType)1, 170f));
		val6.ColumnStyles.Add(new ColumnStyle((SizeType)1, 90f));
		val6.ColumnStyles.Add(new ColumnStyle((SizeType)1, 90f));
		((Control)val5).Controls.Add((Control)(object)val6);
		((TableLayoutPanel)artTable2).Controls.Add((Control)(object)val5, 0, 0);
		AddStatusRow(val6, "MySQL", delegate
		{
			StartMySql();
		}, delegate
		{
			StopMySql();
		});
		AddStatusRow(val6, "Login", delegate
		{
			StartOne(login);
		}, delegate
		{
			StopOne(login);
		});
		AddStatusRow(val6, "Char", delegate
		{
			StartOne(chr);
		}, delegate
		{
			StopOne(chr);
		});
		AddStatusRow(val6, "Map", delegate
		{
			StartOne(map);
		}, delegate
		{
			StopOne(map);
		});
		AddStatusRow(val6, "Web", delegate
		{
			StartOne(web);
		}, delegate
		{
			StopOne(web);
		});
		FlowLayoutPanel val7 = new FlowLayoutPanel();
		((Control)val7).Dock = (DockStyle)5;
		val7.FlowDirection = (FlowDirection)0;
		val7.WrapContents = true;
		((Control)val7).Padding = new Padding(8, 10, 6, 6);
		((Control)val7).Margin = new Padding(6, 12, 6, 6);
		((Control)val7).BackColor = Theme.Glass(200);
		FlowLayoutPanel val8 = val7;
		Button val9 = Btn("▶  เป\u0e34ดท\u0e31\u0e49งหมด", [AsyncStateMachine(typeof(_003CBuildServerTab_003Ec__async6))] (object s, EventArgs e) =>
		{
			_003CBuildServerTab_003Ec__async6 stateMachine3 = default(_003CBuildServerTab_003Ec__async6);
			stateMachine3._0024this = this;
			stateMachine3._0024builder = AsyncVoidMethodBuilder.Create();
			stateMachine3._0024builder.Start(ref stateMachine3);
		}, 150);
		((Control)val9).BackColor = Theme.BtnGo;
		((Control)val9).Height = 40;
		Button val10 = Btn("■  ป\u0e34ดท\u0e31\u0e49งหมด", [AsyncStateMachine(typeof(_003CBuildServerTab_003Ec__async8))] (object s, EventArgs e) =>
		{
			_003CBuildServerTab_003Ec__async8 stateMachine2 = default(_003CBuildServerTab_003Ec__async8);
			stateMachine2._0024this = this;
			stateMachine2._0024builder = AsyncVoidMethodBuilder.Create();
			stateMachine2._0024builder.Start(ref stateMachine2);
		}, 150);
		((Control)val10).BackColor = Theme.BtnStop;
		((Control)val10).Height = 40;
		Button val11 = Btn("↻  ร\u0e35สตาร\u0e4cท", [AsyncStateMachine(typeof(_003CBuildServerTab_003Ec__asyncA))] (object s, EventArgs e) =>
		{
			_003CBuildServerTab_003Ec__asyncA stateMachine = default(_003CBuildServerTab_003Ec__asyncA);
			stateMachine._0024this = this;
			stateMachine._0024builder = AsyncVoidMethodBuilder.Create();
			stateMachine._0024builder.Start(ref stateMachine);
		}, 150);
		((Control)val11).Height = 40;
		((Control)val8).Controls.Add((Control)(object)val9);
		((Control)val8).Controls.Add((Control)(object)val10);
		((Control)val8).Controls.Add((Control)(object)val11);
		val8.SetFlowBreak((Control)(object)val11, true);
		CheckBox val12 = new CheckBox();
		((Control)val12).Text = "เป\u0e34ด Web server ด\u0e49วย";
		((Control)val12).AutoSize = true;
		val12.Checked = true;
		((Control)val12).Margin = new Padding(6, 10, 12, 6);
		chkWeb = val12;
		val12 = new CheckBox();
		((Control)val12).Text = "เป\u0e34ดใหม\u0e48อ\u0e31ตโนม\u0e31ต\u0e34ถ\u0e49าแครช";
		((Control)val12).AutoSize = true;
		((Control)val12).Margin = new Padding(6, 10, 3, 6);
		chkAutoRestart = val12;
		((Control)val8).Controls.Add((Control)(object)chkWeb);
		((Control)val8).Controls.Add((Control)(object)chkAutoRestart);
		val8.SetFlowBreak((Control)(object)chkAutoRestart, true);
		((Control)val8).Controls.Add((Control)(object)Btn("โฟลเดอร\u0e4c Server", delegate
		{
			if (Directory.Exists(serverDir))
			{
				Process.Start("explorer.exe", "\"" + serverDir + "\"");
			}
		}, 150));
		((Control)val8).Controls.Add((Control)(object)Btn("phpMyAdmin", delegate
		{
			OpenPhpMyAdmin();
		}, 150));
		((Control)val8).Controls.Add((Control)(object)Btn("เล\u0e37อกโฟลเดอร\u0e4cเกม", delegate
		{
			ChooseRoot();
		}, 150));
		((Control)val8).Controls.Add((Control)(object)Btn((!Theme.Dark) ? "\ud83c\udf19  โหมดม\u0e37ด" : "☀  โหมดสว\u0e48าง", delegate
		{
			ToggleTheme();
		}, 150));
		Control.ControlCollection controls = ((Control)val8).Controls;
		Label val13 = new Label();
		((Control)val13).Text = "ป\u0e34ดเซ\u0e34ร\u0e4cฟผ\u0e48านโปรแกรมน\u0e35\u0e49จะเซฟข\u0e49อม\u0e39ลก\u0e48อนป\u0e34ดท\u0e38กคร\u0e31\u0e49ง  •  MySQL ใช\u0e49 USBWebserver ในโฟลเดอร\u0e4c Database";
		((Control)val13).AutoSize = true;
		((Control)val13).ForeColor = Theme.Muted;
		((Control)val13).Margin = new Padding(6, 12, 3, 3);
		((Control)val13).MaximumSize = new Size(470, 0);
		controls.Add((Control)(object)val13);
		((TableLayoutPanel)artTable2).Controls.Add((Control)(object)val8, 1, 0);
		ThemedTab themedTab = new ThemedTab();
		((Control)themedTab).Dock = (DockStyle)5;
		logTabs = (TabControl)(object)themedTab;
		ServerProc[] array = new ServerProc[5] { login, chr, map, web, tool };
		foreach (ServerProc serverProc in array)
		{
			TabPage val14 = new TabPage(serverProc.Title);
			RichTextBox val15 = new RichTextBox();
			((Control)val15).Dock = (DockStyle)5;
			((TextBoxBase)val15).ReadOnly = true;
			((Control)val15).Font = MonoFont;
			((TextBoxBase)val15).WordWrap = false;
			((Control)val15).BackColor = Color.FromArgb(24, 26, 30);
			((Control)val15).ForeColor = Color.Gainsboro;
			((TextBoxBase)val15).BorderStyle = (BorderStyle)0;
			val15.DetectUrls = false;
			RichTextBox val16 = val15;
			val7 = new FlowLayoutPanel();
			((Control)val7).Dock = (DockStyle)1;
			((Control)val7).Height = 34;
			val7.FlowDirection = (FlowDirection)0;
			FlowLayoutPanel val17 = val7;
			RichTextBox box = val16;
			((Control)val17).Controls.Add((Control)(object)Btn("ล\u0e49าง log", delegate
			{
				((TextBoxBase)box).Clear();
			}, 90));
			((Control)val17).Controls.Add((Control)(object)Btn("บ\u0e31นท\u0e36ก log", delegate
			{
				SaveLog(box);
			}, 90));
			((Control)val14).Controls.Add((Control)(object)val16);
			((Control)val14).Controls.Add((Control)(object)val17);
			logTabs.TabPages.Add(val14);
			logBoxes[serverProc] = val16;
		}
		val3.Controls.Add((Control)(object)logTabs, 0, 1);
		return val;
	}

	private void AddStatusRow(TableLayoutPanel t, string name, Action start, Action stop)
	{
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Expected O, but got Unknown
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Expected O, but got Unknown
		//IL_0043: Unknown result type (might be due to invalid IL or missing references)
		//IL_0049: Expected O, but got Unknown
		Label val = new Label();
		((Control)val).Text = name;
		((Control)val).AutoSize = true;
		((Control)val).Anchor = (AnchorStyles)4;
		((Control)val).Font = new Font(UiFont, (FontStyle)1);
		Label val2 = val;
		val = new Label();
		((Control)val).Text = "● ...";
		((Control)val).AutoSize = true;
		((Control)val).Anchor = (AnchorStyles)4;
		((Control)val).ForeColor = Theme.Muted;
		Label val3 = val;
		statusLabels[name] = val3;
		((Control.ControlCollection)t.Controls).Add((Control)(object)val2);
		((Control.ControlCollection)t.Controls).Add((Control)(object)val3);
		((Control.ControlCollection)t.Controls).Add((Control)(object)Btn("เป\u0e34ด", delegate
		{
			start();
		}, 80));
		((Control.ControlCollection)t.Controls).Add((Control)(object)Btn("ป\u0e34ด", delegate
		{
			stop();
		}, 80));
	}

	private TabPage BuildOnlineTab()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_0058: Expected O, but got Unknown
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a0: Expected O, but got Unknown
		//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cc: Expected O, but got Unknown
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Expected O, but got Unknown
		//IL_0129: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Expected O, but got Unknown
		TabPage val = new TabPage("ผ\u0e39\u0e49เล\u0e48นออนไลน\u0e4c");
		FlowLayoutPanel val2 = new FlowLayoutPanel();
		((Control)val2).Dock = (DockStyle)1;
		((Control)val2).Height = 40;
		((Control)val2).Padding = new Padding(4);
		FlowLayoutPanel val3 = val2;
		((Control)val3).Controls.Add((Control)(object)Btn("ร\u0e35เฟรช", delegate
		{
			LoadOnline(silent: false);
		}, 90));
		CheckBox val4 = new CheckBox();
		((Control)val4).Text = "ร\u0e35เฟรชอ\u0e31ตโนม\u0e31ต\u0e34 (10 ว\u0e34)";
		((Control)val4).AutoSize = true;
		val4.Checked = true;
		((Control)val4).Margin = new Padding(8, 9, 3, 3);
		chkAutoOnline = val4;
		((Control)val3).Controls.Add((Control)(object)chkAutoOnline);
		Label val5 = new Label();
		((Control)val5).AutoSize = true;
		((Control)val5).Margin = new Padding(16, 9, 3, 3);
		((Control)val5).Font = new Font(UiFont, (FontStyle)1);
		lblOnline = val5;
		((Control)val3).Controls.Add((Control)(object)lblOnline);
		gridOnline = Grid();
		gridOnline.CellDoubleClick += (DataGridViewCellEventHandler)delegate(object s, DataGridViewCellEventArgs e)
		{
			string text = CellStr(gridOnline, e.RowIndex, "ช\u0e37\u0e48อ");
			if (text != null)
			{
				((Control)txtCharSearch).Text = text;
				tabs.SelectedIndex = 3;
				SearchChars();
			}
		};
		((Control)val).Controls.Add((Control)(object)gridOnline);
		((Control)val).Controls.Add((Control)(object)val3);
		Control.ControlCollection controls = ((Control)val).Controls;
		val5 = new Label();
		((Control)val5).Dock = (DockStyle)2;
		((Control)val5).Height = 22;
		((Control)val5).Text = "  ด\u0e31บเบ\u0e34ลคล\u0e34กช\u0e37\u0e48อต\u0e31วละครเพ\u0e37\u0e48อด\u0e39ของในต\u0e31ว  •  ตำแหน\u0e48งแผนท\u0e35\u0e48อ\u0e31ปเดตตอนเซ\u0e34ร\u0e4cฟเซฟ (ไม\u0e48ใช\u0e48เร\u0e35ยลไทม\u0e4c)";
		((Control)val5).ForeColor = Theme.Muted;
		val5.TextAlign = (ContentAlignment)16;
		controls.Add((Control)(object)val5);
		return val;
	}

	private TabPage BuildAccountTab()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e3: Expected O, but got Unknown
		//IL_01ff: Unknown result type (might be due to invalid IL or missing references)
		//IL_023e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0245: Expected O, but got Unknown
		//IL_026d: Unknown result type (might be due to invalid IL or missing references)
		//IL_027f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0286: Expected O, but got Unknown
		//IL_030a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0311: Expected O, but got Unknown
		//IL_0336: Unknown result type (might be due to invalid IL or missing references)
		//IL_0340: Expected O, but got Unknown
		//IL_03a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ae: Expected O, but got Unknown
		TabPage val = new TabPage("จ\u0e31ดการไอด\u0e35");
		FlowLayoutPanel val2 = new FlowLayoutPanel();
		((Control)val2).Dock = (DockStyle)1;
		((Control)val2).Height = 76;
		((Control)val2).Padding = new Padding(4);
		FlowLayoutPanel val3 = val2;
		TextBox val4 = new TextBox();
		((Control)val4).Width = 200;
		((Control)val4).Margin = new Padding(3, 7, 3, 3);
		txtAccSearch = val4;
		((Control)txtAccSearch).KeyDown += (KeyEventHandler)delegate(object s, KeyEventArgs e)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Invalid comparison between Unknown and I4
			if ((int)e.KeyCode == 13)
			{
				e.SuppressKeyPress = true;
				SearchAccounts();
			}
		};
		Control.ControlCollection controls = ((Control)val3).Controls;
		Label val5 = new Label();
		((Control)val5).Text = "ค\u0e49นหาไอด\u0e35:";
		((Control)val5).AutoSize = true;
		((Control)val5).Margin = new Padding(3, 10, 0, 3);
		controls.Add((Control)(object)val5);
		((Control)val3).Controls.Add((Control)(object)txtAccSearch);
		((Control)val3).Controls.Add((Control)(object)Btn("ค\u0e49นหา", delegate
		{
			SearchAccounts();
		}, 80));
		((Control)val3).Controls.Add((Control)(object)Btn("＋ สร\u0e49างไอด\u0e35", delegate
		{
			CreateAccount();
		}));
		((Control)val3).Controls.Add((Control)(object)Btn("เปล\u0e35\u0e48ยนรห\u0e31ส", delegate
		{
			ChangePassword();
		}, 100));
		((Control)val3).Controls.Add((Control)(object)Btn("ต\u0e31\u0e49งกล\u0e38\u0e48ม / GM", delegate
		{
			SetGroup();
		}));
		val3.SetFlowBreak(((Control)val3).Controls[((ArrangedElementCollection)((Control)val3).Controls).Count - 1], true);
		((Control)val3).Controls.Add((Control)(object)Btn("แบนถาวร", delegate
		{
			Ban(perma: true);
		}, 100));
		((Control)val3).Controls.Add((Control)(object)Btn("แบนช\u0e31\u0e48วคราว", delegate
		{
			Ban(perma: false);
		}, 100));
		((Control)val3).Controls.Add((Control)(object)Btn("ปลดแบน", delegate
		{
			Unban();
		}, 100));
		((Control)val3).Controls.Add((Control)(object)Btn("ลบไอดี", delegate
		{
			DeleteAccount();
		}, 100));
		CheckBox val6 = new CheckBox();
		((Control)val6).Text = "แสดงรห\u0e31สผ\u0e48าน";
		((Control)val6).AutoSize = true;
		((Control)val6).Margin = new Padding(12, 9, 3, 3);
		CheckBox chkPass = val6;
		chkPass.CheckedChanged += delegate
		{
			showPass = chkPass.Checked;
			((Control)gridAcc).Invalidate();
		};
		((Control)val3).Controls.Add((Control)(object)chkPass);
		Control.ControlCollection controls2 = ((Control)val3).Controls;
		val5 = new Label();
		((Control)val5).Text = "ด\u0e31บเบ\u0e34ลคล\u0e34กช\u0e48องท\u0e35\u0e48ม\u0e35 ✎ เพ\u0e37\u0e48อแก\u0e49ไข แล\u0e49วกด Enter บ\u0e31นท\u0e36กท\u0e31นท\u0e35  •  แบน/เปล\u0e35\u0e48ยนกล\u0e38\u0e48ม ม\u0e35ผลตอนล\u0e47อกอ\u0e34นคร\u0e31\u0e49งถ\u0e31ดไป";
		((Control)val5).AutoSize = true;
		((Control)val5).ForeColor = Theme.Muted;
		((Control)val5).Margin = new Padding(12, 10, 3, 3);
		controls2.Add((Control)(object)val5);
		SplitContainer val7 = new SplitContainer();
		val7.Dock = (DockStyle)5;
		val7.Orientation = (Orientation)0;
		SplitContainer split = val7;
		gridAcc = Grid();
		gridAcc.SelectionChanged += delegate
		{
			LoadAccountChars();
		};
		gridAccChars = Grid();
		SetupEditGrid(gridAcc, AccountSpec());
		SetupEditGrid(gridAccChars, CharSpec(full: false));
		((Control)split.Panel1).Controls.Add((Control)(object)gridAcc);
		val5 = new Label();
		((Control)val5).Text = "ต\u0e31วละครในไอด\u0e35น\u0e35\u0e49";
		((Control)val5).Dock = (DockStyle)1;
		((Control)val5).Height = 22;
		((Control)val5).Font = new Font(UiFont, (FontStyle)1);
		val5.TextAlign = (ContentAlignment)16;
		Label val8 = val5;
		((Control)split.Panel2).Controls.Add((Control)(object)gridAccChars);
		((Control)split.Panel2).Controls.Add((Control)(object)val8);
		((Control)val).Controls.Add((Control)(object)split);
		((Control)val).Controls.Add((Control)(object)val3);
		((Control)val).Layout += (LayoutEventHandler)delegate
		{
			if (((Control)split).Height > 200)
			{
				split.SplitterDistance = (int)((double)((Control)split).Height * 0.6);
			}
		};
		return val;
	}

	private TabPage BuildCharTab()
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0018: Expected O, but got Unknown
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Expected O, but got Unknown
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0043: Expected O, but got Unknown
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Expected O, but got Unknown
		//IL_0083: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_0131: Unknown result type (might be due to invalid IL or missing references)
		//IL_0138: Expected O, but got Unknown
		//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Expected O, but got Unknown
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Expected O, but got Unknown
		//IL_0214: Unknown result type (might be due to invalid IL or missing references)
		//IL_021b: Expected O, but got Unknown
		//IL_028a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0294: Expected O, but got Unknown
		TabPage val = new TabPage("ค\u0e49นหาต\u0e31วละคร");
		FlowLayoutPanel val2 = new FlowLayoutPanel();
		((Control)val2).Dock = (DockStyle)1;
		((Control)val2).Height = 40;
		((Control)val2).Padding = new Padding(4);
		FlowLayoutPanel val3 = val2;
		TextBox val4 = new TextBox();
		((Control)val4).Width = 200;
		((Control)val4).Margin = new Padding(3, 7, 3, 3);
		txtCharSearch = val4;
		((Control)txtCharSearch).KeyDown += (KeyEventHandler)delegate(object s, KeyEventArgs e)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Invalid comparison between Unknown and I4
			if ((int)e.KeyCode == 13)
			{
				e.SuppressKeyPress = true;
				SearchChars();
			}
		};
		Control.ControlCollection controls = ((Control)val3).Controls;
		Label val5 = new Label();
		((Control)val5).Text = "ช\u0e37\u0e48อต\u0e31วละคร / ไอด\u0e35:";
		((Control)val5).AutoSize = true;
		((Control)val5).Margin = new Padding(3, 10, 0, 3);
		controls.Add((Control)(object)val5);
		((Control)val3).Controls.Add((Control)(object)txtCharSearch);
		((Control)val3).Controls.Add((Control)(object)Btn("ค\u0e49นหา", delegate
		{
			SearchChars();
		}, 80));
		((Control)val3).Controls.Add((Control)(object)Btn("อ\u0e31นด\u0e31บ Zeny", delegate
		{
			TopList("zeny");
		}, 100));
		((Control)val3).Controls.Add((Control)(object)Btn("อ\u0e31นด\u0e31บเลเวล", delegate
		{
			TopList("level");
		}, 100));
		SplitContainer val6 = new SplitContainer();
		val6.Dock = (DockStyle)5;
		val6.Orientation = (Orientation)0;
		SplitContainer split = val6;
		gridChars = Grid();
		gridChars.SelectionChanged += delegate
		{
			LoadCharItems();
		};
		SetupEditGrid(gridChars, CharSpec(full: true));
		((Control)split.Panel1).Controls.Add((Control)(object)gridChars);
		ThemedTab themedTab = new ThemedTab();
		((Control)themedTab).Dock = (DockStyle)5;
		ThemedTab themedTab2 = themedTab;
		gridInv = Grid();
		gridCart = Grid();
		gridStor = Grid();
		TabPage val7 = new TabPage("ของในต\u0e31ว");
		((Control)val7).Controls.Add((Control)(object)gridInv);
		TabPage val8 = new TabPage("รถเข\u0e47น");
		((Control)val8).Controls.Add((Control)(object)gridCart);
		TabPage val9 = new TabPage("คล\u0e31ง (ท\u0e31\u0e49งไอด\u0e35)");
		((Control)val9).Controls.Add((Control)(object)gridStor);
		((TabControl)themedTab2).TabPages.AddRange((TabPage[])(object)new TabPage[3] { val7, val8, val9 });
		((Control)split.Panel2).Controls.Add((Control)(object)themedTab2);
		((Control)val).Controls.Add((Control)(object)split);
		((Control)val).Controls.Add((Control)(object)val3);
		((Control)val).Layout += (LayoutEventHandler)delegate
		{
			if (((Control)split).Height > 200)
			{
				split.SplitterDistance = (int)((double)((Control)split).Height * 0.45);
			}
		};
		return val;
	}

	private TabPage BuildItemTab()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_002f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0035: Expected O, but got Unknown
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_006c: Expected O, but got Unknown
		//IL_0072: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Expected O, but got Unknown
		//IL_0094: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Expected O, but got Unknown
		//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
		TabPage val = new TabPage("ค\u0e49นหาไอเท\u0e47ม");
		FlowLayoutPanel val2 = new FlowLayoutPanel();
		((Control)val2).Dock = (DockStyle)1;
		((Control)val2).Height = 40;
		((Control)val2).Padding = new Padding(4);
		FlowLayoutPanel val3 = val2;
		TextBox val4 = new TextBox();
		((Control)val4).Width = 240;
		((Control)val4).Margin = new Padding(3, 7, 3, 3);
		txtItemSearch = val4;
		((Control)txtItemSearch).KeyDown += (KeyEventHandler)delegate(object s, KeyEventArgs e)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Invalid comparison between Unknown and I4
			if ((int)e.KeyCode == 13)
			{
				e.SuppressKeyPress = true;
				SearchItemOwners();
			}
		};
		Control.ControlCollection controls = ((Control)val3).Controls;
		Label val5 = new Label();
		((Control)val5).Text = "ID หร\u0e37อช\u0e37\u0e48อไอเท\u0e47ม:";
		((Control)val5).AutoSize = true;
		((Control)val5).Margin = new Padding(3, 10, 0, 3);
		controls.Add((Control)(object)val5);
		((Control)val3).Controls.Add((Control)(object)txtItemSearch);
		((Control)val3).Controls.Add((Control)(object)Btn("ค\u0e49นหาว\u0e48าใครม\u0e35", delegate
		{
			SearchItemOwners();
		}, 120));
		val5 = new Label();
		((Control)val5).AutoSize = true;
		((Control)val5).Margin = new Padding(12, 10, 3, 3);
		((Control)val5).ForeColor = Theme.Muted;
		lblItemInfo = val5;
		((Control)val3).Controls.Add((Control)(object)lblItemInfo);
		gridItemOwners = Grid();
		((Control)val).Controls.Add((Control)(object)gridItemOwners);
		((Control)val).Controls.Add((Control)(object)val3);
		return val;
	}

	private void LoadPaths()
	{
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		string directoryName = Path.GetDirectoryName(Application.ExecutablePath);
		iniPath = Path.Combine(directoryName, "RO_Manager.ini");
		ini = Ini.Load(iniPath);
		if (!ini.TryGetValue("Root", out var value) || !IsRoot(value))
		{
			value = null;
			string[] array = new string[2]
			{
				directoryName,
				Path.GetDirectoryName(directoryName)
			};
			foreach (string text in array)
			{
				if (text != null && IsRoot(text))
				{
					value = text;
					break;
				}
			}
			if (value == null && File.Exists(Path.Combine(directoryName, "map-server.exe")))
			{
				value = Path.GetDirectoryName(directoryName);
			}
		}
		if (value == null || !IsRoot(value))
		{
			MessageBox.Show((IWin32Window)(object)this, "หาโฟลเดอร\u0e4cเกมไม\u0e48เจอ กร\u0e38ณาเล\u0e37อกโฟลเดอร\u0e4cท\u0e35\u0e48ม\u0e35โฟลเดอร\u0e4c Server และ Database อย\u0e39\u0e48ข\u0e49างใน", ((Control)this).Text);
			ChooseRoot();
		}
		else
		{
			ApplyRoot(value);
		}
	}

	private static bool IsRoot(string d)
	{
		return !string.IsNullOrEmpty(d) && File.Exists(Path.Combine(d, "Server", "map-server.exe"));
	}

	private void ChooseRoot()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_002c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0032: Invalid comparison between Unknown and I4
		//IL_0058: Unknown result type (might be due to invalid IL or missing references)
		FolderBrowserDialog val = new FolderBrowserDialog();
		val.Description = "เล\u0e37อกโฟลเดอร\u0e4cหล\u0e31ก (ท\u0e35\u0e48ม\u0e35 Server, Database, Game)";
		FolderBrowserDialog val2 = val;
		try
		{
			if (root != null)
			{
				val2.SelectedPath = root;
			}
			if ((int)((CommonDialog)val2).ShowDialog((IWin32Window)(object)this) == 1)
			{
				if (!IsRoot(val2.SelectedPath))
				{
					MessageBox.Show((IWin32Window)(object)this, "โฟลเดอร\u0e4cน\u0e35\u0e49ไม\u0e48ม\u0e35 Server\\map-server.exe", ((Control)this).Text);
				}
				else
				{
					ApplyRoot(val2.SelectedPath);
				}
			}
		}
		finally
		{
			((IDisposable)val2)?.Dispose();
		}
	}

	private void ApplyRoot(string r)
	{
		root = r;
		serverDir = Path.Combine(r, "Server");
		dbDir = Path.Combine(r, "Database");
		ini["Root"] = r;
		Ini.Save(iniPath, ini);
		ServerProc[] array = servers;
		foreach (ServerProc serverProc in array)
		{
			serverProc.Dir = serverDir;
		}
		try
		{
			K32.SetDllDirectory(serverDir);
		}
		catch
		{
		}
		try
		{
			string path = Path.Combine(serverDir, "src", "config", "renewal.hpp");
			if (File.Exists(path))
			{
				preRe = Regex.IsMatch(File.ReadAllText(path), "^\\s*#define\\s+PRERE\\b", RegexOptions.Multiline);
			}
		}
		catch
		{
		}
		Dictionary<string, string> d = Conf.Read(serverDir, "conf\\inter_athena.conf");
		db.Host = Conf.Get(d, "char_server_ip", "127.0.0.1");
		db.Port = ((!uint.TryParse(Conf.Get(d, "char_server_port", "3306"), out var result)) ? 3306u : result);
		db.User = Conf.Get(d, "char_server_id", "root");
		db.Pass = Conf.Get(d, "char_server_pw", string.Empty);
		db.Name = Conf.Get(d, "char_server_db", "rathena_main");
		db.Codepage = Conf.Get(d, "default_codepage", string.Empty);
		db.Close();
		groups = Groups.Load(serverDir);
		items.Loaded = false;
		((ToolStripItem)stMode).Text = ((!preRe) ? "โหมด: Renewal" : "โหมด: Pre-Renewal");
		((ToolStripItem)stPath).Text = r;
		tool.Add("[Tool] โฟลเดอร\u0e4c: " + r);
		tool.Add("[Tool] ฐานข\u0e49อม\u0e39ล: " + db.User + "@" + db.Host + ":" + db.Port + "/" + db.Name + ((!(db.Codepage != string.Empty)) ? string.Empty : (" (" + db.Codepage + ")")));
	}

	private bool MySqlUp()
	{
		return Net.PortOpen(db.Host, (int)db.Port);
	}

	private void StartMySql()
	{
		if (MySqlUp())
		{
			tool.Add("[Tool] MySQL ทำงานอย\u0e39\u0e48แล\u0e49ว");
			return;
		}
		string text = Path.Combine(dbDir, "usbwebserver.exe");
		if (!File.Exists(text))
		{
			tool.Add("[Tool][Error] ไม\u0e48พบ " + text + " — เป\u0e34ด MySQL เองก\u0e48อน");
			return;
		}
		try
		{
			ProcessStartInfo processStartInfo = new ProcessStartInfo(text);
			processStartInfo.WorkingDirectory = dbDir;
			processStartInfo.UseShellExecute = true;
			Process.Start(processStartInfo);
			tool.Add("[Tool] เป\u0e34ด USBWebserver (MySQL) แล\u0e49ว");
		}
		catch (Exception ex)
		{
			tool.Add("[Tool][Error] เป\u0e34ด USBWebserver ไม\u0e48ได\u0e49: " + ex.Message);
		}
	}

	private void StopMySql()
	{
		//IL_0039: Unknown result type (might be due to invalid IL or missing references)
		if (servers.Any((ServerProc s) => s.Running))
		{
			MessageBox.Show((IWin32Window)(object)this, "ป\u0e34ดเซ\u0e34ร\u0e4cฟ Login/Char/Map ก\u0e48อน แล\u0e49วค\u0e48อยป\u0e34ด MySQL", ((Control)this).Text);
			return;
		}
		db.Close();
		Process[] processesByName = Process.GetProcessesByName("usbwebserver");
		if (processesByName.Length == 0)
		{
			tool.Add("[Tool] ไม\u0e48พบ USBWebserver ท\u0e35\u0e48เป\u0e34ดอย\u0e39\u0e48 (ถ\u0e49าเป\u0e34ด MySQL แบบอ\u0e37\u0e48น ให\u0e49ป\u0e34ดเอง)");
			return;
		}
		Process[] array = processesByName;
		foreach (Process process in array)
		{
			try
			{
				if (!process.CloseMainWindow())
				{
					process.Kill();
				}
			}
			catch
			{
			}
		}
		tool.Add("[Tool] ส\u0e31\u0e48งป\u0e34ด USBWebserver แล\u0e49ว");
	}

	[DebuggerStepThrough]
	[AsyncStateMachine(typeof(_003CWaitMySql_003Ec__async0))]
	private Task<bool> WaitMySql(int seconds)
	{
		_003CWaitMySql_003Ec__async0 stateMachine = default(_003CWaitMySql_003Ec__async0);
		stateMachine.seconds = seconds;
		stateMachine._0024this = this;
		stateMachine._0024builder = AsyncTaskMethodBuilder<bool>.Create();
		ref AsyncTaskMethodBuilder<bool> _0024builder = ref stateMachine._0024builder;
		_0024builder.Start(ref stateMachine);
		return _0024builder.Task;
	}

	private void StartOne(ServerProc s)
	{
		//IL_0018: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Invalid comparison between Unknown and I4
		if (MySqlUp() || (int)MessageBox.Show((IWin32Window)(object)this, "MySQL ย\u0e31งไม\u0e48ทำงาน ต\u0e49องการเป\u0e34ดต\u0e48อไหม?", ((Control)this).Text, (MessageBoxButtons)4) == 6)
		{
			s.AutoRestart = chkAutoRestart.Checked;
			s.Start();
			ShowLog(s);
		}
	}

	[DebuggerStepThrough]
	[AsyncStateMachine(typeof(_003CStopOne_003Ec__async1))]
	private void StopOne(ServerProc s)
	{
		_003CStopOne_003Ec__async1 stateMachine = default(_003CStopOne_003Ec__async1);
		stateMachine.s = s;
		stateMachine._0024this = this;
		stateMachine._0024builder = AsyncVoidMethodBuilder.Create();
		stateMachine._0024builder.Start(ref stateMachine);
	}

	[DebuggerStepThrough]
	[AsyncStateMachine(typeof(_003CStartAll_003Ec__async2))]
	private Task StartAll()
	{
		_003CStartAll_003Ec__async2 stateMachine = default(_003CStartAll_003Ec__async2);
		stateMachine._0024this = this;
		stateMachine._0024builder = AsyncTaskMethodBuilder.Create();
		ref AsyncTaskMethodBuilder _0024builder = ref stateMachine._0024builder;
		_0024builder.Start(ref stateMachine);
		return _0024builder.Task;
	}

	[DebuggerStepThrough]
	[AsyncStateMachine(typeof(_003CStopAll_003Ec__async3))]
	private Task StopAll()
	{
		_003CStopAll_003Ec__async3 stateMachine = default(_003CStopAll_003Ec__async3);
		stateMachine._0024this = this;
		stateMachine._0024builder = AsyncTaskMethodBuilder.Create();
		ref AsyncTaskMethodBuilder _0024builder = ref stateMachine._0024builder;
		_0024builder.Start(ref stateMachine);
		return _0024builder.Task;
	}

	private void OnCrashed(ServerProc s, int code)
	{
		_003COnCrashed_003Ec__AnonStorey10 CS_0024_003C_003E8__locals0 = new _003COnCrashed_003Ec__AnonStorey10();
		CS_0024_003C_003E8__locals0.s = s;
		CS_0024_003C_003E8__locals0.code = code;
		CS_0024_003C_003E8__locals0._0024this = this;
		((Control)this).BeginInvoke((Delegate)(Action)([AsyncStateMachine(typeof(_003COnCrashed_003Ec__AnonStorey10._003COnCrashed_003Ec__asyncF))] () =>
		{
			_003COnCrashed_003Ec__AnonStorey10._003COnCrashed_003Ec__asyncF stateMachine = default(_003COnCrashed_003Ec__AnonStorey10._003COnCrashed_003Ec__asyncF);
			stateMachine._003C_003Ef__ref_002416 = CS_0024_003C_003E8__locals0;
			stateMachine._0024builder = AsyncVoidMethodBuilder.Create();
			stateMachine._0024builder.Start(ref stateMachine);
		}));
	}

	private void ShowLog(ServerProc s)
	{
		int num = Array.IndexOf(new ServerProc[5] { login, chr, map, web, tool }, s);
		if (num >= 0)
		{
			logTabs.SelectedIndex = num;
		}
	}

	[DebuggerStepThrough]
	[AsyncStateMachine(typeof(_003CRefreshStatus_003Ec__async4))]
	private void RefreshStatus()
	{
		_003CRefreshStatus_003Ec__async4 stateMachine = default(_003CRefreshStatus_003Ec__async4);
		stateMachine._0024this = this;
		stateMachine._0024builder = AsyncVoidMethodBuilder.Create();
		stateMachine._0024builder.Start(ref stateMachine);
	}

	private void SetStatus(string name, bool on, bool external)
	{
		if (statusLabels.TryGetValue(name, out var value))
		{
			((Control)value).Text = ((!on) ? "● หย\u0e38ด" : ((!external) ? "● ทำงาน" : "● ทำงาน (เป\u0e34ดจากท\u0e35\u0e48อ\u0e37\u0e48น)"));
			((Control)value).ForeColor = ((!on) ? Theme.Muted : ((!external) ? Theme.Good : Theme.Warn));
		}
	}

	private void ToggleTheme()
	{
		//IL_0087: Unknown result type (might be due to invalid IL or missing references)
		//IL_008d: Invalid comparison between Unknown and I4
		//IL_0073: Unknown result type (might be due to invalid IL or missing references)
		ini["Theme"] = ((!Theme.Dark) ? "dark" : "light");
		Ini.Save(iniPath, ini);
		if (servers.Any((ServerProc x) => x.OwnRunning))
		{
			MessageBox.Show((IWin32Window)(object)this, "บ\u0e31นท\u0e36กแล\u0e49ว ธ\u0e35มใหม\u0e48จะม\u0e35ผลตอนเป\u0e34ดโปรแกรมคร\u0e31\u0e49งถ\u0e31ดไป\n(ตอนน\u0e35\u0e49ม\u0e35เซ\u0e34ร\u0e4cฟท\u0e35\u0e48เป\u0e34ดจากโปรแกรมน\u0e35\u0e49อย\u0e39\u0e48 เลยย\u0e31งไม\u0e48ร\u0e35สตาร\u0e4cทโปรแกรมให\u0e49)", ((Control)this).Text);
		}
		else if ((int)MessageBox.Show((IWin32Window)(object)this, "เปล\u0e35\u0e48ยนธ\u0e35มต\u0e49องเป\u0e34ดโปรแกรมใหม\u0e48 ต\u0e49องการเป\u0e34ดใหม\u0e48เลยไหม?", ((Control)this).Text, (MessageBoxButtons)4) == 6)
		{
			db.Close();
			Application.Restart();
			Environment.Exit(0);
		}
	}

	private void OpenPhpMyAdmin()
	{
		string text = "8080";
		try
		{
			Dictionary<string, string> dictionary = Ini.Load(Path.Combine(dbDir, "settings\\usbwebserver.ini"));
			string[] array = File.ReadAllLines(Path.Combine(dbDir, "settings\\usbwebserver.ini"));
			foreach (string input in array)
			{
				Match match = Regex.Match(input, "^port=(\\d+)");
				if (match.Success)
				{
					text = match.Groups[1].Value;
					break;
				}
			}
		}
		catch
		{
		}
		try
		{
			Process.Start("http://localhost:" + text + "/phpmyadmin/");
		}
		catch
		{
		}
	}

	private void SaveLog(RichTextBox box)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0042: Invalid comparison between Unknown and I4
		SaveFileDialog val = new SaveFileDialog();
		((FileDialog)val).Filter = "Text|*.txt";
		((FileDialog)val).FileName = "log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt";
		SaveFileDialog val2 = val;
		try
		{
			if ((int)((CommonDialog)val2).ShowDialog((IWin32Window)(object)this) == 1)
			{
				File.WriteAllText(((FileDialog)val2).FileName, ((Control)box).Text, Encoding.UTF8);
			}
		}
		finally
		{
			((IDisposable)val2)?.Dispose();
		}
	}

	private void FlushLogs()
	{
		foreach (KeyValuePair<ServerProc, RichTextBox> logBox in logBoxes)
		{
			ConcurrentQueue<string> log = logBox.Key.Log;
			if (!log.IsEmpty)
			{
				RichTextBox value = logBox.Value;
				int num = 0;
				((Control)value).SuspendLayout();
				string result;
				while (num < 800 && log.TryDequeue(out result))
				{
					num++;
					((TextBoxBase)value).SelectionStart = ((TextBoxBase)value).TextLength;
					((TextBoxBase)value).SelectionLength = 0;
					value.SelectionColor = LineColor(result);
					((TextBoxBase)value).AppendText(result + "\n");
				}
				if (((TextBoxBase)value).Lines.Length > 6000)
				{
					int firstCharIndexFromLine = ((TextBoxBase)value).GetFirstCharIndexFromLine(1500);
					((TextBoxBase)value).ReadOnly = false;
					((TextBoxBase)value).Select(0, firstCharIndexFromLine);
					((TextBoxBase)value).SelectedText = string.Empty;
					((TextBoxBase)value).ReadOnly = true;
					((TextBoxBase)value).SelectionStart = ((TextBoxBase)value).TextLength;
				}
				((TextBoxBase)value).ScrollToCaret();
				((Control)value).ResumeLayout();
			}
		}
	}

	private static Color LineColor(string l)
	{
		if (l.Contains("[Error]") || l.Contains("[Fatal Error]") || l.Contains("[Debug]"))
		{
			return Color.FromArgb(255, 110, 100);
		}
		if (l.Contains("[Warning]"))
		{
			return Color.FromArgb(255, 190, 80);
		}
		if (l.Contains("[Status]"))
		{
			return Color.FromArgb(120, 220, 130);
		}
		if (l.Contains("[SQL]"))
		{
			return Color.FromArgb(200, 150, 255);
		}
		if (l.StartsWith("[Tool]"))
		{
			return Color.FromArgb(100, 190, 255);
		}
		if (l.Contains("[Notice]") || l.Contains("[Info]"))
		{
			return Color.FromArgb(230, 230, 230);
		}
		return Color.Gainsboro;
	}

	private bool TryConnectDb(bool log)
	{
		if (db.Connected)
		{
			return true;
		}
		bool flag;
		try
		{
			flag = db.Connect();
		}
		catch (DllNotFoundException)
		{
			tool.Add("[Tool][Error] ไม\u0e48พบ libmysql.dll ในโฟลเดอร\u0e4c Server");
			return false;
		}
		catch (Exception ex2)
		{
			tool.Add("[Tool][Error] " + ex2.Message);
			return false;
		}
		if (!flag && log)
		{
			tool.Add("[Tool][Error] ต\u0e48อฐานข\u0e49อม\u0e39ลไม\u0e48ได\u0e49: " + db.LastError);
		}
		if (flag)
		{
			tool.Add("[Tool] เช\u0e37\u0e48อมต\u0e48อฐานข\u0e49อม\u0e39ลแล\u0e49ว");
			UpdateCounts();
		}
		UpdateDbLabel();
		return flag;
	}

	private void UpdateDbLabel()
	{
		((ToolStripItem)stDb).Text = ((!db.Connected) ? "DB: ไม\u0e48ได\u0e49เช\u0e37\u0e48อมต\u0e48อ" : "DB: เช\u0e37\u0e48อมต\u0e48อแล\u0e49ว");
		((ToolStripItem)stDb).ForeColor = ((!db.Connected) ? Theme.Bad : Theme.Good);
	}

	private bool NeedDb()
	{
		//IL_002d: Unknown result type (might be due to invalid IL or missing references)
		if (TryConnectDb(log: false))
		{
			return true;
		}
		MessageBox.Show((IWin32Window)(object)this, "ย\u0e31งต\u0e48อฐานข\u0e49อม\u0e39ลไม\u0e48ได\u0e49 — เป\u0e34ด MySQL ก\u0e48อน (แท\u0e47บเซ\u0e34ร\u0e4cฟเวอร\u0e4c)\n\n" + db.LastError, ((Control)this).Text, (MessageBoxButtons)0, (MessageBoxIcon)48);
		return false;
	}

	private static DataTable Fix(DataTable t)
	{
		if (t == null)
		{
			return null;
		}
		foreach (DataColumn column in t.Columns)
		{
			if (Heads.TryGetValue(column.ColumnName, out var value))
			{
				column.ColumnName = value;
			}
		}
		foreach (DataColumn column2 in t.Columns)
		{
			if (column2.DataType != typeof(string))
			{
				continue;
			}
			foreach (DataRow row in t.Rows)
			{
				string text = row[column2] as string;
				if (text == "ON")
				{
					row[column2] = "ออนไลน\u0e4c";
				}
			}
		}
		return t;
	}

	private DataTable Q(string sql)
	{
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			return Fix(db.Query(sql));
		}
		catch (Exception ex)
		{
			MessageBox.Show((IWin32Window)(object)this, ex.Message, ((Control)this).Text, (MessageBoxButtons)0, (MessageBoxIcon)16);
			return null;
		}
	}

	private void UpdateCounts()
	{
		if (!db.Connected)
		{
			((ToolStripItem)stCounts).Text = string.Empty;
			return;
		}
		try
		{
			DataTable dataTable = db.Query("SELECT (SELECT COUNT(*) FROM `login` WHERE sex<>'S'), (SELECT COUNT(*) FROM `char`), (SELECT COUNT(*) FROM `char` WHERE online=1)");
			if (dataTable.Rows.Count > 0)
			{
				((ToolStripItem)stCounts).Text = $"ไอด\u0e35 {dataTable.Rows[0][0]}   ต\u0e31วละคร {dataTable.Rows[0][1]}   ออนไลน\u0e4c {dataTable.Rows[0][2]}";
			}
		}
		catch
		{
		}
	}

	private static string CellStr(DataGridView g, int row, string col)
	{
		if (row < 0 || row >= g.Rows.Count || !g.Columns.Contains(col))
		{
			return null;
		}
		object value = g.Rows[row].Cells[col].Value;
		return (value != null && value != DBNull.Value) ? value.ToString() : null;
	}

	private static long SelId(DataGridView g, string col)
	{
		if (g.CurrentRow == null || !g.Columns.Contains(col))
		{
			return -1L;
		}
		object value = g.CurrentRow.Cells[col].Value;
		long result;
		return (value == null || !long.TryParse(value.ToString(), out result)) ? (-1) : result;
	}

	private void Bind(DataGridView g, DataTable t)
	{
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0066: Expected O, but got Unknown
		//IL_00be: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Expected O, but got Unknown
		g.AutoSizeColumnsMode = (DataGridViewAutoSizeColumnsMode)1;
		if (((Control)g).Tag is EditSpec)
		{
			if (g.IsCurrentCellInEditMode)
			{
				g.CancelEdit();
			}
			g.DataSource = null;
			g.Columns.Clear();
		}
		g.DataSource = t;
		g.AutoResizeColumns((DataGridViewAutoSizeColumnsMode)6);
		foreach (DataGridViewColumn item in (BaseCollection)g.Columns)
		{
			DataGridViewColumn val = item;
			if (val.Width > 320)
			{
				val.Width = 320;
			}
		}
		foreach (DataGridViewColumn item2 in (BaseCollection)g.Columns)
		{
			DataGridViewColumn val2 = item2;
			if (val2.ValueType == typeof(long))
			{
				((DataGridViewBand)val2).DefaultCellStyle.Alignment = (DataGridViewContentAlignment)64;
			}
		}
		ApplyEditable(g);
	}

	private void AddJob(DataTable t, string classCol = "อาช\u0e35พ")
	{
		if (!t.Columns.Contains(classCol))
		{
			return;
		}
		int ordinal = t.Columns[classCol].Ordinal;
		t.Columns.Add("_job", typeof(string));
		foreach (DataRow row in t.Rows)
		{
			row["_job"] = ((row[classCol] != DBNull.Value) ? Jobs.Name(Convert.ToInt64(row[classCol])) : string.Empty);
		}
		t.Columns.Remove(classCol);
		t.Columns["_job"].ColumnName = classCol;
		t.Columns[classCol].SetOrdinal(ordinal);
	}

	private static void StrCols(DataTable t, params string[] names)
	{
		foreach (string text in names)
		{
			if (!t.Columns.Contains(text) || t.Columns[text].DataType == typeof(string))
			{
				continue;
			}
			DataColumn dataColumn = t.Columns[text];
			int ordinal = dataColumn.Ordinal;
			DataColumn dataColumn2 = t.Columns.Add(text + "_s", typeof(string));
			foreach (DataRow row in t.Rows)
			{
				row[dataColumn2] = ((row[dataColumn] != DBNull.Value) ? ((IConvertible)Convert.ToString(row[dataColumn])) : ((IConvertible)DBNull.Value));
			}
			t.Columns.Remove(dataColumn);
			dataColumn2.ColumnName = text;
			dataColumn2.SetOrdinal(ordinal);
		}
	}

	private string GroupName(long id)
	{
		foreach (KeyValuePair<int, string> group in groups)
		{
			if (group.Key == id)
			{
				return group.Value;
			}
		}
		return id.ToString();
	}

	private static long Now()
	{
		return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds;
	}

	private void LoadOnline(bool silent)
	{
		if ((!silent) ? (!NeedDb()) : (!db.Connected))
		{
			((Control)lblOnline).Text = "ย\u0e31งไม\u0e48ได\u0e49เช\u0e37\u0e48อมต\u0e48อฐานข\u0e49อม\u0e39ล";
			return;
		}
		DataTable dataTable;
		try
		{
			dataTable = Fix(db.Query("SELECT c.name AS `h0_`, l.userid AS `h1_`, c.class AS `h2_`, c.base_level AS `Base Lv`, c.job_level AS `Job Lv`, c.last_map AS `h3_`, c.last_x AS `X`, c.last_y AS `Y`, c.zeny AS `Zeny`, c.char_id AS `char_id` FROM `char` c LEFT JOIN `login` l ON l.account_id=c.account_id WHERE c.online=1 ORDER BY c.name"));
		}
		catch (Exception ex)
		{
			((Control)lblOnline).Text = ex.Message;
			return;
		}
		AddJob(dataTable);
		Bind(gridOnline, dataTable);
		((Control)lblOnline).Text = "ออนไลน\u0e4c " + dataTable.Rows.Count + " คน   (" + DateTime.Now.ToString("HH:mm:ss") + ")";
	}

	private void SearchAccounts()
	{
		if (!NeedDb())
		{
			return;
		}
		selectedAccountId = -1L;
		string text = ((Control)txtAccSearch).Text.Trim();
		string text2 = string.Empty;
		if (text != string.Empty)
		{
			text2 = " WHERE userid LIKE '%" + Db.Esc(text).Replace("%", "\\%").Replace("_", "\\_") + "%'" + ((!long.TryParse(text, out var result)) ? string.Empty : (" OR account_id=" + result));
		}
		DataTable dataTable = Q("SELECT account_id AS `account_id`, userid AS `h1_`, user_pass AS `h11_`, sex AS `h4_`, group_id, state, unban_time, logincount AS `h5_`, lastlogin AS `h6_`, last_ip AS `h7_`, (SELECT COUNT(*) FROM `char` c WHERE c.account_id=login.account_id) AS `h8_`, email AS `h12_` FROM `login`" + text2 + " ORDER BY account_id LIMIT 1000");
		if (dataTable == null)
		{
			return;
		}
		StrCols(dataTable, "ไอด\u0e35", "รห\u0e31สผ\u0e48าน", "อ\u0e35เมล", "เพศ");
		dataTable.Columns.Add("สถานะ", typeof(string));
		long num = Now();
		foreach (DataRow row in dataTable.Rows)
		{
			long num2 = Convert.ToInt64(row["group_id"]);
			long num3 = Convert.ToInt64(row["state"]);
			long num4 = Convert.ToInt64(row["unban_time"]);
			if ((string)row["เพศ"] == "S")
			{
				row["สถานะ"] = "บ\u0e31ญช\u0e35ระบบเซ\u0e34ร\u0e4cฟ";
				continue;
			}
			switch (num3)
			{
			case 5L:
				row["สถานะ"] = "แบนถาวร";
				break;
			default:
				row["สถานะ"] = "ถ\u0e39กระง\u0e31บ (state " + num3 + ")";
				break;
			case 0L:
				if (num4 > num)
				{
					row["สถานะ"] = "แบนถ\u0e36ง " + new DateTime(1970, 1, 1).AddSeconds(num4).ToLocalTime().ToString("dd/MM/yyyy HH:mm");
				}
				else
				{
					row["สถานะ"] = "ปกต\u0e34";
				}
				break;
			}
		}
		dataTable.Columns.Remove("state");
		dataTable.Columns.Remove("unban_time");
		dataTable.Columns["group_id"].ColumnName = "กล\u0e38\u0e48ม";
		dataTable.Columns["กล\u0e38\u0e48ม"].SetOrdinal(4);
		dataTable.Columns["สถานะ"].SetOrdinal(5);
		Bind(gridAcc, dataTable);
	}

	private void LoadAccountChars()
	{
		long num = SelId(gridAcc, "account_id");
		if (num >= 0 && num != selectedAccountId && db.Connected)
		{
			selectedAccountId = num;
			DataTable dataTable = Q("SELECT char_num AS `h9_`, name AS `h0_`, class AS `h2_`, base_level AS `Base Lv`, job_level AS `Job Lv`, zeny AS `Zeny`, last_map AS `h3_`, IF(online=1,'ON','') AS `h10_`, char_id FROM `char` WHERE account_id=" + num + " ORDER BY char_num");
			if (dataTable != null)
			{
				StrCols(dataTable, "ช\u0e37\u0e48อ", "แผนท\u0e35\u0e48");
				Bind(gridAccChars, dataTable);
			}
		}
	}

	private bool SelectedAccount(out long id, out string userid)
	{
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
		id = SelId(gridAcc, "account_id");
		userid = ((gridAcc.CurrentRow != null) ? Convert.ToString(gridAcc.CurrentRow.Cells["ไอด\u0e35"].Value) : null);
		if (id < 0)
		{
			MessageBox.Show((IWin32Window)(object)this, "เล\u0e37อกไอด\u0e35ในตารางก\u0e48อน", ((Control)this).Text);
			return false;
		}
		if (Convert.ToString(gridAcc.CurrentRow.Cells["เพศ"].Value) == "S")
		{
			MessageBox.Show((IWin32Window)(object)this, "ไอด\u0e35น\u0e35\u0e49เป\u0e47นบ\u0e31ญช\u0e35ท\u0e35\u0e48 char/map server ใช\u0e49เช\u0e37\u0e48อมก\u0e31น (s1) ห\u0e49ามแก\u0e49ไขจากตรงน\u0e35\u0e49", ((Control)this).Text);
			return false;
		}
		return true;
	}

	private IEnumerable<string> GroupItems()
	{
		return groups.Select((KeyValuePair<int, string> g) => g.Key + " - " + g.Value);
	}

	private void CreateAccount()
	{
		//IL_0271: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_018e: Unknown result type (might be due to invalid IL or missing references)
		if (!NeedDb())
		{
			return;
		}
		string[] array = new Prompt("สร\u0e49างไอด\u0e35ใหม\u0e48").Text_("ไอด\u0e35", string.Empty).Text_("รห\u0e31สผ\u0e48าน", string.Empty, password: true).Text_("ย\u0e37นย\u0e31นรห\u0e31ส", string.Empty, password: true)
			.Combo("เพศ", new string[2] { "ชาย (M)", "หญ\u0e34ง (F)" })
			.Combo("กล\u0e38\u0e48ม", GroupItems())
			.Note("ไอด\u0e35 4-23 ต\u0e31วอ\u0e31กษร, รห\u0e31ส 4-32 ต\u0e31วอ\u0e31กษร (ภาษาอ\u0e31งกฤษ/ต\u0e31วเลข)")
			.Ask((IWin32Window)(object)this);
		if (array == null)
		{
			return;
		}
		string text = array[0].Trim();
		string text2 = array[1];
		if (text.Length < 4 || text.Length > 23)
		{
			MessageBox.Show((IWin32Window)(object)this, "ไอด\u0e35ต\u0e49องยาว 4-23 ต\u0e31วอ\u0e31กษร", ((Control)this).Text);
			return;
		}
		if (text2.Length < 4 || text2.Length > 32)
		{
			MessageBox.Show((IWin32Window)(object)this, "รห\u0e31สผ\u0e48านต\u0e49องยาว 4-32 ต\u0e31วอ\u0e31กษร", ((Control)this).Text);
			return;
		}
		if (text2 != array[2])
		{
			MessageBox.Show((IWin32Window)(object)this, "รห\u0e31สผ\u0e48านไม\u0e48ตรงก\u0e31น", ((Control)this).Text);
			return;
		}
		string text3 = ((!(array[3] == "1")) ? "M" : "F");
		int key = groups[int.Parse(array[4])].Key;
		try
		{
			if (Convert.ToInt64(db.Scalar("SELECT COUNT(*) FROM `login` WHERE userid='" + Db.Esc(text) + "'")) > 0)
			{
				MessageBox.Show((IWin32Window)(object)this, "ม\u0e35ไอด\u0e35น\u0e35\u0e49อย\u0e39\u0e48แล\u0e49ว", ((Control)this).Text);
				return;
			}
			db.Exec("INSERT INTO `login` (userid, user_pass, sex, email, group_id) VALUES ('" + Db.Esc(text) + "','" + Db.Esc(PassValue(text2)) + "','" + text3 + "','a@a.com'," + key + ")");
			tool.Add("[Tool] สร\u0e49างไอด\u0e35 " + text + " (account_id " + db.InsertId() + ")");
			((Control)txtAccSearch).Text = text;
			SearchAccounts();
			UpdateCounts();
		}
		catch (Exception ex)
		{
			MessageBox.Show((IWin32Window)(object)this, ex.Message, ((Control)this).Text);
		}
	}

	private string PassValue(string pass)
	{
		Dictionary<string, string> d = Conf.Read(serverDir, "conf\\login_athena.conf");
		if (Conf.Get(d, "use_MD5_passwords", "no").ToLowerInvariant() == "yes")
		{
			using (MD5 mD = MD5.Create())
			{
				return string.Concat(from b in mD.ComputeHash(Encoding.ASCII.GetBytes(pass))
					select b.ToString("x2"));
			}
		}
		return pass;
	}

	private void ChangePassword()
	{
		//IL_0128: Unknown result type (might be due to invalid IL or missing references)
		//IL_0082: Unknown result type (might be due to invalid IL or missing references)
		//IL_010f: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		if (!NeedDb() || !SelectedAccount(out var id, out var userid))
		{
			return;
		}
		string[] array = new Prompt("เปล\u0e35\u0e48ยนรห\u0e31ส: " + userid).Text_("รห\u0e31สใหม\u0e48", string.Empty, password: true).Text_("ย\u0e37นย\u0e31นรห\u0e31ส", string.Empty, password: true).Ask((IWin32Window)(object)this);
		if (array == null)
		{
			return;
		}
		if (array[0].Length < 4 || array[0].Length > 32)
		{
			MessageBox.Show((IWin32Window)(object)this, "รห\u0e31สผ\u0e48านต\u0e49องยาว 4-32 ต\u0e31วอ\u0e31กษร", ((Control)this).Text);
			return;
		}
		if (array[0] != array[1])
		{
			MessageBox.Show((IWin32Window)(object)this, "รห\u0e31สผ\u0e48านไม\u0e48ตรงก\u0e31น", ((Control)this).Text);
			return;
		}
		try
		{
			db.Exec("UPDATE `login` SET user_pass='" + Db.Esc(PassValue(array[0])) + "' WHERE account_id=" + id);
			tool.Add("[Tool] เปล\u0e35\u0e48ยนรห\u0e31สไอด\u0e35 " + userid);
			MessageBox.Show((IWin32Window)(object)this, "เปล\u0e35\u0e48ยนรห\u0e31สแล\u0e49ว", ((Control)this).Text);
		}
		catch (Exception ex)
		{
			MessageBox.Show((IWin32Window)(object)this, ex.Message, ((Control)this).Text);
		}
	}

	private void SetGroup()
	{
		//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
		if (!NeedDb() || !SelectedAccount(out var id, out var userid))
		{
			return;
		}
		string[] array = new Prompt("ต\u0e31\u0e49งกล\u0e38\u0e48ม: " + userid).Combo("กล\u0e38\u0e48ม", GroupItems()).Note("99 = Admin (GM ส\u0e39งส\u0e38ด), 0 = ผ\u0e39\u0e49เล\u0e48นท\u0e31\u0e48วไป").Ask((IWin32Window)(object)this);
		if (array == null)
		{
			return;
		}
		int key = groups[int.Parse(array[0])].Key;
		try
		{
			db.Exec("UPDATE `login` SET group_id=" + key + " WHERE account_id=" + id);
			tool.Add("[Tool] ต\u0e31\u0e49งกล\u0e38\u0e48มไอด\u0e35 " + userid + " = " + key);
			SearchAccounts();
		}
		catch (Exception ex)
		{
			MessageBox.Show((IWin32Window)(object)this, ex.Message, ((Control)this).Text);
		}
	}

	private void Ban(bool perma)
	{
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Invalid comparison between Unknown and I4
		if (!NeedDb() || !SelectedAccount(out var id, out var userid))
		{
			return;
		}
		string sql;
		if (perma)
		{
			if ((int)MessageBox.Show((IWin32Window)(object)this, "แบนไอด\u0e35 " + userid + " ถาวร?", ((Control)this).Text, (MessageBoxButtons)4, (MessageBoxIcon)48) != 6)
			{
				return;
			}
			sql = "UPDATE `login` SET state=5 WHERE account_id=" + id;
		}
		else
		{
			string[] array = new Prompt("แบนช\u0e31\u0e48วคราว: " + userid).Number("จำนวนช\u0e31\u0e48วโมง", 24m, 1m, 87600m).Ask((IWin32Window)(object)this);
			if (array == null)
			{
				return;
			}
			long num = (long)decimal.Parse(array[0]);
			sql = "UPDATE `login` SET state=0, unban_time=" + (Now() + num * 3600) + " WHERE account_id=" + id;
		}
		try
		{
			db.Exec(sql);
			tool.Add("[Tool] แบนไอด\u0e35 " + userid);
			SearchAccounts();
		}
		catch (Exception ex)
		{
			MessageBox.Show((IWin32Window)(object)this, ex.Message, ((Control)this).Text);
		}
	}

	private void Unban()
	{
		//IL_0066: Unknown result type (might be due to invalid IL or missing references)
		if (!NeedDb() || !SelectedAccount(out var id, out var userid))
		{
			return;
		}
		try
		{
			db.Exec("UPDATE `login` SET state=0, unban_time=0 WHERE account_id=" + id);
			tool.Add("[Tool] ปลดแบนไอด\u0e35 " + userid);
			SearchAccounts();
		}
		catch (Exception ex)
		{
			MessageBox.Show((IWin32Window)(object)this, ex.Message, ((Control)this).Text);
		}
	}

	private void SearchChars()
	{
		if (NeedDb())
		{
			lastCharItems = -1L;
			string text = Db.Esc(((Control)txtCharSearch).Text.Trim()).Replace("%", "\\%").Replace("_", "\\_");
			DataTable dataTable = Q("SELECT c.char_id, c.name AS `h0_`, l.userid AS `h1_`, c.class AS `h2_`, c.base_level AS `Base Lv`, c.job_level AS `Job Lv`, c.zeny AS `Zeny`, c.str AS `STR`, c.agi AS `AGI`, c.vit AS `VIT`, c.`int` AS `INT`, c.dex AS `DEX`, c.luk AS `LUK`, c.last_map AS `h3_`, IF(c.online=1,'ON','') AS `h10_`, c.account_id FROM `char` c LEFT JOIN `login` l ON l.account_id=c.account_id " + ((!(text == string.Empty)) ? ("WHERE c.name LIKE '%" + text + "%' OR l.userid LIKE '%" + text + "%' ") : string.Empty) + "ORDER BY c.name LIMIT 1000");
			if (dataTable != null)
			{
				StrCols(dataTable, "ช\u0e37\u0e48อ", "ไอด\u0e35", "แผนท\u0e35\u0e48");
				Bind(gridChars, dataTable);
			}
		}
	}

	private void TopList(string by)
	{
		if (NeedDb())
		{
			lastCharItems = -1L;
			string text = ((!(by == "zeny")) ? "c.base_level DESC, c.base_exp DESC" : "c.zeny DESC");
			DataTable dataTable = Q("SELECT c.char_id, c.name AS `h0_`, l.userid AS `h1_`, c.class AS `h2_`, c.base_level AS `Base Lv`, c.job_level AS `Job Lv`, c.zeny AS `Zeny`, c.str AS `STR`, c.agi AS `AGI`, c.vit AS `VIT`, c.`int` AS `INT`, c.dex AS `DEX`, c.luk AS `LUK`, c.last_map AS `h3_`, IF(c.online=1,'ON','') AS `h10_`, c.account_id FROM `char` c LEFT JOIN `login` l ON l.account_id=c.account_id WHERE l.group_id < 10 OR l.group_id IS NULL ORDER BY " + text + " LIMIT 100");
			if (dataTable != null)
			{
				StrCols(dataTable, "ช\u0e37\u0e48อ", "ไอด\u0e35", "แผนท\u0e35\u0e48");
				Bind(gridChars, dataTable);
			}
		}
	}

	private void EnsureItems()
	{
		if (items.Loaded)
		{
			return;
		}
		((Control)this).Cursor = Cursors.WaitCursor;
		try
		{
			items.Load(serverDir, preRe);
			tool.Add("[Tool] โหลดช\u0e37\u0e48อไอเท\u0e47ม " + items.Names.Count + " รายการ");
		}
		catch (Exception ex)
		{
			tool.Add("[Tool][Error] โหลด item_db ไม\u0e48ได\u0e49: " + ex.Message);
			items.Loaded = true;
		}
		finally
		{
			((Control)this).Cursor = Cursors.Default;
		}
	}

	private void LoadCharItems()
	{
		long num = SelId(gridChars, "char_id");
		long num2 = SelId(gridChars, "account_id");
		if (num >= 0 && num != lastCharItems && db.Connected)
		{
			lastCharItems = num;
			EnsureItems();
			Bind(gridInv, ItemTable("SELECT nameid, amount, refine, equip, identify, attribute, card0, card1, card2, card3 FROM `inventory` WHERE char_id=" + num + " ORDER BY equip DESC, nameid"));
			Bind(gridCart, ItemTable("SELECT nameid, amount, refine, equip, identify, attribute, card0, card1, card2, card3 FROM `cart_inventory` WHERE char_id=" + num + " ORDER BY nameid"));
			Bind(gridStor, ItemTable("SELECT nameid, amount, refine, equip, identify, attribute, card0, card1, card2, card3 FROM `storage` WHERE account_id=" + num2 + " ORDER BY nameid"));
		}
	}

	private DataTable ItemTable(string sql)
	{
		DataTable dataTable = new DataTable();
		dataTable.Columns.Add("ID", typeof(long));
		dataTable.Columns.Add("ช\u0e37\u0e48อไอเท\u0e47ม", typeof(string));
		dataTable.Columns.Add("จำนวน", typeof(long));
		dataTable.Columns.Add("ต\u0e35บวก", typeof(string));
		dataTable.Columns.Add("สวมใส\u0e48", typeof(string));
		dataTable.Columns.Add("การ\u0e4cด / หมายเหต\u0e38", typeof(string));
		DataTable dataTable2;
		try
		{
			dataTable2 = db.Query(sql);
		}
		catch (Exception ex)
		{
			tool.Add("[Tool][Error] " + ex.Message);
			return dataTable;
		}
		foreach (DataRow row in dataTable2.Rows)
		{
			long num = Convert.ToInt64(row["nameid"]);
			long num2 = Convert.ToInt64(row["refine"]);
			string text;
			switch (Convert.ToInt64(row["card0"]))
			{
			case 254L:
			case 255L:
				text = "(ของท\u0e35\u0e48ต\u0e35ข\u0e36\u0e49น/สร\u0e49างเอง)";
				break;
			case -256L:
			case 65280L:
				text = "(ไข\u0e48ส\u0e31ตว\u0e4cเล\u0e35\u0e49ยง)";
				break;
			default:
			{
				List<string> list = new List<string>();
				string[] array = new string[4] { "card0", "card1", "card2", "card3" };
				foreach (string columnName in array)
				{
					long num3 = Convert.ToInt64(row[columnName]);
					if (num3 > 0)
					{
						list.Add(items.Name(num3));
					}
				}
				text = string.Join(", ", list);
				break;
			}
			}
			if (Convert.ToInt64(row["identify"]) == 0)
			{
				text = "(ย\u0e31งไม\u0e48ตรวจสอบ) " + text;
			}
			if (Convert.ToInt64(row["attribute"]) == 1)
			{
				text = "(พ\u0e31ง) " + text;
			}
			dataTable.Rows.Add(num, items.Name(num), Convert.ToInt64(row["amount"]), (num2 <= 0) ? string.Empty : ("+" + num2), (Convert.ToInt64(row["equip"]) == 0) ? string.Empty : "✓", text.Trim());
		}
		return dataTable;
	}

	private void SearchItemOwners()
	{
		if (!NeedDb())
		{
			return;
		}
		string text = ((Control)txtItemSearch).Text.Trim();
		if (text == string.Empty)
		{
			return;
		}
		EnsureItems();
		List<long> list = items.Find(text);
		if (list.Count == 0)
		{
			((Control)lblItemInfo).Text = "ไม\u0e48พบไอเท\u0e47มช\u0e37\u0e48อน\u0e35\u0e49ใน item_db";
			gridItemOwners.DataSource = null;
			return;
		}
		bool flag = list.Count > 300;
		if (flag)
		{
			list = list.Take(300).ToList();
		}
		string text2 = string.Join(",", list);
		string sql = "SELECT 'INV' AS src, c.name AS owner, l.userid AS acc, i.nameid, SUM(i.amount) AS amt, IF(c.online=1,'ON','') AS onl FROM `inventory` i JOIN `char` c ON c.char_id=i.char_id LEFT JOIN `login` l ON l.account_id=c.account_id WHERE i.nameid IN (" + text2 + ") GROUP BY c.char_id, i.nameid UNION ALL SELECT 'CART', c.name, l.userid, i.nameid, SUM(i.amount), IF(c.online=1,'ON','') FROM `cart_inventory` i JOIN `char` c ON c.char_id=i.char_id LEFT JOIN `login` l ON l.account_id=c.account_id WHERE i.nameid IN (" + text2 + ") GROUP BY c.char_id, i.nameid UNION ALL SELECT 'STOR', '-', l.userid, i.nameid, SUM(i.amount), '' FROM `storage` i LEFT JOIN `login` l ON l.account_id=i.account_id WHERE i.nameid IN (" + text2 + ") GROUP BY i.account_id, i.nameid UNION ALL SELECT 'GSTOR', g.name, '-', i.nameid, SUM(i.amount), '' FROM `guild_storage` i LEFT JOIN `guild` g ON g.guild_id=i.guild_id WHERE i.nameid IN (" + text2 + ") GROUP BY i.guild_id, i.nameid ORDER BY amt DESC LIMIT 2000";
		DataTable dataTable = Q(sql);
		if (dataTable == null)
		{
			return;
		}
		DataTable dataTable2 = new DataTable();
		dataTable2.Columns.Add("ท\u0e35\u0e48อย\u0e39\u0e48", typeof(string));
		dataTable2.Columns.Add("ต\u0e31วละคร / ก\u0e34ลด\u0e4c", typeof(string));
		dataTable2.Columns.Add("ไอด\u0e35", typeof(string));
		dataTable2.Columns.Add("ID", typeof(long));
		dataTable2.Columns.Add("ช\u0e37\u0e48อไอเท\u0e47ม", typeof(string));
		dataTable2.Columns.Add("จำนวน", typeof(long));
		dataTable2.Columns.Add("สถานะ", typeof(string));
		long num = 0L;
		foreach (DataRow row in dataTable.Rows)
		{
			long num2 = Convert.ToInt64(row["nameid"]);
			long num3 = Convert.ToInt64(row["amt"]);
			num += num3;
			dataTable2.Rows.Add(SrcName(Convert.ToString(row["src"])), row["owner"], row["acc"], num2, items.Name(num2), num3, row["onl"]);
		}
		Bind(gridItemOwners, dataTable2);
		((Control)lblItemInfo).Text = "ตรงก\u0e31บไอเท\u0e47ม " + list.Count + ((!flag) ? string.Empty : "+ (แสดง 300 แรก)") + " รายการ   •   พบ " + dataTable2.Rows.Count + " แถว   •   รวม " + num.ToString("N0") + " ช\u0e34\u0e49น";
	}

	private static string SrcName(string s)
	{
		return s switch
		{
			"INV" => "ของในต\u0e31ว", 
			"CART" => "รถเข\u0e47น", 
			"STOR" => "คล\u0e31ง", 
			"GSTOR" => "คล\u0e31งก\u0e34ลด\u0e4c", 
			_ => s, 
		};
	}

	protected override void OnFormClosing(FormClosingEventArgs e)
	{
		//IL_0047: Unknown result type (might be due to invalid IL or missing references)
		//IL_004c: Unknown result type (might be due to invalid IL or missing references)
		//IL_004d: Unknown result type (might be due to invalid IL or missing references)
		//IL_004f: Invalid comparison between Unknown and I4
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Invalid comparison between Unknown and I4
		if (!((CancelEventArgs)(object)e).Cancel && servers.Any((ServerProc s) => s.Running))
		{
			DialogResult val = MessageBox.Show((IWin32Window)(object)this, "เซ\u0e34ร\u0e4cฟย\u0e31งทำงานอย\u0e39\u0e48\n\nYes = ป\u0e34ดเซ\u0e34ร\u0e4cฟ (เซฟข\u0e49อม\u0e39ล) แล\u0e49วออก\nNo = ออกโดยปล\u0e48อยเซ\u0e34ร\u0e4cฟทำงานต\u0e48อเบ\u0e37\u0e49องหล\u0e31ง\nCancel = ยกเล\u0e34ก", ((Control)this).Text, (MessageBoxButtons)3, (MessageBoxIcon)32);
			if ((int)val == 2)
			{
				((CancelEventArgs)(object)e).Cancel = true;
				return;
			}
			if ((int)val == 6)
			{
				((CancelEventArgs)(object)e).Cancel = true;
				((Control)this).Enabled = false;
				StopAll().ContinueWith((Task _) => ((Control)this).BeginInvoke((Delegate)(Action)delegate
				{
					ServerProc[] array2 = servers;
					foreach (ServerProc serverProc2 in array2)
					{
						serverProc2.Stopping = true;
					}
					db.Close();
					Environment.Exit(0);
				}));
				return;
			}
			ServerProc[] array = servers;
			foreach (ServerProc serverProc in array)
			{
				serverProc.Stopping = true;
			}
		}
		db.Close();
		base.OnFormClosing(e);
	}

	private TabPage BuildMailTab()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected O, but got Unknown
		//IL_000b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0011: Expected O, but got Unknown
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Expected O, but got Unknown
		//IL_0057: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Expected O, but got Unknown
		//IL_006e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		//IL_0097: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Expected O, but got Unknown
		//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Expected O, but got Unknown
		//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cf: Expected O, but got Unknown
		//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Expected O, but got Unknown
		//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Expected O, but got Unknown
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Expected O, but got Unknown
		//IL_0138: Unknown result type (might be due to invalid IL or missing references)
		//IL_0142: Expected O, but got Unknown
		//IL_0150: Unknown result type (might be due to invalid IL or missing references)
		//IL_015a: Expected O, but got Unknown
		//IL_015b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Expected O, but got Unknown
		//IL_0177: Unknown result type (might be due to invalid IL or missing references)
		//IL_017e: Expected O, but got Unknown
		//IL_01a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a9: Expected O, but got Unknown
		//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01cc: Expected O, but got Unknown
		//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ef: Expected O, but got Unknown
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0212: Expected O, but got Unknown
		//IL_0234: Unknown result type (might be due to invalid IL or missing references)
		//IL_0285: Unknown result type (might be due to invalid IL or missing references)
		//IL_028c: Expected O, but got Unknown
		//IL_02ab: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b2: Expected O, but got Unknown
		//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_02de: Expected O, but got Unknown
		//IL_03aa: Unknown result type (might be due to invalid IL or missing references)
		//IL_03b1: Expected O, but got Unknown
		//IL_03c9: Unknown result type (might be due to invalid IL or missing references)
		//IL_03cf: Expected O, but got Unknown
		//IL_03ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f7: Expected O, but got Unknown
		//IL_0405: Unknown result type (might be due to invalid IL or missing references)
		//IL_040f: Expected O, but got Unknown
		//IL_041d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0427: Expected O, but got Unknown
		//IL_0435: Unknown result type (might be due to invalid IL or missing references)
		//IL_043f: Expected O, but got Unknown
		//IL_044d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0457: Expected O, but got Unknown
		//IL_0465: Unknown result type (might be due to invalid IL or missing references)
		//IL_046f: Expected O, but got Unknown
		//IL_0471: Unknown result type (might be due to invalid IL or missing references)
		//IL_0478: Expected O, but got Unknown
		//IL_0494: Unknown result type (might be due to invalid IL or missing references)
		//IL_049b: Expected O, but got Unknown
		//IL_04b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_04be: Expected O, but got Unknown
		//IL_04ea: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f1: Expected O, but got Unknown
		//IL_052f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0536: Expected O, but got Unknown
		//IL_0551: Unknown result type (might be due to invalid IL or missing references)
		//IL_0633: Unknown result type (might be due to invalid IL or missing references)
		//IL_063a: Expected O, but got Unknown
		//IL_0645: Unknown result type (might be due to invalid IL or missing references)
		//IL_06a4: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ab: Expected O, but got Unknown
		//IL_06c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ca: Expected O, but got Unknown
		//IL_06e0: Unknown result type (might be due to invalid IL or missing references)
		//IL_06e7: Expected O, but got Unknown
		//IL_06f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_0717: Unknown result type (might be due to invalid IL or missing references)
		//IL_0721: Expected O, but got Unknown
		//IL_0722: Unknown result type (might be due to invalid IL or missing references)
		//IL_0729: Expected O, but got Unknown
		//IL_0763: Unknown result type (might be due to invalid IL or missing references)
		//IL_0775: Unknown result type (might be due to invalid IL or missing references)
		//IL_077c: Expected O, but got Unknown
		//IL_07b3: Unknown result type (might be due to invalid IL or missing references)
		//IL_07cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_07d2: Expected O, but got Unknown
		//IL_07ed: Unknown result type (might be due to invalid IL or missing references)
		//IL_0817: Unknown result type (might be due to invalid IL or missing references)
		//IL_081e: Expected O, but got Unknown
		//IL_0839: Unknown result type (might be due to invalid IL or missing references)
		//IL_0863: Unknown result type (might be due to invalid IL or missing references)
		//IL_086a: Expected O, but got Unknown
		//IL_0885: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f1: Unknown result type (might be due to invalid IL or missing references)
		//IL_08f8: Expected O, but got Unknown
		//IL_09bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_09c2: Expected O, but got Unknown
		//IL_0a6d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a74: Expected O, but got Unknown
		//IL_0a80: Unknown result type (might be due to invalid IL or missing references)
		//IL_0a87: Expected O, but got Unknown
		//IL_0aa3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0aaa: Expected O, but got Unknown
		//IL_0ac6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0ad0: Expected O, but got Unknown
		//IL_0ad8: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b6f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0b76: Expected O, but got Unknown
		TabPage val = new TabPage("ส\u0e48งเมล / แจกของ");
		TableLayoutPanel val2 = new TableLayoutPanel();
		((Control)val2).Dock = (DockStyle)5;
		val2.ColumnCount = 2;
		val2.RowCount = 1;
		((Control)val2).Padding = new Padding(6);
		TableLayoutPanel val3 = val2;
		val3.ColumnStyles.Add(new ColumnStyle((SizeType)1, 560f));
		val3.ColumnStyles.Add(new ColumnStyle((SizeType)2, 100f));
		((Control)val).Controls.Add((Control)(object)val3);
		val2 = new TableLayoutPanel();
		((Control)val2).Dock = (DockStyle)5;
		val2.ColumnCount = 1;
		val2.RowCount = 4;
		TableLayoutPanel val4 = val2;
		val4.RowStyles.Add(new RowStyle((SizeType)1, 190f));
		val4.RowStyles.Add(new RowStyle((SizeType)1, 215f));
		val4.RowStyles.Add(new RowStyle((SizeType)2, 100f));
		val4.RowStyles.Add(new RowStyle((SizeType)1, 50f));
		val3.Controls.Add((Control)(object)val4, 0, 0);
		GroupBox val5 = new GroupBox();
		((Control)val5).Text = "1. ผ\u0e39\u0e49ร\u0e31บ";
		((Control)val5).Dock = (DockStyle)5;
		GroupBox val6 = val5;
		val2 = new TableLayoutPanel();
		((Control)val2).Dock = (DockStyle)5;
		val2.ColumnCount = 2;
		TableLayoutPanel val7 = val2;
		val7.ColumnStyles.Add(new ColumnStyle((SizeType)1, 230f));
		val7.ColumnStyles.Add(new ColumnStyle((SizeType)2, 100f));
		FlowLayoutPanel val8 = new FlowLayoutPanel();
		((Control)val8).Dock = (DockStyle)5;
		val8.FlowDirection = (FlowDirection)1;
		FlowLayoutPanel val9 = val8;
		RadioButton val10 = new RadioButton();
		((Control)val10).Text = "ระบ\u0e38ช\u0e37\u0e48อต\u0e31วละคร";
		((Control)val10).AutoSize = true;
		val10.Checked = true;
		rbNames = val10;
		val10 = new RadioButton();
		((Control)val10).Text = "ผ\u0e39\u0e49เล\u0e48นท\u0e35\u0e48ออนไลน\u0e4cอย\u0e39\u0e48ตอนน\u0e35\u0e49";
		((Control)val10).AutoSize = true;
		rbOnline = val10;
		val10 = new RadioButton();
		((Control)val10).Text = "ท\u0e38กต\u0e31วละครในเซ\u0e34ร\u0e4cฟ";
		((Control)val10).AutoSize = true;
		rbAllChars = val10;
		val10 = new RadioButton();
		((Control)val10).Text = "ท\u0e38กไอด\u0e35 (ไอด\u0e35ละ 1 ต\u0e31ว)";
		((Control)val10).AutoSize = true;
		rbPerAccount = val10;
		CheckBox val11 = new CheckBox();
		((Control)val11).Text = "ไม\u0e48ส\u0e48งให\u0e49ไอด\u0e35 GM";
		((Control)val11).AutoSize = true;
		val11.Checked = true;
		((Control)val11).Margin = new Padding(3, 8, 3, 3);
		chkNoGm = val11;
		((Control)val9).Controls.AddRange((Control[])(object)new Control[5]
		{
			(Control)rbNames,
			(Control)rbOnline,
			(Control)rbAllChars,
			(Control)rbPerAccount,
			(Control)chkNoGm
		});
		TextBox val12 = new TextBox();
		((TextBoxBase)val12).Multiline = true;
		((Control)val12).Dock = (DockStyle)5;
		val12.ScrollBars = (ScrollBars)2;
		txtMailNames = val12;
		Panel val13 = new Panel();
		((Control)val13).Dock = (DockStyle)5;
		Panel val14 = val13;
		((Control)val14).Controls.Add((Control)(object)txtMailNames);
		Control.ControlCollection controls = ((Control)val14).Controls;
		Label val15 = new Label();
		((Control)val15).Text = "ช\u0e37\u0e48อต\u0e31วละคร (บรรท\u0e31ดละช\u0e37\u0e48อ หร\u0e37อค\u0e31\u0e48นด\u0e49วย ,)";
		((Control)val15).Dock = (DockStyle)1;
		((Control)val15).Height = 20;
		((Control)val15).ForeColor = Theme.Muted;
		controls.Add((Control)(object)val15);
		RadioButton[] array = (RadioButton[])(object)new RadioButton[4] { rbNames, rbOnline, rbAllChars, rbPerAccount };
		foreach (RadioButton val16 in array)
		{
			val16.CheckedChanged += delegate
			{
				((Control)txtMailNames).Enabled = rbNames.Checked;
			};
		}
		val7.Controls.Add((Control)(object)val9, 0, 0);
		val7.Controls.Add((Control)(object)val14, 1, 0);
		((Control)val6).Controls.Add((Control)(object)val7);
		val4.Controls.Add((Control)(object)val6, 0, 0);
		val5 = new GroupBox();
		((Control)val5).Text = "2. ข\u0e49อความ";
		((Control)val5).Dock = (DockStyle)5;
		GroupBox val17 = val5;
		val2 = new TableLayoutPanel();
		((Control)val2).Dock = (DockStyle)5;
		val2.ColumnCount = 2;
		TableLayoutPanel val18 = val2;
		val18.ColumnStyles.Add(new ColumnStyle((SizeType)1, 80f));
		val18.ColumnStyles.Add(new ColumnStyle((SizeType)2, 100f));
		val18.RowStyles.Add(new RowStyle((SizeType)1, 30f));
		val18.RowStyles.Add(new RowStyle((SizeType)1, 30f));
		val18.RowStyles.Add(new RowStyle((SizeType)2, 100f));
		val18.RowStyles.Add(new RowStyle((SizeType)1, 30f));
		val12 = new TextBox();
		((Control)val12).Text = "GM";
		((Control)val12).Dock = (DockStyle)5;
		txtMailSender = val12;
		val12 = new TextBox();
		((Control)val12).Text = "ของขว\u0e31ญจากท\u0e35มงาน";
		((Control)val12).Dock = (DockStyle)5;
		txtMailTitle = val12;
		val12 = new TextBox();
		((TextBoxBase)val12).Multiline = true;
		((Control)val12).Dock = (DockStyle)5;
		val12.ScrollBars = (ScrollBars)2;
		((Control)val12).Text = "ขอบค\u0e38ณท\u0e35\u0e48ร\u0e48วมเล\u0e48นก\u0e31บเรา";
		txtMailBody = val12;
		NumericUpDown val19 = new NumericUpDown();
		val19.Maximum = 1000000000m;
		val19.ThousandsSeparator = true;
		((Control)val19).Width = 160;
		val19.Increment = 10000m;
		numMailZeny = val19;
		val15 = new Label();
		((Control)val15).AutoSize = true;
		((Control)val15).ForeColor = Theme.Muted;
		((Control)val15).Margin = new Padding(12, 6, 3, 3);
		lblMailBytes = val15;
		EventHandler eventHandler = delegate
		{
			UpdateMailBytes();
		};
		((Control)txtMailSender).TextChanged += eventHandler;
		((Control)txtMailTitle).TextChanged += eventHandler;
		((Control)txtMailBody).TextChanged += eventHandler;
		val18.Controls.Add((Control)(object)MsgLabel("ผ\u0e39\u0e49ส\u0e48ง"), 0, 0);
		val18.Controls.Add((Control)(object)txtMailSender, 1, 0);
		val18.Controls.Add((Control)(object)MsgLabel("ห\u0e31วข\u0e49อ"), 0, 1);
		val18.Controls.Add((Control)(object)txtMailTitle, 1, 1);
		val18.Controls.Add((Control)(object)MsgLabel("ข\u0e49อความ"), 0, 2);
		val18.Controls.Add((Control)(object)txtMailBody, 1, 2);
		val18.Controls.Add((Control)(object)MsgLabel("Zeny"), 0, 3);
		val8 = new FlowLayoutPanel();
		((Control)val8).Dock = (DockStyle)5;
		((Control)val8).Margin = new Padding(0);
		FlowLayoutPanel val20 = val8;
		((Control)val20).Controls.Add((Control)(object)numMailZeny);
		((Control)val20).Controls.Add((Control)(object)lblMailBytes);
		val18.Controls.Add((Control)(object)val20, 1, 3);
		((Control)val17).Controls.Add((Control)(object)val18);
		val4.Controls.Add((Control)(object)val17, 0, 1);
		val5 = new GroupBox();
		((Control)val5).Text = "3. ไอเท\u0e47ม (ส\u0e39งส\u0e38ด 5 ช\u0e48อง)";
		((Control)val5).Dock = (DockStyle)5;
		GroupBox val21 = val5;
		val8 = new FlowLayoutPanel();
		((Control)val8).Dock = (DockStyle)1;
		((Control)val8).Height = 36;
		FlowLayoutPanel val22 = val8;
		val12 = new TextBox();
		((Control)val12).Width = 170;
		((Control)val12).Margin = new Padding(3, 6, 3, 3);
		txtMailItem = val12;
		((Control)txtMailItem).KeyDown += (KeyEventHandler)delegate(object s, KeyEventArgs e)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Invalid comparison between Unknown and I4
			if ((int)e.KeyCode == 13)
			{
				e.SuppressKeyPress = true;
				AddMailItem();
			}
		};
		val19 = new NumericUpDown();
		val19.Minimum = 1m;
		val19.Maximum = 30000m;
		val19.Value = 1m;
		((Control)val19).Width = 70;
		((Control)val19).Margin = new Padding(3, 6, 3, 3);
		numItemAmount = val19;
		val19 = new NumericUpDown();
		val19.Minimum = 0m;
		val19.Maximum = 20m;
		val19.Value = 0m;
		((Control)val19).Width = 50;
		((Control)val19).Margin = new Padding(3, 6, 3, 3);
		numItemRefine = val19;
		Control.ControlCollection controls2 = ((Control)val22).Controls;
		val15 = new Label();
		((Control)val15).Text = "ID/ช\u0e37\u0e48อ";
		((Control)val15).AutoSize = true;
		((Control)val15).Margin = new Padding(3, 9, 0, 3);
		controls2.Add((Control)(object)val15);
		((Control)val22).Controls.Add((Control)(object)txtMailItem);
		Control.ControlCollection controls3 = ((Control)val22).Controls;
		val15 = new Label();
		((Control)val15).Text = "จำนวน";
		((Control)val15).AutoSize = true;
		((Control)val15).Margin = new Padding(3, 9, 0, 3);
		controls3.Add((Control)(object)val15);
		((Control)val22).Controls.Add((Control)(object)numItemAmount);
		Control.ControlCollection controls4 = ((Control)val22).Controls;
		val15 = new Label();
		((Control)val15).Text = "+";
		((Control)val15).AutoSize = true;
		((Control)val15).Margin = new Padding(3, 9, 0, 3);
		controls4.Add((Control)(object)val15);
		((Control)val22).Controls.Add((Control)(object)numItemRefine);
		((Control)val22).Controls.Add((Control)(object)Btn("เพ\u0e34\u0e48ม", delegate
		{
			AddMailItem();
		}, 60));
		((Control)val22).Controls.Add((Control)(object)Btn("ลบ", delegate
		{
			RemoveMailItem();
		}, 50));
		ListView val23 = new ListView();
		((Control)val23).Dock = (DockStyle)5;
		val23.View = (View)1;
		val23.FullRowSelect = true;
		val23.GridLines = true;
		val23.HideSelection = false;
		lvMailItems = val23;
		lvMailItems.Columns.Add("ID", 70);
		lvMailItems.Columns.Add("ช\u0e37\u0e48อไอเท\u0e47ม", 280);
		lvMailItems.Columns.Add("จำนวน", 70, (HorizontalAlignment)1);
		lvMailItems.Columns.Add("ต\u0e35บวก", 60, (HorizontalAlignment)1);
		((Control)val21).Controls.Add((Control)(object)lvMailItems);
		((Control)val21).Controls.Add((Control)(object)val22);
		val4.Controls.Add((Control)(object)val21, 0, 2);
		val8 = new FlowLayoutPanel();
		((Control)val8).Dock = (DockStyle)5;
		val8.FlowDirection = (FlowDirection)2;
		FlowLayoutPanel val24 = val8;
		Button val25 = Btn("✉  ส\u0e48งเมล", delegate
		{
			SendMail();
		}, 150);
		((Control)val25).Height = 38;
		((Control)val25).BackColor = Theme.BtnSend;
		((Control)val24).Controls.Add((Control)(object)val25);
		((Control)val24).Controls.Add((Control)(object)Btn("ตรวจสอบผ\u0e39\u0e49ร\u0e31บ", delegate
		{
			CheckRecipients();
		}, 120));
		((Control)val24).Controls.Add((Control)(object)Btn("ล\u0e49างฟอร\u0e4cม", delegate
		{
			ClearMailForm();
		}, 90));
		val4.Controls.Add((Control)(object)val24, 0, 3);
		val13 = new Panel();
		((Control)val13).Dock = (DockStyle)5;
		Panel val26 = val13;
		val8 = new FlowLayoutPanel();
		((Control)val8).Dock = (DockStyle)1;
		((Control)val8).Height = 40;
		FlowLayoutPanel val27 = val8;
		Control.ControlCollection controls5 = ((Control)val27).Controls;
		val15 = new Label();
		((Control)val15).Text = "เมลท\u0e35\u0e48ระบบส\u0e48ง (ล\u0e48าส\u0e38ด 300)";
		((Control)val15).AutoSize = true;
		((Control)val15).Font = new Font(UiFont, (FontStyle)1);
		((Control)val15).Margin = new Padding(3, 10, 12, 3);
		controls5.Add((Control)(object)val15);
		((Control)val27).Controls.Add((Control)(object)Btn("ร\u0e35เฟรช", delegate
		{
			LoadMailHistory();
		}, 80));
		((Control)val27).Controls.Add((Control)(object)Btn("ลบเมลท\u0e35\u0e48เล\u0e37อก", delegate
		{
			DeleteMail();
		}));
		gridMailHist = Grid();
		gridMailHist.MultiSelect = true;
		((Control)val26).Controls.Add((Control)(object)gridMailHist);
		((Control)val26).Controls.Add((Control)(object)val27);
		Control.ControlCollection controls6 = ((Control)val26).Controls;
		val15 = new Label();
		((Control)val15).Dock = (DockStyle)2;
		((Control)val15).Height = 84;
		((Control)val15).ForeColor = Theme.Muted;
		((Control)val15).Text = "• ผ\u0e39\u0e49เล\u0e48นกดเป\u0e34ดกล\u0e48องจดหมาย (RODEX) แล\u0e49วร\u0e31บของได\u0e49เลย ถ\u0e49าออนไลน\u0e4cอย\u0e39\u0e48ให\u0e49ป\u0e34ด-เป\u0e34ดกล\u0e48องจดหมายใหม\u0e48\n• เมลท\u0e35\u0e48ย\u0e31งไม\u0e48ร\u0e31บจะถ\u0e39กลบอ\u0e31ตโนม\u0e31ต\u0e34ตาม mail_return_days ใน char_athena.conf (ตอนน\u0e35\u0e49 15 ว\u0e31น)\n• กล\u0e48องจดหมายแสดงได\u0e49 30 ฉบ\u0e31บ ถ\u0e49าเต\u0e47ม เมลใหม\u0e48จะโผล\u0e48หล\u0e31งผ\u0e39\u0e49เล\u0e48นลบเมลเก\u0e48า";
		controls6.Add((Control)(object)val15);
		val3.Controls.Add((Control)(object)val26, 1, 0);
		UpdateMailBytes();
		return val;
	}

	private static Label MsgLabel(string t)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Expected O, but got Unknown
		//IL_0020: Unknown result type (might be due to invalid IL or missing references)
		Label val = new Label();
		((Control)val).Text = t;
		((Control)val).AutoSize = true;
		((Control)val).Anchor = (AnchorStyles)4;
		((Control)val).Margin = new Padding(3, 7, 3, 3);
		return val;
	}

	private int Bytes(string s)
	{
		return db.Enc.GetByteCount(s ?? string.Empty);
	}

	private void UpdateMailBytes()
	{
		if (lblMailBytes != null)
		{
			int num = Bytes(((Control)txtMailTitle).Text);
			int num2 = Bytes(((Control)txtMailBody).Text);
			int num3 = Bytes(((Control)txtMailSender).Text);
			((Control)lblMailBytes).Text = $"ห\u0e31วข\u0e49อ {num}/39   ข\u0e49อความ {num2}/499   ผ\u0e39\u0e49ส\u0e48ง {num3}/23";
			((Control)lblMailBytes).ForeColor = ((num <= 39 && num2 <= 499 && num3 <= 23 && num3 != 0) ? Theme.Muted : Theme.Bad);
		}
	}

	private void AddMailItem()
	{
		//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Invalid comparison between Unknown and I4
		//IL_0059: Unknown result type (might be due to invalid IL or missing references)
		//IL_0196: Unknown result type (might be due to invalid IL or missing references)
		//IL_0123: Unknown result type (might be due to invalid IL or missing references)
		EnsureItems();
		string text = ((Control)txtMailItem).Text.Trim();
		if (text == string.Empty)
		{
			return;
		}
		if (!long.TryParse(text, out var result))
		{
			List<long> list = items.Find(text);
			if (list.Count == 0)
			{
				MessageBox.Show((IWin32Window)(object)this, "ไม\u0e48พบไอเท\u0e47มช\u0e37\u0e48อน\u0e35\u0e49", ((Control)this).Text);
				return;
			}
			result = ((list.Count != 1) ? PickItem(list) : list[0]);
			if (result <= 0)
			{
				return;
			}
		}
		if (!items.Exists(result) && (int)MessageBox.Show((IWin32Window)(object)this, "ไม\u0e48พบ ID " + result + " ใน item_db ต\u0e49องการเพ\u0e34\u0e48มต\u0e48อไหม?", ((Control)this).Text, (MessageBoxButtons)4) != 6)
		{
			return;
		}
		int num = (int)numItemAmount.Value;
		int num2 = (int)numItemRefine.Value;
		if (items.Stackable(result))
		{
			if (num2 > 0)
			{
				num2 = 0;
			}
			if (mailItems.Count >= 5)
			{
				MessageBox.Show((IWin32Window)(object)this, "ใส\u0e48ได\u0e49ส\u0e39งส\u0e38ด 5 ช\u0e48อง", ((Control)this).Text);
				return;
			}
			mailItems.Add(new MailItem
			{
				Id = result,
				Amount = num,
				Refine = 0
			});
		}
		else
		{
			if (mailItems.Count + num > 5)
			{
				MessageBox.Show((IWin32Window)(object)this, "อ\u0e38ปกรณ\u0e4cใช\u0e49 1 ช\u0e48องต\u0e48อช\u0e34\u0e49น เหล\u0e37อช\u0e48องว\u0e48าง " + (5 - mailItems.Count) + " ช\u0e48อง", ((Control)this).Text);
				return;
			}
			for (int i = 0; i < num; i++)
			{
				mailItems.Add(new MailItem
				{
					Id = result,
					Amount = 1,
					Refine = num2
				});
			}
		}
		RefreshMailItems();
		((TextBoxBase)txtMailItem).Clear();
		numItemAmount.Value = 1m;
		numItemRefine.Value = 0m;
		((Control)txtMailItem).Focus();
	}

	private long PickItem(List<long> ids)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_000d: Expected O, but got Unknown
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006e: Expected O, but got Unknown
		//IL_011b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0122: Expected O, but got Unknown
		//IL_019f: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Invalid comparison between Unknown and I4
		Form val = new Form();
		((Control)val).Text = "เล\u0e37อกไอเท\u0e47ม (" + ids.Count + " รายการ)";
		val.Size = new Size(460, 480);
		val.StartPosition = (FormStartPosition)4;
		((Control)val).Font = UiFont;
		val.MinimizeBox = false;
		val.MaximizeBox = false;
		Form f = val;
		try
		{
			ListBox val2 = new ListBox();
			((Control)val2).Dock = (DockStyle)5;
			ListBox val3 = val2;
			foreach (long item in ids.OrderBy((long x) => x).Take(500))
			{
				val3.Items.Add((object)(item + "  -  " + items.Name(item)));
			}
			if (val3.Items.Count > 0)
			{
				((ListControl)val3).SelectedIndex = 0;
			}
			Button val4 = new Button();
			((Control)val4).Text = "เล\u0e37อก";
			((Control)val4).Dock = (DockStyle)2;
			((Control)val4).Height = 32;
			val4.DialogResult = (DialogResult)1;
			Button val5 = val4;
			((Control)val3).DoubleClick += delegate
			{
				f.DialogResult = (DialogResult)1;
				f.Close();
			};
			((Control)f).Controls.Add((Control)(object)val3);
			((Control)f).Controls.Add((Control)(object)val5);
			f.AcceptButton = (IButtonControl)(object)val5;
			Theme.Apply((Control)(object)f);
			if ((int)f.ShowDialog((IWin32Window)(object)this) != 1 || val3.SelectedItem == null)
			{
				return -1L;
			}
			return long.Parse(val3.SelectedItem.ToString().Split(new char[1] { ' ' })[0]);
		}
		finally
		{
			if (f != null)
			{
				((IDisposable)f).Dispose();
			}
		}
	}

	private void RemoveMailItem()
	{
		if (lvMailItems.SelectedIndices.Count == 0)
		{
			return;
		}
		foreach (int item in from int x in (IEnumerable)lvMailItems.SelectedIndices
			orderby x descending
			select x)
		{
			mailItems.RemoveAt(item);
		}
		RefreshMailItems();
	}

	private void RefreshMailItems()
	{
		//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ad: Expected O, but got Unknown
		lvMailItems.Items.Clear();
		foreach (MailItem mailItem in mailItems)
		{
			lvMailItems.Items.Add(new ListViewItem(new string[4]
			{
				mailItem.Id.ToString(),
				items.Name(mailItem.Id),
				mailItem.Amount.ToString("N0"),
				(mailItem.Refine <= 0) ? string.Empty : ("+" + mailItem.Refine)
			}));
		}
	}

	private void ClearMailForm()
	{
		mailItems.Clear();
		RefreshMailItems();
		((TextBoxBase)txtMailNames).Clear();
		numMailZeny.Value = 0m;
	}

	private string GmFilter(string loginAlias)
	{
		if (!chkNoGm.Checked)
		{
			return string.Empty;
		}
		List<string> list = (from kv in Groups.Levels
			where kv.Value > 0
			select kv.Key.ToString()).ToList();
		if (list.Count == 0)
		{
			list.Add("99");
		}
		return " AND (" + loginAlias + ".group_id IS NULL OR " + loginAlias + ".group_id NOT IN (" + string.Join(",", list) + "))";
	}

	private List<KeyValuePair<long, string>> ResolveRecipients(out List<string> notFound)
	{
		notFound = new List<string>();
		List<KeyValuePair<long, string>> list = new List<KeyValuePair<long, string>>();
		if (rbNames.Checked)
		{
			List<string> list2 = (from x in ((Control)txtMailNames).Text.Split(new char[3] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries)
				select x.Trim() into x
				where x != string.Empty
				select x).Distinct().ToList();
			if (list2.Count == 0)
			{
				return list;
			}
			DataTable dataTable = db.Query("SELECT c.char_id, c.name FROM `char` c LEFT JOIN `login` l ON l.account_id=c.account_id WHERE 1=1 AND c.name IN (" + string.Join(",", list2.Select((string n) => "'" + Db.Esc(n) + "'")) + ")");
			HashSet<string> got = new HashSet<string>();
			foreach (DataRow row in dataTable.Rows)
			{
				list.Add(new KeyValuePair<long, string>(Convert.ToInt64(row[0]), Convert.ToString(row[1])));
				got.Add(Convert.ToString(row[1]).ToLowerInvariant());
			}
			notFound = list2.Where((string n) => !got.Contains(n.ToLowerInvariant())).ToList();
			return list;
		}
		string text = (rbOnline.Checked ? ("SELECT c.char_id, c.name FROM `char` c LEFT JOIN `login` l ON l.account_id=c.account_id WHERE 1=1 AND c.online=1" + GmFilter("l")) : ((!rbAllChars.Checked) ? ("SELECT c.char_id, c.name FROM `char` c JOIN (SELECT account_id, MIN(char_num) n FROM `char` GROUP BY account_id) f ON f.account_id=c.account_id AND f.n=c.char_num LEFT JOIN `login` l ON l.account_id=c.account_id WHERE 1=1" + GmFilter("l")) : ("SELECT c.char_id, c.name FROM `char` c LEFT JOIN `login` l ON l.account_id=c.account_id WHERE 1=1" + GmFilter("l"))));
		foreach (DataRow row2 in db.Query(text + " ORDER BY c.char_id").Rows)
		{
			list.Add(new KeyValuePair<long, string>(Convert.ToInt64(row2[0]), Convert.ToString(row2[1])));
		}
		return list;
	}

	private string NoRecipientReason(List<string> nf)
	{
		string text = "ไม\u0e48ม\u0e35ผ\u0e39\u0e49ร\u0e31บ";
		if (rbNames.Checked)
		{
			if (((Control)txtMailNames).Text.Trim() == string.Empty)
			{
				return text + "\nพ\u0e34มพ\u0e4cช\u0e37\u0e48อต\u0e31วละครในช\u0e48องด\u0e49านขวาก\u0e48อน";
			}
			if (nf.Count > 0)
			{
				text = text + "\nไม\u0e48พบช\u0e37\u0e48อ: " + string.Join(", ", nf);
			}
			return text;
		}
		if (chkNoGm.Checked)
		{
			bool @checked = chkNoGm.Checked;
			try
			{
				chkNoGm.Checked = false;
				List<string> notFound;
				int count = ResolveRecipients(out notFound).Count;
				if (count > 0)
				{
					return text + "\n\nม\u0e35ต\u0e31วละครท\u0e35\u0e48ตรงเง\u0e37\u0e48อนไข " + count + " ต\u0e31ว แต\u0e48เป\u0e47นไอด\u0e35 GM ท\u0e31\u0e49งหมด\nถ\u0e49าจะส\u0e48งให\u0e49 GM ด\u0e49วย ให\u0e49เอาต\u0e34\u0e4aก \"ไม\u0e48ส\u0e48งให\u0e49ไอด\u0e35 GM\" ออก";
				}
			}
			catch
			{
			}
			finally
			{
				chkNoGm.Checked = @checked;
			}
		}
		if (rbOnline.Checked)
		{
			text += "\nตอนน\u0e35\u0e49ไม\u0e48ม\u0e35ผ\u0e39\u0e49เล\u0e48นออนไลน\u0e4c";
		}
		return text;
	}

	private void CheckRecipients()
	{
		//IL_0169: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_014e: Unknown result type (might be due to invalid IL or missing references)
		if (!NeedDb())
		{
			return;
		}
		try
		{
			List<string> notFound;
			List<KeyValuePair<long, string>> list = ResolveRecipients(out notFound);
			StringBuilder stringBuilder = new StringBuilder();
			if (list.Count == 0)
			{
				MessageBox.Show((IWin32Window)(object)this, NoRecipientReason(notFound), "ตรวจสอบผ\u0e39\u0e49ร\u0e31บ");
				return;
			}
			stringBuilder.AppendLine("ผ\u0e39\u0e49ร\u0e31บท\u0e31\u0e49งหมด " + list.Count + " ต\u0e31วละคร");
			if (list.Count > 0)
			{
				stringBuilder.AppendLine(string.Join(", ", from x in list.Take(30)
					select x.Value) + ((list.Count <= 30) ? string.Empty : " ..."));
			}
			if (notFound.Count > 0)
			{
				stringBuilder.AppendLine("\nไม\u0e48พบช\u0e37\u0e48อ: " + string.Join(", ", notFound));
			}
			List<string> list2 = FullInboxes(list);
			if (list2.Count > 0)
			{
				stringBuilder.AppendLine("\nกล\u0e48องจดหมายเต\u0e47ม (30): " + string.Join(", ", list2.Take(20)) + ((list2.Count <= 20) ? string.Empty : " ..."));
			}
			MessageBox.Show((IWin32Window)(object)this, stringBuilder.ToString(), "ตรวจสอบผ\u0e39\u0e49ร\u0e31บ");
		}
		catch (Exception ex)
		{
			MessageBox.Show((IWin32Window)(object)this, ex.Message, ((Control)this).Text);
		}
	}

	private List<string> FullInboxes(List<KeyValuePair<long, string>> r)
	{
		List<string> list = new List<string>();
		if (r.Count == 0)
		{
			return list;
		}
		Dictionary<long, string> dictionary = r.ToDictionary((KeyValuePair<long, string> x) => x.Key, (KeyValuePair<long, string> x) => x.Value);
		foreach (var item in from x in r.Select((KeyValuePair<long, string> x) => x.Key).Select((long v, int i) => new { v, i })
			group x by x.i / 500)
		{
			DataTable dataTable = db.Query("SELECT dest_id FROM `mail` WHERE status<3 AND dest_id IN (" + string.Join(",", item.Select(x => x.v)) + ") GROUP BY dest_id HAVING COUNT(*) >= " + 30);
			foreach (DataRow row in dataTable.Rows)
			{
				if (dictionary.TryGetValue(Convert.ToInt64(row[0]), out var value))
				{
					list.Add(value);
				}
			}
		}
		return list;
	}

	private void SendMail()
	{
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_008f: Unknown result type (might be due to invalid IL or missing references)
		//IL_068d: Unknown result type (might be due to invalid IL or missing references)
		//IL_065d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_0116: Invalid comparison between Unknown and I4
		//IL_0161: Unknown result type (might be due to invalid IL or missing references)
		//IL_032d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0333: Invalid comparison between Unknown and I4
		if (!NeedDb())
		{
			return;
		}
		EnsureItems();
		string text = ((Control)txtMailSender).Text.Trim();
		string text2 = ((Control)txtMailTitle).Text.Trim();
		string s = ((Control)txtMailBody).Text.Replace("\r\n", "\n").Trim();
		long num = (long)numMailZeny.Value;
		if (text == string.Empty || Bytes(text) > 23)
		{
			MessageBox.Show((IWin32Window)(object)this, "ช\u0e37\u0e48อผ\u0e39\u0e49ส\u0e48งต\u0e49องม\u0e35 1-23 ต\u0e31วอ\u0e31กษร", ((Control)this).Text);
		}
		else if (text2 == string.Empty || Bytes(text2) > 39)
		{
			MessageBox.Show((IWin32Window)(object)this, "ห\u0e31วข\u0e49อต\u0e49องม\u0e35 1-39 ต\u0e31วอ\u0e31กษร", ((Control)this).Text);
		}
		else if (Bytes(s) > 499)
		{
			MessageBox.Show((IWin32Window)(object)this, "ข\u0e49อความยาวเก\u0e34น 499 ต\u0e31วอ\u0e31กษร", ((Control)this).Text);
		}
		else
		{
			if (mailItems.Count == 0 && num == 0 && (int)MessageBox.Show((IWin32Window)(object)this, "ไม\u0e48ได\u0e49ใส\u0e48ไอเท\u0e47มหร\u0e37อ Zeny จะส\u0e48งเป\u0e47นข\u0e49อความอย\u0e48างเด\u0e35ยวไหม?", ((Control)this).Text, (MessageBoxButtons)4) != 6)
			{
				return;
			}
			List<KeyValuePair<long, string>> list;
			List<string> notFound;
			try
			{
				list = ResolveRecipients(out notFound);
			}
			catch (Exception ex)
			{
				MessageBox.Show((IWin32Window)(object)this, ex.Message, ((Control)this).Text);
				return;
			}
			if (list.Count == 0)
			{
				MessageBox.Show((IWin32Window)(object)this, NoRecipientReason(notFound), ((Control)this).Text);
				return;
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("ส\u0e48งเมลให\u0e49 " + list.Count.ToString("N0") + " ต\u0e31วละคร");
			if (list.Count <= 10)
			{
				stringBuilder.AppendLine("(" + string.Join(", ", list.Select((KeyValuePair<long, string> x) => x.Value)) + ")");
			}
			stringBuilder.AppendLine();
			stringBuilder.AppendLine("ห\u0e31วข\u0e49อ: " + text2);
			if (num > 0)
			{
				stringBuilder.AppendLine("Zeny: " + num.ToString("N0"));
			}
			foreach (MailItem mailItem in mailItems)
			{
				stringBuilder.AppendLine("• " + items.Name(mailItem.Id) + " x" + mailItem.Amount + ((mailItem.Refine <= 0) ? string.Empty : (" (+" + mailItem.Refine + ")")));
			}
			if (notFound.Count > 0)
			{
				stringBuilder.AppendLine("\nไม\u0e48พบช\u0e37\u0e48อ (จะข\u0e49าม): " + string.Join(", ", notFound));
			}
			stringBuilder.AppendLine("\nย\u0e37นย\u0e31นการส\u0e48ง?");
			if ((int)MessageBox.Show((IWin32Window)(object)this, stringBuilder.ToString(), "ย\u0e37นย\u0e31นส\u0e48งเมล", (MessageBoxButtons)4, (MessageBoxIcon)32) != 6)
			{
				return;
			}
			long num2 = Now();
			int num3 = 0;
			string text3 = null;
			((Control)this).Cursor = Cursors.WaitCursor;
			try
			{
				foreach (KeyValuePair<long, string> item in list)
				{
					db.Exec("INSERT INTO `mail` (send_name, send_id, dest_name, dest_id, title, message, time, status, zeny, type) VALUES ('" + Db.Esc(text) + "', 0, '" + Db.Esc(item.Value) + "', " + item.Key + ", '" + Db.Esc(text2) + "', '" + Db.Esc(s) + "', " + num2 + ", 0, " + num + ", 0)");
					long mailId = db.InsertId();
					if (mailItems.Count > 0)
					{
						IEnumerable<string> values = mailItems.Select((MailItem m, int i) => "(" + mailId + ", " + i + ", " + m.Id + ", " + m.Amount + ", " + m.Refine + ", 0, 1, 0, 0, 0, 0, 0, 0)");
						db.Exec("INSERT INTO `mail_attachments` (id, `index`, nameid, amount, refine, attribute, identify, card0, card1, card2, card3, unique_id, bound) VALUES " + string.Join(", ", values));
					}
					num3++;
				}
			}
			catch (Exception ex2)
			{
				text3 = ex2.Message;
			}
			finally
			{
				((Control)this).Cursor = Cursors.Default;
			}
			string text4 = string.Join(", ", mailItems.Select((MailItem m) => m.Id + "x" + m.Amount + ((m.Refine <= 0) ? string.Empty : ("+" + m.Refine))));
			string text5 = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " | ส\u0e48ง " + num3 + "/" + list.Count + " | " + text2 + " | zeny " + num + " | items " + text4 + " | to " + ((list.Count > 20) ? RecipientMode() : string.Join(",", list.Select((KeyValuePair<long, string> x) => x.Value)));
			tool.Add("[Tool] ส\u0e48งเมล: " + text5);
			try
			{
				File.AppendAllText(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "RO_Manager_mail.log"), text5 + Environment.NewLine, Encoding.UTF8);
			}
			catch
			{
			}
			if (text3 != null)
			{
				MessageBox.Show((IWin32Window)(object)this, "ส\u0e48งได\u0e49 " + num3 + " จาก " + list.Count + " แล\u0e49วเก\u0e34ดข\u0e49อผ\u0e34ดพลาด:\n" + text3, ((Control)this).Text, (MessageBoxButtons)0, (MessageBoxIcon)16);
			}
			else
			{
				MessageBox.Show((IWin32Window)(object)this, "ส\u0e48งเมลเร\u0e35ยบร\u0e49อย " + num3.ToString("N0") + " ฉบ\u0e31บ\nผ\u0e39\u0e49เล\u0e48นเป\u0e34ดกล\u0e48องจดหมายในเกมเพ\u0e37\u0e48อร\u0e31บของ", ((Control)this).Text, (MessageBoxButtons)0, (MessageBoxIcon)64);
			}
			LoadMailHistory();
		}
	}

	private string RecipientMode()
	{
		if (rbOnline.Checked)
		{
			return "ผ\u0e39\u0e49เล\u0e48นออนไลน\u0e4c";
		}
		if (rbAllChars.Checked)
		{
			return "ท\u0e38กต\u0e31วละคร";
		}
		if (rbPerAccount.Checked)
		{
			return "ท\u0e38กไอด\u0e35";
		}
		return "ระบ\u0e38ช\u0e37\u0e48อ";
	}

	private void LoadMailHistory()
	{
		if (!db.Connected)
		{
			return;
		}
		EnsureItems();
		DataTable dataTable;
		try
		{
			dataTable = db.Query("SELECT m.id, m.time, m.send_name, m.dest_name, m.title, m.zeny, m.status, (SELECT GROUP_CONCAT(CONCAT(a.nameid,':',a.amount) ORDER BY a.`index` SEPARATOR ';') FROM `mail_attachments` a WHERE a.id=m.id) AS att FROM `mail` m WHERE m.send_id=0 AND m.type=0 ORDER BY m.id DESC LIMIT 300");
		}
		catch (Exception ex)
		{
			tool.Add("[Tool][Error] " + ex.Message);
			return;
		}
		DataTable dataTable2 = new DataTable();
		dataTable2.Columns.Add("mail_id", typeof(long));
		dataTable2.Columns.Add("เวลา", typeof(string));
		dataTable2.Columns.Add("ถ\u0e36ง", typeof(string));
		dataTable2.Columns.Add("ห\u0e31วข\u0e49อ", typeof(string));
		dataTable2.Columns.Add("Zeny", typeof(long));
		dataTable2.Columns.Add("ไอเท\u0e47มท\u0e35\u0e48ย\u0e31งไม\u0e48ร\u0e31บ", typeof(string));
		dataTable2.Columns.Add("สถานะ", typeof(string));
		DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
		foreach (DataRow row in dataTable.Rows)
		{
			string text = ((row["att"] != DBNull.Value) ? Convert.ToString(row["att"]) : string.Empty);
			string text2 = string.Join(", ", text.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries).Select(delegate(string x)
			{
				string[] array = x.Split(new char[1] { ':' });
				long result;
				return (!long.TryParse(array[0], out result)) ? x : (items.Name(result) + " x" + ((array.Length <= 1) ? "?" : array[1]));
			}));
			long num = Convert.ToInt64(row["status"]);
			long num2 = Convert.ToInt64(row["zeny"]);
			string text3 = ((num != 2) ? "ย\u0e31งไม\u0e48อ\u0e48าน" : "อ\u0e48านแล\u0e49ว");
			if (num == 2 && text == string.Empty && num2 == 0)
			{
				text3 = "ร\u0e31บของแล\u0e49ว";
			}
			dataTable2.Rows.Add(Convert.ToInt64(row["id"]), dateTime.AddSeconds(Convert.ToInt64(row["time"])).ToLocalTime().ToString("dd/MM/yyyy HH:mm"), row["dest_name"], row["title"], num2, text2, text3);
		}
		Bind(gridMailHist, dataTable2);
	}

	private void DeleteMail()
	{
		//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0083: Invalid comparison between Unknown and I4
		if (!NeedDb() || ((BaseCollection)gridMailHist.SelectedRows).Count == 0)
		{
			return;
		}
		List<long> list = (from DataGridViewRow r in (IEnumerable)gridMailHist.SelectedRows
			select Convert.ToInt64(r.Cells["mail_id"].Value)).ToList();
		if ((int)MessageBox.Show((IWin32Window)(object)this, "ลบเมล " + list.Count + " ฉบ\u0e31บ (รวมของท\u0e35\u0e48ย\u0e31งไม\u0e48ได\u0e49ร\u0e31บ)?", ((Control)this).Text, (MessageBoxButtons)4, (MessageBoxIcon)48) == 6)
		{
			try
			{
				string text = string.Join(",", list);
				db.Exec("DELETE FROM `mail_attachments` WHERE id IN (" + text + ")");
				db.Exec("DELETE FROM `mail` WHERE id IN (" + text + ") AND send_id=0");
				tool.Add("[Tool] ลบเมล id " + text);
			}
			catch (Exception ex)
			{
				MessageBox.Show((IWin32Window)(object)this, ex.Message, ((Control)this).Text);
			}
			LoadMailHistory();
		}
	}

	private EditSpec AccountSpec()
	{
		EditSpec editSpec = new EditSpec();
		editSpec.Table = "login";
		editSpec.KeyCol = "account_id";
		editSpec.KeyDb = "account_id";
		EditSpec editSpec2 = editSpec;
		editSpec2.Cols["ไอด\u0e35"] = new EditCol("userid", EK.Text)
		{
			Min = 4L,
			MaxBytes = 23
		};
		editSpec2.Cols["รห\u0e31สผ\u0e48าน"] = new EditCol("user_pass", EK.Pass)
		{
			Min = 4L,
			MaxBytes = 32
		};
		editSpec2.Cols["เพศ"] = new EditCol("sex", EK.Sex);
		editSpec2.Cols["กล\u0e38\u0e48ม"] = new EditCol("group_id", EK.Group);
		editSpec2.Cols["อ\u0e35เมล"] = new EditCol("email", EK.Text)
		{
			Min = 0L,
			MaxBytes = 39
		};
		editSpec2.LockReason = (DataGridViewRow r) => (!(Convert.ToString(r.Cells["เพศ"].Value) == "S")) ? null : "บ\u0e31ญช\u0e35น\u0e35\u0e49 char/map server ใช\u0e49เช\u0e37\u0e48อมก\u0e31น (s1) ห\u0e49ามแก\u0e49";
		return editSpec2;
	}

	private EditSpec CharSpec(bool full)
	{
		EditSpec editSpec = new EditSpec();
		editSpec.Table = "char";
		editSpec.KeyCol = "char_id";
		editSpec.KeyDb = "char_id";
		EditSpec editSpec2 = editSpec;
		editSpec2.Cols["ช\u0e37\u0e48อ"] = new EditCol("name", EK.Text)
		{
			Min = 4L,
			MaxBytes = 23
		};
		editSpec2.Cols["อาช\u0e35พ"] = new EditCol("class", EK.Job);
		editSpec2.Cols["Base Lv"] = new EditCol("base_level", EK.Int)
		{
			Min = 1L,
			Max = 999L
		};
		editSpec2.Cols["Job Lv"] = new EditCol("job_level", EK.Int)
		{
			Min = 1L,
			Max = 999L
		};
		editSpec2.Cols["Zeny"] = new EditCol("zeny", EK.Int)
		{
			Min = 0L,
			Max = 1000000000L
		};
		editSpec2.Cols["แผนท\u0e35\u0e48"] = new EditCol("last_map", EK.Map)
		{
			Min = 1L,
			MaxBytes = 11
		};
		if (full)
		{
			string[] array = new string[6] { "STR", "AGI", "VIT", "INT", "DEX", "LUK" };
			foreach (string text in array)
			{
				editSpec2.Cols[text] = new EditCol(text.ToLowerInvariant(), EK.Int)
				{
					Min = 1L,
					Max = 32767L
				};
			}
		}
		editSpec2.LockReason = (DataGridViewRow r) => (!((DataGridViewElement)r).DataGridView.Columns.Contains("สถานะ") || !(Convert.ToString(r.Cells["สถานะ"].Value) == "ออนไลน\u0e4c")) ? null : "ต\u0e31วละครน\u0e35\u0e49ออนไลน\u0e4cอย\u0e39\u0e48 ให\u0e49ออกจากเกมก\u0e48อนแล\u0e49วค\u0e48อยแก\u0e49 (ไม\u0e48ง\u0e31\u0e49นเซ\u0e34ร\u0e4cฟจะเซฟท\u0e31บ)";
		return editSpec2;
	}

	private void SetupEditGrid(DataGridView g, EditSpec spec)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		//IL_005d: Expected O, but got Unknown
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0074: Expected O, but got Unknown
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Expected O, but got Unknown
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Expected O, but got Unknown
		//IL_00af: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b9: Expected O, but got Unknown
		((Control)g).Tag = spec;
		g.EnableHeadersVisualStyles = false;
		g.ColumnHeadersDefaultCellStyle.BackColor = Theme.Head;
		g.EditMode = (DataGridViewEditMode)2;
		g.CellDoubleClick += (DataGridViewCellEventHandler)delegate(object s, DataGridViewCellEventArgs e)
		{
			if (e.RowIndex >= 0 && e.ColumnIndex >= 0 && !((DataGridViewBand)g.Columns[e.ColumnIndex]).ReadOnly)
			{
				g.BeginEdit(true);
			}
		};
		g.CellBeginEdit += new DataGridViewCellCancelEventHandler(Grid_CellBeginEdit);
		g.CellEndEdit += new DataGridViewCellEventHandler(Grid_CellEndEdit);
		g.CellFormatting += new DataGridViewCellFormattingEventHandler(Grid_CellFormatting);
		g.EditingControlShowing += new DataGridViewEditingControlShowingEventHandler(Grid_EditingControlShowing);
		g.CurrentCellDirtyStateChanged += delegate
		{
			if (g.IsCurrentCellDirty && g.CurrentCell is DataGridViewComboBoxCell)
			{
				g.CommitEdit((DataGridViewDataErrorContexts)512);
				g.EndEdit();
			}
		};
	}

	private void ApplyEditable(DataGridView g)
	{
		//IL_0406: Unknown result type (might be due to invalid IL or missing references)
		//IL_040d: Expected O, but got Unknown
		//IL_0296: Unknown result type (might be due to invalid IL or missing references)
		//IL_029d: Expected O, but got Unknown
		if (!(((Control)g).Tag is EditSpec editSpec))
		{
			return;
		}
		g.ReadOnly = false;
		DataTable dataTable = g.DataSource as DataTable;
		foreach (DataGridViewColumn item in ((IEnumerable)g.Columns).Cast<DataGridViewColumn>().ToList())
		{
			if (!editSpec.Cols.TryGetValue(item.Name, out var value))
			{
				((DataGridViewBand)item).ReadOnly = true;
				continue;
			}
			DataGridViewColumn val = item;
			if (value.Kind == EK.Sex || value.Kind == EK.Group || value.Kind == EK.Job)
			{
				List<Opt> list = new List<Opt>();
				if (value.Kind == EK.Sex)
				{
					list.Add(new Opt
					{
						Key = "M",
						Name = "M"
					});
					list.Add(new Opt
					{
						Key = "F",
						Name = "F"
					});
					list.Add(new Opt
					{
						Key = "S",
						Name = "S"
					});
				}
				if (value.Kind == EK.Group)
				{
					list.AddRange(groups.Select((KeyValuePair<int, string> x) => new Opt
					{
						Key = (long)x.Key,
						Name = x.Value + " (" + x.Key + ")"
					}));
				}
				if (value.Kind == EK.Job)
				{
					list.AddRange(from x in Jobs.All
						orderby x.Key
						select new Opt
						{
							Key = (long)x.Key,
							Name = x.Value
						});
				}
				if (dataTable != null && dataTable.Columns.Contains(item.DataPropertyName))
				{
					foreach (DataRow row in dataTable.Rows)
					{
						object v = row[item.DataPropertyName];
						if (v != DBNull.Value && !list.Any((Opt o) => o.Key.Equals(v)))
						{
							list.Add(new Opt
							{
								Key = v,
								Name = Convert.ToString(v)
							});
						}
					}
				}
				DataGridViewComboBoxColumn val2 = new DataGridViewComboBoxColumn();
				((DataGridViewColumn)val2).Name = item.Name;
				((DataGridViewColumn)val2).HeaderText = item.HeaderText;
				((DataGridViewColumn)val2).DataPropertyName = item.DataPropertyName;
				val2.DataSource = list;
				val2.ValueMember = "Key";
				val2.DisplayMember = "Name";
				val2.DisplayStyle = (DataGridViewComboBoxDisplayStyle)2;
				val2.FlatStyle = (FlatStyle)0;
				((DataGridViewColumn)val2).Width = Math.Max(item.Width, (value.Kind != EK.Job) ? 90 : 140);
				((DataGridViewColumn)val2).ValueType = ((value.Kind != EK.Sex) ? typeof(long) : typeof(string));
				DataGridViewComboBoxColumn val3 = val2;
				int index = ((DataGridViewBand)item).Index;
				g.Columns.RemoveAt(index);
				g.Columns.Insert(index, (DataGridViewColumn)(object)val3);
				val = (DataGridViewColumn)(object)val3;
			}
			((DataGridViewBand)val).ReadOnly = false;
			val.HeaderText = val.HeaderText.TrimEnd(' ', '✎') + " ✎";
			((DataGridViewCell)val.HeaderCell).Style.BackColor = Theme.EditHead;
		}
		g.AutoResizeColumns((DataGridViewAutoSizeColumnsMode)6);
		foreach (DataGridViewColumn item2 in (BaseCollection)g.Columns)
		{
			DataGridViewColumn val4 = item2;
			if (editSpec.Cols.TryGetValue(val4.Name, out var value2))
			{
				val4.Width = Math.Max(val4.Width + 6, (value2.Kind == EK.Job) ? 150 : ((value2.Kind == EK.Group) ? 130 : ((value2.Kind != EK.Pass) ? 60 : 90)));
			}
			if (val4.Width > 320)
			{
				val4.Width = 320;
			}
		}
	}

	private void Grid_CellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		DataGridView val = (DataGridView)sender;
		if (((Control)val).Tag is EditSpec editSpec && e.ColumnIndex >= 0 && e.RowIndex >= 0 && editSpec.Cols.TryGetValue(val.Columns[e.ColumnIndex].Name, out var value) && value.Kind == EK.Pass && !showPass)
		{
			((ConvertEventArgs)e).Value = ((((ConvertEventArgs)e).Value != null && ((ConvertEventArgs)e).Value != DBNull.Value && !(Convert.ToString(((ConvertEventArgs)e).Value) == string.Empty)) ? "••••••" : string.Empty);
			e.FormattingApplied = true;
		}
	}

	private void Grid_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		DataGridView val = (DataGridView)sender;
		if (!(((Control)val).Tag is EditSpec editSpec) || val.CurrentCell == null || !editSpec.Cols.TryGetValue(val.Columns[val.CurrentCell.ColumnIndex].Name, out var value))
		{
			return;
		}
		Control control = e.Control;
		TextBox val2 = (TextBox)(object)((control is TextBox) ? control : null);
		if (val2 != null)
		{
			((TextBoxBase)val2).MaxLength = ((value.Kind != EK.Int) ? Math.Max(value.MaxBytes, 1) : 12);
			if (value.Kind == EK.Pass && !showPass)
			{
				((Control)val2).Text = string.Empty;
			}
		}
	}

	private void Grid_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		DataGridView val = (DataGridView)sender;
		if (((Control)val).Tag is EditSpec editSpec)
		{
			string text = ((editSpec.LockReason != null) ? editSpec.LockReason(val.Rows[e.RowIndex]) : null);
			if (text != null)
			{
				((CancelEventArgs)(object)e).Cancel = true;
				Msg(text, warn: true);
			}
			else if (!db.Connected)
			{
				((CancelEventArgs)(object)e).Cancel = true;
				Msg("ย\u0e31งไม\u0e48ได\u0e49เช\u0e37\u0e48อมต\u0e48อฐานข\u0e49อม\u0e39ล", warn: true);
			}
			else
			{
				editOld = val.Rows[e.RowIndex].Cells[e.ColumnIndex].Value;
			}
		}
	}

	private void Grid_CellEndEdit(object sender, DataGridViewCellEventArgs e)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Expected O, but got Unknown
		//IL_060e: Unknown result type (might be due to invalid IL or missing references)
		DataGridView val = (DataGridView)sender;
		if (!(((Control)val).Tag is EditSpec editSpec))
		{
			return;
		}
		DataGridViewCell val2 = val.Rows[e.RowIndex].Cells[e.ColumnIndex];
		string name = val.Columns[e.ColumnIndex].Name;
		if (!editSpec.Cols.TryGetValue(name, out var value))
		{
			return;
		}
		object obj = editOld;
		object value2 = val2.Value;
		editOld = null;
		string text = ((obj != null && obj != DBNull.Value) ? Convert.ToString(obj) : string.Empty);
		string text2 = ((value2 != null && value2 != DBNull.Value) ? Convert.ToString(value2).Trim() : string.Empty);
		if (text2 == text)
		{
			return;
		}
		if (value.Kind == EK.Pass && text2 == string.Empty)
		{
			val2.Value = obj;
			return;
		}
		long num = Convert.ToInt64(val.Rows[e.RowIndex].Cells[editSpec.KeyCol].Value);
		string text3 = Convert.ToString(val.Rows[e.RowIndex].Cells[(!(editSpec.Table == "login")) ? "ช\u0e37\u0e48อ" : "ไอด\u0e35"].Value);
		string text4 = null;
		string text5 = null;
		string text6 = string.Empty;
		try
		{
			switch (value.Kind)
			{
			case EK.Int:
			case EK.Group:
			case EK.Job:
			{
				if (!long.TryParse(text2.Replace(",", string.Empty), out var result))
				{
					text4 = "ต\u0e49องเป\u0e47นต\u0e31วเลข";
				}
				else if (value.Kind == EK.Int && (result < value.Min || result > value.Max))
				{
					text4 = "ต\u0e49องอย\u0e39\u0e48ระหว\u0e48าง " + value.Min.ToString("N0") + " - " + value.Max.ToString("N0");
				}
				else
				{
					text5 = result.ToString();
				}
				break;
			}
			case EK.Sex:
				if (text2 != "M" && text2 != "F")
				{
					text4 = "เพศต\u0e49องเป\u0e47น M หร\u0e37อ F";
				}
				else
				{
					text5 = "'" + text2 + "'";
				}
				break;
			case EK.Text:
			case EK.Pass:
			case EK.Map:
			{
				int byteCount = db.Enc.GetByteCount(text2);
				if (byteCount < value.Min || byteCount > value.MaxBytes)
				{
					text4 = "ความยาวต\u0e49อง " + value.Min + "-" + value.MaxBytes + " ต\u0e31วอ\u0e31กษร";
					break;
				}
				if (value.Kind == EK.Pass)
				{
					text5 = "'" + Db.Esc(PassValue(text2)) + "'";
					break;
				}
				if (value.Kind == EK.Map)
				{
					text2 = text2.ToLowerInvariant();
					text6 = ", last_x=0, last_y=0";
				}
				if (value.Db == "userid" || value.Db == "name")
				{
					long num2 = Convert.ToInt64(db.Scalar("SELECT COUNT(*) FROM `" + editSpec.Table + "` WHERE `" + value.Db + "`='" + Db.Esc(text2) + "' AND `" + editSpec.KeyDb + "`<>" + num));
					if (num2 > 0)
					{
						text4 = "ช\u0e37\u0e48อ \"" + text2 + "\" ม\u0e35คนใช\u0e49แล\u0e49ว";
						break;
					}
				}
				text5 = "'" + Db.Esc(text2) + "'";
				break;
			}
			}
			if (text4 == null)
			{
				db.Exec("UPDATE `" + editSpec.Table + "` SET `" + value.Db + "`=" + text5 + text6 + " WHERE `" + editSpec.KeyDb + "`=" + num);
				string text7 = ((value.Kind != EK.Pass) ? text2 : "(รห\u0e31สใหม\u0e48)");
				Msg("บ\u0e31นท\u0e36กแล\u0e49ว: " + text3 + " • " + name + " = " + text7, warn: false);
				tool.Add("[Tool] แก\u0e49ไข " + editSpec.Table + " " + text3 + " : " + name + " " + ((value.Kind != EK.Pass) ? (text + " → " + text2) : string.Empty));
				if (value.Kind == EK.Pass && !showPass)
				{
					val2.Value = PassValue(text2);
				}
				else if (value.Kind == EK.Map)
				{
					val2.Value = text2;
				}
				return;
			}
		}
		catch (Exception ex)
		{
			text4 = ex.Message;
		}
		val2.Value = obj;
		Msg("แก\u0e49ไม\u0e48สำเร\u0e47จ: " + text4, warn: true);
		MessageBox.Show((IWin32Window)(object)this, name + ": " + text4, ((Control)this).Text, (MessageBoxButtons)0, (MessageBoxIcon)48);
	}

	private void Msg(string text, bool warn)
	{
		if (stMsg != null)
		{
			((ToolStripItem)stMsg).Text = text;
			((ToolStripItem)stMsg).ForeColor = ((!warn) ? Theme.Good : Theme.Bad);
		}
	}

	private TabPage BuildRateTab()
	{
		//IL_0013: Unknown result type (might be due to invalid IL or missing references)
		//IL_001d: Expected O, but got Unknown
		//IL_001d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Expected O, but got Unknown
		//IL_003a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0052: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Expected O, but got Unknown
		//IL_0069: Unknown result type (might be due to invalid IL or missing references)
		//IL_0073: Expected O, but got Unknown
		//IL_0080: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Expected O, but got Unknown
		//IL_009c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a3: Expected O, but got Unknown
		//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Expected O, but got Unknown
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e5: Expected O, but got Unknown
		//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0104: Expected O, but got Unknown
		//IL_0124: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Expected O, but got Unknown
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_0198: Expected O, but got Unknown
		//IL_01b0: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b6: Expected O, but got Unknown
		//IL_01db: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e5: Expected O, but got Unknown
		//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fd: Expected O, but got Unknown
		//IL_020b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0215: Expected O, but got Unknown
		//IL_022b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0235: Expected O, but got Unknown
		//IL_0251: Unknown result type (might be due to invalid IL or missing references)
		//IL_025b: Expected O, but got Unknown
		//IL_025d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0264: Expected O, but got Unknown
		//IL_02ba: Unknown result type (might be due to invalid IL or missing references)
		//IL_02c1: Expected O, but got Unknown
		//IL_0317: Unknown result type (might be due to invalid IL or missing references)
		//IL_031e: Expected O, but got Unknown
		//IL_0379: Unknown result type (might be due to invalid IL or missing references)
		//IL_0380: Expected O, but got Unknown
		//IL_03da: Unknown result type (might be due to invalid IL or missing references)
		//IL_03e1: Expected O, but got Unknown
		//IL_043b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0442: Expected O, but got Unknown
		//IL_0482: Unknown result type (might be due to invalid IL or missing references)
		//IL_0489: Expected O, but got Unknown
		//IL_04bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_04c2: Expected O, but got Unknown
		//IL_04ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_04f5: Expected O, but got Unknown
		//IL_0542: Unknown result type (might be due to invalid IL or missing references)
		//IL_0549: Expected O, but got Unknown
		//IL_0557: Unknown result type (might be due to invalid IL or missing references)
		//IL_05fe: Unknown result type (might be due to invalid IL or missing references)
		//IL_0605: Expected O, but got Unknown
		//IL_0621: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_06cf: Expected O, but got Unknown
		//IL_0714: Unknown result type (might be due to invalid IL or missing references)
		//IL_071b: Expected O, but got Unknown
		TabPage page = new TabPage("อ\u0e31ตรา EXP / Drop");
		TableLayoutPanel val = new TableLayoutPanel();
		((Control)val).Dock = (DockStyle)5;
		val.ColumnCount = 1;
		val.RowCount = 3;
		((Control)val).Padding = new Padding(8);
		TableLayoutPanel val2 = val;
		val2.RowStyles.Add(new RowStyle((SizeType)1, 92f));
		val2.RowStyles.Add(new RowStyle((SizeType)1, 250f));
		val2.RowStyles.Add(new RowStyle((SizeType)2, 100f));
		((Control)page).Controls.Add((Control)(object)val2);
		GroupBox val3 = new GroupBox();
		((Control)val3).Text = "สถานะป\u0e31จจ\u0e38บ\u0e31นในเกม";
		((Control)val3).Dock = (DockStyle)5;
		GroupBox val4 = val3;
		FlowLayoutPanel val5 = new FlowLayoutPanel();
		((Control)val5).Dock = (DockStyle)5;
		val5.FlowDirection = (FlowDirection)1;
		val5.WrapContents = false;
		FlowLayoutPanel val6 = val5;
		Label val7 = new Label();
		((Control)val7).AutoSize = true;
		((Control)val7).Font = new Font("Tahoma", 12f, (FontStyle)1);
		((Control)val7).ForeColor = Theme.Accent;
		((Control)val7).Text = "-";
		lblRateNow = val7;
		val7 = new Label();
		((Control)val7).AutoSize = true;
		((Control)val7).ForeColor = Theme.Muted;
		((Control)val7).Text = string.Empty;
		lblRateBase = val7;
		((Control)val6).Controls.Add((Control)(object)lblRateNow);
		((Control)val6).Controls.Add((Control)(object)lblRateBase);
		((Control)val4).Controls.Add((Control)(object)val6);
		val2.Controls.Add((Control)(object)val4, 0, 0);
		val3 = new GroupBox();
		((Control)val3).Text = "ต\u0e31\u0e49งอ\u0e31ตราใหม\u0e48 (เท\u0e35ยบก\u0e31บค\u0e48าปกต\u0e34ใน conf — x1 = ปกต\u0e34)";
		((Control)val3).Dock = (DockStyle)5;
		GroupBox val8 = val3;
		val = new TableLayoutPanel();
		((Control)val).Dock = (DockStyle)5;
		val.ColumnCount = 3;
		val.RowCount = 5;
		TableLayoutPanel val9 = val;
		val9.ColumnStyles.Add(new ColumnStyle((SizeType)1, 150f));
		val9.ColumnStyles.Add(new ColumnStyle((SizeType)1, 180f));
		val9.ColumnStyles.Add(new ColumnStyle((SizeType)2, 100f));
		for (int i = 0; i < 4; i++)
		{
			val9.RowStyles.Add(new RowStyle((SizeType)1, 34f));
		}
		val9.RowStyles.Add(new RowStyle((SizeType)2, 100f));
		NumericUpDown val10 = new NumericUpDown();
		val10.Minimum = 0.1m;
		val10.Maximum = 100m;
		val10.DecimalPlaces = 1;
		val10.Increment = 0.5m;
		val10.Value = 1m;
		((Control)val10).Width = 90;
		numRateExp = val10;
		val10 = new NumericUpDown();
		val10.Minimum = 0.1m;
		val10.Maximum = 100m;
		val10.DecimalPlaces = 1;
		val10.Increment = 0.5m;
		val10.Value = 1m;
		((Control)val10).Width = 90;
		numRateDrop = val10;
		val10 = new NumericUpDown();
		val10.Minimum = 0m;
		val10.Maximum = 720m;
		val10.DecimalPlaces = 1;
		val10.Increment = 0.5m;
		val10.Value = 0m;
		((Control)val10).Width = 90;
		numRateHours = val10;
		TableLayoutControlCollection controls = val9.Controls;
		val7 = new Label();
		((Control)val7).Text = "EXP (Base + Job)  ×";
		((Control)val7).AutoSize = true;
		((Control)val7).Anchor = (AnchorStyles)4;
		controls.Add((Control)(object)val7, 0, 0);
		val9.Controls.Add((Control)(object)numRateExp, 1, 0);
		val9.Controls.Add((Control)(object)Presets(numRateExp), 2, 0);
		TableLayoutControlCollection controls2 = val9.Controls;
		val7 = new Label();
		((Control)val7).Text = "Drop (ท\u0e38กประเภท)  ×";
		((Control)val7).AutoSize = true;
		((Control)val7).Anchor = (AnchorStyles)4;
		controls2.Add((Control)(object)val7, 0, 1);
		val9.Controls.Add((Control)(object)numRateDrop, 1, 1);
		val9.Controls.Add((Control)(object)Presets(numRateDrop), 2, 1);
		TableLayoutControlCollection controls3 = val9.Controls;
		val7 = new Label();
		((Control)val7).Text = "ระยะเวลา (ช\u0e31\u0e48วโมง)";
		((Control)val7).AutoSize = true;
		((Control)val7).Anchor = (AnchorStyles)4;
		controls3.Add((Control)(object)val7, 0, 2);
		val9.Controls.Add((Control)(object)numRateHours, 1, 2);
		TableLayoutControlCollection controls4 = val9.Controls;
		val7 = new Label();
		((Control)val7).Text = "0 = ไม\u0e48จำก\u0e31ด (ใช\u0e49จนกว\u0e48าจะกดค\u0e37นค\u0e48าปกต\u0e34) / ครบเวลาแล\u0e49วระบบค\u0e37นค\u0e48า x1 และประกาศให\u0e49เอง";
		((Control)val7).AutoSize = true;
		((Control)val7).ForeColor = Theme.Muted;
		((Control)val7).Anchor = (AnchorStyles)4;
		controls4.Add((Control)(object)val7, 2, 2);
		CheckBox val11 = new CheckBox();
		((Control)val11).Text = "ข\u0e49อความอ\u0e31ตโนม\u0e31ต\u0e34";
		((Control)val11).AutoSize = true;
		val11.Checked = true;
		((Control)val11).Anchor = (AnchorStyles)4;
		chkRateAutoMsg = val11;
		TextBox val12 = new TextBox();
		((Control)val12).Dock = (DockStyle)5;
		((Control)val12).Enabled = false;
		txtRateMsg = val12;
		val9.Controls.Add((Control)(object)chkRateAutoMsg, 0, 3);
		val9.Controls.Add((Control)(object)txtRateMsg, 1, 3);
		val9.SetColumnSpan((Control)(object)txtRateMsg, 2);
		val5 = new FlowLayoutPanel();
		((Control)val5).Dock = (DockStyle)5;
		((Control)val5).Padding = new Padding(0, 6, 0, 0);
		FlowLayoutPanel val13 = val5;
		Button val14 = Btn("ใช\u0e49ท\u0e31นท\u0e35 + ประกาศ", delegate
		{
			ApplyRate(reset: false);
		}, 170);
		((Control)val14).Height = 38;
		((Control)val14).BackColor = Theme.BtnGo;
		Button val15 = Btn("ค\u0e37นค\u0e48าปกต\u0e34 (x1)", delegate
		{
			ApplyRate(reset: true);
		}, 150);
		((Control)val15).Height = 38;
		Button val16 = Btn("ร\u0e35เฟรช", delegate
		{
			LoadRate();
		}, 90);
		((Control)val16).Height = 38;
		Control.ControlCollection controls5 = ((Control)val13).Controls;
		Control[] obj = new Control[4]
		{
			(Control)val14,
			(Control)val15,
			(Control)val16,
			default(Control)
		};
		val7 = new Label();
		((Control)val7).AutoSize = true;
		((Control)val7).ForeColor = Theme.Muted;
		((Control)val7).Margin = new Padding(12, 12, 3, 3);
		((Control)val7).Text = "ต\u0e49องเป\u0e34ด Map server อย\u0e39\u0e48 — ระบบใช\u0e49ค\u0e48าใหม\u0e48ภายใน ~3 ว\u0e34นาท\u0e35 (เซ\u0e34ร\u0e4cฟอาจหน\u0e48วง 1-2 ว\u0e34 ตอนโหลดข\u0e49อม\u0e39ลมอนใหม\u0e48)";
		obj[3] = (Control)val7;
		controls5.AddRange((Control[])(object)obj);
		val9.Controls.Add((Control)(object)val13, 0, 4);
		val9.SetColumnSpan((Control)(object)val13, 3);
		EventHandler eventHandler = delegate
		{
			UpdateRateMsg();
		};
		numRateExp.ValueChanged += eventHandler;
		numRateDrop.ValueChanged += eventHandler;
		numRateHours.ValueChanged += eventHandler;
		chkRateAutoMsg.CheckedChanged += delegate
		{
			((Control)txtRateMsg).Enabled = !chkRateAutoMsg.Checked;
			UpdateRateMsg();
		};
		UpdateRateMsg();
		((Control)val8).Controls.Add((Control)(object)val9);
		val2.Controls.Add((Control)(object)val8, 0, 1);
		val3 = new GroupBox();
		((Control)val3).Text = "ประว\u0e31ต\u0e34การปร\u0e31บอ\u0e31ตรา";
		((Control)val3).Dock = (DockStyle)5;
		GroupBox val17 = val3;
		gridRateHist = Grid();
		((Control)val17).Controls.Add((Control)(object)gridRateHist);
		val2.Controls.Add((Control)(object)val17, 0, 2);
		Timer val18 = new Timer();
		val18.Interval = 3000;
		rateTimer = val18;
		rateTimer.Tick += delegate
		{
			if (tabs != null && tabs.SelectedTab == page)
			{
				LoadRate(quiet: true);
			}
		};
		rateTimer.Start();
		return page;
	}

	private FlowLayoutPanel Presets(NumericUpDown target)
	{
		//IL_000d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0013: Expected O, but got Unknown
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d7: Expected O, but got Unknown
		//IL_0109: Unknown result type (might be due to invalid IL or missing references)
		FlowLayoutPanel val2 = new FlowLayoutPanel();
		((Control)val2).Dock = (DockStyle)5;
		((Control)val2).Margin = new Padding(0);
		FlowLayoutPanel val3 = val2;
		decimal[] array = new decimal[6] { 1m, 1.5m, 2m, 3m, 5m, 10m };
		for (int i = 0; i < array.Length; i++)
		{
			decimal num = array[i];
			decimal val = num;
			Button val4 = new Button();
			((Control)val4).Text = "x" + num.ToString("0.#");
			((Control)val4).Width = 52;
			((Control)val4).Height = 26;
			((Control)val4).Margin = new Padding(2);
			Button val5 = val4;
			((Control)val5).Click += delegate
			{
				target.Value = val;
			};
			((Control)val3).Controls.Add((Control)(object)val5);
		}
		return val3;
	}

	private static string Mult(decimal v)
	{
		return "x" + v.ToString("0.#");
	}

	private void UpdateRateMsg()
	{
		if (chkRateAutoMsg != null && chkRateAutoMsg.Checked)
		{
			string text = "[Ro-X] Event! EXP " + Mult(numRateExp.Value) + " / Drop " + Mult(numRateDrop.Value) + " เร\u0e34\u0e48มแล\u0e49วตอนน\u0e35\u0e49!";
			if (numRateHours.Value > 0m)
			{
				text = text + " (ถ\u0e36ง " + DateTime.Now.AddMinutes((double)(numRateHours.Value * 60m)).ToString("HH:mm") + " น.)";
			}
			if (numRateExp.Value == 1m && numRateDrop.Value == 1m)
			{
				text = "[Ro-X] อ\u0e31ตรา EXP / Drop กล\u0e31บส\u0e39\u0e48ค\u0e48าปกต\u0e34แล\u0e49ว";
			}
			((Control)txtRateMsg).Text = text;
		}
	}

	private bool EnsureRateTables()
	{
		try
		{
			db.Exec("CREATE TABLE IF NOT EXISTS `rox_rate` (`id` INT UNSIGNED NOT NULL AUTO_INCREMENT PRIMARY KEY,`exp_mult` INT NOT NULL DEFAULT 100,`drop_mult` INT NOT NULL DEFAULT 100,`minutes` INT NOT NULL DEFAULT 0,`msg` VARCHAR(200) NOT NULL DEFAULT '',`status` TINYINT NOT NULL DEFAULT 0,`until_ts` INT UNSIGNED NOT NULL DEFAULT 0,`created` DATETIME NULL,`applied` DATETIME NULL) ENGINE=MyISAM");
			db.Exec("CREATE TABLE IF NOT EXISTS `rox_rate_state` (`id` TINYINT NOT NULL PRIMARY KEY,`exp_mult` INT NOT NULL DEFAULT 100,`drop_mult` INT NOT NULL DEFAULT 100,`base_exp` INT NOT NULL DEFAULT 0,`job_exp` INT NOT NULL DEFAULT 0,`drop_common` INT NOT NULL DEFAULT 0,`def_exp` INT NOT NULL DEFAULT 0,`def_drop` INT NOT NULL DEFAULT 0,`until_ts` INT UNSIGNED NOT NULL DEFAULT 0,`updated` DATETIME NULL) ENGINE=MyISAM");
			return true;
		}
		catch (Exception ex)
		{
			tool.Add("[Tool][Error] " + ex.Message);
			return false;
		}
	}

	private void ApplyRate(bool reset)
	{
		//IL_02f0: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Invalid comparison between Unknown and I4
		//IL_011d: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
		//IL_01ff: Invalid comparison between Unknown and I4
		if (!NeedDb() || (!map.Running && (int)MessageBox.Show((IWin32Window)(object)this, "Map server ย\u0e31งไม\u0e48ได\u0e49เป\u0e34ด — คำส\u0e31\u0e48งจะรอใช\u0e49ตอนเป\u0e34ด Map server\nต\u0e49องการบ\u0e31นท\u0e36กไว\u0e49ก\u0e48อนไหม?", ((Control)this).Text, (MessageBoxButtons)4, (MessageBoxIcon)32) != 6) || !EnsureRateTables())
		{
			return;
		}
		UpdateRateMsg();
		int num = ((!reset) ? ((int)Math.Round(numRateExp.Value * 100m)) : 100);
		int num2 = ((!reset) ? ((int)Math.Round(numRateDrop.Value * 100m)) : 100);
		int num3 = ((!reset) ? ((int)Math.Round(numRateHours.Value * 60m)) : 0);
		string text = ((!reset) ? ((Control)txtRateMsg).Text.Trim() : "[Ro-X] อ\u0e31ตรา EXP / Drop กล\u0e31บส\u0e39\u0e48ค\u0e48าปกต\u0e34แล\u0e49ว");
		if (db.Enc.GetByteCount(text) > 190)
		{
			MessageBox.Show((IWin32Window)(object)this, "ข\u0e49อความประกาศยาวเก\u0e34นไป", ((Control)this).Text);
			return;
		}
		string text2 = ((!reset) ? ("ต\u0e31\u0e49ง EXP " + Mult((decimal)num / 100m) + " / Drop " + Mult((decimal)num2 / 100m) + ((num3 <= 0) ? " (ไม\u0e48จำก\u0e31ดเวลา)" : (" เป\u0e47นเวลา " + ((decimal)num3 / 60m).ToString("0.#") + " ช\u0e31\u0e48วโมง")) + "\n\nประกาศ: " + ((!(text == string.Empty)) ? text : "(ไม\u0e48ประกาศ)")) : "ค\u0e37นอ\u0e31ตรา EXP / Drop เป\u0e47นค\u0e48าปกต\u0e34 (x1) ?");
		if ((int)MessageBox.Show((IWin32Window)(object)this, text2, "ย\u0e37นย\u0e31น", (MessageBoxButtons)4, (MessageBoxIcon)32) == 6)
		{
			try
			{
				db.Exec("INSERT INTO `rox_rate` (exp_mult, drop_mult, minutes, msg, status, created) VALUES (" + num + "," + num2 + "," + num3 + ",'" + Db.Esc(text) + "',0,NOW())");
				tool.Add("[Tool] ส\u0e48งคำส\u0e31\u0e48งปร\u0e31บอ\u0e31ตรา EXP " + Mult((decimal)num / 100m) + " / Drop " + Mult((decimal)num2 / 100m) + " แล\u0e49ว");
				((ToolStripItem)stMsg).Text = "ส\u0e48งคำส\u0e31\u0e48งแล\u0e49ว รอ Map server ใช\u0e49ค\u0e48าใหม\u0e48...";
			}
			catch (Exception ex)
			{
				MessageBox.Show((IWin32Window)(object)this, ex.Message, ((Control)this).Text);
				return;
			}
			LoadRate();
		}
	}

	private void LoadRate(bool quiet = false)
	{
		if (!db.Connected)
		{
			if (!quiet)
			{
				TryConnectDb(log: false);
			}
			if (!db.Connected)
			{
				return;
			}
		}
		if (!EnsureRateTables())
		{
			return;
		}
		try
		{
			DataTable dataTable = db.Query("SELECT exp_mult, drop_mult, base_exp, job_exp, drop_common, def_exp, def_drop, until_ts, updated FROM `rox_rate_state` WHERE id=1");
			if (dataTable.Rows.Count == 0)
			{
				((Control)lblRateNow).Text = "ย\u0e31งไม\u0e48ม\u0e35ข\u0e49อม\u0e39ลจาก Map server";
				((Control)lblRateBase).Text = "เป\u0e34ด Map server (ท\u0e35\u0e48โหลด npc/Npc RoX/rate_control.txt) แล\u0e49วระบบจะแสดงค\u0e48าให\u0e49เอง";
			}
			else
			{
				DataRow dataRow = dataTable.Rows[0];
				decimal num = Convert.ToDecimal(dataRow["exp_mult"]) / 100m;
				decimal num2 = Convert.ToDecimal(dataRow["drop_mult"]) / 100m;
				long num3 = Convert.ToInt64(dataRow["until_ts"]);
				string text = string.Empty;
				if (num3 > 0)
				{
					DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(num3).ToLocalTime();
					TimeSpan timeSpan = dateTime - DateTime.Now;
					text = ((!(timeSpan.TotalSeconds > 0.0)) ? "   (หมดเวลาแล\u0e49ว กำล\u0e31งค\u0e37นค\u0e48า)" : ("   (เหล\u0e37อ " + (int)timeSpan.TotalHours + " ชม. " + timeSpan.Minutes + " นาท\u0e35 — ถ\u0e36ง " + dateTime.ToString("dd/MM HH:mm") + ")"));
				}
				((Control)lblRateNow).Text = "EXP " + Mult(num) + "   |   Drop " + Mult(num2) + text;
				((Control)lblRateNow).ForeColor = ((!(num == 1m) || !(num2 == 1m)) ? Theme.Accent : Theme.Text);
				((Control)lblRateBase).Text = string.Concat("ค\u0e48าในเกมตอนน\u0e35\u0e49: base_exp_rate ", dataRow["base_exp"], "%, job_exp_rate ", dataRow["job_exp"], "%, item_rate_common ", dataRow["drop_common"], "   —   ค\u0e48าปกต\u0e34ใน conf: EXP ", dataRow["def_exp"], "%, Drop ", dataRow["def_drop"], "   (อ\u0e31ปเดต ", dataRow["updated"], ")");
			}
			DataTable dataTable2 = db.Query("SELECT id, created, applied, exp_mult, drop_mult, minutes, status, msg FROM `rox_rate` ORDER BY id DESC LIMIT 50");
			DataTable dataTable3 = new DataTable();
			dataTable3.Columns.Add("#", typeof(long));
			dataTable3.Columns.Add("ส\u0e31\u0e48งเม\u0e37\u0e48อ", typeof(string));
			dataTable3.Columns.Add("ใช\u0e49เม\u0e37\u0e48อ", typeof(string));
			dataTable3.Columns.Add("EXP", typeof(string));
			dataTable3.Columns.Add("Drop", typeof(string));
			dataTable3.Columns.Add("ระยะเวลา", typeof(string));
			dataTable3.Columns.Add("สถานะ", typeof(string));
			dataTable3.Columns.Add("ประกาศ", typeof(string));
			foreach (DataRow row in dataTable2.Rows)
			{
				int num4 = Convert.ToInt32(row["minutes"]);
				dataTable3.Rows.Add(Convert.ToInt64(row["id"]), row["created"], row["applied"], Mult(Convert.ToDecimal(row["exp_mult"]) / 100m), Mult(Convert.ToDecimal(row["drop_mult"]) / 100m), (num4 <= 0) ? "-" : (((decimal)num4 / 60m).ToString("0.#") + " ชม."), (!(Convert.ToString(row["status"]) == "1")) ? "รอ Map server" : "ใช\u0e49แล\u0e49ว", row["msg"]);
			}
			gridRateHist.DataSource = dataTable3;
		}
		catch (Exception ex)
		{
			if (!quiet)
			{
				tool.Add("[Tool][Error] " + ex.Message);
			}
		}
	}
}
