using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Blueprints.Compatibility;
using BlueprintsUI;
using BusinessLayoutSets;
using Steam;
using UnityEngine;

namespace Blueprints;

public static class BlueprintsFolderLoader
{
	private static List<Blueprint> PlayerBlueprints;

	public const string MetadataFileName = "Metadata.json";

	public const string LayoutFileName = "Layout.json";

	public const string ThumbnailFileName = "Thumbnail.png";

	private static string BlueprintsPath;

	private static string[] BlueprintsFolders => Directory.GetDirectories(GetBlueprintsFolderPath());

	public static async Task Init()
	{
		BlueprintsPath = Path.Combine(Application.persistentDataPath, "Blueprints");
		SteamWorkshopBlueprint.onBlueprintUploaded = (Action<Blueprint>)Delegate.Combine(SteamWorkshopBlueprint.onBlueprintUploaded, new Action<Blueprint>(OnBlueprintUploadedToWorkshop));
		SteamWorkshopBlueprint.onBlueprintRemoved = (Action<Blueprint>)Delegate.Combine(SteamWorkshopBlueprint.onBlueprintRemoved, new Action<Blueprint>(OnBlueprintRemovedFromWorkshop));
		await GetBlueprints();
	}

	private static void OnBlueprintUploadedToWorkshop(Blueprint blueprint)
	{
		if (PlayerBlueprints == null)
		{
			PlayerBlueprints = new List<Blueprint>();
		}
		if (PlayerBlueprints.All((Blueprint bp) => bp.metadata.itemId != blueprint.metadata.itemId))
		{
			PlayerBlueprints.Add(blueprint);
		}
	}

	private static void OnBlueprintRemovedFromWorkshop(Blueprint blueprint)
	{
		if (PlayerBlueprints != null && PlayerBlueprints.Count != 0)
		{
			PlayerBlueprints.Clear();
		}
	}

	public static void ClearCache()
	{
		PlayerBlueprints?.Clear();
	}

	public static async Task<Blueprint> GetBlueprint(string blueprintName)
	{
		Blueprint blueprint = (await GetBlueprints()).FirstOrDefault((Blueprint x) => x.name == blueprintName);
		if (blueprint == null)
		{
			Debug.LogError("Couldn't find blueprint with name '" + blueprintName + "'");
			return null;
		}
		return blueprint;
	}

	public static bool IsBlueprintNameInUse(string blueprintName)
	{
		return BlueprintsFolders.Any((string x) => Path.GetFileName(x) == blueprintName);
	}

	public static string GetBlueprintFolder(string blueprintName)
	{
		if (string.IsNullOrEmpty(blueprintName))
		{
			Debug.LogError("Cannot get blueprint folder. Blueprint name is null or empty.");
			return string.Empty;
		}
		return Path.Combine(GetBlueprintsFolderPath(), blueprintName).Replace("\\", "/");
	}

	private static string GetBlueprintsFolderPath()
	{
		if (string.IsNullOrEmpty(BlueprintsPath))
		{
			BlueprintsPath = Path.Combine(Application.persistentDataPath, "Blueprints");
		}
		Directory.CreateDirectory(BlueprintsPath);
		return BlueprintsPath;
	}

	public static void UnloadPlayerBlueprints()
	{
		if (PlayerBlueprints == null)
		{
			return;
		}
		foreach (Blueprint playerBlueprint in PlayerBlueprints)
		{
			playerBlueprint.CleanCachedThumbnail();
		}
		PlayerBlueprints.Clear();
	}

	private static async Task<BlueprintMetadata> LoadBlueprintMetadata(string filePath)
	{
		return JsonUtility.FromJson<BlueprintMetadata>(await File.ReadAllTextAsync(filePath));
	}

	private static async Task<byte[]> LoadBlueprintThumbnailBytes(string filePath)
	{
		return await File.ReadAllBytesAsync(filePath);
	}

	public static async Task<BusinessLayoutSet> LoadBlueprintLayout(string filePath)
	{
		return await BusinessLayoutSetHelper.DeserializeAsync(filePath);
	}

	public static Task ApplyCompatibilityFixes(Blueprint blueprint, CompatibilityFixScope scope = CompatibilityFixScope.Both, string layoutPath = null)
	{
		if (blueprint?.metadata != null)
		{
			return BlueprintCompatibilityFixes.ApplyCompatibilityFixes(blueprint, scope, layoutPath);
		}
		return Task.CompletedTask;
	}

	public static async Task UpdateMetadata(this Blueprint blueprint)
	{
		if (blueprint?.metadata == null)
		{
			Debug.LogError("Blueprint or its metadata is null, cannot update metadata.");
			return;
		}
		BlueprintType blueprintType = blueprint.metadata.blueprintType;
		if (blueprintType != BlueprintType.DevBusinessLayout && blueprintType != BlueprintType.DevInteriorDesign)
		{
			string blueprintFolder = GetBlueprintFolder(blueprint.name);
			if (Directory.Exists(blueprintFolder))
			{
				string path = Path.Combine(blueprintFolder, "Metadata.json");
				await blueprint.metadata.Serialize(path);
			}
			else
			{
				Debug.LogError("Blueprint folder '" + blueprintFolder + "' does not exist.");
			}
		}
	}

	public static bool IsWorkshopBlueprintInLibrary(ulong itemId)
	{
		if (PlayerBlueprints == null || PlayerBlueprints.Count == 0)
		{
			GetBlueprints().GetAwaiter();
			if (PlayerBlueprints == null || PlayerBlueprints.Count == 0)
			{
				return false;
			}
		}
		foreach (Blueprint playerBlueprint in PlayerBlueprints)
		{
			if (playerBlueprint.metadata.itemId == itemId && playerBlueprint.metadata.itemId != 0L)
			{
				return true;
			}
		}
		return false;
	}

	public static async Task<List<Blueprint>> GetBlueprints()
	{
		List<Blueprint> playerBlueprints = PlayerBlueprints;
		if (playerBlueprints != null && playerBlueprints.Count > 0)
		{
			return PlayerBlueprints;
		}
		string[] blueprintsFolders = BlueprintsFolders;
		List<Task<Blueprint>> list = new List<Task<Blueprint>>(blueprintsFolders.Length);
		string[] array = blueprintsFolders;
		foreach (string folderPath in array)
		{
			list.Add(LoadLightBlueprintFromFolder(folderPath));
		}
		PlayerBlueprints = (await Task.WhenAll(list)).Where((Blueprint bp) => bp != null).ToList();
		return PlayerBlueprints;
	}

	private static async Task<Blueprint> LoadLightBlueprintFromFolder(string folderPath)
	{
		_ = 1;
		try
		{
			string text = Path.Combine(folderPath, "Metadata.json");
			if (!File.Exists(text))
			{
				return null;
			}
			BlueprintMetadata blueprintMetadata = await LoadBlueprintMetadata(text);
			if (blueprintMetadata == null)
			{
				return null;
			}
			int buildNumber = GameVersion.GetCurrent().buildNumber;
			if (blueprintMetadata.buildNumber != 0 && blueprintMetadata.buildNumber > buildNumber)
			{
				return null;
			}
			Blueprint blueprint = new Blueprint
			{
				name = Path.GetFileName(folderPath),
				metadata = blueprintMetadata
			};
			await ApplyCompatibilityFixes(blueprint, CompatibilityFixScope.Metadata);
			return blueprint;
		}
		catch (Exception arg)
		{
			Debug.LogError($"Light-load of blueprint at '{folderPath}' failed: {arg}");
			return null;
		}
	}

	public static async Task<byte[]> LoadBlueprintThumbnailBytes(Blueprint blueprint)
	{
		if (blueprint.metadata.blueprintType == BlueprintType.Workshop)
		{
			return null;
		}
		BlueprintType blueprintType = blueprint.metadata.blueprintType;
		string text = (((uint)(blueprintType - 4) > 1u) ? Path.Combine(GetBlueprintFolder(blueprint.name), "Thumbnail.png") : GetDevBlueprintThumbnailPath(blueprint));
		string thumbnailFilePath = text;
		if (!File.Exists(thumbnailFilePath))
		{
			return null;
		}
		try
		{
			return await File.ReadAllBytesAsync(thumbnailFilePath);
		}
		catch (Exception arg)
		{
			Debug.LogError($"Failed to load thumbnail bytes from '{thumbnailFilePath}': {arg}");
			return null;
		}
	}

	public static void RemoveLocalBlueprint(Blueprint blueprint)
	{
		PlayerBlueprints?.RemoveAll((Blueprint bp) => bp.name == blueprint.name);
		string blueprintFolder = GetBlueprintFolder(blueprint.name);
		if (Directory.Exists(blueprintFolder))
		{
			try
			{
				Directory.Delete(blueprintFolder, recursive: true);
				Debug.Log("Successfully removed blueprint folder: " + blueprintFolder);
				return;
			}
			catch (Exception arg)
			{
				Debug.LogError($"Failed to remove blueprint folder '{blueprintFolder}': {arg}");
				return;
			}
		}
		Debug.LogWarning("Blueprint folder does not exist: " + blueprintFolder);
	}

	private static string GetDevBlueprintThumbnailPath(Blueprint blueprint)
	{
		BlueprintType blueprintType = blueprint.metadata.blueprintType;
		string buildingType = blueprint.metadata.buildingType;
		string dataElementValue = blueprint.metadata.GetDataElementValue(DataElement.BusinessTypeName);
		string name = blueprint.name;
		switch (blueprintType)
		{
		case BlueprintType.DevBusinessLayout:
			return BlueprintBusinessLayoutsController.GetThumbnailPath(buildingType, blueprint.metadata.buildingSizeInfo, dataElementValue, name);
		case BlueprintType.DevInteriorDesign:
			return BlueprintInteriorDesignsController.GetThumbnailPath(buildingType, blueprint.metadata.buildingSizeInfo, dataElementValue, name);
		default:
			Debug.LogError("Unknown controller type for dev blueprint thumbnail path.");
			return string.Empty;
		}
	}
}
