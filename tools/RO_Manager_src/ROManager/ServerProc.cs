using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ROManager;

internal class ServerProc
{
	[StructLayout(LayoutKind.Auto)]
	[CompilerGenerated]
	internal struct _003CStop_003Ec__async0 : IAsyncStateMachine
	{
		internal List<Process> _003Ctargets_003E__0;

		internal List<Process>.Enumerator _0024locvar0;

		internal Process _003Cp_003E__1;

		internal bool _003Csent_003E__2;

		internal int _003Cwaited_003E__2;

		internal int timeoutMs;

		internal bool _003Cexited_003E__3;

		internal ServerProc _0024this;

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
					_0024this.Stopping = true;
					_003Ctargets_003E__0 = _0024this.External().ToList();
					if (_0024this.OwnRunning)
					{
						_003Ctargets_003E__0.Add(_0024this.Proc);
					}
					if (_003Ctargets_003E__0.Count != 0)
					{
						_0024locvar0 = _003Ctargets_003E__0.GetEnumerator();
						num = 4294967293u;
						break;
					}
					goto end_IL_0010;
				case 1u:
					break;
				}
				try
				{
					switch (num)
					{
					case 1u:
						_0024awaiter0.GetResult();
						_003Cwaited_003E__2 += 250;
						goto IL_01d9;
					default:
						{
							if (_0024locvar0.MoveNext())
							{
								_003Cp_003E__1 = _0024locvar0.Current;
								_0024this.Add("[Tool] กำล\u0e31งป\u0e34ด " + _0024this.Title + " (PID " + _003Cp_003E__1.Id + ") ...");
								_003Csent_003E__2 = SendCtrlC((uint)_003Cp_003E__1.Id);
								if (!_003Csent_003E__2)
								{
									_0024this.Add("[Tool][Warning] ส\u0e48ง Ctrl+C ไม\u0e48สำเร\u0e47จ");
								}
								_003Cwaited_003E__2 = 0;
								goto IL_01d9;
							}
							break;
						}
						IL_01d9:
						if (_003Cwaited_003E__2 < timeoutMs)
						{
							try
							{
								_003Cexited_003E__3 = _003Cp_003E__1.HasExited;
							}
							catch
							{
								_003Cexited_003E__3 = true;
							}
							if (!_003Cexited_003E__3)
							{
								_0024awaiter0 = Task.Delay(250).GetAwaiter();
								if (!_0024awaiter0.IsCompleted)
								{
									_0024PC = 1;
									flag = true;
									_0024builder.AwaitUnsafeOnCompleted(ref _0024awaiter0, ref this);
									return;
								}
								goto case 1u;
							}
						}
						try
						{
							if (!_003Cp_003E__1.HasExited)
							{
								_0024this.Add("[Tool][Warning] " + _0024this.Title + " ไม\u0e48ยอมป\u0e34ด บ\u0e31งค\u0e31บป\u0e34ด");
								_003Cp_003E__1.Kill();
							}
						}
						catch
						{
						}
						goto default;
					}
				}
				finally
				{
					if (!flag)
					{
						((IDisposable)_0024locvar0).Dispose();
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

	public readonly string Title;

	public readonly string ExeName;

	public string Dir;

	public Process Proc;

	public volatile bool Stopping;

	public bool AutoRestart;

	public readonly ConcurrentQueue<string> Log = new ConcurrentQueue<string>();

	private static readonly Regex Ansi = new Regex("\\x1b\\[[0-9;]*[A-Za-z]");

	private static readonly object ctrlLock = new object();

	public string ExePath => Path.Combine(Dir ?? string.Empty, ExeName);

	public bool OwnRunning
	{
		get
		{
			try
			{
				return Proc != null && !Proc.HasExited;
			}
			catch
			{
				return false;
			}
		}
	}

	public bool Running => OwnRunning || External().Length > 0;

	public event Action<ServerProc, int> Crashed;

	public ServerProc(string title, string exe)
	{
		Title = title;
		ExeName = exe;
	}

	public void Add(string s)
	{
		Log.Enqueue(s);
	}

	public Process[] External()
	{
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(ExeName);
		return Process.GetProcessesByName(fileNameWithoutExtension).Where(delegate(Process p)
		{
			if (Proc != null && !Proc.HasExited && p.Id == Proc.Id)
			{
				return false;
			}
			try
			{
				return string.Equals(Path.GetFullPath(p.MainModule.FileName), Path.GetFullPath(ExePath), StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return true;
			}
		}).ToArray();
	}

	public bool Start()
	{
		if (Running)
		{
			Add("[Tool] " + Title + " กำล\u0e31งทำงานอย\u0e39\u0e48แล\u0e49ว");
			return true;
		}
		if (!File.Exists(ExePath))
		{
			Add("[Tool][Error] ไม\u0e48พบไฟล\u0e4c " + ExePath);
			return false;
		}
		ProcessStartInfo processStartInfo = new ProcessStartInfo(ExePath);
		processStartInfo.WorkingDirectory = Dir;
		processStartInfo.UseShellExecute = false;
		processStartInfo.CreateNoWindow = true;
		processStartInfo.RedirectStandardOutput = true;
		processStartInfo.RedirectStandardError = true;
		processStartInfo.RedirectStandardInput = true;
		processStartInfo.StandardOutputEncoding = Encoding.GetEncoding(874);
		processStartInfo.StandardErrorEncoding = Encoding.GetEncoding(874);
		ProcessStartInfo startInfo = processStartInfo;
		Process p = new Process
		{
			StartInfo = startInfo,
			EnableRaisingEvents = true
		};
		p.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				Add(Ansi.Replace(e.Data, string.Empty));
			}
		};
		p.ErrorDataReceived += delegate(object s, DataReceivedEventArgs e)
		{
			if (e.Data != null)
			{
				Add(Ansi.Replace(e.Data, string.Empty));
			}
		};
		p.Exited += delegate
		{
			int num = 0;
			try
			{
				num = p.ExitCode;
			}
			catch
			{
			}
			Add("[Tool] " + Title + " ป\u0e34ดแล\u0e49ว (exit code " + num + ")");
			if (!Stopping && this.Crashed != null)
			{
				this.Crashed(this, num);
			}
		};
		try
		{
			Stopping = false;
			lock (ctrlLock)
			{
				p.Start();
			}
			p.BeginOutputReadLine();
			p.BeginErrorReadLine();
			Proc = p;
			Add("[Tool] เร\u0e34\u0e48ม " + Title + " (PID " + p.Id + ")");
			return true;
		}
		catch (Exception ex)
		{
			Add("[Tool][Error] เป\u0e34ด " + Title + " ไม\u0e48ได\u0e49: " + ex.Message);
			return false;
		}
	}

	[DebuggerStepThrough]
	[AsyncStateMachine(typeof(_003CStop_003Ec__async0))]
	public Task Stop(int timeoutMs = 20000)
	{
		_003CStop_003Ec__async0 stateMachine = default(_003CStop_003Ec__async0);
		stateMachine.timeoutMs = timeoutMs;
		stateMachine._0024this = this;
		stateMachine._0024builder = AsyncTaskMethodBuilder.Create();
		ref AsyncTaskMethodBuilder _0024builder = ref stateMachine._0024builder;
		_0024builder.Start(ref stateMachine);
		return _0024builder.Task;
	}

	public void Kill()
	{
		Stopping = true;
		Process[] array = External();
		foreach (Process process in array)
		{
			try
			{
				process.Kill();
			}
			catch
			{
			}
		}
		try
		{
			if (OwnRunning)
			{
				Proc.Kill();
			}
		}
		catch
		{
		}
	}

	private static bool SendCtrlC(uint pid)
	{
		lock (ctrlLock)
		{
			K32.FreeConsole();
			if (!K32.AttachConsole(pid))
			{
				return false;
			}
			K32.SetConsoleCtrlHandler(IntPtr.Zero, add: true);
			bool result = K32.GenerateConsoleCtrlEvent(0u, 0u);
			Thread.Sleep(300);
			K32.FreeConsole();
			Thread.Sleep(200);
			K32.SetConsoleCtrlHandler(IntPtr.Zero, add: false);
			return result;
		}
	}
}
