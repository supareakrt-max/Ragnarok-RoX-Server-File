using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace ROManager;

internal static class Ini
{
	public static Dictionary<string, string> Load(string path)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		if (!File.Exists(path))
		{
			return dictionary;
		}
		string[] array = File.ReadAllLines(path, Encoding.UTF8);
		foreach (string text in array)
		{
			int num = text.IndexOf('=');
			if (num > 0)
			{
				dictionary[text.Substring(0, num).Trim()] = text.Substring(num + 1).Trim();
			}
		}
		return dictionary;
	}

	public static void Save(string path, Dictionary<string, string> d)
	{
		StringBuilder stringBuilder = new StringBuilder();
		foreach (KeyValuePair<string, string> item in d)
		{
			stringBuilder.AppendLine(item.Key + "=" + item.Value);
		}
		try
		{
			File.WriteAllText(path, stringBuilder.ToString(), Encoding.UTF8);
		}
		catch
		{
		}
	}
}
