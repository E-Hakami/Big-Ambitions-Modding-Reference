using System;
using System.Collections.Generic;
using System.Text;
using Localizor.LanguageChangeEvent;
using UnityEngine;

[Serializable]
public class GameVersion : ScriptableObject
{
	[Serializable]
	public class ChangelogEntry
	{
		public EntryType entryType;

		public string text;
	}

	[Serializable]
	public struct VersionInfo
	{
		public int latestBuildNumber;

		public string version;
	}

	[Serializable]
	public enum EntryType
	{
		Title,
		EmptyLine,
		Default
	}

	public const int BlueprintMinTrackedBuildNumber = 2463;

	public bool experimental;

	public int buildNumber;

	public List<VersionInfo> latestBuildNumberForVersion;

	public ChangelogEntry[] changelogEntries;

	private static GameVersion CurrentGameVersion;

	public static GameVersion GetCurrent()
	{
		if (CurrentGameVersion == null)
		{
			CurrentGameVersion = Resources.Load<GameVersion>("Versioning/Current");
		}
		return CurrentGameVersion;
	}

	public string GetFullVersionString()
	{
		string text = (experimental ? "Experimental " : string.Empty);
		return GetVersionString(buildNumber, useBlueprintPreVersionSystem: false, includeBuildNumber: false) + " (" + text + GetBuildVersionString() + ")";
	}

	public List<string> GetPreviousVersionsString()
	{
		string versionByBuildNumber = GetVersionByBuildNumber(buildNumber);
		List<string> list = new List<string>();
		foreach (VersionInfo item in latestBuildNumberForVersion)
		{
			if (versionByBuildNumber == item.version)
			{
				return list;
			}
			list.Add(item.version);
		}
		return list;
	}

	public string GetSaveGameFolderName()
	{
		if (HasCommandLineArg("-forceExperimentalVersion"))
		{
			experimental = true;
		}
		else if (HasCommandLineArg("-forceNoExperimentalVersion"))
		{
			experimental = false;
		}
		return GetVersionString(buildNumber, useBlueprintPreVersionSystem: false, includeBuildNumber: false);
	}

	private string GetBuildVersionString()
	{
		return $"Build {buildNumber}";
	}

	public string GetChangelog()
	{
		StringBuilder stringBuilder = new StringBuilder();
		ChangelogEntry[] array = changelogEntries;
		foreach (ChangelogEntry changelogEntry in array)
		{
			if (changelogEntry.entryType == EntryType.Default)
			{
				stringBuilder.Append("• ");
			}
			if (changelogEntry.entryType == EntryType.Title)
			{
				stringBuilder.Append("<b>");
			}
			if (changelogEntry.entryType != EntryType.EmptyLine)
			{
				stringBuilder.Append(changelogEntry.text);
			}
			if (changelogEntry.entryType == EntryType.Title)
			{
				stringBuilder.Append("</b>");
			}
			stringBuilder.Append("\n");
		}
		stringBuilder.Length--;
		return stringBuilder.ToString();
	}

	public static string GetVersionString(int buildNumber, bool useBlueprintPreVersionSystem = false, bool includeBuildNumber = true)
	{
		string versionByBuildNumber = GetVersionByBuildNumber(buildNumber, useBlueprintPreVersionSystem);
		if (buildNumber <= 0 || !includeBuildNumber)
		{
			return versionByBuildNumber;
		}
		return new LanguageChangeEventDataHolder
		{
			Key = "common_version_with_buildnumber",
			Arguments = new
			{
				version = versionByBuildNumber,
				buildNumber = buildNumber
			}
		}.ToString();
	}

	public static string GetVersionByBuildNumber(int buildNumber, bool useBlueprintPreVersionSystem = false)
	{
		GameVersion current = GetCurrent();
		if (current?.latestBuildNumberForVersion == null || current.latestBuildNumberForVersion.Count == 0)
		{
			return string.Empty;
		}
		VersionInfo versionInfo = default(VersionInfo);
		bool flag = false;
		foreach (VersionInfo item in current.latestBuildNumberForVersion)
		{
			if (buildNumber <= item.latestBuildNumber)
			{
				versionInfo = item;
				flag = true;
				break;
			}
		}
		if (!flag || string.IsNullOrEmpty(versionInfo.version))
		{
			return string.Empty;
		}
		if (!useBlueprintPreVersionSystem || buildNumber >= 2463)
		{
			return versionInfo.version;
		}
		string versionByBuildNumber = GetVersionByBuildNumber(2463);
		if (string.IsNullOrEmpty(versionByBuildNumber))
		{
			return versionInfo.version;
		}
		return new LanguageChangeEventDataHolder
		{
			Key = "blueprint_filter_pre_version",
			Arguments = new
			{
				gameVersion = versionByBuildNumber
			}
		}.ToString();
	}

	public static bool IsBuildFromOlderVersionGroup(int buildNumber)
	{
		GameVersion current = GetCurrent();
		return IsBuildFromOlderVersionGroup(buildNumber, current.buildNumber);
	}

	public static bool IsBuildFromOlderVersionGroup(int buildNumber, int comparedBuildNumber)
	{
		List<VersionInfo> list = GetCurrent().latestBuildNumberForVersion;
		if (list == null || list.Count == 0)
		{
			return false;
		}
		int versionGroupIndex = GetVersionGroupIndex(buildNumber, list);
		int versionGroupIndex2 = GetVersionGroupIndex(comparedBuildNumber, list);
		if (versionGroupIndex < 0 || versionGroupIndex2 < 0)
		{
			return false;
		}
		return versionGroupIndex < versionGroupIndex2;
	}

	private static int GetVersionGroupIndex(int buildNumber, List<VersionInfo> versions)
	{
		for (int i = 0; i < versions.Count; i++)
		{
			if (buildNumber <= versions[i].latestBuildNumber)
			{
				return i;
			}
		}
		return versions.Count - 1;
	}

	private static bool HasCommandLineArg(string argument)
	{
		string[] commandLineArgs = Environment.GetCommandLineArgs();
		for (int i = 0; i < commandLineArgs.Length; i++)
		{
			if (commandLineArgs[i] == argument)
			{
				return true;
			}
		}
		return false;
	}
}
