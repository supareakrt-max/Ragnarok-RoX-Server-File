using System;
using System.Data;
using System.Runtime.InteropServices;
using System.Text;

namespace ROManager;

internal class Db : IDisposable
{
	public string Host = "127.0.0.1";

	public string User = "root";

	public string Pass = string.Empty;

	public string Name = "rathena_main";

	public string Codepage = string.Empty;

	public uint Port = 3306u;

	public Encoding Enc = Encoding.GetEncoding(874);

	private IntPtr conn = IntPtr.Zero;

	public string LastError = string.Empty;

	public bool Connected => conn != IntPtr.Zero;

	public bool Connect()
	{
		Close();
		IntPtr intPtr = My.mysql_init(IntPtr.Zero);
		if (intPtr == IntPtr.Zero)
		{
			LastError = "mysql_init failed";
			return false;
		}
		uint arg = 3u;
		My.mysql_options(intPtr, 0, ref arg);
		if (My.mysql_real_connect(intPtr, Host, User, Pass, Name, Port, IntPtr.Zero, 0u) == IntPtr.Zero)
		{
			LastError = Marshal.PtrToStringAnsi(My.mysql_error(intPtr));
			My.mysql_close(intPtr);
			return false;
		}
		conn = intPtr;
		if (!string.IsNullOrEmpty(Codepage))
		{
			My.mysql_set_character_set(conn, Codepage);
		}
		LastError = string.Empty;
		return true;
	}

	public void Close()
	{
		if (conn != IntPtr.Zero)
		{
			My.mysql_close(conn);
			conn = IntPtr.Zero;
		}
	}

	public void Dispose()
	{
		Close();
	}

	private bool Ensure()
	{
		if (conn != IntPtr.Zero && My.mysql_ping(conn) == 0)
		{
			return true;
		}
		return Connect();
	}

	public static string Esc(string s)
	{
		if (s == null)
		{
			return string.Empty;
		}
		StringBuilder stringBuilder = new StringBuilder(s.Length + 8);
		foreach (char c in s)
		{
			switch (c)
			{
			case '\\':
				stringBuilder.Append("\\\\");
				break;
			case '\'':
				stringBuilder.Append("\\'");
				break;
			case '"':
				stringBuilder.Append("\\\"");
				break;
			case '\0':
				stringBuilder.Append("\\0");
				break;
			case '\n':
				stringBuilder.Append("\\n");
				break;
			case '\r':
				stringBuilder.Append("\\r");
				break;
			case '\u001a':
				stringBuilder.Append("\\Z");
				break;
			default:
				stringBuilder.Append(c);
				break;
			}
		}
		return stringBuilder.ToString();
	}

	private void Run(string sql)
	{
		if (!Ensure())
		{
			throw new Exception("เช\u0e37\u0e48อมต\u0e48อฐานข\u0e49อม\u0e39ลไม\u0e48ได\u0e49: " + LastError);
		}
		byte[] bytes = Enc.GetBytes(sql);
		if (My.mysql_real_query(conn, bytes, (uint)bytes.Length) != 0)
		{
			throw new Exception("SQL error: " + Marshal.PtrToStringAnsi(My.mysql_error(conn)));
		}
	}

	public long Exec(string sql)
	{
		Run(sql);
		IntPtr intPtr = My.mysql_store_result(conn);
		if (intPtr != IntPtr.Zero)
		{
			My.mysql_free_result(intPtr);
		}
		return (long)My.mysql_affected_rows(conn);
	}

	public long InsertId()
	{
		return (long)My.mysql_insert_id(conn);
	}

	public object Scalar(string sql)
	{
		DataTable dataTable = Query(sql);
		if (dataTable.Rows.Count == 0 || dataTable.Columns.Count == 0)
		{
			return null;
		}
		return (dataTable.Rows[0][0] != DBNull.Value) ? dataTable.Rows[0][0] : null;
	}

	public DataTable Query(string sql)
	{
		Run(sql);
		DataTable dataTable = new DataTable();
		IntPtr intPtr = My.mysql_store_result(conn);
		if (intPtr == IntPtr.Zero)
		{
			return dataTable;
		}
		try
		{
			uint num = My.mysql_num_fields(intPtr);
			for (uint num2 = 0u; num2 < num; num2++)
			{
				IntPtr ptr = My.mysql_fetch_field_direct(intPtr, num2);
				string text = Marshal.PtrToStringAnsi(Marshal.ReadIntPtr(ptr));
				string text2 = text;
				int num3 = 2;
				while (dataTable.Columns.Contains(text2))
				{
					text2 = text + num3++;
				}
				dataTable.Columns.Add(text2, typeof(string));
			}
			IntPtr ptr2;
			while ((ptr2 = My.mysql_fetch_row(intPtr)) != IntPtr.Zero)
			{
				IntPtr ptr3 = My.mysql_fetch_lengths(intPtr);
				object[] array = new object[num];
				for (int i = 0; i < num; i++)
				{
					IntPtr intPtr2 = Marshal.ReadIntPtr(ptr2, i * IntPtr.Size);
					if (intPtr2 == IntPtr.Zero)
					{
						array[i] = DBNull.Value;
						continue;
					}
					int num4 = Marshal.ReadInt32(ptr3, i * 4);
					byte[] array2 = new byte[num4];
					Marshal.Copy(intPtr2, array2, 0, num4);
					array[i] = Enc.GetString(array2);
				}
				dataTable.Rows.Add(array);
			}
		}
		finally
		{
			My.mysql_free_result(intPtr);
		}
		return Typed(dataTable);
	}

	private static DataTable Typed(DataTable dt)
	{
		DataTable dataTable = new DataTable();
		bool[] array = new bool[dt.Columns.Count];
		for (int i = 0; i < dt.Columns.Count; i++)
		{
			bool flag = dt.Rows.Count > 0;
			foreach (DataRow row in dt.Rows)
			{
				if (row[i] != DBNull.Value)
				{
					string text = (string)row[i];
					if (text.Length == 0 || text.Length > 18 || (text.Length > 1 && text[0] == '0') || !long.TryParse(text, out var _))
					{
						flag = false;
						break;
					}
				}
			}
			array[i] = flag;
			dataTable.Columns.Add(dt.Columns[i].ColumnName, (!flag) ? typeof(string) : typeof(long));
		}
		foreach (DataRow row2 in dt.Rows)
		{
			object[] array2 = new object[dt.Columns.Count];
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j] = ((!array[j] || row2[j] == DBNull.Value) ? row2[j] : ((object)long.Parse((string)row2[j])));
			}
			dataTable.Rows.Add(array2);
		}
		return dataTable;
	}
}
