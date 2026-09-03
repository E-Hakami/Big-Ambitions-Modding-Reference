using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

public static class SaveGameCompatibilityHelper
{
	private static string GetPreviousGameVersionWithSavesFolderPath()
	{
		List<string> previousVersionsString = MainMenuController.currentVersion.GetPreviousVersionsString();
		for (int num = previousVersionsString.Count - 1; num >= 0; num--)
		{
			string path = previousVersionsString[num];
			string text = Path.Combine(SaveGamePathHelper.SaveGameFolderPath, path);
			if (Directory.Exists(text) && ContainsSaveGames(text))
			{
				return text;
			}
		}
		return null;
	}

	public static bool HasSaveGamesInPreviousVersion()
	{
		return GetPreviousGameVersionWithSavesFolderPath() != null;
	}

	public static bool HasCurrentVersionSaveGamesFolder()
	{
		return Directory.Exists(SaveGamePathHelper.CurrentVersionFolderPath());
	}

	public static async Task<int> CopySaveGamesBetweenPreviousAndCurrentVersion()
	{
		string previousGameVersionWithSavesFolderPath = GetPreviousGameVersionWithSavesFolderPath();
		if (previousGameVersionWithSavesFolderPath == null)
		{
			return 0;
		}
		string path = SaveGamePathHelper.CurrentVersionFolderPath();
		DirectoryInfo source = new DirectoryInfo(previousGameVersionWithSavesFolderPath);
		DirectoryInfo currentDirectoryInfo = new DirectoryInfo(path);
		int previousSaveGamesCount = currentDirectoryInfo.GetDirectories().Length;
		await CopyAllElementsFromFolderToFolder(source, currentDirectoryInfo);
		return currentDirectoryInfo.GetDirectories().Length - previousSaveGamesCount;
	}

	private static async Task CopyAllElementsFromFolderToFolder(DirectoryInfo source, DirectoryInfo target)
	{
		FileInfo[] files = source.GetFiles();
		foreach (FileInfo fileInfo in files)
		{
			if (!fileInfo.Extension.Equals(".vdf", StringComparison.OrdinalIgnoreCase))
			{
				string text = Path.Combine(target.FullName, fileInfo.Name);
				if (!File.Exists(text))
				{
					await CopyFileAsync(fileInfo.FullName, text);
				}
			}
		}
		DirectoryInfo[] directories = source.GetDirectories();
		foreach (DirectoryInfo directoryInfo in directories)
		{
			if (!(directoryInfo.FullName == target.FullName))
			{
				DirectoryInfo target2 = target.CreateSubdirectory(directoryInfo.Name);
				await CopyAllElementsFromFolderToFolder(directoryInfo, target2);
			}
		}
	}

	private static bool ContainsSaveGames(string versionFolderPath)
	{
		string[] directories = Directory.GetDirectories(versionFolderPath);
		for (int i = 0; i < directories.Length; i++)
		{
			string[] files = Directory.GetFiles(directories[i]);
			for (int j = 0; j < files.Length; j++)
			{
				string extension = Path.GetExtension(files[j]);
				if (extension.Equals(".hsg", StringComparison.OrdinalIgnoreCase) || extension.Equals(".json", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static async Task CopyFileAsync(string sourceFile, string destinationFile)
	{
		await using FileStream sourceStream = new FileStream(sourceFile, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
		await using FileStream destinationStream = new FileStream(destinationFile, FileMode.CreateNew, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous | FileOptions.SequentialScan);
		await sourceStream.CopyToAsync(destinationStream);
	}
}
