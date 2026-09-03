using System.Collections.Generic;
using BigAmbitions.Items;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes;

public static class CompatibilityItemValidator
{
	private static readonly Dictionary<string, bool> ItemValidityCache = new Dictionary<string, bool>();

	private static readonly Dictionary<string, float> WholesalePriceCache = new Dictionary<string, float>();

	private static readonly Dictionary<string, string> RenamedItemNames = new Dictionary<string, string> { { "ba:itemname_moderncountercorner", "ba:itemname_countercorneroutermodern" } };

	private static float CompensationMoney;

	public static void ValidateGameInstance(GameInstance gameInstance)
	{
		ItemValidityCache.Clear();
		WholesalePriceCache.Clear();
		CompensationMoney = 0f;
		if (gameInstance != null)
		{
			ValidateImportPartnerships(gameInstance);
			ValidateDeliveryContracts(gameInstance);
			ValidateCharacterItemInHands(gameInstance);
			ValidateVehicleCargo(gameInstance);
			HashSet<string> removedItemInstanceIds = new HashSet<string>();
			ValidateBuildingRegistrations(gameInstance, removedItemInstanceIds);
			ValidateTodoTasks(gameInstance, removedItemInstanceIds);
			if (CompensationMoney > 0f)
			{
				SaveGameManager.Current.Money += CompensationMoney;
				Dictionary<string, string> data = new Dictionary<string, string> { { "text", "Invalid items were sold (caused by compatibility support)" } };
				TransactionInfo info = new TransactionInfo("ba:transaction_compatibilityfix", data);
				SaveGameManager.Current.Transactions.Enqueue(new Transaction(info)
				{
					amount = CompensationMoney
				});
			}
		}
	}

	private static void ValidateImportPartnerships(GameInstance gameInstance)
	{
		if (gameInstance.importPartnerships == null || gameInstance.importPartnerships.Count == 0)
		{
			return;
		}
		foreach (ImportPartnership importPartnership in gameInstance.importPartnerships)
		{
			if (importPartnership.products == null || importPartnership.products.Count == 0)
			{
				continue;
			}
			for (int num = importPartnership.products.Count - 1; num >= 0; num--)
			{
				ImportProduct importProduct = importPartnership.products[num];
				RenameItemName(ref importProduct.itemName);
				if (!IsValidItemName(importProduct.itemName))
				{
					importPartnership.products.RemoveAt(num);
				}
			}
		}
	}

	private static void ValidateDeliveryContracts(GameInstance gameInstance)
	{
		if (gameInstance.DeliveryContracts == null || gameInstance.DeliveryContracts.Count == 0)
		{
			return;
		}
		foreach (DeliveryContract deliveryContract in gameInstance.DeliveryContracts)
		{
			if (deliveryContract.items == null || deliveryContract.items.Count == 0)
			{
				continue;
			}
			for (int num = deliveryContract.items.Count - 1; num >= 0; num--)
			{
				DeliveryContractItem deliveryContractItem = deliveryContract.items[num];
				RenameItemName(ref deliveryContractItem.itemName);
				if (!IsValidItemName(deliveryContractItem.itemName))
				{
					deliveryContract.items.RemoveAt(num);
				}
			}
		}
	}

	private static void ValidateCharacterItemInHands(GameInstance gameInstance)
	{
		if (gameInstance.charactersData == null || gameInstance.charactersData.Count == 0)
		{
			return;
		}
		ItemInstance itemInHands = gameInstance.charactersData[0].itemInHands;
		if (itemInHands == null || itemInHands.cargoInstances == null || itemInHands.cargoInstances.Count == 0)
		{
			return;
		}
		for (int num = itemInHands.cargoInstances.Count - 1; num >= 0; num--)
		{
			CargoInstance cargoInstance = itemInHands.cargoInstances[num];
			RenameItemName(ref cargoInstance.itemName);
			if (!IsValidItemName(cargoInstance.itemName))
			{
				CompensationMoney += GetCargoCompensation(cargoInstance);
				itemInHands.RemoveFromCargo(cargoInstance);
			}
		}
	}

	private static void ValidateVehicleCargo(GameInstance gameInstance)
	{
		if (gameInstance.VehicleInstances == null || gameInstance.VehicleInstances.Count == 0)
		{
			return;
		}
		foreach (VehicleInstance vehicleInstance in gameInstance.VehicleInstances)
		{
			List<CargoInstance> cargoInstances = vehicleInstance.cargoInstances;
			if (cargoInstances == null || cargoInstances.Count == 0)
			{
				continue;
			}
			for (int num = cargoInstances.Count - 1; num >= 0; num--)
			{
				CargoInstance cargoInstance = cargoInstances[num];
				RenameItemName(ref cargoInstance.itemName);
				if (!IsValidItemName(cargoInstance.itemName))
				{
					CompensationMoney += GetCargoCompensation(cargoInstance);
					vehicleInstance.RemoveFromCargo(cargoInstance);
				}
			}
		}
	}

	private static void ValidateBuildingRegistrations(GameInstance gameInstance, HashSet<string> removedItemInstanceIds)
	{
		if (gameInstance.BuildingRegistrations == null || gameInstance.BuildingRegistrations.Count == 0)
		{
			return;
		}
		for (int num = gameInstance.BuildingRegistrations.Count - 1; num >= 0; num--)
		{
			BuildingRegistration buildingRegistration = gameInstance.BuildingRegistrations[num];
			if (buildingRegistration == null)
			{
				gameInstance.BuildingRegistrations.RemoveAt(num);
			}
			else
			{
				ValidateRegistration(buildingRegistration, removedItemInstanceIds);
			}
		}
	}

	private static void ValidateRegistration(BuildingRegistration registration, HashSet<string> removedItemInstanceIds)
	{
		if (string.IsNullOrEmpty(registration.businessTypeName))
		{
			registration.businessTypeName = "ba:businesstype_empty";
		}
		if (registration.businessOwnerRivalId == null)
		{
			registration.businessOwnerRivalId = string.Empty;
		}
		if (registration.dailyIncomes == null)
		{
			registration.dailyIncomes = new List<float>();
		}
		if (registration.itemInstances == null)
		{
			registration.itemInstances = new Dictionary<string, ItemInstance>();
		}
		else
		{
			if (registration.itemInstances.Count == 0)
			{
				return;
			}
			List<string> list = new List<string>(registration.itemInstances.Keys);
			for (int num = list.Count - 1; num >= 0; num--)
			{
				string text = list[num];
				if (registration.itemInstances.TryGetValue(text, out var itemInstance))
				{
					if (itemInstance == null)
					{
						registration.itemInstances.Remove(text);
						removedItemInstanceIds.Add(text);
					}
					else
					{
						RenameItemInstanceItemNames(itemInstance);
						if (string.IsNullOrEmpty(itemInstance.itemName))
						{
							registration.RemoveItemInstanceFromBuilding(itemInstance);
							removedItemInstanceIds.Add(itemInstance.id);
						}
						else if (!IsValidItem(itemInstance))
						{
							List<ScheduleDay> scheduleDays = registration.scheduleDays;
							if (scheduleDays != null)
							{
								foreach (ScheduleDay item in scheduleDays)
								{
									item.workShifts?.RemoveAll((WorkShift workShift) => workShift.itemInstanceId == itemInstance.id);
								}
							}
							CompensationMoney += GetItemInstanceCompensation(itemInstance);
							registration.RemoveItemInstanceFromBuilding(itemInstance);
							removedItemInstanceIds.Add(itemInstance.id);
						}
						else
						{
							ValidateItemInstanceCargo(itemInstance);
						}
					}
				}
			}
		}
	}

	private static void ValidateItemInstanceCargo(ItemInstance itemInstance)
	{
		bool flag = (itemInstance.ItemCached.type & (ItemType.PointOfSale | ItemType.ShowcaseShelf)) != 0;
		List<CargoInstance> cargoInstances = itemInstance.cargoInstances;
		if (cargoInstances == null)
		{
			if (flag)
			{
				itemInstance.cargoInstances = new List<CargoInstance>
				{
					new CargoInstance(null, 0, 0f)
				};
			}
			return;
		}
		for (int num = cargoInstances.Count - 1; num >= 0; num--)
		{
			CargoInstance cargoInstance = cargoInstances[num];
			RenameItemName(ref cargoInstance.itemName);
			bool flag2 = !string.IsNullOrEmpty(cargoInstance.itemName);
			if (!flag2 || !IsValidItemName(cargoInstance.itemName))
			{
				if (flag && cargoInstances.Count == 1)
				{
					cargoInstances[0] = new CargoInstance(null, 0, 0f);
					return;
				}
				if (flag2)
				{
					CompensationMoney += GetCargoCompensation(cargoInstance);
				}
				itemInstance.RemoveFromCargo(cargoInstance);
			}
		}
		if (!flag)
		{
			return;
		}
		if (cargoInstances.Count == 0)
		{
			cargoInstances.Add(new CargoInstance(null, 0, 0f));
		}
		else
		{
			if (cargoInstances.Count == 1)
			{
				return;
			}
			CargoInstance item = cargoInstances[0];
			for (int i = 0; i < cargoInstances.Count; i++)
			{
				if (!string.IsNullOrEmpty(cargoInstances[i].itemName))
				{
					item = cargoInstances[i];
					break;
				}
			}
			cargoInstances.Clear();
			cargoInstances.Add(item);
		}
	}

	private static void ValidateTodoTasks(GameInstance gameInstance, HashSet<string> removedItemInstanceIds)
	{
		List<TodoTask> todoTasks = gameInstance.TodoTasks;
		if (todoTasks == null || todoTasks.Count == 0)
		{
			return;
		}
		for (int num = todoTasks.Count - 1; num >= 0; num--)
		{
			TodoTask todoTask = todoTasks[num];
			if (removedItemInstanceIds.Contains(todoTask.itemInstanceId))
			{
				todoTasks.RemoveAt(num);
			}
			else
			{
				RenameItemName(ref todoTask.itemName);
				if (!IsValidItemName(todoTask.itemName))
				{
					todoTasks.RemoveAt(num);
				}
			}
		}
	}

	private static bool IsValidItem(ItemInstance itemInstance)
	{
		if (!IsValidItemName(itemInstance.itemName))
		{
			return false;
		}
		if (SerializableVector3.IsZero(itemInstance.position))
		{
			return false;
		}
		return true;
	}

	internal static bool IsValidItemName(string itemName)
	{
		if (string.IsNullOrEmpty(itemName))
		{
			return false;
		}
		if (ItemValidityCache.TryGetValue(itemName, out var value))
		{
			return value;
		}
		value = ItemsGetter.GetByName(itemName, suppressError: true) != null;
		ItemValidityCache[itemName] = value;
		return value;
	}

	private static void RenameItemInstanceItemNames(ItemInstance itemInstance)
	{
		RenameItemName(ref itemInstance.itemName);
		RenameItemName(ref itemInstance.linkedItemName);
		PlayerItemPurchaserSettings playerItemPurchaserSettings = itemInstance.playerItemPurchaserSettings;
		if (playerItemPurchaserSettings != null)
		{
			RenameItemName(ref playerItemPurchaserSettings.itemName);
		}
		if (itemInstance.stackedItems != null)
		{
			foreach (AttachableChild stackedItem in itemInstance.stackedItems)
			{
				RenameItemName(ref stackedItem.childItemName);
			}
		}
		if (itemInstance.cargoInstances == null)
		{
			return;
		}
		foreach (CargoInstance cargoInstance in itemInstance.cargoInstances)
		{
			if (cargoInstance != null)
			{
				RenameItemName(ref cargoInstance.itemName);
			}
		}
	}

	private static void RenameItemName(ref string itemName)
	{
		if (!string.IsNullOrEmpty(itemName) && RenamedItemNames.TryGetValue(itemName, out var value))
		{
			itemName = value;
		}
	}

	private static float GetItemInstanceCompensation(ItemInstance itemInstance)
	{
		List<CargoInstance> cargoInstances = itemInstance.cargoInstances;
		if (cargoInstances == null)
		{
			return itemInstance.GetWorth();
		}
		foreach (CargoInstance item in cargoInstances)
		{
			if (!(item.pricePerUnit > 0f) && IsValidItemName(item.itemName))
			{
				item.pricePerUnit = GetWholesalePrice(item.itemName, item.ItemCached.GetWholesalePrice());
			}
		}
		return itemInstance.GetWorth();
	}

	private static float GetCargoCompensation(CargoInstance cargoInstance)
	{
		if (cargoInstance.pricePerUnit > 0f)
		{
			return cargoInstance.pricePerUnit * (float)cargoInstance.amount;
		}
		if (!IsValidItemName(cargoInstance.itemName))
		{
			return 0f;
		}
		return GetWholesalePrice(cargoInstance.itemName, cargoInstance.ItemCached.GetWholesalePrice()) * (float)cargoInstance.amount;
	}

	private static float GetWholesalePrice(string itemName, float fallbackWholesalePrice)
	{
		if (WholesalePriceCache.TryGetValue(itemName, out var value))
		{
			return value;
		}
		WholesalePriceCache[itemName] = fallbackWholesalePrice;
		return fallbackWholesalePrice;
	}

	public static void ClearCache()
	{
		ItemValidityCache.Clear();
		WholesalePriceCache.Clear();
	}
}
