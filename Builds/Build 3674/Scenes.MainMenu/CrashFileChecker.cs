using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.Windows;

namespace Scenes.MainMenu;

public static class CrashFileChecker
{
	public static bool AnyCrashInLast7Days()
	{
		if (Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor && Application.platform != RuntimePlatform.OSXPlayer && Application.platform != RuntimePlatform.OSXEditor)
		{
			return false;
		}
		string crashDirectory = GetCrashDirectory();
		if (crashDirectory == null)
		{
			return false;
		}
		if (!AnyCrashInLast7Days(crashDirectory, out var latestCrashDate))
		{
			return false;
		}
		SaveLatestCrashDate(latestCrashDate);
		return true;
	}

	private static string GetCrashDirectory()
	{
		return CrashReporting.crashReportFolder;
	}

	private static bool AnyCrashInLast7Days(string crashDirectory, out DateTime latestCrashDate)
	{
		DateTime dateTime = (latestCrashDate = ((!string.IsNullOrEmpty(PlayerPrefSettings.LatestCrashDate)) ? DateTime.Parse(PlayerPrefSettings.LatestCrashDate, CultureInfo.InvariantCulture) : DateTime.MinValue));
		if (!Directory.Exists(crashDirectory))
		{
			return false;
		}
		try
		{
			string[] fileSystemEntries = Directory.GetFileSystemEntries(crashDirectory);
			for (int i = 0; i < fileSystemEntries.Length; i++)
			{
				if (TryParseCrashDate(Path.GetFileName(fileSystemEntries[i]), out var crashDate) && (DateTime.Now - crashDate).TotalDays <= 7.0 && crashDate > latestCrashDate)
				{
					latestCrashDate = crashDate;
				}
			}
		}
		catch (Exception value)
		{
			Console.WriteLine(value);
			return false;
		}
		if (latestCrashDate != DateTime.MinValue)
		{
			return latestCrashDate != dateTime;
		}
		return false;
	}

	private static bool TryParseCrashDate(string folderName, out DateTime crashDate)
	{
		crashDate = DateTime.MinValue;
		RuntimePlatform platform = Application.platform;
		if (platform == RuntimePlatform.WindowsPlayer || platform == RuntimePlatform.WindowsEditor)
		{
			string[] array = folderName.Split('_');
			if (array.Length > 1 && DateTime.TryParseExact(array[1], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out crashDate))
			{
				return true;
			}
		}
		else
		{
			string[] array2 = folderName.Split('-');
			if (array2.Length > 4)
			{
				if (array2[0] != "Big Ambitions")
				{
					return false;
				}
				if (DateTime.TryParseExact(array2[1] + "-" + array2[2] + "-" + array2[3], "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out crashDate))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static void SaveLatestCrashDate(DateTime crashDate)
	{
		PlayerPrefSettings.LatestCrashDate = crashDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
	}
}
