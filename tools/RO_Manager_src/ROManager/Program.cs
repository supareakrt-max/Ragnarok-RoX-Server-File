using System;
using System.Threading;
using System.Windows.Forms;

namespace ROManager;

internal static class Program
{
	[STAThread]
	private static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);
		Application.ThreadException += delegate(object s, ThreadExceptionEventArgs e)
		{
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			MessageBox.Show(e.Exception.ToString(), "RO Server Manager - Error");
		};
		Application.Run((Form)(object)new MainForm());
	}
}
