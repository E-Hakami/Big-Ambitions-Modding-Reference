using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Helpers;
using UnityEngine;

public static class ReStockingHelper
{
	private sealed class RestockTarget
	{
		public ItemInstance itemInstance;

		public CargoInstance stockInstance;

		public int maxStockCapacity;

		public int amountToAdd;

		public float missingPercentage;

		public float amountRemainder;
	}

	private const float MinimumSingleItemRefillPercentage = 0.1f;

	private const float SingleItemRefillStorageShare = 0.5f;

	private static readonly List<RestockTarget> RestockTargets = new List<RestockTarget>();

	private static readonly List<ItemInstance> ItemInstancesToRemove = new List<ItemInstance>();

	private static readonly List<ItemInstance> ReStockableItems = new List<ItemInstance>();

	public static void RedistributeStockByPercentage(BuildingRegistration buildingRegistration, List<ItemInstance> itemInstances)
	{
		if (ItemHelper.PauseAutoFillShelves || buildingRegistration == null || itemInstances == null || itemInstances.Count == 0)
		{
			return;
		}
		string itemName = itemInstances[0].GetStockInstance().itemName;
		if (string.IsNullOrEmpty(itemName))
		{
			return;
		}
		RestockTargets.Clear();
		int num = 0;
		foreach (ItemInstance itemInstance in itemInstances)
		{
			if (IsReStockableItem(itemInstance, itemName))
			{
				CargoInstance stockInstance = itemInstance.GetStockInstance();
				int maxStockCapacity = stockInstance.GetMaxStockCapacity(itemInstance);
				if (maxStockCapacity > 0)
				{
					RestockTargets.Add(new RestockTarget
					{
						itemInstance = itemInstance,
						stockInstance = stockInstance,
						maxStockCapacity = maxStockCapacity
					});
					num += Math.Max(0, maxStockCapacity - stockInstance.amount);
				}
			}
		}
		if (RestockTargets.Count == 0 || num <= 0)
		{
			return;
		}
		int val = CountAvailableStock(buildingRegistration, itemName);
		int num2 = Math.Min(num, val);
		if (num2 <= 0)
		{
			return;
		}
		CalculateRestockPlan(num2);
		bool flag = false;
		foreach (RestockTarget restockTarget in RestockTargets)
		{
			if (restockTarget.amountToAdd > 0 && MoveStockToItem(buildingRegistration, restockTarget.itemInstance, itemName, restockTarget.amountToAdd) > 0)
			{
				flag = true;
			}
		}
		if (flag)
		{
			GameEvent.Invoke("ba:gameevent_itemstockedup");
		}
	}

	public static void RedistributeStockByPercentage(ItemInstance itemInstance)
	{
		if (itemInstance == null)
		{
			return;
		}
		CargoInstance stockInstance = itemInstance.GetStockInstance();
		if (stockInstance == null || string.IsNullOrEmpty(stockInstance.itemName))
		{
			return;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(itemInstance.AddressCached);
		if (buildingRegistration == null)
		{
			return;
		}
		ReStockableItems.Clear();
		foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
		{
			if (IsReStockableItem(value, stockInstance.itemName))
			{
				ReStockableItems.Add(value);
			}
		}
		RedistributeStockByPercentage(buildingRegistration, ReStockableItems);
	}

	public static void RefillSingleItemWithLimit(ItemInstance itemInstance)
	{
		if (ItemHelper.PauseAutoFillShelves || itemInstance == null || !itemInstance.CanRestockItem())
		{
			return;
		}
		CargoInstance stockInstance = itemInstance.GetStockInstance();
		int maxStockCapacity = stockInstance.GetMaxStockCapacity(itemInstance);
		if (maxStockCapacity <= 0 || stockInstance.amount >= maxStockCapacity)
		{
			return;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(itemInstance.AddressCached);
		if (buildingRegistration == null)
		{
			return;
		}
		int num = CountAvailableStock(buildingRegistration, stockInstance.itemName);
		if (num > 0)
		{
			int num2 = maxStockCapacity - stockInstance.amount;
			int num3 = (HasMultipleRestockableItems(buildingRegistration, stockInstance.itemName) ? GetSingleItemRefillAmount(num, ItemsGetter.GetByName(stockInstance.itemName).boxSize, num2) : Math.Min(num, num2));
			if (num3 > 0 && MoveStockToItem(buildingRegistration, itemInstance, stockInstance.itemName, num3) > 0)
			{
				GameEvent.Invoke("ba:gameevent_itemstockedup");
			}
		}
	}

	public static int TryAddStockAmount(this ItemInstance itemInstance, CargoInstance sourceCargoInstance, int amount)
	{
		if (itemInstance == null || sourceCargoInstance == null || amount <= 0 || sourceCargoInstance.amount <= 0)
		{
			return 0;
		}
		CargoInstance stockInstance = itemInstance.GetStockInstance();
		if (stockInstance == null || string.IsNullOrEmpty(stockInstance.itemName) || stockInstance.itemName != sourceCargoInstance.itemName)
		{
			return 0;
		}
		int num = stockInstance.GetMaxStockCapacity(itemInstance) - stockInstance.amount;
		if (num <= 0)
		{
			return 0;
		}
		int num2 = Math.Min(Math.Min(amount, num), sourceCargoInstance.amount);
		stockInstance.MergeAmount(sourceCargoInstance, num2);
		return num2;
	}

	private static int GetSingleItemRefillAmount(int availableStock, int boxSize, int remainingCapacity)
	{
		int num = Mathf.CeilToInt((float)boxSize * 0.1f);
		if (availableStock <= num)
		{
			return Math.Min(availableStock, remainingCapacity);
		}
		int val = Mathf.CeilToInt((float)availableStock * 0.5f);
		return Math.Min(Math.Min(Math.Max(num, val), availableStock), remainingCapacity);
	}

	private static bool HasMultipleRestockableItems(BuildingRegistration buildingRegistration, string stockItemName)
	{
		int num = 0;
		foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
		{
			if (IsReStockableItem(value, stockItemName))
			{
				num++;
				if (num > 1)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static bool IsReStockableItem(ItemInstance itemInstance, string stockItemName)
	{
		if (itemInstance == null || !itemInstance.CanRestockItem())
		{
			return false;
		}
		return itemInstance.GetStockInstance().itemName == stockItemName;
	}

	private static int CountAvailableStock(BuildingRegistration buildingRegistration, string stockItemName)
	{
		int num = 0;
		foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
		{
			if (!value.ItemCached.HasTag(TagRef.Itemtag.isstockcontainer))
			{
				continue;
			}
			foreach (CargoInstance cargoInstance in value.cargoInstances)
			{
				if (CanUseCargoForRestocking(cargoInstance, stockItemName))
				{
					num += cargoInstance.amount;
				}
			}
		}
		foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances)
		{
			if (vehicleInstance.Address != buildingRegistration.Address || vehicleInstance.id == SaveGameManager.Current.ActiveVehicleId)
			{
				continue;
			}
			foreach (CargoInstance cargoInstance2 in vehicleInstance.cargoInstances)
			{
				if (CanUseCargoForRestocking(cargoInstance2, stockItemName))
				{
					num += cargoInstance2.amount;
				}
			}
		}
		return num;
	}

	private static bool CanUseCargoForRestocking(CargoInstance cargoInstance, string stockItemName)
	{
		if (cargoInstance.itemName == stockItemName)
		{
			return cargoInstance.nestedCargoInstances.Count == 0;
		}
		return false;
	}

	private static void CalculateRestockPlan(int amountToDistribute)
	{
		float num = 0f;
		foreach (RestockTarget restockTarget in RestockTargets)
		{
			int num2 = restockTarget.maxStockCapacity - restockTarget.stockInstance.amount;
			restockTarget.missingPercentage = (float)num2 / (float)restockTarget.maxStockCapacity;
			num += restockTarget.missingPercentage;
		}
		if (num <= 0f)
		{
			return;
		}
		int num3 = 0;
		foreach (RestockTarget restockTarget2 in RestockTargets)
		{
			float num4 = (float)amountToDistribute * (restockTarget2.missingPercentage / num);
			int num5 = (restockTarget2.amountToAdd = Mathf.Clamp(Mathf.FloorToInt(num4), 0, restockTarget2.maxStockCapacity - restockTarget2.stockInstance.amount));
			restockTarget2.amountRemainder = num4 - (float)num5;
			num3 += num5;
		}
		for (int num6 = amountToDistribute - num3; num6 > 0; num6--)
		{
			RestockTarget largestRemainderTarget = GetLargestRemainderTarget();
			if (largestRemainderTarget == null)
			{
				break;
			}
			largestRemainderTarget.amountToAdd++;
			largestRemainderTarget.amountRemainder = 0f;
		}
	}

	private static RestockTarget GetLargestRemainderTarget()
	{
		RestockTarget restockTarget = null;
		foreach (RestockTarget restockTarget2 in RestockTargets)
		{
			if (restockTarget2.stockInstance.amount + restockTarget2.amountToAdd < restockTarget2.maxStockCapacity && (restockTarget == null || restockTarget2.amountRemainder > restockTarget.amountRemainder))
			{
				restockTarget = restockTarget2;
			}
		}
		return restockTarget;
	}

	private static int MoveStockToItem(BuildingRegistration buildingRegistration, ItemInstance targetItemInstance, string stockItemName, int amount)
	{
		ItemInstancesToRemove.Clear();
		int num = MoveStockFromBuildingStorage(buildingRegistration, targetItemInstance, stockItemName, amount);
		foreach (ItemInstance item in ItemInstancesToRemove)
		{
			buildingRegistration.RemoveItemInstanceFromBuilding(item);
		}
		ItemInstancesToRemove.Clear();
		if (num < amount)
		{
			num += MoveStockFromVehicles(buildingRegistration.Address, targetItemInstance, stockItemName, amount - num);
		}
		return num;
	}

	private static int MoveStockFromBuildingStorage(BuildingRegistration buildingRegistration, ItemInstance targetItemInstance, string stockItemName, int amount)
	{
		int num = 0;
		foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
		{
			if (!value.ItemCached.HasTag(TagRef.Itemtag.isstockcontainer))
			{
				continue;
			}
			for (int num2 = value.cargoInstances.Count - 1; num2 >= 0; num2--)
			{
				CargoInstance cargoInstance = value.cargoInstances[num2];
				if (CanUseCargoForRestocking(cargoInstance, stockItemName))
				{
					int num3 = targetItemInstance.TryAddStockAmount(cargoInstance, amount - num);
					if (num3 > 0)
					{
						num += num3;
						value.OnItemsInCargoUpdated()?.Invoke();
						targetItemInstance.OnItemsInCargoUpdated()?.Invoke();
						if (cargoInstance.amount == 0)
						{
							value.RemoveFromCargo(cargoInstance);
							if (value.cargoInstances.Count == 0 && value.ItemCached.HasTag(TagRef.Itemtag.discardcontainerwhenempty))
							{
								ItemInstancesToRemove.Add(value);
							}
						}
						if (num >= amount)
						{
							return num;
						}
					}
				}
			}
		}
		return num;
	}

	private static int MoveStockFromVehicles(Address address, ItemInstance targetItemInstance, string stockItemName, int amount)
	{
		int num = 0;
		foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances)
		{
			if (vehicleInstance.Address != address || vehicleInstance.id == SaveGameManager.Current.ActiveVehicleId)
			{
				continue;
			}
			for (int num2 = vehicleInstance.cargoInstances.Count - 1; num2 >= 0; num2--)
			{
				CargoInstance cargoInstance = vehicleInstance.cargoInstances[num2];
				if (CanUseCargoForRestocking(cargoInstance, stockItemName))
				{
					int num3 = targetItemInstance.TryAddStockAmount(cargoInstance, amount - num);
					if (num3 > 0)
					{
						num += num3;
						targetItemInstance.OnItemsInCargoUpdated()?.Invoke();
						if (cargoInstance.amount == 0)
						{
							vehicleInstance.RemoveFromCargo(cargoInstance);
						}
						if (num >= amount)
						{
							return num;
						}
					}
				}
			}
		}
		return num;
	}
}
