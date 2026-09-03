using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BigAmbitions.Items;
using BigAmbitions.SaveSystem;
using BigAmbitions.Tags;
using Blueprints;
using BusinessLayoutSets;
using Entities;
using Helpers;
using IngameDebugConsole;
using Streets;
using UI;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;

namespace Buildings;

public static class InteriorInstallationFirmHelper
{
	private const float FeeCostPerSqm = 586f;

	private const string InteriorDesignsFolderName = "InteriorDesigns";

	private const string InteriorDesignExtension = ".json";

	private static readonly List<Building> InstallationFirmBuildings = new List<Building>();

	public static string[] GetInteriorDesignsNamesForBuilding(string buildingType, BuildingSizeInfo sizeInfo, string businessTypeName = "ba:businesstype_empty")
	{
		string designsListPath = GetDesignsListPath(buildingType, sizeInfo, businessTypeName);
		if (!Directory.Exists(designsListPath))
		{
			return Array.Empty<string>();
		}
		string[] files = Directory.GetFiles(designsListPath, "*.json", SearchOption.TopDirectoryOnly);
		string[] array = new string[files.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = Path.GetFileNameWithoutExtension(files[i]);
		}
		return array;
	}

	public static BusinessLayoutSet GetInteriorDesignLayout(string designName, string buildingType, BuildingSizeInfo sizeInfo, string businessTypeName = "ba:businesstype_empty")
	{
		return BusinessLayoutSetHelper.Deserialize(Path.Combine(GetDesignsListPath(buildingType, sizeInfo, businessTypeName), designName + ".json"));
	}

	private static string GetDesignsListPath(string buildingType, BuildingSizeInfo sizeInfo, string businessTypeName = "ba:businesstype_empty")
	{
		string text = Path.Combine(Application.streamingAssetsPath, "InteriorDesigns", buildingType.GetIdWithoutType(), sizeInfo.ToString());
		if (businessTypeName != "ba:businesstype_empty")
		{
			text = Path.Combine(text, businessTypeName.GetIdWithoutType());
		}
		return text;
	}

	public static string[] GetBusinessTypesForBuilding(string buildingType, BuildingSizeInfo sizeInfo)
	{
		string path = Path.Combine(Application.streamingAssetsPath, "InteriorDesigns", buildingType.GetIdWithoutType(), sizeInfo.ToString());
		if (!Directory.Exists(path))
		{
			return Array.Empty<string>();
		}
		string[] directories = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
		string[] array = new string[directories.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = "ba:businesstype_" + Path.GetFileName(directories[i]).ToLowerInvariant();
		}
		return array;
	}

	public static async Task<List<string>> GetBlueprintNames(string buildingType, BuildingSizeInfo sizeInfo)
	{
		List<string> result = new List<string>();
		foreach (Blueprint item in await BlueprintsFolderLoader.GetBlueprints())
		{
			if (!(item.metadata.buildingType != buildingType) && !(item.metadata.buildingSizeInfo.buildingSize != sizeInfo.buildingSize) && item.metadata.buildingSizeInfo.buildingVersion == sizeInfo.buildingVersion)
			{
				result.Add(item.name);
			}
		}
		return result;
	}

	public static async Task<List<string>> GetBlueprintNames(string buildingType, BuildingSizeInfo sizeInfo, string businessType)
	{
		List<string> result = new List<string>();
		foreach (Blueprint item in await BlueprintsFolderLoader.GetBlueprints())
		{
			if (item.metadata.buildingType == buildingType && item.metadata.buildingSizeInfo.buildingSize == sizeInfo.buildingSize && item.metadata.buildingSizeInfo.buildingVersion == sizeInfo.buildingVersion && (string.IsNullOrEmpty(businessType) || businessType == "ba:businesstype_empty" || item.metadata.GetDataElementValue(DataElement.BusinessTypeName) == businessType))
			{
				result.Add(item.name);
			}
		}
		return result;
	}

	[ConsoleMethod("SaveDesign", "Save the current layout as a design for an interior installation firm", new string[] { })]
	public static void SaveDesign(string designName)
	{
		string businessTypeName = (BuildingTypeHelper.GetData(InstanceBehavior<BuildingManager>.Instance.building).HasTag(TagRef.Buildingtypetag.containsnobusiness) ? "ba:businesstype_empty" : InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName);
		SaveDesign(designName, businessTypeName);
	}

	[ConsoleMethod("SaveDesign", "Save the current layout as a design for an interior installation firm", new string[] { }, AutoCompleteMap = new string[] { "businessTypeName=BusinessTypes" })]
	public static async void SaveDesign(string designName, string businessTypeName)
	{
		if (BuildingManager.IsInsideBuilding)
		{
			BusinessLayoutSet businessLayoutSet = BusinessLayoutSetHelper.Collect();
			businessLayoutSet.BusinessType = businessTypeName;
			businessLayoutSet.LayoutName = designName;
			string designsListPath = GetDesignsListPath(InstanceBehavior<BuildingManager>.Instance.building.BuildingType, new BuildingSizeInfo(InstanceBehavior<BuildingManager>.Instance.building), businessTypeName);
			Directory.CreateDirectory(designsListPath);
			await businessLayoutSet.Serialize(Path.Combine(designsListPath, designName + ".json"));
		}
	}

	[ConsoleMethod("LoadDesign", "Loads an interior installation firm design", new string[] { })]
	public static void LoadDesign(string designName)
	{
		string businessTypeName = (BuildingTypeHelper.GetData(InstanceBehavior<BuildingManager>.Instance.building).HasTag(TagRef.Buildingtypetag.containsnobusiness) ? "ba:businesstype_empty" : InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName);
		LoadDesign(designName, businessTypeName);
	}

	[ConsoleMethod("LoadDesign", "Loads an interior installation firm design", new string[] { }, AutoCompleteMap = new string[] { "businessTypeName=BusinessTypes" })]
	public static void LoadDesign(string designName, string businessTypeName)
	{
		foreach (ItemInstance item in InstanceBehavior<BuildingManager>.Instance.buildingRegistration.itemInstances.Values.ToList())
		{
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RemoveItemInstanceFromBuilding(item);
		}
		BusinessLayoutSet interiorDesignLayout = GetInteriorDesignLayout(designName, InstanceBehavior<BuildingManager>.Instance.buildingRegistration.BuildingCached.BuildingType, new BuildingSizeInfo(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.BuildingCached), businessTypeName);
		BusinessLayoutSetHelper.InsertLayoutSet(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address, interiorDesignLayout);
		InstanceBehavior<BuildingManager>.Instance.buildingRegistration.GenerateInteriorDesignerLookup();
		if (InstanceBehavior<BuildingManager>.Instance.LoadBuilding())
		{
			InstanceBehavior<BuildingManager>.Instance.LoadItems();
		}
	}

	[ConsoleMethod("PreviewDesign", "Previews an interior installation firm design", new string[] { })]
	public static void PreviewDesign(string designName)
	{
		string businessTypeName = (BuildingTypeHelper.GetData(InstanceBehavior<BuildingManager>.Instance.building).HasTag(TagRef.Buildingtypetag.containsnobusiness) ? "ba:businesstype_empty" : InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName);
		PreviewDesign(designName, businessTypeName);
	}

	[ConsoleMethod("PreviewDesign", "Previews an interior installation firm design", new string[] { }, AutoCompleteMap = new string[] { "businessTypeName=BusinessTypes" })]
	public static void PreviewDesign(string designName, string businessTypeName)
	{
		BusinessLayoutSet interiorDesignLayout = GetInteriorDesignLayout(designName, InstanceBehavior<BuildingManager>.Instance.building.BuildingType, new BuildingSizeInfo(InstanceBehavior<BuildingManager>.Instance.building), businessTypeName);
		if (interiorDesignLayout != null)
		{
			InstanceBehavior<UIs>.Instance.buildingPreview.PreviewLayout(interiorDesignLayout);
		}
	}

	public static float GetInstallationFee(Address selectedAddress)
	{
		if (selectedAddress == null || selectedAddress.IsUndefined())
		{
			return 0f;
		}
		return GetInstallationFee(BuildingHelper.GetBuilding(selectedAddress).BuildingSize);
	}

	public static float GetInstallationFee(string buildingSize)
	{
		return (float)BuildingSizeHelper.GetData(buildingSize).squareMeters * 586f;
	}

	public static Contact GetContactByBuildingType(string buildingType)
	{
		if (InstallationFirmBuildings.Count == 0)
		{
			List<Building> list = new List<Building>();
			foreach (Building allBuilding in BuildingHelper.allBuildings)
			{
				if (!(allBuilding?.SpecialService == null) && !(allBuilding.SpecialService.settings as InteriorInstallationFirmSettings == null))
				{
					list.Add(allBuilding);
				}
			}
			InstallationFirmBuildings.AddRange(list);
		}
		BuildingRegistration buildingRegistration = null;
		foreach (Building installationFirmBuilding in InstallationFirmBuildings)
		{
			if (installationFirmBuilding.SpecialService?.settings is InteriorInstallationFirmSettings interiorInstallationFirmSettings)
			{
				List<string> buildingTypesThatCanInstall = interiorInstallationFirmSettings.buildingTypesThatCanInstall;
				if (buildingTypesThatCanInstall != null && buildingTypesThatCanInstall.Contains(buildingType))
				{
					buildingRegistration = installationFirmBuilding.GetRegistration();
					break;
				}
			}
		}
		if (buildingRegistration != null)
		{
			return Contact.GetContact(buildingRegistration, ContactCategoryName.FurnitureAndEquipment);
		}
		return null;
	}

	[ConsoleMethod("InstallPendingInstallationContracts", "Installs pending installation contracts", new string[] { })]
	public static void InstallPendingInstallationContracts()
	{
		for (int num = SaveGameManager.Current.interiorInstallationFirmContracts.Count - 1; num >= 0; num--)
		{
			SaveGameManager.Current.interiorInstallationFirmContracts[num].DoInstallation();
		}
	}
}
