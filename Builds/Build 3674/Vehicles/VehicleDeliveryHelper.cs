using System.Collections.Generic;
using Blueprints;
using Buildings;
using Entities;
using Extensions;
using Helpers;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;
using Vehicles.VehicleTypes;

namespace Vehicles;

public static class VehicleDeliveryHelper
{
	private static readonly Dictionary<Address, int> VehicleDeliveryContractsCountByAddress = new Dictionary<Address, int>();

	private static readonly List<BuildingRegistration> AvailableWarehousesToDeliver = new List<BuildingRegistration>();

	public static void RunHourly()
	{
		List<VehicleDeliveryContract> vehicleDeliveryContracts = SaveGameManager.Current.vehicleDeliveryContracts;
		for (int num = vehicleDeliveryContracts.Count - 1; num >= 0; num--)
		{
			if (TimeHelper.IsInThePast(vehicleDeliveryContracts[num].deliveryDay, vehicleDeliveryContracts[num].deliveryHour))
			{
				DeliverVehicle(vehicleDeliveryContracts[num]);
				vehicleDeliveryContracts.RemoveAt(num);
			}
		}
	}

	private static void DeliverVehicle(VehicleDeliveryContract contract)
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(contract.deliveryAddress);
		BuildingRegistration buildingRegistration2 = BuildingHelper.GetBuildingRegistration(contract.vehicleStoreAddress);
		Contact contact = Contact.GetContact(buildingRegistration2, ContactCategoryName.FurnitureAndEquipment);
		string displayName = buildingRegistration2.GetDisplayName();
		Dictionary<string, string> messageData = new Dictionary<string, string> { { "businessName", displayName } };
		if (!buildingRegistration.RentedByPlayer)
		{
			contact.SendMessage(new TextMessage("ba:messagetype_dialog_vehicle_store_cant_deliver_in_unrented_building", messageData));
		}
		else if (SaveGameManager.Current.CurrentStreetName == contract.deliveryAddress.streetName && SaveGameManager.Current.CurrentStreetNumber == contract.deliveryAddress.streetNumber)
		{
			contact.SendMessage(new TextMessage("ba:messagetype_dialog_vehicle_store_cant_deliver_while_inside_building", messageData));
		}
		else if (!TryPlaceVehicleInWarehouse(contract, buildingRegistration, contact))
		{
			contact.SendMessage(new TextMessage("ba:messagetype_dialog_vehicle_store_no_free_slots", messageData));
		}
	}

	private static bool TryPlaceVehicleInWarehouse(VehicleDeliveryContract contract, BuildingRegistration registration, Contact contact)
	{
		if (!VehicleTypeHelper.IsValidVehicleType(contract.vehicleTypeName))
		{
			Debug.LogError("[VehicleDelivery] Unknown vehicle type '" + contract.vehicleTypeName + "'. Cancelling delivery.");
			return true;
		}
		Warehouse warehouse = (Warehouse)registration;
		WarehouseSlotController[] componentsInChildren = InstanceBehavior<BuildingManager>.Instance.GetBuildingTransform(new BuildingSizeInfo(registration)).GetComponentsInChildren<WarehouseSlotController>();
		Dictionary<string, string> messageData = new Dictionary<string, string> { 
		{
			"businessName",
			registration.GetDisplayName()
		} };
		for (int i = 0; i < warehouse.vehicleSlots.Count; i++)
		{
			VehicleSlot vehicleSlot = warehouse.vehicleSlots[i];
			if (string.IsNullOrEmpty(vehicleSlot.vehicleInstanceId))
			{
				if (!TryGetWarehouseSlotController(i, componentsInChildren, out var result))
				{
					Debug.LogError($"[VehicleDelivery] No WarehouseSlotController for slot {i}. Cancelling delivery.");
					return true;
				}
				VehicleType vehicleType = VehicleTypeHelper.GetVehicleType(contract.vehicleTypeName);
				Dictionary<string, string> data = new Dictionary<string, string> { { "vehicleName", contract.vehicleTypeName } };
				TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_vehiclebought", data);
				if (vehicleType.taxDeductible)
				{
					transactionInfo.SetTaxDeductibleName("tax_vehicle");
				}
				if (!GameManager.ChangeMoneySafe(0f - contract.deliveryPrice, transactionInfo, null, null, force: false, showNotification: true))
				{
					contact.SendMessage(new TextMessage("ba:messagetype_dialog_vehicle_store_delivery_not_enough_money", messageData));
					return true;
				}
				VehicleInstance vehicleInstance = CreateVehicleInstance(contract, registration, result);
				vehicleSlot.AssignVehicle(vehicleInstance);
				SaveGameManager.Current.VehicleInstances.Add(vehicleInstance);
				contact.SendMessage(new TextMessage("ba:messagetype_dialog_vehicle_delivery_done", messageData));
				return true;
			}
		}
		return false;
	}

	private static VehicleInstance CreateVehicleInstance(VehicleDeliveryContract contract, BuildingRegistration registration, WarehouseSlotController warehouseSlotController)
	{
		return new VehicleInstance(contract.vehicleTypeName)
		{
			id = UuidHelper.GenerateBase64Uuid(),
			vehicleColorName = contract.vehicleColor,
			fuel = VehicleTypeHelper.GetVehicleType(contract.vehicleTypeName).maxFuel * Random.Range(0.97f, 0.98f),
			streetName = registration.StreetName,
			streetNumber = registration.StreetNumber,
			position = warehouseSlotController.vehiclePosition.position,
			rotation = warehouseSlotController.vehiclePosition.rotation
		};
	}

	private static bool TryGetWarehouseSlotController(int slotIndex, WarehouseSlotController[] controllers, out WarehouseSlotController result)
	{
		foreach (WarehouseSlotController warehouseSlotController in controllers)
		{
			if (warehouseSlotController.slotIndex == slotIndex + 1)
			{
				result = warehouseSlotController;
				return true;
			}
		}
		result = null;
		return false;
	}

	public static bool HasAnyWarehouseAvailableToDeliver()
	{
		UpdateDeliveryContractsCountByAddress();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer || buildingRegistration.GetBuildingType() != "ba:buildingtype_warehouse" || !(buildingRegistration is Warehouse warehouse))
			{
				continue;
			}
			VehicleDeliveryContractsCountByAddress.TryGetValue(buildingRegistration.Address, out var value);
			int num = 0;
			foreach (VehicleSlot vehicleSlot in warehouse.vehicleSlots)
			{
				if (string.IsNullOrEmpty(vehicleSlot.vehicleInstanceId))
				{
					num++;
				}
			}
			if (num > value)
			{
				return true;
			}
		}
		return false;
	}

	public static IReadOnlyCollection<BuildingRegistration> GetAvailableWarehousesToDeliver()
	{
		AvailableWarehousesToDeliver.Clear();
		UpdateDeliveryContractsCountByAddress();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer || buildingRegistration.GetBuildingType() != "ba:buildingtype_warehouse" || !(buildingRegistration is Warehouse warehouse))
			{
				continue;
			}
			VehicleDeliveryContractsCountByAddress.TryGetValue(buildingRegistration.Address, out var value);
			int num = 0;
			foreach (VehicleSlot vehicleSlot in warehouse.vehicleSlots)
			{
				if (string.IsNullOrEmpty(vehicleSlot.vehicleInstanceId))
				{
					num++;
				}
			}
			if (num > value)
			{
				AvailableWarehousesToDeliver.Add(buildingRegistration);
			}
		}
		return AvailableWarehousesToDeliver;
	}

	private static void UpdateDeliveryContractsCountByAddress()
	{
		VehicleDeliveryContractsCountByAddress.Clear();
		foreach (VehicleDeliveryContract vehicleDeliveryContract in SaveGameManager.Current.vehicleDeliveryContracts)
		{
			if (!VehicleDeliveryContractsCountByAddress.TryAdd(vehicleDeliveryContract.deliveryAddress, 1))
			{
				VehicleDeliveryContractsCountByAddress[vehicleDeliveryContract.deliveryAddress]++;
			}
		}
	}

	public static VehicleStoreSettings GetVehicleStoreSettings(Address address)
	{
		return BuildingHelper.GetBuilding(address)?.SpecialService.settings as VehicleStoreSettings;
	}
}
