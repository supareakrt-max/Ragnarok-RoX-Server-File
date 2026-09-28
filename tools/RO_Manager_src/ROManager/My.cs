using System;
using System.Runtime.InteropServices;

namespace ROManager;

internal static class My
{
	private const string DLL = "libmysql.dll";

	[DllImport("libmysql.dll")]
	public static extern IntPtr mysql_init(IntPtr m);

	[DllImport("libmysql.dll")]
	public static extern int mysql_options(IntPtr m, int option, ref uint arg);

	[DllImport("libmysql.dll", CharSet = CharSet.Ansi)]
	public static extern IntPtr mysql_real_connect(IntPtr m, string host, string user, string pass, string db, uint port, IntPtr sock, uint flags);

	[DllImport("libmysql.dll", CharSet = CharSet.Ansi)]
	public static extern int mysql_set_character_set(IntPtr m, string cs);

	[DllImport("libmysql.dll")]
	public static extern int mysql_real_query(IntPtr m, byte[] q, uint len);

	[DllImport("libmysql.dll")]
	public static extern IntPtr mysql_store_result(IntPtr m);

	[DllImport("libmysql.dll")]
	public static extern uint mysql_num_fields(IntPtr res);

	[DllImport("libmysql.dll")]
	public static extern uint mysql_field_count(IntPtr m);

	[DllImport("libmysql.dll")]
	public static extern IntPtr mysql_fetch_row(IntPtr res);

	[DllImport("libmysql.dll")]
	public static extern IntPtr mysql_fetch_lengths(IntPtr res);

	[DllImport("libmysql.dll")]
	public static extern IntPtr mysql_fetch_field_direct(IntPtr res, uint nr);

	[DllImport("libmysql.dll")]
	public static extern void mysql_free_result(IntPtr res);

	[DllImport("libmysql.dll")]
	public static extern IntPtr mysql_error(IntPtr m);

	[DllImport("libmysql.dll")]
	public static extern void mysql_close(IntPtr m);

	[DllImport("libmysql.dll")]
	public static extern ulong mysql_affected_rows(IntPtr m);

	[DllImport("libmysql.dll")]
	public static extern ulong mysql_insert_id(IntPtr m);

	[DllImport("libmysql.dll")]
	public static extern int mysql_ping(IntPtr m);
}
