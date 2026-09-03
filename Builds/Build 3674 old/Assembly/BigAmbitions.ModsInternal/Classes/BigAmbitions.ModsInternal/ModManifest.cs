// BigAmbitions.ModsInternal, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.ModsInternal.ModManifest
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class ModManifest
{
	private const string ManifestFileName = "steam_mod_manifest.txt";

	private static readonly HashSet<ulong> ModIds = new HashSet<ulong>();

	private static HashSet<ulong> _snapshotModIds;

	private static string ManifestPath => Path.Combine(Application.persistentDataPath, "steam_mod_manifest.txt");

	public static void Initialize()
	{
		EnsureCreated();
		ReloadFromDisk();
	}

	public static void ReloadFromDisk()
	{
		EnsureCreated();
		ModIds.Clear();
		string[] array = File.ReadAllLines(ManifestPath);
		foreach (string text in array)
		{
			if (!string.IsNullOrWhiteSpace(text) && ulong.TryParse(text.Trim(), out var result))
			{
				ModIds.Add(result);
			}
		}
	}

	public static bool Contains(ulong modId)
	{
		return ModIds.Contains(modId);
	}

	public static bool Add(ulong modId)
	{
		if (!ModIds.Add(modId))
		{
			return false;
		}
		Save();
		return true;
	}

	public static bool Remove(ulong modId)
	{
		if (!ModIds.Remove(modId))
		{
			return false;
		}
		Save();
		return true;
	}

	public static IReadOnlyCollection<ulong> GetIds()
	{
		return ModIds;
	}

	private static void EnsureCreated()
	{
		if (!File.Exists(ManifestPath))
		{
			File.WriteAllText(ManifestPath, string.Empty);
		}
	}

	private static void Save()
	{
		EnsureCreated();
		string[] array = new string[ModIds.Count];
		int num = 0;
		foreach (ulong modId in ModIds)
		{
			array[num++] = modId.ToString();
		}
		Array.Sort(array, StringComparer.Ordinal);
		File.WriteAllLines(ManifestPath, array);
	}

	public static void TakeSnapshot()
	{
		_snapshotModIds = new HashSet<ulong>(ModIds);
	}

	public static bool HasChangedSinceSnapshot()
	{
		if (_snapshotModIds == null)
		{
			return true;
		}
		return !_snapshotModIds.SetEquals(ModIds);
	}

	public static HashSet<ulong> GetAddedSinceSnapshot()
	{
		if (_snapshotModIds == null)
		{
			return new HashSet<ulong>(ModIds);
		}
		HashSet<ulong> hashSet = new HashSet<ulong>(ModIds);
		hashSet.ExceptWith(_snapshotModIds);
		return hashSet;
	}

	public static HashSet<ulong> GetRemovedSinceSnapshot()
	{
		if (_snapshotModIds == null)
		{
			return new HashSet<ulong>();
		}
		HashSet<ulong> hashSet = new HashSet<ulong>(_snapshotModIds);
		hashSet.ExceptWith(ModIds);
		return hashSet;
	}
}
