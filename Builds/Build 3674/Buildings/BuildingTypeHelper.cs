using System;
using System.Collections.Generic;
using HGAttributes;
using UnityEngine;

namespace Buildings;

public static class BuildingTypeHelper
{
	public const string AddressableLabel = "BuildingTypes";

	public static readonly Dictionary<string, BuildingTypeData> BuildingTypes = new Dictionary<string, BuildingTypeData>();

	private static readonly Dictionary<string, HashSet<string>> ModBusinessTypesByBuildingType = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

	private static readonly Dictionary<string, HashSet<string>> AppliedModBusinessTypesByBuildingType = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);

	[AutocompleteProvider("BuildingTypes")]
	private static readonly IEnumerable<string> BuildingTypeNames = BuildingTypes.Keys;

	public static void OnBuildingTypesLoaded(IList<BuildingTypeData> buildingTypes)
	{
		BuildingTypes.Clear();
		BuildingTypes.EnsureCapacity(buildingTypes.Count);
		foreach (BuildingTypeData buildingType in buildingTypes)
		{
			BuildingTypes.Add(buildingType.buildingType, buildingType);
		}
		ApplyModBusinessTypeAvailability();
	}

	internal static bool RegisterModBusinessTypeAvailability(string buildingType, string businessTypeName)
	{
		if (string.IsNullOrEmpty(buildingType) || string.IsNullOrEmpty(businessTypeName))
		{
			Debug.LogError("BuildingTypeHelper.RegisterModBusinessTypeAvailability: buildingType or businessTypeName is null/empty.");
			return false;
		}
		if (!ModBusinessTypesByBuildingType.TryGetValue(buildingType, out var value))
		{
			value = new HashSet<string>(StringComparer.Ordinal);
			ModBusinessTypesByBuildingType[buildingType] = value;
		}
		value.Add(businessTypeName);
		ApplyModBusinessTypeAvailability(buildingType, businessTypeName);
		return true;
	}

	internal static bool UnregisterModBusinessTypeAvailability(string buildingType, string businessTypeName)
	{
		if (string.IsNullOrEmpty(buildingType) || string.IsNullOrEmpty(businessTypeName))
		{
			return false;
		}
		if (!ModBusinessTypesByBuildingType.TryGetValue(buildingType, out var value) || !value.Remove(businessTypeName))
		{
			return false;
		}
		if (value.Count == 0)
		{
			ModBusinessTypesByBuildingType.Remove(buildingType);
		}
		RemoveAppliedModBusinessTypeAvailability(buildingType, businessTypeName);
		return true;
	}

	public static BuildingTypeData GetData(BuildingRegistration registration)
	{
		return GetData(registration?.BuildingCached?.BuildingType);
	}

	public static BuildingTypeData GetData(Building building)
	{
		return GetData(building?.BuildingType);
	}

	public static BuildingTypeData GetData(string buildingType)
	{
		if (string.IsNullOrEmpty(buildingType))
		{
			return null;
		}
		if (!BuildingTypes.TryGetValue(buildingType, out var value))
		{
			return null;
		}
		return value;
	}

	public static bool TryGetData(BuildingRegistration registration, out BuildingTypeData buildingTypeData)
	{
		return TryGetData(registration?.BuildingCached?.BuildingType, out buildingTypeData);
	}

	public static bool TryGetData(Building building, out BuildingTypeData buildingTypeData)
	{
		return TryGetData(building?.BuildingType, out buildingTypeData);
	}

	public static bool TryGetData(string buildingType, out BuildingTypeData buildingTypeData)
	{
		if (string.IsNullOrEmpty(buildingType))
		{
			buildingTypeData = null;
			return false;
		}
		return BuildingTypes.TryGetValue(buildingType, out buildingTypeData);
	}

	private static void ApplyModBusinessTypeAvailability()
	{
		AppliedModBusinessTypesByBuildingType.Clear();
		foreach (KeyValuePair<string, HashSet<string>> item in ModBusinessTypesByBuildingType)
		{
			foreach (string item2 in item.Value)
			{
				ApplyModBusinessTypeAvailability(item.Key, item2);
			}
		}
	}

	private static void ApplyModBusinessTypeAvailability(string buildingType, string businessTypeName)
	{
		if (!BuildingTypes.TryGetValue(buildingType, out var value))
		{
			return;
		}
		BuildingTypeData buildingTypeData = value;
		if (buildingTypeData.availableBusinessTypes == null)
		{
			buildingTypeData.availableBusinessTypes = Array.Empty<string>();
		}
		if (Array.IndexOf(value.availableBusinessTypes, businessTypeName) < 0)
		{
			string[] array = new string[value.availableBusinessTypes.Length + 1];
			Array.Copy(value.availableBusinessTypes, array, value.availableBusinessTypes.Length);
			array[^1] = businessTypeName;
			value.availableBusinessTypes = array;
			if (!AppliedModBusinessTypesByBuildingType.TryGetValue(buildingType, out var value2))
			{
				value2 = new HashSet<string>(StringComparer.Ordinal);
				AppliedModBusinessTypesByBuildingType[buildingType] = value2;
			}
			value2.Add(businessTypeName);
		}
	}

	private static void RemoveAppliedModBusinessTypeAvailability(string buildingType, string businessTypeName)
	{
		if (!AppliedModBusinessTypesByBuildingType.TryGetValue(buildingType, out var value) || !value.Remove(businessTypeName))
		{
			return;
		}
		if (value.Count == 0)
		{
			AppliedModBusinessTypesByBuildingType.Remove(buildingType);
		}
		if (BuildingTypes.TryGetValue(buildingType, out var value2))
		{
			value2.availableBusinessTypes = Array.FindAll(value2.availableBusinessTypes ?? Array.Empty<string>(), (string x) => x != businessTypeName);
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		BuildingTypes.Clear();
		ModBusinessTypesByBuildingType.Clear();
		AppliedModBusinessTypesByBuildingType.Clear();
	}
}
