using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ROManager;

internal class ItemDb
{
	public readonly Dictionary<long, string> Names = new Dictionary<long, string>();

	public readonly Dictionary<long, string> Types = new Dictionary<long, string>();

	public bool Loaded;

	public bool Stackable(long id)
	{
		if (!Types.TryGetValue(id, out var value))
		{
			return true;
		}
		switch (value.ToLowerInvariant())
		{
		case "weapon":
		case "armor":
		case "shadowgear":
		case "petegg":
		case "petarmor":
			return false;
		default:
			return true;
		}
	}

	public bool Exists(long id)
	{
		return Names.ContainsKey(id);
	}

	public void Load(string serverDir, bool preRe)
	{
		Names.Clear();
		Types.Clear();
		string path = ((!preRe) ? "re" : "pre-re");
		List<string> list = new List<string>();
		string[] array = new string[2]
		{
			Path.Combine("db", path),
			Path.Combine("db", "import")
		};
		foreach (string path2 in array)
		{
			string path3 = Path.Combine(serverDir, path2);
			if (Directory.Exists(path3))
			{
				list.AddRange(Directory.GetFiles(path3, "item_db*.yml"));
			}
		}
		Regex regex = new Regex("^\\s*-\\s*Id:\\s*(\\d+)");
		Regex regex2 = new Regex("^\\s*Name:\\s*(.+?)\\s*$");
		Regex regex3 = new Regex("^\\s*Type:\\s*(\\w+)");
		foreach (string item in list)
		{
			long num = -1L;
			bool flag = false;
			bool flag2 = false;
			foreach (string item2 in File.ReadLines(item, Encoding.UTF8))
			{
				Match match = regex.Match(item2);
				if (match.Success)
				{
					num = long.Parse(match.Groups[1].Value);
					flag = (flag2 = false);
					Types.Remove(num);
				}
				else if (num >= 0)
				{
					if (!flag && (match = regex2.Match(item2)).Success)
					{
						Names[num] = match.Groups[1].Value.Trim('"', '\'');
						flag = true;
					}
					else if (!flag2 && (match = regex3.Match(item2)).Success)
					{
						Types[num] = match.Groups[1].Value;
						flag2 = true;
					}
				}
			}
		}
		Loaded = true;
	}

	public string Name(long id)
	{
		if (id == 0)
		{
			return string.Empty;
		}
		string value;
		return (!Names.TryGetValue(id, out value)) ? ("#" + id) : value;
	}

	public List<long> Find(string text)
	{
		List<long> list = new List<long>();
		if (long.TryParse(text.Trim(), out var result))
		{
			list.Add(result);
			return list;
		}
		string value = text.Trim().ToLowerInvariant();
		foreach (KeyValuePair<long, string> name in Names)
		{
			if (name.Value.ToLowerInvariant().Contains(value))
			{
				list.Add(name.Key);
			}
		}
		return list;
	}
}
