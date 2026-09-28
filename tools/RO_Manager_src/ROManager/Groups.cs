using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace ROManager;

internal static class Groups
{
	public static readonly Dictionary<int, int> Levels = new Dictionary<int, int>();

	public static List<KeyValuePair<int, string>> Load(string serverDir)
	{
		List<KeyValuePair<int, string>> list = new List<KeyValuePair<int, string>>();
		Levels.Clear();
		string[] array = new string[2]
		{
			Path.Combine("conf", "groups.yml"),
			Path.Combine("conf", "import", "groups.yml")
		};
		foreach (string path in array)
		{
			string path2 = Path.Combine(serverDir, path);
			if (!File.Exists(path2))
			{
				continue;
			}
			int id = -1;
			int num = -1;
			foreach (string item in File.ReadLines(path2))
			{
				Match match = Regex.Match(item, "^\\s*-\\s*Id:\\s*(\\d+)");
				if (match.Success)
				{
					id = int.Parse(match.Groups[1].Value);
					num = id;
					continue;
				}
				match = Regex.Match(item, "^\\s*Level:\\s*(\\d+)");
				if (match.Success && num >= 0)
				{
					Levels[num] = int.Parse(match.Groups[1].Value);
					continue;
				}
				match = Regex.Match(item, "^\\s*Name:\\s*(.+?)\\s*$");
				if (match.Success && id >= 0)
				{
					int num2 = list.FindIndex((KeyValuePair<int, string> x) => x.Key == id);
					KeyValuePair<int, string> keyValuePair = new KeyValuePair<int, string>(id, match.Groups[1].Value.Trim(new char[1] { '"' }));
					if (num2 >= 0)
					{
						list[num2] = keyValuePair;
					}
					else
					{
						list.Add(keyValuePair);
					}
					id = -1;
				}
			}
		}
		if (list.Count == 0)
		{
			list.Add(new KeyValuePair<int, string>(0, "Player"));
			list.Add(new KeyValuePair<int, string>(99, "Admin"));
		}
		list.Sort((KeyValuePair<int, string> a, KeyValuePair<int, string> b) => a.Key.CompareTo(b.Key));
		return list;
	}
}
