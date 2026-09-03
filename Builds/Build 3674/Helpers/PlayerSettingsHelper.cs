using System;
using System.Collections.Generic;
using System.IO;
using JetBrains.Annotations;
using UI.DraggableWindows;
using UnityEngine;

namespace Helpers;

public static class PlayerSettingsHelper
{
	private const string FileName = "PlayerSettings.json";

	private static readonly string FilePath = Path.Combine(Application.persistentDataPath, "PlayerSettings.json");

	private static PlayerSettingsData DataInstance;

	private static bool IsLoaded;

	private static DateTime LastWriteTimeUtc;

	private static bool PendingSave;

	[CanBeNull]
	public static PlayerSettingsData Data
	{
		get
		{
			EnsureLoaded();
			return DataInstance;
		}
	}

	private static void FlushPendingSave()
	{
		if (PendingSave)
		{
			SavePlayerSettings();
			Application.onBeforeRender -= FlushPendingSave;
			PendingSave = false;
		}
	}

	private static void EnsureLoaded()
	{
		try
		{
			if (IsLoaded && (!File.Exists(FilePath) || File.GetLastWriteTimeUtc(FilePath) <= LastWriteTimeUtc))
			{
				return;
			}
			if (File.Exists(FilePath))
			{
				string text = File.ReadAllText(FilePath);
				if (string.IsNullOrWhiteSpace(text))
				{
					DataInstance = new PlayerSettingsData();
					LastWriteTimeUtc = DateTime.MinValue;
				}
				else
				{
					DataInstance = JsonUtility.FromJson<PlayerSettingsData>(text);
					LastWriteTimeUtc = File.GetLastWriteTimeUtc(FilePath);
				}
			}
			else
			{
				DataInstance = new PlayerSettingsData();
				LastWriteTimeUtc = DateTime.MinValue;
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
			DataInstance = new PlayerSettingsData();
			LastWriteTimeUtc = DateTime.MinValue;
		}
		finally
		{
			IsLoaded = true;
		}
	}

	private static void SavePlayerSettings()
	{
		string contents = JsonUtility.ToJson(DataInstance, prettyPrint: true);
		File.WriteAllText(FilePath, contents);
		LastWriteTimeUtc = File.GetLastWriteTimeUtc(FilePath);
	}

	public static List<Color> GetPlayerColors()
	{
		List<Color> list = new List<Color>();
		PlayerSettingsData data = Data;
		if (data == null)
		{
			return list;
		}
		foreach (string playerColorHex in data.playerColorHexes)
		{
			if (ColorUtility.TryParseHtmlString(playerColorHex, out var color))
			{
				list.Add(color);
			}
		}
		list.Reverse();
		return list;
	}

	public static void AddPlayerColor(Color color)
	{
		color.a = 1f;
		string item = color.ToHex();
		PlayerSettingsData data = Data;
		if (data == null || !data.playerColorHexes.Contains(item))
		{
			Data?.playerColorHexes.Add(item);
			if (!PendingSave)
			{
				PendingSave = true;
				Application.onBeforeRender += FlushPendingSave;
			}
		}
	}

	public static void RemovePlayerColor(Color color)
	{
		string item = color.ToHex();
		PlayerSettingsData data = Data;
		if (data == null || data.playerColorHexes.Contains(item))
		{
			Data?.playerColorHexes.Remove(item);
			if (!PendingSave)
			{
				PendingSave = true;
				Application.onBeforeRender += FlushPendingSave;
			}
		}
	}

	public static void SaveDraggableWindows(List<DraggableWindowData> windowData)
	{
		PlayerSettingsData data = Data;
		if (data != null)
		{
			data.draggableWindows = windowData;
			if (!PendingSave)
			{
				PendingSave = true;
				Application.onBeforeRender += FlushPendingSave;
			}
		}
	}

	public static void ToggleFurnitureFavorite(string itemName)
	{
		PlayerSettingsData data = Data;
		if (data != null)
		{
			if (data.idFurnitureFavorites.Contains(itemName))
			{
				data.idFurnitureFavorites.Remove(itemName);
			}
			else
			{
				data.idFurnitureFavorites.Add(itemName);
			}
			SavePlayerSettings();
		}
	}

	public static HashSet<string> GetIDFurnitureFavorites()
	{
		PlayerSettingsData data = Data;
		if (data != null)
		{
			return new HashSet<string>(data.idFurnitureFavorites);
		}
		return new HashSet<string>();
	}

	public static bool IsFurnitureFavorite(string itemName)
	{
		return Data?.idFurnitureFavorites.Contains(itemName) ?? false;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		DataInstance = null;
		IsLoaded = false;
		LastWriteTimeUtc = DateTime.MinValue;
		PendingSave = false;
		Application.onBeforeRender -= FlushPendingSave;
	}
}
