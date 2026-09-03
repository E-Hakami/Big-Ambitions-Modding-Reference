using System.Collections.Generic;
using Blueprints;
using HGAttributes;
using UnityEngine;

namespace Buildings;

public static class BuildingSizeHelper
{
	public const string AddressableLabel = "BuildingSizes";

	private static readonly Dictionary<string, BuildingSizeData> BuildingSizes = new Dictionary<string, BuildingSizeData>();

	private static readonly Dictionary<string, List<BuildingSizeInfo>> BuildingVersionsByBuildingType = new Dictionary<string, List<BuildingSizeInfo>>();

	[AutocompleteProvider("BuildingSizes")]
	private static IEnumerable<string> BuildingSizeNames => BuildingSizes.Keys;

	public static void OnBuildingSizesLoaded(IList<BuildingSizeData> buildingSizes)
	{
		BuildingSizes.Clear();
		BuildingVersionsByBuildingType.Clear();
		BuildingSizes.EnsureCapacity(buildingSizes.Count);
		foreach (BuildingSizeData buildingSize in buildingSizes)
		{
			BuildingSizes.Add(buildingSize.buildingSize, buildingSize);
			BuildingVersion[] buildingVersions = buildingSize.buildingVersions;
			foreach (BuildingVersion buildingVersion in buildingVersions)
			{
				foreach (string supportedBuildingType in buildingVersion.supportedBuildingTypes)
				{
					if (!BuildingVersionsByBuildingType.TryGetValue(supportedBuildingType, out var value))
					{
						value = new List<BuildingSizeInfo>();
						BuildingVersionsByBuildingType[supportedBuildingType] = value;
					}
					value.Add(new BuildingSizeInfo(buildingSize.buildingSize, buildingVersion.number));
				}
			}
		}
	}

	public static BuildingSizeData GetData(BuildingRegistration registration)
	{
		return GetData(registration?.BuildingCached?.BuildingSize);
	}

	public static BuildingSizeData GetData(Building building)
	{
		return GetData(building?.BuildingSize);
	}

	public static BuildingSizeData GetData(string buildingSize)
	{
		if (!string.IsNullOrEmpty(buildingSize))
		{
			Dictionary<string, BuildingSizeData> buildingSizes = BuildingSizes;
			if (buildingSizes == null || buildingSizes.Count != 0)
			{
				return BuildingSizes[buildingSize];
			}
		}
		return null;
	}

	public static float GetBuildingWallHeight(string buildingSize, int heightIndex = -1)
	{
		float[] wallHeights = GetData(buildingSize).wallHeights;
		if (heightIndex != -1)
		{
			return wallHeights[heightIndex];
		}
		return wallHeights[0];
	}

	public static float GetBuildingRoofPosition(Building building, int heightIndex = -1)
	{
		float[] wallHeights = GetData(building).wallHeights;
		if (heightIndex >= 0)
		{
			return wallHeights[heightIndex];
		}
		if (wallHeights.Length == 1)
		{
			return wallHeights[0];
		}
		MultipleHeightsBuildingController multipleHeightsBuildingController = InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController;
		if (!(multipleHeightsBuildingController != null))
		{
			return wallHeights[0];
		}
		return wallHeights[multipleHeightsBuildingController.currentHeightIndex];
	}

	public static float GetBuildingRoofPosition(string buildingSize, int heightIndex)
	{
		return GetData(buildingSize).wallHeights[heightIndex];
	}

	public static List<BuildingSizeData> GetAllBuildingSizes()
	{
		List<BuildingSizeData> list = new List<BuildingSizeData>();
		foreach (BuildingSizeData value in BuildingSizes.Values)
		{
			list.Add(value);
		}
		return list;
	}

	public static string GetBuildingTypeBySizeInfo(BuildingSizeInfo info, string defaultType = null)
	{
		foreach (KeyValuePair<string, List<BuildingSizeInfo>> item in BuildingVersionsByBuildingType)
		{
			item.Deconstruct(out var key, out var value);
			string text = key;
			foreach (BuildingSizeInfo item2 in value)
			{
				if (item2.buildingSize == info.buildingSize && item2.buildingVersion == info.buildingVersion)
				{
					key = text;
					return key;
				}
			}
		}
		return defaultType ?? "ba:buildingtype_special";
	}

	public static List<BuildingSizeInfo> GetBuildingVersionsByBuildingType(string buildingType)
	{
		if (!BuildingVersionsByBuildingType.TryGetValue(buildingType, out var value))
		{
			return new List<BuildingSizeInfo>();
		}
		return value;
	}

	public static List<BuildingSizeInfo> GetBuildingSizesForBuildingType(string buildingType)
	{
		return BuildingVersionsByBuildingType[buildingType];
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		BuildingSizes.Clear();
		BuildingVersionsByBuildingType.Clear();
	}
}
