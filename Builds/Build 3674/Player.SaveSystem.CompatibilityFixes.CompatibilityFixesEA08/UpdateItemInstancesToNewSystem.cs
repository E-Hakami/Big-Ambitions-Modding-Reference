using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Entities;
using Streets;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class UpdateItemInstancesToNewSystem : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		ItemHelper.Init();
		Dictionary<string, ItemInstance> worldItemsDictionary = new Dictionary<string, ItemInstance>();
		foreach (ItemInstance item5 in gameInstance.WorldItemsHashSet)
		{
			worldItemsDictionary.TryAdd(item5.id, item5);
		}
		if (!string.IsNullOrEmpty(gameInstance.InventoryItemInstanceId))
		{
			CharacterData characterData = gameInstance.charactersData?.FirstOrDefault();
			if (characterData != null && worldItemsDictionary.TryGetValue(gameInstance.InventoryItemInstanceId, out var value))
			{
				value.cargoInstances = new List<CargoInstance>();
				foreach (string cargoId in value.cargoIds)
				{
					if (worldItemsDictionary.TryGetValue(cargoId, out var value2) && value2 != null)
					{
						CargoInstance item = new CargoInstance(value2.itemName, value2.instanceQuantity, value2.pricePerUnitOnPurchaseTime, value2.paid, value2.customColors);
						value.cargoInstances.Add(item);
					}
				}
				value.cargoIds = null;
				if (value.itemName == "ba:itemname_paperbag" || value.itemName == "ba:itemname_shoppingbasket")
				{
					characterData.itemInHands = value;
				}
				else
				{
					characterData.itemInHands = ItemHelper.InitializeItemInHandsWithCargo(new CargoInstance(value.itemName, (value.instanceQuantity == 0) ? 1 : value.instanceQuantity, value.pricePerUnitOnPurchaseTime, value.paid, value.customColors));
				}
			}
			gameInstance.InventoryItemInstanceId = null;
		}
		int num = 0;
		int num2 = 0;
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			buildingRegistration.itemInstances = new Dictionary<string, ItemInstance>();
			if (buildingRegistration.itemsInBuilding == null)
			{
				continue;
			}
			List<ItemInstance> list = new List<ItemInstance>();
			foreach (string item6 in buildingRegistration.itemsInBuilding)
			{
				if (!worldItemsDictionary.TryGetValue(item6, out var value3) || value3.itemName == "ba:itemname_undefined" || !buildingRegistration.itemInstances.TryAdd(item6, value3))
				{
					num++;
					continue;
				}
				if (value3.instanceQuantity != 0)
				{
					value3.packedItemName = value3.itemName;
					value3.itemName = "ba:itemname_closedcardboardbox";
				}
				else if ((value3.ItemCached.type & ItemType.StorageShelf) != 0)
				{
					list.Add(value3);
				}
				value3.cargoInstances = new List<CargoInstance>();
				value3.priceOnPurchase = value3.pricePerUnitOnPurchaseTime;
				value3.pricePerUnitOnPurchaseTime = 0f;
				foreach (string cargoId2 in value3.cargoIds)
				{
					if (!worldItemsDictionary.TryGetValue(cargoId2, out var cargoItemInstance) || cargoItemInstance.itemName == "ba:itemname_undefined" || cargoItemInstance == null)
					{
						continue;
					}
					if (cargoItemInstance.instanceQuantity != 0)
					{
						CargoInstance cargoInstance = value3.cargoInstances.FirstOrDefault((CargoInstance x) => x.itemName == cargoItemInstance.itemName && x.paid == cargoItemInstance.paid && x.amount < x.ItemCached.boxSize && x.customColors == cargoItemInstance.customColors);
						if (cargoInstance != null)
						{
							int num3 = cargoInstance.ItemCached.boxSize - cargoInstance.amount;
							int num4 = ((num3 > cargoItemInstance.instanceQuantity) ? cargoItemInstance.instanceQuantity : num3);
							CargoInstance cargoInstanceToMerge = new CargoInstance(cargoItemInstance.itemName, num4, cargoItemInstance.pricePerUnitOnPurchaseTime);
							cargoInstance.MergeAmount(cargoInstanceToMerge, num4);
							cargoItemInstance.instanceQuantity -= num4;
							if (cargoItemInstance.instanceQuantity <= 0)
							{
								continue;
							}
						}
					}
					CargoInstance item2 = new CargoInstance(cargoItemInstance.itemName, (cargoItemInstance.instanceQuantity == 0) ? 1 : cargoItemInstance.instanceQuantity, cargoItemInstance.pricePerUnitOnPurchaseTime, cargoItemInstance.paid, cargoItemInstance.customColors);
					value3.cargoInstances.Add(item2);
				}
				value3.cargoIds = null;
				for (int num5 = value3.stackedItems.Count - 1; num5 >= 0; num5--)
				{
					AttachableChild attachableChild = value3.stackedItems[num5];
					if (!worldItemsDictionary.TryGetValue(attachableChild.childId, out var value4))
					{
						value3.stackedItems.RemoveAt(num5);
						num2++;
					}
					else
					{
						attachableChild.childItemName = value4.itemName;
					}
				}
				if (value3.itemName == "ba:itemname_closedcardboardbox")
				{
					CargoInstance item3 = new CargoInstance(value3.packedItemName, (value3.instanceQuantity == 0) ? 1 : value3.instanceQuantity, value3.pricePerUnitOnPurchaseTime, value3.paid, value3.customColors);
					value3.packedItemName = "ba:itemname_undefined";
					value3.customColors = null;
					value3.cargoInstances.Add(item3);
				}
				else if ((value3.ItemCached.type & (ItemType.PointOfSale | ItemType.ShowcaseShelf)) != 0)
				{
					CargoInstance cargoInstance2 = new CargoInstance("ba:itemname_undefined", 0, 0f);
					if (value3.shelfItem != null && value3.shelfItem.itemName != "ba:itemname_undefined")
					{
						cargoInstance2.itemName = value3.shelfItem.itemName;
						cargoInstance2.amount = value3.shelfItem.stock;
						cargoInstance2.pricePerUnit = value3.shelfItem.pricePerUnitOnPurchase;
					}
					value3.shelfItem = null;
					value3.cargoInstances.Add(cargoInstance2);
				}
			}
			buildingRegistration.itemsInBuilding = null;
			for (int num6 = list.Count - 1; num6 >= 0; num6--)
			{
				ItemInstance itemInstance = list[num6];
				for (int num7 = itemInstance.cargoInstances.Count - 1; num7 >= 0; num7--)
				{
					CargoInstance cargoInstance3 = itemInstance.cargoInstances[num7];
					if (string.IsNullOrEmpty(cargoInstance3.itemName))
					{
						itemInstance.cargoInstances.Remove(cargoInstance3);
					}
					else if (cargoInstance3.amount < cargoInstance3.ItemCached.boxSize)
					{
						for (int num8 = num6 - 1; num8 >= 0; num8--)
						{
							ItemInstance itemInstance2 = list[num8];
							if (itemInstance != itemInstance2)
							{
								itemInstance2.MergeIntoCargo(cargoInstance3);
								if (cargoInstance3.amount <= 0)
								{
									itemInstance.cargoInstances.RemoveAt(num7);
									break;
								}
							}
						}
					}
				}
			}
			if (buildingRegistration.deliveredItems == null || !buildingRegistration.RentedByPlayer)
			{
				continue;
			}
			ItemInstance itemInstance3 = buildingRegistration.itemInstances.Values.FirstOrDefault((ItemInstance x) => x.itemName == "ba:itemname_deliveryspot");
			if (itemInstance3 == null)
			{
				if (buildingRegistration.deliveredItems.Count > 0)
				{
					Debug.LogError("No delivery spot found for " + buildingRegistration.Address.ToFormattedString() + ", delivered items will be lost");
				}
				continue;
			}
			foreach (string deliveredItem in buildingRegistration.deliveredItems)
			{
				if (worldItemsDictionary.TryGetValue(deliveredItem, out var value5))
				{
					itemInstance3.AddToCargo(new CargoInstance(value5.itemName, (value5.instanceQuantity == 0) ? 1 : value5.instanceQuantity, value5.pricePerUnitOnPurchaseTime, value5.paid));
				}
			}
		}
		foreach (VehicleInstance vehicleInstance in gameInstance.VehicleInstances)
		{
			vehicleInstance.cargoInstances = new List<CargoInstance>();
			if (vehicleInstance.cargoIds == null)
			{
				continue;
			}
			foreach (string cargoId3 in vehicleInstance.cargoIds)
			{
				if (!worldItemsDictionary.TryGetValue(cargoId3, out var value6))
				{
					num++;
				}
				else if (value6 != null)
				{
					CargoInstance item4 = new CargoInstance(value6.itemName, (value6.instanceQuantity == 0) ? 1 : value6.instanceQuantity, value6.pricePerUnitOnPurchaseTime, value6.paid, value6.customColors);
					vehicleInstance.cargoInstances.Add(item4);
				}
			}
		}
		gameInstance.TodoTasks.RemoveAll((TodoTask x) => !string.IsNullOrEmpty(x.itemInstanceId) && !worldItemsDictionary.ContainsKey(x.itemInstanceId));
		gameInstance.WorldItemsHashSet = null;
		if (num > 0 || num2 > 0)
		{
			Debug.LogWarning($"{num} corrupted items removed from buildings and vehicles," + $"and {num2} corrupted attached items removed from items");
		}
	}
}
