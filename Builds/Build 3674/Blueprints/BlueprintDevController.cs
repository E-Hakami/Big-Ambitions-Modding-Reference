using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BigAmbitions.SaveSystem;
using BlueprintsUI;
using Buildings;
using Helpers;
using UnityEngine;

namespace Blueprints;

public abstract class BlueprintDevController : BlueprintController
{
	public List<Blueprint> devBlueprints = new List<Blueprint>();

	public override int GetMaxPageNumber()
	{
		return blueprintsCache.Count;
	}

	protected static void FilterAndSortBlueprints(ref List<Blueprint> blueprints, BlueprintSortInfo sortInfo)
	{
		List<string> buildingTypeActiveTags = sortInfo.GetBuildingTypeActiveTags();
		List<string> buildingSizeActiveTags = sortInfo.GetBuildingSizeActiveTags();
		List<string> businessTypeActiveTags = sortInfo.GetBusinessTypeActiveTags();
		List<Blueprint> blueprints2 = new List<Blueprint>();
		foreach (Blueprint blueprint in blueprints)
		{
			if ((buildingTypeActiveTags.Count != 0 && !buildingTypeActiveTags.Contains(blueprint.metadata.buildingType)) || (buildingSizeActiveTags.Count != 0 && !buildingSizeActiveTags.Contains(blueprint.metadata.buildingSizeInfo.ToString())))
			{
				continue;
			}
			string text = null;
			foreach (BlueprintDataElement otherDatum in blueprint.metadata.otherData)
			{
				if (otherDatum.dataElement == DataElement.BusinessTypeName)
				{
					text = otherDatum.value.GetIdWithoutType();
					break;
				}
			}
			if (businessTypeActiveTags.Count != 0 && (text == null || !businessTypeActiveTags.Contains(text)))
			{
				continue;
			}
			if (!string.IsNullOrEmpty(sortInfo.SearchQuery))
			{
				string value = sortInfo.SearchQuery.ToLowerInvariant();
				if (!blueprint.name.ToLowerInvariant().Contains(value))
				{
					continue;
				}
			}
			blueprints2.Add(blueprint);
		}
		sortInfo.SortBlueprints(ref blueprints2);
		blueprints = blueprints2;
	}

	public static async Task<List<BusinessLayoutSet>> LoadLayoutSets(string layoutsPath)
	{
		List<BusinessLayoutSet> layoutSets = new List<BusinessLayoutSet>();
		string[] files = Directory.GetFiles(layoutsPath, "*.json", SearchOption.AllDirectories);
		string[] array = files;
		for (int i = 0; i < array.Length; i++)
		{
			BusinessLayoutSet businessLayoutSet = JsonUtility.FromJson<BusinessLayoutSet>(await File.ReadAllTextAsync(array[i]));
			if (businessLayoutSet != null)
			{
				layoutSets.Add(businessLayoutSet);
			}
		}
		return layoutSets;
	}

	protected void HandlePaging(List<Blueprint> blueprints)
	{
		if (blueprints == null || blueprints.Count == 0)
		{
			return;
		}
		blueprintsCache.Clear();
		List<Blueprint> list = new List<Blueprint>();
		int num = 1;
		foreach (Blueprint blueprint in blueprints)
		{
			list.Add(blueprint);
			if (list.Count == 16)
			{
				blueprintsCache[num] = list.Copy();
				list.Clear();
				num++;
			}
		}
		if (list.Count > 0)
		{
			blueprintsCache[num] = list.Copy();
		}
	}

	protected static string GetBuildingType(BusinessLayoutSet set)
	{
		string suitableBuildingType = BusinessTypeHelper.GetSuitableBuildingType(set.BusinessType);
		if (suitableBuildingType != "ba:buildingtype_special")
		{
			return suitableBuildingType;
		}
		return BuildingSizeHelper.GetBuildingTypeBySizeInfo(new BuildingSizeInfo(set.BuildingSize, set.BuildingVersion), "ba:buildingtype_residential");
	}
}
