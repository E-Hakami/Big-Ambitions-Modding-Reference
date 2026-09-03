using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BigAmbitions.SaveSystem;
using BlueprintsUI;
using UnityEngine;

namespace Blueprints;

public class BlueprintBusinessLayoutsController : BlueprintDevController
{
	private const string DefaultBlueprintName = "default";

	private static string LayoutsPath => Application.streamingAssetsPath + "/BusinessLayouts";

	private static string ThumbnailsPath => Directory.GetParent(Application.dataPath)?.FullName + "/EditorFiles/Blueprints/Thumbnails/BusinessLayouts";

	public static string GetThumbnailPath(string buildingType, BuildingSizeInfo sizeInfo, string businessType, string blueprintName)
	{
		return Path.Combine(ThumbnailsPath, buildingType.GetIdWithoutType(), sizeInfo.ToString(), businessType.GetIdWithoutType(), blueprintName + ".jpg");
	}

	public static string GetLayoutPath(BuildingSizeInfo sizeInfo, string businessType, string blueprintName)
	{
		return Path.Combine(LayoutsPath, (blueprintName == "default") ? ("ba:businesstype_empty".GetIdWithoutType() + "/" + sizeInfo.ToString() + "/" + blueprintName + ".json") : (businessType.GetIdWithoutType() + "/" + sizeInfo.ToString() + "/" + blueprintName + ".json"));
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
		List<BusinessLayoutSet> obj = await BlueprintDevController.LoadLayoutSets(LayoutsPath);
		List<Blueprint> list = new List<Blueprint>();
		foreach (BusinessLayoutSet item2 in obj)
		{
			Blueprint item = new Blueprint
			{
				name = item2.LayoutName,
				author = "Amazing Hovgaard Team!",
				metadata = new BlueprintMetadata
				{
					blueprintType = BlueprintType.DevBusinessLayout,
					buildingType = BlueprintDevController.GetBuildingType(item2),
					buildingSizeInfo = new BuildingSizeInfo(item2.BuildingSize, item2.BuildingVersion),
					requiredModIds = item2.requiredModIds,
					otherData = new List<BlueprintDataElement>
					{
						new BlueprintDataElement(DataElement.BusinessTypeName, item2.BusinessType)
					}
				}
			};
			list.Add(item);
		}
		return list;
	}
}
