using System;
using System.IO;
using System.Linq;
using Helpers;
using Newtonsoft.Json.Linq;
using UI.DraggableWindows;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class UpdatePlayerSettings : ICompatibilityFix
{
	[Serializable]
	private class DraggableWindowDataWrapper
	{
		public DraggableWindowData[] windows;
	}

	public void Apply(GameInstance gameInstance)
	{
		foreach (SerializableColor playerColor in gameInstance.playerColors)
		{
			PlayerSettingsHelper.AddPlayerColor(playerColor);
		}
		string path = Path.Combine(Application.persistentDataPath, "DraggableWindowData.json");
		if (File.Exists(path))
		{
			DraggableWindowData[] array = ParseDraggableWindows(RemoveZValues(File.ReadAllText(path)));
			if (array != null && array.Length > 0)
			{
				PlayerSettingsHelper.SaveDraggableWindows(array.ToList());
				File.Delete(path);
			}
		}
	}

	private static string RemoveZValues(string json)
	{
		JArray jArray = JArray.Parse(json);
		foreach (JObject item in jArray)
		{
			((JObject)item["position"])?.Remove("z");
		}
		return jArray.ToString();
	}

	private static DraggableWindowData[] ParseDraggableWindows(string json)
	{
		return JsonUtility.FromJson<DraggableWindowDataWrapper>("{\"windows\":" + json + "}").windows;
	}
}
