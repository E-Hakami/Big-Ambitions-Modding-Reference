using System;
using System.Collections.Generic;
using BigAmbitions.Tags;
using Buildings;
using HGAttributes;
using UnityEngine;

namespace Helpers;

public static class BusinessTypeHelper
{
	public const string AddressableLabel = "BusinessTypes";

	private static readonly Dictionary<string, BusinessType> BusinessTypes = new Dictionary<string, BusinessType>();

	private static readonly Dictionary<string, BusinessType> ModBusinessTypes = new Dictionary<string, BusinessType>(StringComparer.Ordinal);

	private static readonly List<BusinessType> PlayerAvailableBusinessTypes = new List<BusinessType>();

	[AutocompleteProvider("BusinessTypes")]
	public static IEnumerable<string> BusinessTypeNames => BusinessTypes.Keys;

	public static void OnBusinessTypesLoaded(IList<BusinessType> businessTypes)
	{
		BusinessTypes.Clear();
		BusinessTypes.EnsureCapacity(businessTypes.Count + ModBusinessTypes.Count);
		foreach (BusinessType businessType in businessTypes)
		{
			AddBusinessTypeInternal(businessType);
		}
		List<BusinessType> list = new List<BusinessType>();
		foreach (BusinessType value in ModBusinessTypes.Values)
		{
			if (BusinessTypes.ContainsKey(value.businessTypeName))
			{
				Debug.LogWarning("BusinessTypeHelper.OnBusinessTypesLoaded: mod business type '" + value.businessTypeName + "' collides with a vanilla business type; vanilla wins and the mod business type is skipped for this load.");
				list.Add(value);
			}
			else
			{
				AddBusinessTypeInternal(value);
			}
		}
		foreach (BusinessType item in list)
		{
			ModBusinessTypes.Remove(item.businessTypeName);
			BuildingTypeHelper.UnregisterModBusinessTypeAvailability(item.suitableBuildingType, item.businessTypeName);
		}
		PlayerAvailableBusinessTypes.Clear();
	}

	internal static bool RegisterModBusinessType(BusinessType businessType)
	{
		if (businessType == null || string.IsNullOrEmpty(businessType.businessTypeName))
		{
			Debug.LogError("BusinessTypeHelper.RegisterModBusinessType: business type or businessTypeName is null/empty.");
			return false;
		}
		if (BusinessTypes.TryGetValue(businessType.businessTypeName, out var value) && !ModBusinessTypes.ContainsKey(businessType.businessTypeName))
		{
			Debug.LogWarning("BusinessTypeHelper.RegisterModBusinessType: business type '" + businessType.businessTypeName + "' is already registered by the base game; ignoring mod registration.");
			return false;
		}
		if (value != null && value != businessType)
		{
			Debug.LogWarning("BusinessTypeHelper.RegisterModBusinessType: business type '" + businessType.businessTypeName + "' is already registered; ignoring new registration.");
			return false;
		}
		businessType.BuildTagCache();
		ModBusinessTypes[businessType.businessTypeName] = businessType;
		AddBusinessTypeInternal(businessType);
		PlayerAvailableBusinessTypes.Clear();
		return true;
	}

	internal static bool UnregisterModBusinessType(string businessTypeName)
	{
		if (string.IsNullOrEmpty(businessTypeName))
		{
			return false;
		}
		BusinessType valueOrDefault = ModBusinessTypes.GetValueOrDefault(businessTypeName);
		if (!ModBusinessTypes.Remove(businessTypeName))
		{
			return false;
		}
		if (BusinessTypes.TryGetValue(businessTypeName, out var value) && value == valueOrDefault)
		{
			BusinessTypes.Remove(businessTypeName);
		}
		PlayerAvailableBusinessTypes.Clear();
		return true;
	}

	internal static BusinessType GetModBusinessType(string businessTypeName)
	{
		if (!string.IsNullOrEmpty(businessTypeName))
		{
			return ModBusinessTypes.GetValueOrDefault(businessTypeName);
		}
		return null;
	}

	private static void AddBusinessTypeInternal(BusinessType businessType)
	{
		if (!(businessType == null) && !string.IsNullOrEmpty(businessType.businessTypeName))
		{
			BusinessTypes.TryAdd(businessType.businessTypeName, businessType);
		}
	}

	public static BusinessType GetData(string businessTypeName)
	{
		if (!string.IsNullOrEmpty(businessTypeName))
		{
			return BusinessTypes.GetValueOrDefault(businessTypeName);
		}
		return null;
	}

	public static BusinessType GetData(BuildingRegistration registration)
	{
		if (registration != null)
		{
			return GetData(registration.businessTypeName);
		}
		return null;
	}

	public static BusinessType GetData(Building building)
	{
		return GetData(building.GetRegistration()?.businessTypeName);
	}

	public static BusinessType GetData(BusinessLayoutSet set)
	{
		return GetData(set.BusinessType);
	}

	public static BusinessType GetData(BizManBusiness business)
	{
		return GetData(business.buildingRegistration.businessTypeName);
	}

	public static string GetSuitableBuildingType(string businessTypeName)
	{
		return GetData(businessTypeName)?.suitableBuildingType ?? "ba:buildingtype_special";
	}

	public static HashSet<string> GetPrimaryProducts(string businessTypeName)
	{
		return GetData(businessTypeName)?.GetPrimaryProducts() ?? new HashSet<string>();
	}

	public static HashSet<string> GetAllProducts(string businessTypeName)
	{
		return GetData(businessTypeName)?.GetAllProducts() ?? new HashSet<string>();
	}

	public static List<string> GetPrimaryRetailProducts(string businessTypeName)
	{
		return GetData(businessTypeName)?.GetPrimaryRetailProducts() ?? new List<string>();
	}

	public static string GetEntranceFeeNameForBusinessType(BusinessType businessType)
	{
		if (businessType == null)
		{
			return null;
		}
		if (!businessType.hasWeekendOnlyEntranceFee || !TimeHelper.IsWeekend)
		{
			return businessType.defaultEntranceFee;
		}
		return businessType.weekendOnlyEntranceFee;
	}

	public static IEnumerable<BusinessType> GetAllPlayerAvailableBusinesses()
	{
		if (PlayerAvailableBusinessTypes.Count > 0)
		{
			return PlayerAvailableBusinessTypes;
		}
		foreach (KeyValuePair<string, BusinessType> businessType in BusinessTypes)
		{
			if (businessType.Value.HasTag(TagRef.Businesstag.allowplayercreation))
			{
				PlayerAvailableBusinessTypes.Add(businessType.Value);
			}
		}
		return PlayerAvailableBusinessTypes;
	}

	public static IEnumerable<BusinessType> GetAllPlayerAvailableBusinesses(string buildingType)
	{
		List<BusinessType> list = new List<BusinessType>();
		foreach (KeyValuePair<string, BusinessType> businessType in BusinessTypes)
		{
			if (businessType.Value.HasTag(TagRef.Businesstag.allowplayercreation) && businessType.Value.suitableBuildingType == buildingType)
			{
				list.Add(businessType.Value);
			}
		}
		return list;
	}

	public static List<BusinessType> GetSpecialBusinesses()
	{
		List<BusinessType> list = new List<BusinessType>();
		foreach (KeyValuePair<string, BusinessType> businessType in BusinessTypes)
		{
			if (!businessType.Value.HasTag(TagRef.Businesstag.allowplayercreation) && !businessType.Value.HasTag(TagRef.Businesstag.hideincitymapfilters))
			{
				list.Add(businessType.Value);
			}
		}
		return list;
	}

	public static List<string> GetPossibleBusinessTypes(List<string> skills)
	{
		List<string> list = new List<string>();
		foreach (BusinessType value in BusinessTypes.Values)
		{
			string[] employeePrimarySkills = value.employeePrimarySkills;
			foreach (string item in employeePrimarySkills)
			{
				if (list.Contains(value.businessTypeName))
				{
					break;
				}
				if (skills.Contains(item))
				{
					list.Add(value.businessTypeName);
					break;
				}
			}
		}
		return list;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		BusinessTypes.Clear();
		ModBusinessTypes.Clear();
		PlayerAvailableBusinessTypes.Clear();
	}
}
