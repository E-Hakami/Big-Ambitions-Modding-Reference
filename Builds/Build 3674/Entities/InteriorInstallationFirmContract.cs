using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AI.Customers.CustomerEntries;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Blueprints;
using Buildings;
using BusinessLayoutSets;
using Extensions;
using Helpers;
using Streets;
using UI.Smartphone.Apps.BizMan.Schedule;
using UI.Smartphone.Apps.Contacts;

namespace Entities;

[Serializable]
public class InteriorInstallationFirmContract
{
	public Address interiorInstallationFirmAddress;

	public Address addressToDoTheInstallation;

	public string designName;

	public bool isBlueprint;

	public bool isCompatBlueprint;

	public bool hasDiscontinuedItems;

	public int dayOfInstallation;

	public string businessTypeName;

	private Contact _interiorInstallationFirmContact;

	public bool IsInstallationDay => dayOfInstallation <= SaveGameManager.Current.Day;

	public async void DoInstallation()
	{
		Blueprint blueprint = ((!isBlueprint) ? null : (await BlueprintsFolderLoader.GetBlueprint(designName)));
		Blueprint blueprint2 = blueprint;
		BuildingRegistration interiorInstallationFirmRegistration = BuildingHelper.GetBuildingRegistration(interiorInstallationFirmAddress);
		_interiorInstallationFirmContact = Contact.GetContact(interiorInstallationFirmRegistration, ContactCategoryName.FurnitureAndEquipment);
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(addressToDoTheInstallation);
		if (!buildingRegistration.RentedByPlayer)
		{
			HandleAddressNotRentedByPlayer(blueprint2);
			return;
		}
		if (IsPlayerInsideAddress())
		{
			HandlePlayerIsInsideAddress(blueprint2);
			return;
		}
		BusinessLayoutSet businessLayoutSet = await GetInteriorDesignLayout(buildingRegistration);
		if (businessLayoutSet == null)
		{
			HandleLayoutNotFound(blueprint2);
			return;
		}
		List<ItemInstance> itemsInAddress = buildingRegistration.itemInstances.Values.ToList();
		float itemsSoldAmount = GetItemsSoldAmount(itemsInAddress);
		float installationPrice = GetInstallationPrice(itemsSoldAmount, businessLayoutSet);
		if (TryPayInstallation(installationPrice, interiorInstallationFirmRegistration))
		{
			await Install(buildingRegistration, itemsInAddress, businessLayoutSet, blueprint2, itemsSoldAmount);
		}
		else
		{
			SendMessage("ba:messagetype_dialog_interior_installation_not_enough_money");
			AddCompatBlueprintDataIfNeeded(blueprint2);
		}
		SaveGameManager.Current.interiorInstallationFirmContracts.Remove(this);
		BuildingManager.RefreshHamptonsHouseBlockerCollider(addressToDoTheInstallation);
	}

	private void HandleAddressNotRentedByPlayer(Blueprint blueprint)
	{
		SendMessage("ba:messagetype_dialog_vehicle_store_cant_deliver_in_unrented_building");
		SaveGameManager.Current.interiorInstallationFirmContracts.Remove(this);
		AddCompatBlueprintDataIfNeeded(blueprint);
	}

	private bool IsPlayerInsideAddress()
	{
		if (SaveGameManager.Current.CurrentStreetName == addressToDoTheInstallation.streetName)
		{
			return SaveGameManager.Current.CurrentStreetNumber == addressToDoTheInstallation.streetNumber;
		}
		return false;
	}

	private void HandlePlayerIsInsideAddress(Blueprint blueprint)
	{
		SendMessage("ba:messagetype_dialog_interior_installation_cant_install_while_inside_building");
		AddCompatBlueprintDataIfNeeded(blueprint);
	}

	private async Task<BusinessLayoutSet> GetInteriorDesignLayout(BuildingRegistration buildingRegistration)
	{
		return (!isBlueprint) ? InteriorInstallationFirmHelper.GetInteriorDesignLayout(designName, buildingRegistration.BuildingCached.BuildingType, new BuildingSizeInfo(buildingRegistration), businessTypeName) : (await BlueprintsLayoutHelper.GetLayoutFromBlueprint(designName));
	}

	private void HandleLayoutNotFound(Blueprint blueprint)
	{
		SendMessage("ba:messagetype_dialog_interior_installation_couldnt_find_layout");
		SaveGameManager.Current.interiorInstallationFirmContracts.Remove(this);
		AddCompatBlueprintDataIfNeeded(blueprint);
		BuildingManager.RefreshHamptonsHouseBlockerCollider(addressToDoTheInstallation);
	}

	private void SendMessage(string messageType, float itemsSoldAmount = 0f)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string> { 
		{
			"address",
			addressToDoTheInstallation.ToFormattedString()
		} };
		if (itemsSoldAmount > 0f)
		{
			dictionary.Add("amount", itemsSoldAmount.ToShortCurrencyFormat());
		}
		_interiorInstallationFirmContact.SendMessage(new TextMessage(messageType, dictionary));
	}

	private void AddCompatBlueprintDataIfNeeded(Blueprint blueprint)
	{
		if (isCompatBlueprint)
		{
			blueprint?.metadata.otherData.Add(new BlueprintDataElement(DataElement.CompatBlueprint, SaveGameManager.Current.characterId));
		}
	}

	private static float GetItemsSoldAmount(List<ItemInstance> itemsInAddress)
	{
		return itemsInAddress.SumValues((ItemInstance itemInstance) => itemInstance.GetSellingPrice());
	}

	private float GetInstallationPrice(float itemsSoldAmount, BusinessLayoutSet designLayout)
	{
		return (isCompatBlueprint ? 0f : (InteriorInstallationFirmHelper.GetInstallationFee(addressToDoTheInstallation) + designLayout.Items.SumValues((BusinessLayoutSets.Item x) => ItemHelper.GetDefaultMarketPrice(x.itemName)))) - itemsSoldAmount;
	}

	private bool TryPayInstallation(float installationPrice, BuildingRegistration interiorInstallationFirmRegistration)
	{
		if (installationPrice <= 0f)
		{
			return true;
		}
		Dictionary<string, string> data = new Dictionary<string, string>
		{
			{ "businessName", interiorInstallationFirmRegistration.BusinessName },
			{
				"address",
				addressToDoTheInstallation.ToFormattedString()
			}
		};
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(addressToDoTheInstallation);
		SpecialService specialService = interiorInstallationFirmRegistration.BuildingCached.SpecialService;
		bool num = (object)specialService != null && specialService.hasTaxDeductiblePurchases && BusinessHelper.IsTaxDeductibleBusinessServiceBuilding(buildingRegistration);
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_interiorinstallation", data);
		if (num)
		{
			transactionInfo.SetTaxDeductibleName(interiorInstallationFirmRegistration.BusinessName);
		}
		return GameManager.ChangeMoneySafe(0f - installationPrice, transactionInfo);
	}

	private async Task Install(BuildingRegistration buildingRegistration, List<ItemInstance> itemsInAddress, BusinessLayoutSet designLayout, Blueprint blueprint, float itemsSoldAmount)
	{
		BizManSchedule.AbortAutoFillForBusiness(buildingRegistration);
		foreach (ItemInstance item in itemsInAddress)
		{
			buildingRegistration.RemoveItemInstanceFromBuilding(item);
		}
		BusinessLayoutSetHelper.InsertLayoutSet(addressToDoTheInstallation, designLayout);
		if (hasDiscontinuedItems)
		{
			SendMessage("ba:messagetype_dialog_interior_installation_done_compat_discontinued_items");
		}
		else if (isCompatBlueprint)
		{
			await blueprint.UpdateMetadata();
			SendMessage("ba:messagetype_dialog_interior_installation_done_compat");
		}
		else
		{
			string messageType = ((itemsSoldAmount > 0f) ? "ba:messagetype_dialog_interior_installation_done_items_sold" : "ba:messagetype_dialog_interior_installation_done");
			SendMessage(messageType, itemsSoldAmount);
		}
		GameEvent.Invoke("ba:gameevent_itemdropped");
		UpdateBusinessPostInstallation(buildingRegistration);
		if (buildingRegistration.BuildingCached.IsHamptonsHouse())
		{
			HandleHamptonsHouse();
		}
	}

	private void HandleHamptonsHouse()
	{
		CityHamptonsHouseController cityHamptonsHouseController = InstanceBehavior<CityManager>.Instance.FindCityBuildingController(addressToDoTheInstallation) as CityHamptonsHouseController;
		BuildingManager.ApplyInteriorDesign(cityHamptonsHouseController.building, cityHamptonsHouseController.hamptonsHouse.interiorElements);
		BuildingManager.RequestHamptonsItemReloadIfLoaded(addressToDoTheInstallation);
	}

	private static void UpdateBusinessPostInstallation(BuildingRegistration buildingRegistration)
	{
		UpdateSchedule(buildingRegistration);
		BusinessHelper.UpdateCustomerCapacity(buildingRegistration);
		BusinessHelper.GenerateMissingTodoTasksForBusiness(buildingRegistration);
		CustomerEntriesHelper.UpdateCustomerEntriesForPlayerBusiness(buildingRegistration, TimeHelper.GetDayOfWeek());
		UpdateSecurityIfNeeded(buildingRegistration);
	}

	private static void UpdateSchedule(BuildingRegistration buildingRegistration)
	{
		foreach (ScheduleDay scheduleDay in buildingRegistration.scheduleDays)
		{
			scheduleDay.ClearWorkShifts();
		}
		foreach (EmployeeInstance employeeInstance in EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			withAssignedAddress = buildingRegistration.Address,
			excludeBeingReplaced = true
		}))
		{
			employeeInstance.assignedWeeklyDays.Clear();
			employeeInstance.assignedWeeklyHours = 0;
			employeeInstance.assignedWorkStationItems.Clear();
			employeeInstance.UnAssignWork();
		}
	}

	private static void UpdateSecurityIfNeeded(BuildingRegistration buildingRegistration)
	{
		if (!BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.allowtheft))
		{
			return;
		}
		List<ExitZone> exitZones = InstanceBehavior<BuildingManager>.Instance.GetBuildingTransform(new BuildingSizeInfo(buildingRegistration)).GetComponentsInChildren<ExitZone>().ToList();
		foreach (ItemInstance item in buildingRegistration.itemInstances.Values.Where((ItemInstance x) => x.itemName == "ba:itemname_securitypanel"))
		{
			item.UpdateSecurityPanelCoverage(exitZones);
		}
		buildingRegistration.UpdateSecurityLevel();
	}
}
