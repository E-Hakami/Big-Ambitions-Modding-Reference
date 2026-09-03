using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

public static class SaveGamePathHelper
{
	public static string SaveGameFolderPath => Path.Combine(Application.persistentDataPath, "SaveGames");

	public static List<SaveGameManager.SaveGameStruct> GetAllSaveGamesFromVersion(string versionFolderPath = null)
	{
		if (versionFolderPath == null)
		{
			versionFolderPath = CurrentVersionFolderPath();
		}
		return GetAllSaveGamesFromPath(versionFolderPath);
	}

	private static List<SaveGameManager.SaveGameStruct> GetAllSaveGamesFromPath(string path)
	{
		List<SaveGameManager.SaveGameStruct> list = new List<SaveGameManager.SaveGameStruct>();
		if (!Directory.Exists(path))
		{
			return list;
		}
		string[] directories = Directory.GetDirectories(path);
		foreach (string text in directories)
		{
			string[] files = Directory.GetFiles(text);
			foreach (string text2 in files)
			{
				string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text2);
				string path2 = text2 + ".meta";
				if (File.Exists(path2))
				{
					try
					{
						SaveGameManager.SaveGameStruct saveGameStruct = JsonConvert.DeserializeObject<SaveGameManager.SaveGameStruct>(File.ReadAllText(path2));
						if (saveGameStruct != null)
						{
							list.Add(saveGameStruct);
							continue;
						}
						Debug.LogError("Savegame " + fileNameWithoutExtension + " could not be loaded");
					}
					catch (Exception)
					{
					}
				}
				if (Path.GetExtension(text2) == ".hsg")
				{
					list.Add(new SaveGameManager.SaveGameStruct
					{
						name = fileNameWithoutExtension,
						CharacterPath = text,
						saveGameType = SaveGameManager.SaveGameStruct.SaveGameType.binary,
						lastPlayedDate = File.GetLastWriteTime(text2),
						isRecoverSave = (fileNameWithoutExtension.Length > 7 && fileNameWithoutExtension.Substring(0, 7) == "Recover")
					});
				}
				else if (Path.GetExtension(text2) == ".json")
				{
					list.Add(new SaveGameManager.SaveGameStruct
					{
						name = fileNameWithoutExtension,
						CharacterPath = text,
						saveGameType = SaveGameManager.SaveGameStruct.SaveGameType.json,
						lastPlayedDate = File.GetLastWriteTime(text2),
						isRecoverSave = (fileNameWithoutExtension.Length > 7 && fileNameWithoutExtension.Substring(0, 7) == "Recover")
					});
				}
			}
		}
		return list;
	}

	public static void CreateCurrentVersionSaveGameFolder()
	{
		Directory.CreateDirectory(CurrentVersionFolderPath());
	}

	public static string CurrentVersionFolderPath()
	{
		return Path.Combine(SaveGameFolderPath, GameVersion.GetCurrent()?.GetSaveGameFolderName() ?? "No version");
	}

	public static string GetCharacterFolderPath(string characterName)
	{
		string text = Path.Combine(CurrentVersionFolderPath(), characterName);
		Directory.CreateDirectory(text);
		return text;
	}

	public static string GetTempSavePath()
	{
		return Path.Combine(Application.temporaryCachePath, "tempUncompressedSave");
	}

	public static bool IsSaveGameAlreadyPresent(string characterId, string saveGameName)
	{
		return File.Exists(Path.Combine(GetCharacterFolderPath(characterId), saveGameName + "." + (InstanceBehavior<GameManager>.Instance.useSaveGameTypeJson ? "json" : "hsg")));
	}
}
