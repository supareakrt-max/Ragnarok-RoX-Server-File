using System;
using System.Runtime.InteropServices;

namespace ROManager;

internal static class K32
{
	[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
	public static extern bool SetDllDirectory(string path);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool AttachConsole(uint pid);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool FreeConsole();

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool SetConsoleCtrlHandler(IntPtr handler, bool add);

	[DllImport("kernel32.dll", SetLastError = true)]
	public static extern bool GenerateConsoleCtrlEvent(uint evt, uint group);
}
