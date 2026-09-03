using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UI.MainMenu;

public static class CustomGamePresetsHandler
{
	private const string PresetFolder = "CustomGamePresets";

	private static string PresetPath => Application.persistentDataPath + "/CustomGamePresets";

	public static List<string> PresetNames { get; private set; }

	public static List<GameVariables> Presets { get; private set; }

	public static void FetchPresets()
	{
		PresetNames = new List<string>();
		Presets = new List<GameVariables>();
		DirectoryInfo directoryInfo = new DirectoryInfo(PresetPath);
		if (directoryInfo.Exists)
		{
			FileInfo[] files = directoryInfo.GetFiles("*.txt");
			for (int i = 0; i < files.Length; i++)
			{
				var (item, original) = GetPresetFromFile(files[i].FullName);
				PresetNames.Add(item);
				Presets.Add(original.Copy());
			}
		}
	}

	public static void SavePreset(string presetName, GameVariables preset)
	{
		if (!Directory.Exists(PresetPath))
		{
			Directory.CreateDirectory(PresetPath);
		}
		string path = PresetPath + "/" + presetName + ".txt";
		SaveData(preset, path);
		PresetNames.Add(presetName);
		Presets.Add(preset.Copy());
	}

	public static void DeletePreset(string presetName)
	{
		string path = PresetPath + "/" + presetName + ".txt";
		if (File.Exists(path))
		{
			File.Delete(path);
		}
		int index = PresetNames.IndexOf(presetName);
		PresetNames.RemoveAt(index);
		Presets.RemoveAt(index);
	}

	public static void CopyToClipboard(GameVariables preset)
	{
		GUIUtility.systemCopyBuffer = CustomGamePresetsParser.GetDataAsString(preset);
	}

	public static GameVariables GetFromClipboard()
	{
		return CustomGamePresetsParser.GetStringAsData<GameVariables>(GUIUtility.systemCopyBuffer);
	}

	private static (string, GameVariables) GetPresetFromFile(string filePath)
	{
		return (Path.GetFileNameWithoutExtension(filePath), LoadData<GameVariables>(filePath));
	}

	private static void SaveData<T>(T data, string path)
	{
		if (File.Exists(path))
		{
			int index = PresetNames.IndexOf(Path.GetFileNameWithoutExtension(path));
			PresetNames.RemoveAt(index);
			Presets.RemoveAt(index);
			File.Delete(path);
		}
		string dataAsString = CustomGamePresetsParser.GetDataAsString(data);
		File.WriteAllText(path, dataAsString);
	}

	private static T LoadData<T>(string path) where T : class, new()
	{
		return CustomGamePresetsParser.GetStringAsData<T>(File.ReadAllText(path));
	}
}
