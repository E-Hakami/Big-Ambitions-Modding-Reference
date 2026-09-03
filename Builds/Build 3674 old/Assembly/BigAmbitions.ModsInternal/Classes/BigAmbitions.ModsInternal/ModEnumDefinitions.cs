// BigAmbitions.ModsInternal, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.ModsInternal.ModEnumDefinitions
using System;
using System.Collections.Generic;
using System.IO;
using BAModAPI;
using BigAmbitions;
using UnityEngine;

public static class ModEnumDefinitions
{
	private const string EnumsFile = "enums.txt";

	private static readonly Dictionary<(Type enumType, int value), string> DefinedEnumValueNames = new Dictionary<(Type, int), string>();

	private static readonly Dictionary<string, string> ModTitlesByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<(Type enumType, int value), HashSet<string>> DefinedEnumOwnerModKeysByEnumKey = new Dictionary<(Type, int), HashSet<string>>();

	private static readonly Dictionary<string, List<(Type enumType, int value)>> DefinedEnumKeysByModKey = new Dictionary<string, List<(Type, int)>>(StringComparer.OrdinalIgnoreCase);

	private static readonly HashSet<string> ActiveModKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	public static void Clear()
	{
		DefinedEnumOwnerModKeysByEnumKey.Clear();
		DefinedEnumKeysByModKey.Clear();
		DefinedEnumValueNames.Clear();
		ActiveModKeys.Clear();
		ModTitlesByKey.Clear();
	}

	public static bool TryDefineModEnums(ModInfo modInfo)
	{
		return RegisterModEnums(modInfo, isActive: true);
	}

	public static bool RegisterModEnums(ModInfo modInfo, bool isActive)
	{
		List<(Type, string)> modEnums = GetModEnums(modInfo);
		if (modEnums == null)
		{
			return true;
		}
		string modKey = GetModKey(modInfo);
		ModTitlesByKey[modKey] = (string.IsNullOrWhiteSpace(modInfo.title) ? modKey : modInfo.title);
		if (isActive)
		{
			ActiveModKeys.Add(modKey);
		}
		else
		{
			ActiveModKeys.Remove(modKey);
		}
		if (!DefinedEnumKeysByModKey.TryGetValue(modKey, out List<(Type, int)> value))
		{
			value = new List<(Type, int)>();
			DefinedEnumKeysByModKey[modKey] = value;
		}
		else
		{
			value.Clear();
		}
		bool result = true;
		foreach (var item3 in modEnums)
		{
			Type item = item3.Item1;
			string item2 = item3.Item2;
			int enumSafeHash = GetEnumSafeHash(item2);
			(Type, int) tuple = (item, enumSafeHash);
			value.Add(tuple);
			if (!DefinedEnumOwnerModKeysByEnumKey.TryGetValue(tuple, out var value2))
			{
				value2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				DefinedEnumOwnerModKeysByEnumKey[tuple] = value2;
			}
			DefinedEnumValueNames.TryAdd(tuple, item2);
			if (!value2.Add(modKey) || value2.Count <= 1)
			{
				continue;
			}
			result = false;
			foreach (string item4 in value2)
			{
				if (!string.Equals(item4, modKey, StringComparison.OrdinalIgnoreCase))
				{
					string valueOrDefault = ModTitlesByKey.GetValueOrDefault(modKey, modKey);
					string valueOrDefault2 = DefinedEnumValueNames.GetValueOrDefault(tuple, item2);
					string valueOrDefault3 = ModTitlesByKey.GetValueOrDefault(item4, item4);
					Debug.LogWarning("Enum value '" + item.FullName + "." + item2 + "' defined by mod " + valueOrDefault + " conflicts with hash of '" + item.FullName + "." + valueOrDefault2 + "' defined by mod " + valueOrDefault3);
				}
			}
		}
		return result;
	}

	public static List<string> GetModConflictsList(ModInfo modInfo)
	{
		string modKey = GetModKey(modInfo);
		if (!DefinedEnumKeysByModKey.TryGetValue(modKey, out List<(Type, int)> value) || value.Count == 0)
		{
			return null;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var item in value)
		{
			if (!DefinedEnumOwnerModKeysByEnumKey.TryGetValue(item, out var value2))
			{
				continue;
			}
			foreach (string item2 in value2)
			{
				if (!string.Equals(item2, modKey, StringComparison.OrdinalIgnoreCase) && ActiveModKeys.Contains(item2))
				{
					hashSet.Add(item2);
				}
			}
		}
		if (hashSet.Count == 0)
		{
			return null;
		}
		List<string> list = new List<string>();
		foreach (string item3 in hashSet)
		{
			string valueOrDefault = ModTitlesByKey.GetValueOrDefault(item3, item3);
			list.Add("- " + valueOrDefault);
		}
		list.Sort(StringComparer.InvariantCultureIgnoreCase);
		return list;
	}

	private static List<(Type enumType, string valueName)> GetModEnums(ModInfo modInfo)
	{
		if (string.IsNullOrWhiteSpace(modInfo.modFolder) || !Directory.Exists(modInfo.modFolder))
		{
			return null;
		}
		string path = Path.Combine(modInfo.modFolder, "enums.txt");
		if (!File.Exists(path))
		{
			return null;
		}
		string[] array = File.ReadAllLines(path);
		List<(Type, string)> list = new List<(Type, string)>();
		string[] array2 = array;
		foreach (string text in array2)
		{
			int num = text.LastIndexOf('.');
			if (num <= 0 || num >= text.Length - 1)
			{
				continue;
			}
			string text2 = text.Substring(0, num).Trim();
			string text3 = text;
			int num2 = num + 1;
			string text4 = text3.Substring(num2, text3.Length - num2).Trim();
			if (!string.IsNullOrWhiteSpace(text2) && !string.IsNullOrWhiteSpace(text4))
			{
				if (!text2.Contains(","))
				{
					text2 += ", BigAmbitions, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
				}
				else if (!text2.Contains("Version="))
				{
					text2 += ", Version=0.0.0.0, Culture=neutral, PublicKeyToken=null";
				}
				else if (!text2.Contains("Culture="))
				{
					text2 += ", Culture=neutral, PublicKeyToken=null";
				}
				Type type = Type.GetType(text2);
				if (type == null)
				{
					Debug.LogError($"Failed to find enum type '{text2}' when processing mod enum definitions for mod {modInfo.steamItemId}");
				}
				else
				{
					list.Add((type, text4));
				}
			}
		}
		return list;
	}

	public static List<string> GetAllConflictingModsList()
	{
		List<string> list = new List<string>();
		foreach (string activeModKey in ActiveModKeys)
		{
			if (!DefinedEnumKeysByModKey.TryGetValue(activeModKey, out List<(Type, int)> value) || value.Count == 0)
			{
				continue;
			}
			bool flag = false;
			foreach (var item in value)
			{
				if (!DefinedEnumOwnerModKeysByEnumKey.TryGetValue(item, out var value2))
				{
					continue;
				}
				foreach (string item2 in value2)
				{
					if (!string.Equals(item2, activeModKey, StringComparison.OrdinalIgnoreCase) && ActiveModKeys.Contains(item2))
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					break;
				}
			}
			if (flag)
			{
				string valueOrDefault = ModTitlesByKey.GetValueOrDefault(activeModKey, activeModKey);
				list.Add("- " + valueOrDefault);
			}
		}
		list.Sort(StringComparer.InvariantCultureIgnoreCase);
		return list;
	}

	private static int GetEnumSafeHash(string source)
	{
		return ModEnumHash.GetSafeHash(source);
	}

	private static string GetModKey(ModInfo modInfo)
	{
		if (modInfo == null)
		{
			return "0";
		}
		if (modInfo.steamItemId != 0L)
		{
			return modInfo.steamItemId.ToString();
		}
		if (!string.IsNullOrWhiteSpace(modInfo.modFolder))
		{
			return Path.GetFullPath(modInfo.modFolder);
		}
		if (string.IsNullOrWhiteSpace(modInfo.title))
		{
			return "0";
		}
		return modInfo.title;
	}
}
