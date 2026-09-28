using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ROManager;

internal static class Conf
{
	public static Dictionary<string, string> Read(string serverDir, string rel)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		ReadInto(serverDir, rel, dictionary, 0);
		return dictionary;
	}

	private static void ReadInto(string serverDir, string rel, Dictionary<string, string> d, int depth)
	{
		if (depth > 8)
		{
			return;
		}
		string path = Path.Combine(serverDir, rel.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar));
		if (!File.Exists(path))
		{
			return;
		}
		List<string> list = new List<string>();
		string[] array = File.ReadAllLines(path, Encoding.Default);
		foreach (string text in array)
		{
			string text2 = text.Trim();
			if (text2.Length == 0 || text2.StartsWith("//"))
			{
				continue;
			}
			int num = text2.IndexOf(':');
			if (num > 0)
			{
				string text3 = text2.Substring(0, num).Trim();
				string text4 = text2.Substring(num + 1).Trim();
				if (text3.Equals("import", StringComparison.OrdinalIgnoreCase))
				{
					list.Add(text4);
				}
				else
				{
					d[text3] = text4;
				}
			}
		}
		foreach (string item in list)
		{
			ReadInto(serverDir, item, d, depth + 1);
		}
	}

	public static string Get(Dictionary<string, string> d, string k, string def)
	{
		string value;
		return (!d.TryGetValue(k, out value)) ? def : value;
	}
}
