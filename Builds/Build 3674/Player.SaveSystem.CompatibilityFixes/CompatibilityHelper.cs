using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Rivals;
using Blueprints;
using BlueprintsUI;
using Buildings;
using Buildings.Office.Headquarters;
using BusinessLayoutSets;
using Entities;
using Helpers;
using Localizor;
using Streets;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes;

public static class CompatibilityHelper
{
	private static readonly HashSet<string> RemovedItems = new HashSet<string>();

	private static readonly List<(BuildingRegistration, BuildingSizeInfo, string)> RegistrationsToSaveAsBlueprints = new List<(BuildingRegistration, BuildingSizeInfo, string)>();

	private static readonly List<string> SavedBlueprintNames = new List<string>();

	private static GameInstance GameInstance;

	private static Address CurrentPlayerAddress;

	public static void Init(GameInstance gameInstance)
	{
		GameInstance = gameInstance;
		CurrentPlayerAddress = new Address(gameInstance.CurrentStreetName, gameInstance.CurrentStreetNumber);
	}

	public static void ReturnBuildingToMarket(Address address)
	{
		BuildingRegistration buildingRegistration = GameInstance.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.Address == address);
		if (buildingRegistration == null)
		{
			return;
		}
		GameInstance.BuildingRegistrations.Remove(buildingRegistration);
		buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		buildingRegistration.AvailableForRent = true;
		RivalsHelper.FillData(GameInstance.rivalStates.Select((RivalState x) => x.rivalId).ToList());
		buildingRegistration.buildingOwnerRivalId = RivalsHelper.GetRandomRivalForBuilding(buildingRegistration.Neighborhood);
		if (!(CurrentPlayerAddress != address))
		{
			SaveGameCompatibilityFixes.forcePlayerExitForCompatibility = true;
			(GameInstance.charactersData.FirstOrDefault()?.itemInHands)?.cargoInstances.RemoveAll((CargoInstance x) => !x.paid);
		}
	}

	public static void EvictPlayerFromAddressAndUpdateOccupant(Address address, BuildingSizeInfo oldBuildingSizeInfo, string oldBuildingType)
	{
		EvictPlayerFromAddressAndUpdateOccupant(address, oldBuildingSizeInfo, oldBuildingType, saveBlueprint: true);
	}

	public static void EvictPlayerFromAddressAndUpdateOccupantWithoutBlueprint(Address address)
	{
		EvictPlayerFromAddressAndUpdateOccupant(address, null, null, saveBlueprint: false);
	}

	private static void EvictPlayerFromAddressAndUpdateOccupant(Address address, BuildingSizeInfo oldBuildingSizeInfo, string oldBuildingType, bool saveBlueprint)
	{
		EmployeeHelper.EnsureInit(GameInstance);
		KickOutPlayer(GameInstance, address);
		BuildingRegistration buildingRegistration = GameInstance.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.Address == address);
		if (buildingRegistration == null)
		{
			return;
		}
		if (buildingRegistration.RentedByPlayer)
		{
			if (saveBlueprint && !GameManager.IsDevMode)
			{
				SaveRegistrationAsBlueprint(buildingRegistration, oldBuildingSizeInfo, oldBuildingType);
			}
			BuildingHelper.SellBuilding(address, $"{address} was sold (caused by compatibility support)");
		}
		if (buildingRegistration.BuildingOwnedByPlayer)
		{
			RealEstateHelper.SellBuildingForCompat(buildingRegistration);
		}
		RemoveHqPlans(address);
		GameInstance.BuildingRegistrations.Remove(buildingRegistration);
		GameInstance.DeliveryContracts.RemoveAll((DeliveryContract x) => x.businessAddress == address);
		BuildingHelper.GetBuildingRegistration(address);
	}

	public static void KickOutPlayer(GameInstance gameInstance, Address address)
	{
		if (!SaveGameCompatibilityFixes.forcePlayerExitForCompatibility && address == CurrentPlayerAddress)
		{
			EmployeeHelper.EnsureInit(gameInstance);
			SaveGameCompatibilityFixes.forcePlayerExitForCompatibility = true;
		}
	}

	public static void ReshuffleNeighborhood(string neighbourhood)
	{
		CityGenerator.InitializeCity(neighbourhood);
		CityGenerator.DistributeBuildingsToRivals(neighbourhood);
	}

	private static void SaveRegistrationAsBlueprint(BuildingRegistration registration, BuildingSizeInfo oldBuildingSizeInfo, string oldBuildingType)
	{
		RegistrationsToSaveAsBlueprints.Add((registration.Copy(), oldBuildingSizeInfo, oldBuildingType));
	}

	public static void SaveLayoutSets()
	{
		if (RegistrationsToSaveAsBlueprints == null || RegistrationsToSaveAsBlueprints.Count == 0)
		{
			return;
		}
		foreach (var registrationsToSaveAsBlueprint in RegistrationsToSaveAsBlueprints)
		{
			BuildingRegistration item = registrationsToSaveAsBlueprint.Item1;
			BuildingSizeInfo item2 = registrationsToSaveAsBlueprint.Item2;
			string item3 = registrationsToSaveAsBlueprint.Item3;
			BusinessLayoutSet businessLayoutSet = BusinessLayoutSetHelper.CollectFromRegistration(item, item2, RemovedItems);
			businessLayoutSet.LayoutName += " (Compatibility)";
			SavedBlueprintNames.Add(businessLayoutSet.LayoutName);
			for (int num = businessLayoutSet.Items.Count; num < 0; num--)
			{
				if (RemovedItems.Contains(businessLayoutSet.Items[num].itemName))
				{
					businessLayoutSet.Items.RemoveAt(num);
				}
			}
			BlueprintMetadata obj = new BlueprintMetadata
			{
				blueprintType = BlueprintType.SavedLocally,
				buildNumber = GameVersion.GetCurrent().buildNumber,
				price = businessLayoutSet.Items.Sum((BusinessLayoutSets.Item i) => ItemHelper.GetDefaultMarketPrice(i.itemName)),
				buildingType = item3,
				buildingSizeInfo = item2,
				requiredModIds = businessLayoutSet.requiredModIds,
				otherData = GetBlueprintDataElements(businessLayoutSet)
			};
			if (BlueprintsFolderLoader.IsBlueprintNameInUse(businessLayoutSet.LayoutName))
			{
				businessLayoutSet.LayoutName += UnityEngine.Random.Range(1000, 9999);
				if (BlueprintsFolderLoader.IsBlueprintNameInUse(businessLayoutSet.LayoutName))
				{
					businessLayoutSet.LayoutName += UnityEngine.Random.Range(1000, 9999);
				}
			}
			string blueprintFolder = BlueprintsFolderLoader.GetBlueprintFolder(businessLayoutSet.LayoutName);
			Directory.CreateDirectory(blueprintFolder);
			obj.Serialize(Path.Combine(blueprintFolder, "Metadata.json"));
			businessLayoutSet.Serialize(Path.Combine(blueprintFolder, "Layout.json"));
		}
		SendBlueprintSavedMessages();
		RegistrationsToSaveAsBlueprints.Clear();
		SavedBlueprintNames.Clear();
	}

	private static List<BlueprintDataElement> GetBlueprintDataElements(BusinessLayoutSet layoutSet)
	{
		List<BlueprintDataElement> list = new List<BlueprintDataElement>();
		if (layoutSet.BusinessType != "ba:businesstype_empty")
		{
			list.Add(new BlueprintDataElement(DataElement.BusinessTypeName, layoutSet.BusinessType));
		}
		list.Add(new BlueprintDataElement(DataElement.CompatBlueprint, SaveGameManager.Current.characterId));
		return list;
	}

	private static void SendBlueprintSavedMessages()
	{
		for (int i = 0; i < RegistrationsToSaveAsBlueprints.Count; i++)
		{
			(BuildingRegistration, BuildingSizeInfo, string) tuple = RegistrationsToSaveAsBlueprints[i];
			BuildingRegistration item = tuple.Item1;
			BuildingSizeInfo item2 = tuple.Item2;
			string item3 = tuple.Item3;
			Contact contactByBuildingType = InteriorInstallationFirmHelper.GetContactByBuildingType(item3);
			Dictionary<string, string> messageData = new Dictionary<string, string>
			{
				{
					"address",
					item.Address.ToFormattedString()
				},
				{
					"buildingType",
					GetBuildingTypeName(item3)
				},
				{
					"businessName",
					SavedBlueprintNames[i]
				},
				{
					"sizeInfo",
					item2.ToString()
				}
			};
			contactByBuildingType.SendMessage(new TextMessage("ba:messagetype_dialog_interior_installation_blueprint_saved_compat", messageData));
		}
		static string GetBuildingTypeName(string buildingType)
		{
			if (buildingType == "ba:buildingtype_residential")
			{
				return "common_home".GetLocalization();
			}
			if (buildingType == "ba:buildingtype_retail")
			{
				return "common_business".GetLocalization();
			}
			return buildingType.GetLocalization();
		}
	}

	private static void RemoveHqPlans(Address address)
	{
		RemoveLogisticsManagerPlan(address);
		LogisticsManagerHelper.CancelAllDeliveriesForAddress(address);
		RemoveHeadhunterPlan(address);
		RemoveHrPlan(address);
		RemoveImportPartnership(address);
		RemoveDeliveryContract(address);
	}

	public static void RemoveLogisticsManagerPlan(Address address)
	{
		foreach (LogisticsManagerPlan item in GameInstance.logisticsManagerPlans.FindAll((LogisticsManagerPlan x) => x.headquartersAddress == address))
		{
			foreach (LogisticsManagerPlanDestination destination in item.destinations)
			{
				destination.Reset();
			}
		}
		GameInstance.logisticsManagerPlans.RemoveAll((LogisticsManagerPlan x) => x.headquartersAddress == address);
	}

	public static void RemoveHeadhunterPlan(Address address)
	{
		foreach (HeadhunterPlan item in GameInstance.headhunterPlans.FindAll((HeadhunterPlan x) => x.headquartersAddress == address))
		{
			item.CancelPendingReplacements();
		}
		GameInstance.headhunterPlans.RemoveAll((HeadhunterPlan x) => x.headquartersAddress == address);
	}

	public static void RemoveHrPlan(Address address)
	{
		List<HrManagerPlan> list = GameInstance.hrManagerPlans.FindAll((HrManagerPlan hrPlan) => hrPlan.headquartersAddress == address);
		if (list.Count == 0)
		{
			return;
		}
		HashSet<string> hashSet = new HashSet<string>();
		foreach (HrManagerPlan item in list)
		{
			hashSet.Add(item.id);
		}
		for (int num = GameInstance.headhunterPlans.Count - 1; num >= 0; num--)
		{
			HeadhunterPlan headhunterPlan = GameInstance.headhunterPlans[num];
			for (int num2 = 0; num2 < headhunterPlan.assignedHrPlans.Length; num2++)
			{
				string text = headhunterPlan.assignedHrPlans[num2];
				if (!string.IsNullOrEmpty(text) && hashSet.Contains(text))
				{
					headhunterPlan.assignedHrPlans[num2] = null;
				}
			}
		}
		foreach (HrManagerPlan item2 in list)
		{
			item2.Delete();
		}
	}

	public static void RemoveImportPartnership(Address address)
	{
		GameInstance.importPartnerships.RemoveAll((ImportPartnership x) => x.headquartersAddress == address);
	}

	public static void RemoveDeliveryContract(Address address)
	{
		GameInstance.DeliveryContracts.RemoveAll((DeliveryContract x) => x.businessAddress == address);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		RegistrationsToSaveAsBlueprints.Clear();
		RemovedItems.Clear();
		GameInstance = null;
		CurrentPlayerAddress = null;
	}
}
