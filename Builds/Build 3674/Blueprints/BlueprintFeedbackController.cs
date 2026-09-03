using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Blueprints.Compatibility;
using BlueprintsUI;
using Buildings;
using UnityEngine;

namespace Blueprints;

public class BlueprintFeedbackController : BlueprintDevController
{
	private static string LayoutsPath => Application.persistentDataPath + "/EditorBlueprints";

	public static string GetLayoutPath(string blueprintName)
	{
		return LayoutsPath + "/" + blueprintName + "/Layout.json";
	}

	public override void ClearCache()
	{
		base.ClearCache();
		blueprintsCache.Clear();
	}

	public override async Task<List<Blueprint>> GetBlueprints(int page, BlueprintSortInfo sortInfo)
	{
		if (!GameManager.IsDevMode)
		{
			return new List<Blueprint>();
		}
		if (sortInfo.HasChanged)
		{
			ClearCache();
			sortInfo.HasChanged = false;
		}
		if (blueprintsCache.Count > 0)
		{
			return blueprintsCache[page];
		}
		if (devBlueprints.Count == 0)
		{
			devBlueprints = await LoadBlueprints();
		}
		List<Blueprint> blueprints = new List<Blueprint>(devBlueprints);
		BlueprintDevController.FilterAndSortBlueprints(ref blueprints, sortInfo);
		HandlePaging(blueprints);
		return (blueprintsCache.Count >= page) ? blueprintsCache[page] : new List<Blueprint>();
	}

	private static async Task<List<Blueprint>> LoadBlueprints()
	{
		List<(BusinessLayoutSet, string)> list = await LoadLayoutSetsAndFolderNames(LayoutsPath);
		List<Blueprint> blueprints = new List<Blueprint>();
		foreach (var item3 in list)
		{
			BusinessLayoutSet item = item3.Item1;
			string item2 = item3.Item2;
			Blueprint blueprint = new Blueprint
			{
				name = item2,
				author = "Our awesome community!",
				metadata = new BlueprintMetadata
				{
					blueprintType = BlueprintType.FeedbackSystem,
					buildingType = BuildingSizeHelper.GetBuildingTypeBySizeInfo(new BuildingSizeInfo(item.BuildingSize, item.BuildingVersion)),
					buildingSizeInfo = new BuildingSizeInfo(item.BuildingSize, item.BuildingVersion),
					requiredModIds = item.requiredModIds,
					otherData = new List<BlueprintDataElement>
					{
						new BlueprintDataElement(DataElement.BusinessTypeName, item.BusinessType)
					}
				}
			};
			await BlueprintCompatibilityFixes.ApplyCompatibilityFixes(blueprint);
			blueprints.Add(blueprint);
		}
		return blueprints;
	}

	private static async Task<List<(BusinessLayoutSet, string)>> LoadLayoutSetsAndFolderNames(string layoutsPath)
	{
		List<(BusinessLayoutSet, string)> layoutSets = new List<(BusinessLayoutSet, string)>();
		string[] files = Directory.GetFiles(layoutsPath, "Layout.json", SearchOption.AllDirectories);
		string[] array = files;
		foreach (string filePath in array)
		{
			BusinessLayoutSet businessLayoutSet = JsonUtility.FromJson<BusinessLayoutSet>(await File.ReadAllTextAsync(filePath));
			if (businessLayoutSet != null)
			{
				layoutSets.Add((businessLayoutSet, Path.GetFileName(Path.GetDirectoryName(filePath))));
			}
		}
		return layoutSets;
	}

	public static void RemoveFromFeedbackSystem(string blueprintName)
	{
		string layoutPath = GetLayoutPath(blueprintName);
		if (!File.Exists(layoutPath))
		{
			Debug.LogWarning("Layout file not found: " + layoutPath);
			return;
		}
		try
		{
			string directoryName = Path.GetDirectoryName(layoutPath);
			if (!string.IsNullOrEmpty(directoryName) && Directory.Exists(directoryName))
			{
				Directory.Delete(directoryName, recursive: true);
				BlueprintsListUI.FeedbackController.blueprintsCache.Clear();
				BlueprintsListUI.FeedbackController.devBlueprints.Clear();
				Debug.Log("Layout file successfully removed: " + layoutPath);
			}
		}
		catch (IOException ex)
		{
			Debug.LogError("Failed to remove blueprint: " + ex.Message);
		}
	}
}
