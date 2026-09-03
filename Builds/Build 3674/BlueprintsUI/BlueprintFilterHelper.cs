using System;
using System.Collections.Generic;
using Blueprints;
using Buildings;
using Helpers;

namespace BlueprintsUI;

public static class BlueprintFilterHelper
{
	public static BlueprintFilter GetBuildingTypeFilter()
	{
		List<BlueprintFilterOption> list = new List<BlueprintFilterOption>
		{
			new BlueprintAllFilterOption()
		};
		if (BuildingTypeHelper.BuildingTypes == null)
		{
			return new BlueprintFilter
			{
				localizationKey = "common_building_type",
				filterOptions = list
			};
		}
		foreach (KeyValuePair<string, BuildingTypeData> buildingType in BuildingTypeHelper.BuildingTypes)
		{
			if (!(buildingType.Key == "ba:buildingtype_special") || GameManager.IsDevMode)
			{
				list.Add(new BlueprintBuildingTypeFilterOption(buildingType.Key));
			}
		}
		return new BlueprintFilter
		{
			localizationKey = "common_building_type",
			filterOptions = list
		};
	}

	public static BlueprintFilter GetBuildingSizeFilter()
	{
		List<BlueprintFilterOption> list = new List<BlueprintFilterOption>
		{
			new BlueprintAllFilterOption()
		};
		foreach (BuildingSizeData allBuildingSize in BuildingSizeHelper.GetAllBuildingSizes())
		{
			BuildingVersion[] buildingVersions = allBuildingSize.buildingVersions;
			foreach (BuildingVersion buildingVersion in buildingVersions)
			{
				if (GameManager.IsDevMode || !buildingVersion.specialBuildingOnly)
				{
					list.Add(new BlueprintBuildingSizeFilterOption(new BuildingSizeInfo(allBuildingSize.buildingSize, buildingVersion.number)));
				}
			}
		}
		return new BlueprintFilter
		{
			localizationKey = "common_building_size",
			filterOptions = list
		};
	}

	public static BlueprintFilter GetBusinessTypeFilter()
	{
		List<BlueprintFilterOption> list = new List<BlueprintFilterOption>(1)
		{
			new BlueprintAllFilterOption()
		};
		if (GameManager.IsDevMode)
		{
			foreach (string businessTypeName in BusinessTypeHelper.BusinessTypeNames)
			{
				list.Add(new BlueprintBusinessTypeFilterOption(businessTypeName));
			}
		}
		else
		{
			foreach (BusinessType allPlayerAvailableBusiness in BusinessTypeHelper.GetAllPlayerAvailableBusinesses())
			{
				list.Add(new BlueprintBusinessTypeFilterOption(allPlayerAvailableBusiness.businessTypeName));
			}
		}
		list.Sort(delegate(BlueprintFilterOption a, BlueprintFilterOption b)
		{
			bool flag = a is BlueprintAllFilterOption;
			bool flag2 = b is BlueprintAllFilterOption;
			if (flag & flag2)
			{
				return 0;
			}
			if (flag)
			{
				return -1;
			}
			return flag2 ? 1 : string.Compare(a.text, b.text, StringComparison.OrdinalIgnoreCase);
		});
		return new BlueprintFilter
		{
			localizationKey = "blueprintdata_businesstypename",
			filterOptions = list
		};
	}

	public static BlueprintFilter GetBuildVersionFilter()
	{
		List<BlueprintFilterOption> list = new List<BlueprintFilterOption>
		{
			new BlueprintAllFilterOption()
		};
		GameVersion current = GameVersion.GetCurrent();
		if (current?.latestBuildNumberForVersion == null)
		{
			return new BlueprintFilter
			{
				localizationKey = "common_early_access",
				filterOptions = list
			};
		}
		List<GameVersion.VersionInfo> list2 = new List<GameVersion.VersionInfo>(current.latestBuildNumberForVersion);
		list2.Sort((GameVersion.VersionInfo leftVersion, GameVersion.VersionInfo rightVersion) => rightVersion.latestBuildNumber.CompareTo(leftVersion.latestBuildNumber));
		string value = string.Empty;
		foreach (GameVersion.VersionInfo item in list2)
		{
			if (item.latestBuildNumber >= 2463)
			{
				list.Add(new BlueprintBuildVersionFilterOption(item.version));
				value = item.version;
			}
		}
		if (!string.IsNullOrEmpty(value))
		{
			string versionByBuildNumber = GameVersion.GetVersionByBuildNumber(2462, useBlueprintPreVersionSystem: true);
			list.Add(new BlueprintBuildVersionFilterOption(versionByBuildNumber));
		}
		return new BlueprintFilter
		{
			localizationKey = "common_early_access",
			filterOptions = list
		};
	}
}
