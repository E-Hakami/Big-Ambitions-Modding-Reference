using System.Collections.Generic;
using System.Linq;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan;

public class WarehouseList : MonoBehaviour
{
	private const int numberOfEntriesOnInventory = 4;

	private const int daysLeftToTriggerWarningColor = 3;

	[SerializeField]
	private Transform warehouseEntry;

	public void Load()
	{
		warehouseEntry.ResetTemplate();
		foreach (BuildingRegistration item in SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.RentedByPlayer && x.GetBuildingType() == "ba:buildingtype_warehouse"))
		{
			SetUpEntry((Entities.Warehouse)item);
		}
	}

	private void SetUpEntry(Entities.Warehouse warehouse)
	{
		Transform transform = Object.Instantiate(warehouseEntry, warehouseEntry.parent);
		transform.GetComponent<Button>().onClick.AddListener(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(warehouse.Address);
		});
		transform.GetLabelByName("WarehouseName").text = ((warehouse.businessTypeName == "ba:businesstype_empty") ? "bizman_empty_building".Localize(new
		{
			buildingType = "ba:buildingtype_warehouse".GetLocalization()
		}).ToString() : warehouse.BusinessName);
		transform.GetLanguageChangeEventByName("Address").SetValue(warehouse.Address.ToFormattedString(), clearKey: true);
		transform.GetButtonByName("Address/SetDestinationButton").onClick.AddListener(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.SetDestination(warehouse.Address);
		});
		Transform transform2 = transform.Find("VehicleSlotsList").Find("VehicleSlotEntry");
		transform2.gameObject.SetActive(value: false);
		for (int num = 0; num < warehouse.vehicleSlots.Count; num++)
		{
			Transform obj = Object.Instantiate(transform2, transform2.parent);
			VehicleSlot vehicleSlot = warehouse.vehicleSlots[num];
			obj.GetLanguageChangeEventByName("SlotNumber").SetData("bizman_drivers_slot_number".Localize(new
			{
				number = num + 1
			}));
			TextLocalizationComponent languageChangeEventByName = obj.GetLanguageChangeEventByName("VehicleName");
			VehicleInstance vehicleInstance = SaveGameManager.Current.VehicleInstances.FirstOrDefault((VehicleInstance x) => x.id == vehicleSlot.vehicleInstanceId);
			if (vehicleInstance == null)
			{
				languageChangeEventByName.SetData("common_unassigned".Localize());
				languageChangeEventByName.TextContainer.color = InstanceBehavior<GlobalReferences>.Instance.colors.lightRed;
			}
			else
			{
				languageChangeEventByName.SetData(vehicleInstance.vehicleTypeName.Localize());
				languageChangeEventByName.TextContainer.color = InstanceBehavior<GlobalReferences>.Instance.colors.midnight;
			}
			TextLocalizationComponent languageChangeEventByName2 = obj.GetLanguageChangeEventByName("DriverStatus");
			if (string.IsNullOrEmpty(vehicleSlot.employeeDriverId))
			{
				if (string.IsNullOrEmpty(vehicleSlot.vehicleInstanceId))
				{
					languageChangeEventByName2.SetData("common_value".Localize(new
					{
						value = "-"
					}));
					languageChangeEventByName2.TextContainer.color = InstanceBehavior<GlobalReferences>.Instance.colors.midnight;
				}
				else
				{
					languageChangeEventByName2.SetData("bizman_logisticsmanagers_no_driver".Localize());
					languageChangeEventByName2.TextContainer.color = InstanceBehavior<GlobalReferences>.Instance.colors.red;
				}
			}
			else
			{
				EmployeeInstance employeeById = EmployeeHelper.GetEmployeeById(vehicleSlot.employeeDriverId);
				languageChangeEventByName2.SetData("common_value".Localize(new
				{
					value = employeeById.characterData.name
				}));
				languageChangeEventByName2.TextContainer.color = InstanceBehavior<GlobalReferences>.Instance.colors.midnight;
			}
			obj.gameObject.SetActive(value: true);
		}
		transform.GetButtonByName("ManageDrivers/ManageDriversButton").onClick.AddListener(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(warehouse.Address, "Drivers");
		});
		Transform transform3 = transform.Find("InventoryList").Find("InventoryEntry");
		transform3.gameObject.SetActive(value: false);
		List<(string, int, int)> inventoryForDisplay = warehouse.GetInventoryForDisplay(4);
		for (int num2 = 0; num2 < inventoryForDisplay.Count; num2++)
		{
			Transform obj2 = Object.Instantiate(transform3, transform3.parent);
			(string, int, int) tuple = inventoryForDisplay[num2];
			obj2.GetLanguageChangeEventByName("ItemName").SetData(LanguageChangeEventDataHolder.Create(tuple.Item1));
			obj2.GetLabelByName("Amount").text = $"{tuple.Item2}";
			TextLocalizationComponent languageChangeEventByName3 = obj2.GetLanguageChangeEventByName("DaysLeft");
			if (tuple.Item3 == -1)
			{
				languageChangeEventByName3.SetData(LanguageChangeEventDataHolder.Create("common_value", new
				{
					value = "-"
				}));
				languageChangeEventByName3.TextContainer.color = InstanceBehavior<GlobalReferences>.Instance.colors.green;
			}
			else if (tuple.Item3 == 0)
			{
				languageChangeEventByName3.SetData(LanguageChangeEventDataHolder.Create("bizman_inventory_run_out"));
				languageChangeEventByName3.TextContainer.color = InstanceBehavior<GlobalReferences>.Instance.colors.red;
			}
			else
			{
				languageChangeEventByName3.SetData(LanguageChangeEventDataHolder.Create("bizman_inventory_product_days_until_empty", new
				{
					days = tuple.Item3
				}));
				languageChangeEventByName3.TextContainer.color = ((tuple.Item3 <= 3) ? InstanceBehavior<GlobalReferences>.Instance.colors.lightRed : InstanceBehavior<GlobalReferences>.Instance.colors.midnight);
			}
			obj2.Find("Splitter").gameObject.SetActive(num2 != inventoryForDisplay.Count - 1);
			obj2.gameObject.SetActive(value: true);
		}
		transform.GetButtonByName("ShowFullInventory/ShowFullInventoryButton").onClick.AddListener(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(warehouse.Address, "Inventory");
		});
		transform.gameObject.SetActive(value: true);
	}
}
