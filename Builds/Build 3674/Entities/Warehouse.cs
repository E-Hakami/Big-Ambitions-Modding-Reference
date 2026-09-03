using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings;
using Helpers;
using UnityEngine;
using Vehicles.VehicleTypes;

namespace Entities;

[Serializable]
public class Warehouse : BuildingRegistration
{
	public List<VehicleSlot> vehicleSlots;

	public List<DeliveryTransaction> deliveryTransactions;

	public override void ResetBuildingSpecific()
	{
		ResetVehicleSlots();
		deliveryTransactions = new List<DeliveryTransaction>();
	}

	public void ResetVehicleSlots()
	{
		vehicleSlots = new List<VehicleSlot>();
		int num = BuildingSizeHelper.GetData(BuildingHelper.GetBuilding(base.Address))?.numberOfVehicleSlots ?? 0;
		for (int i = 0; i < num; i++)
		{
			vehicleSlots.Add(new VehicleSlot());
		}
	}

	public IEnumerable<string> GetProducts()
	{
		HashSet<string> hashSet = new HashSet<string>();
		foreach (ItemInstance value in itemInstances.Values)
		{
			Item itemCached = value.ItemCached;
			if (itemCached.HasTag(TagRef.Itemtag.iswarehousestorage))
			{
				foreach (CargoInstance cargoInstance in value.cargoInstances)
				{
					if (IsRetailProductOrBag(cargoInstance.ItemCached))
					{
						hashSet.Add(cargoInstance.itemName);
					}
				}
			}
			if (IsRetailProductOrBag(itemCached))
			{
				hashSet.Add(value.itemName);
			}
		}
		return hashSet;
	}

	private static bool IsRetailProductOrBag(Item item)
	{
		if ((item.type & ItemType.RetailProduct) == 0)
		{
			return item.HasTag(TagRef.Itemtag.isbag);
		}
		return true;
	}

	public int GetProductConsumption(string product)
	{
		int num = 0;
		int num2 = SaveGameManager.Current.Day - 7;
		for (int i = 0; i < deliveryTransactions.Count; i++)
		{
			DeliveryTransaction deliveryTransaction = deliveryTransactions[i];
			if (deliveryTransaction.dayOfDelivery < num2)
			{
				continue;
			}
			List<DeliveryItem> deliveryItems = deliveryTransaction.deliveryItems;
			for (int j = 0; j < deliveryItems.Count; j++)
			{
				DeliveryItem deliveryItem = deliveryItems[j];
				if (deliveryItem.itemName == product && deliveryItem.amountDelivered < 0)
				{
					num -= deliveryItem.amountDelivered;
				}
			}
		}
		return num;
	}

	public int GetProductDeliveries(string product)
	{
		int num = 0;
		int num2 = SaveGameManager.Current.Day - 7;
		for (int i = 0; i < deliveryTransactions.Count; i++)
		{
			DeliveryTransaction deliveryTransaction = deliveryTransactions[i];
			if (deliveryTransaction.dayOfDelivery < num2)
			{
				continue;
			}
			List<DeliveryItem> deliveryItems = deliveryTransaction.deliveryItems;
			for (int j = 0; j < deliveryItems.Count; j++)
			{
				DeliveryItem deliveryItem = deliveryItems[j];
				if (deliveryItem.itemName == product && deliveryItem.amountDelivered > 0)
				{
					num += deliveryItem.amountDelivered;
				}
			}
		}
		return num;
	}

	public List<(string, int, int)> GetInventoryForDisplay(int maxEntries)
	{
		List<(string, int, int)> list = new List<(string, int, int)>();
		foreach (string product in GetProducts())
		{
			int num = BuildingHelper.CountResourcesInPallets(base.Address, product);
			float num2 = (float)GetProductConsumption(product) / 7f;
			int item = ((num2 == 0f) ? (-1) : ((int)Mathf.Floor((float)num / num2)));
			list.Add((product, num, item));
		}
		list.Sort(CompareInventoryEntries);
		if (maxEntries < 0)
		{
			maxEntries = 0;
		}
		if (list.Count > maxEntries)
		{
			list.RemoveRange(maxEntries, list.Count - maxEntries);
		}
		return list;
	}

	private static int CompareInventoryEntries((string, int, int) left, (string, int, int) right)
	{
		int num = left.Item3.CompareTo(right.Item3);
		if (num != 0)
		{
			return num;
		}
		return left.Item2.CompareTo(right.Item2);
	}

	public int GetNumberOfAssignedCars()
	{
		int num = 0;
		foreach (VehicleSlot vehicleSlot in vehicleSlots)
		{
			if (!string.IsNullOrEmpty(vehicleSlot.vehicleInstanceId))
			{
				num++;
			}
		}
		return num;
	}

	public List<string> GetVehiclesInParkingSpots()
	{
		List<string> list = new List<string>();
		foreach (VehicleSlot vehicleSlot in vehicleSlots)
		{
			if (!string.IsNullOrEmpty(vehicleSlot.vehicleInstanceId))
			{
				list.Add(vehicleSlot.vehicleInstanceId);
			}
		}
		return list;
	}

	public VehicleSlot GetVehicleSlotByVehicleInstance(VehicleInstance vehicleInstance)
	{
		VehicleSlot result = null;
		foreach (VehicleSlot vehicleSlot in vehicleSlots)
		{
			if (!(vehicleSlot.vehicleInstanceId != vehicleInstance.id))
			{
				result = vehicleSlot;
				break;
			}
		}
		return result;
	}

	public bool HasDuplicateSlotAssignment(string vehicleId)
	{
		bool flag = false;
		foreach (VehicleSlot vehicleSlot in vehicleSlots)
		{
			if (!(vehicleSlot.vehicleInstanceId != vehicleId))
			{
				if (flag)
				{
					return true;
				}
				flag = true;
			}
		}
		return false;
	}

	public void ClearSlotAssignments(string vehicleId)
	{
		foreach (VehicleSlot vehicleSlot in vehicleSlots)
		{
			if (vehicleSlot.vehicleInstanceId == vehicleId)
			{
				vehicleSlot.vehicleInstanceId = null;
			}
		}
	}

	public VehicleSlot GetVehicleFreeSlot()
	{
		foreach (VehicleSlot vehicleSlot in vehicleSlots)
		{
			if (string.IsNullOrEmpty(vehicleSlot.vehicleInstanceId))
			{
				return vehicleSlot;
			}
		}
		return null;
	}

	public static bool CanVehicleOccupySlot(VehicleInstance vehicleInstance)
	{
		if (vehicleInstance.preventWarehouseAssign)
		{
			return false;
		}
		VehicleType vehicleType = vehicleInstance.VehicleType;
		if (vehicleType == null)
		{
			return false;
		}
		if (!vehicleType.HasTag(TagRef.Vehicletag.ishandvehicle))
		{
			return !vehicleType.HasTag(TagRef.Vehicletag.isscooter);
		}
		return false;
	}

	public void AssignParkedVehiclesToFreeSlots(List<VehicleInstance> vehicleInstances)
	{
		foreach (VehicleInstance vehicleInstance in vehicleInstances)
		{
			if (vehicleInstance.IsAtAddress(base.Address) && CanVehicleOccupySlot(vehicleInstance) && GetVehicleSlotByVehicleInstance(vehicleInstance) == null)
			{
				VehicleSlot vehicleFreeSlot = GetVehicleFreeSlot();
				if (vehicleFreeSlot == null)
				{
					break;
				}
				vehicleFreeSlot.AssignVehicle(vehicleInstance);
			}
		}
	}

	public static bool CanDriverOperateVehicle(EmployeeInstance driver, VehicleInstance vehicleInstance)
	{
		VehicleType vehicleType = vehicleInstance.VehicleType;
		if (vehicleType == null)
		{
			return false;
		}
		return driver.GetSkillValue("ba:skill_deliverydriver") >= (float)vehicleType.requiredDeliveryDriverSkillValue;
	}

	public int GetVehicleSpotIndex(VehicleSlot vehicleSlot)
	{
		foreach (VehicleSlot vehicleSlot2 in vehicleSlots)
		{
			if (vehicleSlot2 == vehicleSlot)
			{
				return vehicleSlots.IndexOf(vehicleSlot2);
			}
		}
		return -1;
	}

	public bool IsDriverAssignedToAnySlot(EmployeeInstance driver)
	{
		foreach (VehicleSlot vehicleSlot in vehicleSlots)
		{
			if (vehicleSlot.employeeDriverId == driver.id)
			{
				return true;
			}
		}
		return false;
	}
}
