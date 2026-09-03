using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AI.Citizens;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using BigAmbitions.Rivals;
using BigAmbitions.SaveSystem;
using BigAmbitions.Tags;
using Buildings;
using Buildings.BuildingTypes.Shared.BusinessRequirement;
using Buildings.Indoors.InteriorDesign;
using Controllers;
using Entities;
using Enums;
using Extensions;
using Helpers;
using IngameDebugConsole;
using JimmysUnityUtilities;
using Localizor;
using Seasons;
using Streets;
using UI;
using UI.Notification;
using UI.Tasks;
using UnityEngine;

public static class ItemHelper
{
	public const float MinRetailPrice = 0f;

	public const float MaxRetailPrice = 10000f;

	public const float RetailPriceStep = 0.1f;

	private const float BlueprintSellingMultiplier = 1f;

	public static float sellingMultiplier;

	public static readonly Dictionary<(string, string), float> LmpDictionary = new Dictionary<(string, string), float>();

	private static readonly Dictionary<(string, string), float> MarketAverageCache = new Dictionary<(string, string), float>();

	public static readonly List<ItemInstance> ItemsToUpdateToDoTasks = new List<ItemInstance>();

	private static readonly Dictionary<string, Sprite> ItemIconsCache = new Dictionary<string, Sprite>();

	private static readonly Dictionary<string, string> RequirementItems = new Dictionary<string, string>();

	private static readonly Dictionary<ItemType, string> RequirementItemTypes = new Dictionary<ItemType, string>();

	private static readonly Dictionary<string, ItemInstance> EmptyItemDictionary = new Dictionary<string, ItemInstance>();

	private static readonly Dictionary<string, HashSet<string>> ItemsSoldInBuildingType = new Dictionary<string, HashSet<string>>();

	private static readonly List<Item.ItemCapacity> TempItemCapacities = new List<Item.ItemCapacity>();

	private static readonly List<ItemInstance> ItemInstancesToRemove = new List<ItemInstance>();

	private static readonly List<ItemInstance> PalletShelvesWithSpace = new List<ItemInstance>();

	private static readonly SocialClass[] SocialClasses = (SocialClass[])Enum.GetValues(typeof(SocialClass));

	private static Func<ItemInstance, BuildingRegistration> GetBuildingRegistrationByItemInstance;

	public static bool PauseAutoFillShelves { get; set; }

	public static void Init(Func<ItemInstance, BuildingRegistration> getBuildingRegistrationByItemInstance = null, bool blueprintCreator = false)
	{
		UpdateItemAddresses();
		ClearPriceCaches();
		GetBuildingRegistrationByItemInstance = getBuildingRegistrationByItemInstance ?? ((Func<ItemInstance, BuildingRegistration>)((ItemInstance x) => BuildingHelper.GetBuildingRegistration(x.AddressCached)));
		sellingMultiplier = ((!blueprintCreator) ? GetSellingMultiplier() : 1f);
	}

	public static float GetSellingMultiplier()
	{
		if (SaveGameManager.Current == null)
		{
			return DifficultySetting.GetDifficultySettings(Difficulty.Normal).sellingMultiplier;
		}
		GameVariables gameVariables = SaveGameManager.Current.gameVariables;
		if (gameVariables.difficulty == Difficulty.Custom && gameVariables.sellingMultiplier > 0f)
		{
			return gameVariables.sellingMultiplier;
		}
		DifficultySetting difficultySettings = DifficultySetting.GetDifficultySettings(gameVariables.difficulty);
		if (difficultySettings != null)
		{
			return difficultySettings.sellingMultiplier;
		}
		return DifficultySetting.GetDifficultySettings(Difficulty.Normal).sellingMultiplier;
	}

	public static ItemInstance InitializeNewInstance(string itemName)
	{
		Item byName = ItemsGetter.GetByName(itemName);
		ItemInstance itemInstance = (((byName.type & ItemType.FactoryMachine) != 0) ? new FactoryWorkstationInstance(itemName) : new ItemInstance(itemName));
		if ((byName.type & (ItemType.PointOfSale | ItemType.ShowcaseShelf)) != 0)
		{
			itemInstance.cargoInstances.Add(new CargoInstance(null, 0, 0f));
		}
		return itemInstance;
	}

	public static CargoInstance GetStockInstance(this ItemInstance itemInstance)
	{
		if (itemInstance.cargoInstances.Count != 1)
		{
			Debug.LogError("Item Instance '" + itemInstance.itemName + "' has wrong cargo instances count " + $"(it's {itemInstance.cargoInstances.Count}, should be 1). \n" + "ID: " + itemInstance.id + ". Address: " + itemInstance.AddressCached.ToFormattedString());
			return new CargoInstance(null, 0, 0f);
		}
		return itemInstance.cargoInstances.First();
	}

	public static bool IsStockCarrier(this Item item)
	{
		if ((item.type & ItemType.PointOfSale) == 0)
		{
			return (item.type & ItemType.ShowcaseShelf) != 0;
		}
		return true;
	}

	public static bool CanRestockItem(this ItemInstance itemInstance)
	{
		if (itemInstance.ItemCached.IsStockCarrier())
		{
			return !string.IsNullOrEmpty(itemInstance.GetStockInstance().itemName);
		}
		return false;
	}

	public static float GetDefaultMarketPrice(string itemName)
	{
		Item byName = ItemsGetter.GetByName(itemName);
		if (!(byName == null))
		{
			return byName.DefaultMarketPrice;
		}
		return 0f;
	}

	[ConsoleMethod("PrintLowestMarketPrice", "Prints lowest price for a product in a specific neighborhood", new string[] { }, AutoCompleteMap = new string[] { "neighborhood=Neighborhoods", "itemName=Items" })]
	public static void PrintLowestMarketPrice(string itemName, string neighborhood)
	{
		Debug.Log("Lowest market price for " + itemName + " in " + neighborhood.GetLocalization() + $" is {GetLowestMarketPrice(itemName, neighborhood)}");
	}

	public static void ClearPriceCaches()
	{
		LmpDictionary.Clear();
		MarketAverageCache.Clear();
		CompetitionHelper.ClearMinimumRivalPrices();
	}

	public static float GetLowestMarketPrice(string itemName, string neighborhood, bool forceUpdate = false)
	{
		if (!forceUpdate && LmpDictionary.TryGetValue((itemName, neighborhood), out var value))
		{
			return value;
		}
		if (forceUpdate)
		{
			LmpDictionary.Remove((itemName, neighborhood));
			MarketAverageCache.Remove((itemName, neighborhood));
		}
		float num = -1f;
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.temporarilyClosed || buildingRegistration.retailPrices == null || buildingRegistration.Neighborhood != neighborhood || string.IsNullOrEmpty(buildingRegistration.BusinessName))
			{
				continue;
			}
			foreach (RetailPrice retailPrice in buildingRegistration.retailPrices)
			{
				if (!(retailPrice.itemName != itemName) && !(retailPrice.price <= 0f) && (buildingRegistration.RentedByPlayer || !(PlayerItemPurchaser.GetShelfFillState(itemName, buildingRegistration) <= 0f)))
				{
					if (num > retailPrice.price || num < 0f)
					{
						num = retailPrice.price;
					}
					break;
				}
			}
		}
		if (num < 0f)
		{
			num = CalculateOptimalPriceByNeighborhood(itemName, neighborhood);
		}
		LmpDictionary.Add((itemName, neighborhood), num);
		return num;
	}

	public static Dictionary<string, ItemInstance> GetItemsByAddress(Address address)
	{
		if (SaveGameManager.Current == null)
		{
			return InstanceBehavior<BuildingManager>.Instance.buildingRegistration.itemInstances;
		}
		if (!(address == null) && !string.IsNullOrEmpty(address.streetName))
		{
			return BuildingHelper.GetBuildingRegistration(address).itemInstances;
		}
		return EmptyItemDictionary;
	}

	public static IEnumerable<Item.ItemCapacity> GetItemsSortedByCapacity(this IEnumerable<ItemInstance> itemInstances, BuildingRegistration registration, bool requireEmployee = false, bool checkMissingRequirements = true)
	{
		BusinessType data = BusinessTypeHelper.GetData(registration);
		List<BusinessRequirement> list = data?.businessRequirements;
		TempItemCapacities.Clear();
		if (list != null)
		{
			FillRequirementsData(list);
		}
		foreach (ItemInstance itemInstance in itemInstances)
		{
			if (itemInstance.ItemCached.addedCustomersPerHour <= 0)
			{
				continue;
			}
			if (RequirementItems.ContainsKey(itemInstance.ItemCached.itemName) || RequirementItemTypesContainsItemType(itemInstance.ItemCached.type))
			{
				if ((!checkMissingRequirements || !HasAnyMissingRequirements(itemInstance)) && (!requireEmployee || !itemInstance.ItemCached.assignable || EmployeeHelper.IsEmployeeStationEmployedAtHour(registration, itemInstance.id, TimeHelper.CurrentHour)))
				{
					AddCapacity(TempItemCapacities, itemInstance.ItemCached, GetCapacityItemName(itemInstance.ItemCached));
				}
			}
			else if ((itemInstance.ItemCached.type & ItemType.ShowcaseShelf) != 0)
			{
				CargoInstance stockInstance = itemInstance.GetStockInstance();
				if (!string.IsNullOrEmpty(stockInstance.itemName) && registration.cachedAvailableProducts.Contains(stockInstance.itemName))
				{
					AddCapacity(TempItemCapacities, itemInstance.ItemCached, stockInstance.itemName);
				}
			}
			else if (data?.suitableBuildingType == "ba:buildingtype_retail" && (itemInstance.ItemCached.type & ItemType.PointOfSale) != 0 && itemInstance.ItemCached.HasTag(TagRef.Itemtag.isfullservicecashregister))
			{
				AddCapacity(TempItemCapacities, itemInstance.ItemCached, itemInstance.ItemCached.itemName);
			}
		}
		RequirementItems.Clear();
		RequirementItemTypes.Clear();
		return TempItemCapacities;
	}

	private static bool RequirementItemTypesContainsItemType(ItemType itemType)
	{
		foreach (KeyValuePair<ItemType, string> requirementItemType in RequirementItemTypes)
		{
			if ((requirementItemType.Key & itemType) != 0)
			{
				return true;
			}
		}
		return false;
	}

	private static void FillRequirementsData(List<BusinessRequirement> businessRequirements)
	{
		foreach (BusinessRequirement businessRequirement in businessRequirements)
		{
			if (businessRequirement.GetItems() != null)
			{
				string[] items = businessRequirement.GetItems();
				foreach (string key in items)
				{
					RequirementItems.TryAdd(key, businessRequirement.GetTodoTaskItemName());
				}
			}
			else if (businessRequirement.GetItemType() != 0)
			{
				RequirementItemTypes.TryAdd(businessRequirement.GetItemType(), businessRequirement.GetTodoTaskItemName());
			}
		}
	}

	private static string GetCapacityItemName(Item item)
	{
		if (RequirementItems.TryGetValue(item.itemName, out var value))
		{
			return value;
		}
		if (!TryGetCapacityNameFromItemType(item.type, out value))
		{
			return string.Empty;
		}
		return value;
	}

	private static bool TryGetCapacityNameFromItemType(ItemType itemType, out string capacityItemName)
	{
		foreach (KeyValuePair<ItemType, string> requirementItemType in RequirementItemTypes)
		{
			if ((requirementItemType.Key & itemType) != 0)
			{
				capacityItemName = requirementItemType.Value;
				return true;
			}
		}
		capacityItemName = string.Empty;
		return false;
	}

	private static void AddCapacity(List<Item.ItemCapacity> itemCapacities, Item item, string capacityItemName)
	{
		foreach (Item.ItemCapacity itemCapacity2 in itemCapacities)
		{
			if (!(itemCapacity2.itemName != capacityItemName))
			{
				itemCapacity2.AddShelf(item.itemName, item.addedCustomersPerHour);
				return;
			}
		}
		Item.ItemCapacity itemCapacity = new Item.ItemCapacity(capacityItemName);
		itemCapacity.AddShelf(item.itemName, item.addedCustomersPerHour);
		itemCapacities.Add(itemCapacity);
	}

	public static bool TryToConsume(this Item item)
	{
		if (Mathf.RoundToInt(SaveGameManager.Current.Hunger) >= 100)
		{
			Notifications.ShowError("itemhelper_notification_you_are_to_stuffed_to_eat_this", "too_stuffed_to_eat");
			return false;
		}
		if (!PlayerHelper.GetAnimator().GetCurrentAnimatorStateInfo(7).IsName("New State"))
		{
			Notifications.Show(NotificationType.Error, "notification_already_in_action", null, 4f, "notification_already_in_action", null, notificationSound: true, trackOnSaveGame: false);
			return false;
		}
		InstanceBehavior<SfxManager>.Instance.PlayAudio(item.consumeSoundType, InstanceBehavior<GameManager>.Instance.playerController.transform.position, 1f, isPlayerCreatedSound: true);
		PlayerHelper.GetAnimator().SetTrigger(item.consumeAnimation);
		InstanceBehavior<GameManager>.Instance.playerController.SetHeldItemToConsume(item.consumeHeldPrefabName);
		InstanceBehavior<GameManager>.Instance.ChangeHunger(item.saturation);
		if (item.energy > 0)
		{
			float num = EnergyHelper.MaxDailyEnergyGeneratedFromConsumables - SaveGameManager.Current.EnergyGeneratedFromConsumables;
			if (num > 0f)
			{
				float num2 = Mathf.Min(item.energy, num);
				SaveGameManager.Current.EnergyGeneratedFromConsumables += num2;
				EnergyHelper.GenerateEnergy(num2);
			}
			else
			{
				Notifications.ShowError("itemhelper_notification_no_more_energy_generation", "itemhelper_notification_no_more_energy_generation");
			}
		}
		return true;
	}

	public static float GetPriceOnCurrentBusiness(string itemName)
	{
		return GetPrice(itemName, InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
	}

	public static float GetPrice(string itemName, BuildingRegistration buildingRegistration)
	{
		foreach (RetailPrice retailPrice in buildingRegistration.retailPrices)
		{
			if (!(retailPrice.itemName != itemName))
			{
				return retailPrice.price;
			}
		}
		return GetDefaultMarketPrice(itemName);
	}

	public static ItemInstance InitializeItemInHandsWithCargo(CargoInstance cargoInstance, string containerName = "ba:itemname_closedcardboardbox")
	{
		if (cargoInstance.ItemCached.HasTag(TagRef.Itemtag.isbag) && cargoInstance.nestedCargoInstances.Count > 0)
		{
			ItemInstance itemInstance = InitializeNewInstance(cargoInstance.itemName);
			cargoInstance.ParseIntoItemInstance(itemInstance);
			return itemInstance;
		}
		if (cargoInstance.ItemCached.HasTag(TagRef.Itemtag.isfuelcontainer))
		{
			ItemInstance itemInstance2 = InitializeNewInstance(cargoInstance.itemName);
			cargoInstance.ParseIntoItemInstance(itemInstance2);
			return itemInstance2;
		}
		ItemInstance itemInstance3 = InitializeNewInstance(containerName);
		itemInstance3.AddToCargo(cargoInstance);
		return itemInstance3;
	}

	[ConsoleMethod("getitem", "Spawn an item in player's hands (if empty)", new string[] { }, AutoCompleteMap = new string[] { "itemName=Items" })]
	public static void Command_GetItem(string itemName)
	{
		Command_GetItem(itemName, 1);
	}

	[ConsoleMethod("getitem", "Spawn an item in player's hands (if empty)", new string[] { }, AutoCompleteMap = new string[] { "itemName=Items" })]
	public static void Command_GetItem(string itemName, int amount)
	{
		if (PlayerHelper.IsHoldingItem)
		{
			Debug.LogError("You'll need empty hands to use this command!");
			return;
		}
		if (PlayerHelper.IsUsingVehicle)
		{
			Debug.LogError("You'll need to be out of a vehicle to use this command!");
			return;
		}
		if (PlacementSystem.IsInPlacementMode)
		{
			Debug.LogError("You'll need to be out of placement mode to use this command!");
			return;
		}
		Item byName = ItemsGetter.GetByName(itemName);
		PlayerHelper.ItemInstanceInHands = InitializeItemInHandsWithCargo(new CargoInstance(itemName, amount, (byName.GetWholesalePrice() != 0f) ? byName.GetWholesalePrice() : byName.DefaultMarketPrice));
	}

	[ConsoleMethod("CloneItem", "Clone the current item in player's hand", new string[] { })]
	public static void Command_CloneItem()
	{
		if (PlacementSystem.CurrentPlaceableItemBeingPlaced is ItemController itemController)
		{
			ItemInstance instance = InitializeItemInHandsWithCargo(itemController.ItemInstance.ConvertToCargoInstance(convertEmptyCargoInstances: true));
			PlayerItemPurchaserSettings playerItemPurchaserSettings = itemController.playerItemPurchaserSettings;
			InstanceBehavior<GameManager>.Instance.StartCoroutine(FinishCloningItem(instance, itemController, playerItemPurchaserSettings));
		}
	}

	[ConsoleMethod("CalculateValueOfItemInHand", "Calculates the value of the item in hand", new string[] { })]
	public static void Command_CalculateValueOfItemInHand()
	{
		if (InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.selectedItemInstance == null)
		{
			Debug.LogError("No item in hand found");
			return;
		}
		string itemName = InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.selectedItemInstance.itemName;
		float worth = InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.selectedItemInstance.GetWorth();
		Debug.Log("The value of " + itemName + " is " + worth.ToCurrencyFormat());
	}

	private static IEnumerator FinishCloningItem(ItemInstance instance, ItemController itemInHands, PlayerItemPurchaserSettings playerItemPurchaserSettings)
	{
		PlacementSystem.RevertPlacement();
		PlacementHelper.CancelPlacementMode();
		yield return null;
		yield return null;
		PlayerHelper.ItemInstanceInHands = instance;
		InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.ClickPlace();
		ItemController obj = PlacementSystem.CurrentPlaceableItemBeingPlaced as ItemController;
		obj.transform.position = itemInHands.transform.position;
		obj.transform.rotation = itemInHands.transform.rotation;
		obj.playerItemPurchaserSettings = new PlayerItemPurchaserSettings
		{
			itemName = playerItemPurchaserSettings.itemName,
			itemQuantity = playerItemPurchaserSettings.itemQuantity,
			enabled = playerItemPurchaserSettings.enabled
		};
	}

	public static ItemController GetItemControllerByID(string id, List<ItemController> itemControllers = null)
	{
		if (itemControllers == null)
		{
			itemControllers = InstanceBehavior<BuildingManager>.Instance.allItemControllers;
		}
		return itemControllers?.Find((ItemController x) => x.ItemInstance?.id == id);
	}

	public static float GetWorth(this ItemInstance itemInstance)
	{
		if (itemInstance == null)
		{
			return 0f;
		}
		float num = ((itemInstance.priceOnPurchase != 0f) ? itemInstance.priceOnPurchase : GetDefaultMarketPrice(itemInstance.itemName));
		if (itemInstance.cargoInstances.Count <= 0)
		{
			return num;
		}
		foreach (CargoInstance cargoInstance in itemInstance.cargoInstances)
		{
			num += cargoInstance.GetWorth();
		}
		return num;
	}

	public static float GetWorth(this CargoInstance cargoInstance)
	{
		float num = cargoInstance.pricePerUnit * (float)cargoInstance.amount;
		foreach (NestedCargoInstance nestedCargoInstance in cargoInstance.nestedCargoInstances)
		{
			num += (float)nestedCargoInstance.amount * nestedCargoInstance.pricePerUnit;
		}
		return num;
	}

	public static float GetSellingPrice(this CargoInstance cargoInstance)
	{
		return cargoInstance.GetWorth() * sellingMultiplier;
	}

	public static float GetSellingPrice(this ItemInstance itemInstance)
	{
		return itemInstance.GetWorth() * sellingMultiplier;
	}

	public static bool HasAnyMissingRequirements(ItemInstance itemInstance)
	{
		if (itemInstance == null || string.IsNullOrEmpty(itemInstance.itemName))
		{
			return false;
		}
		foreach (FurnitureRequirement furnitureRequirement in itemInstance.ItemCached.furnitureRequirements)
		{
			if (!furnitureRequirement.IsRequirementMet(itemInstance))
			{
				return true;
			}
		}
		return false;
	}

	public static List<FurnitureRequirement> GetMissingRequirements(ItemInstance itemInstance)
	{
		if (itemInstance == null || string.IsNullOrEmpty(itemInstance.itemName))
		{
			return new List<FurnitureRequirement>();
		}
		List<FurnitureRequirement> list = new List<FurnitureRequirement>();
		foreach (FurnitureRequirement furnitureRequirement in itemInstance.ItemCached.furnitureRequirements)
		{
			if (!furnitureRequirement.IsRequirementMet(itemInstance))
			{
				list.Add(furnitureRequirement);
			}
		}
		return list;
	}

	public static List<ItemController> GetItemControllersInGroup(ItemController itemController)
	{
		ItemController itemController2 = itemController;
		while (itemController2.parentItemController != null)
		{
			itemController2 = itemController2.parentItemController;
		}
		List<ItemController> itemInGroup = new List<ItemController>(8);
		CollectNodes(itemController2);
		return itemInGroup;
		void CollectNodes(ItemController i)
		{
			itemInGroup.Add(i);
			foreach (ItemController childItemController in i.childItemControllers)
			{
				CollectNodes(childItemController);
			}
		}
	}

	public static bool FitsInBuilding(this Item item)
	{
		if (BuildingManager.isBuildingTemporarilyEditable)
		{
			return true;
		}
		if (InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController != null)
		{
			return InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController.FitsInBuilding(item);
		}
		return BuildingSizeHelper.GetBuildingWallHeight(InstanceBehavior<BuildingManager>.Instance.building.BuildingSize) >= item.buildingHeightRequirement;
	}

	public static bool FitsInBuilding(string itemName)
	{
		return ItemsGetter.GetByName(itemName).FitsInBuilding();
	}

	public static bool AreThereSpecialGiftsInAddress(Address address)
	{
		foreach (ItemInstance value in GetItemsByAddress(address).Values)
		{
			if (string.IsNullOrEmpty(value.itemName))
			{
				continue;
			}
			if (value.ItemCached.isSpecialGift)
			{
				return true;
			}
			foreach (CargoInstance cargoInstance in value.cargoInstances)
			{
				if (!string.IsNullOrEmpty(cargoInstance.itemName) && cargoInstance.ItemCached.isSpecialGift)
				{
					return true;
				}
			}
		}
		return false;
	}

	public static float CalculateOptimalPriceByNeighborhood(string itemName, string neighborhood)
	{
		return CalculateOptimalPriceByNeighborhood(itemName, neighborhood, HasPlayerMonopoly(itemName, neighborhood));
	}

	public static float CalculateOptimalPriceByNeighborhood(string itemName, string neighborhood, bool applyMonopolyBonus)
	{
		float num = CitizenHelper.averagePriceIndicesInNeighborhoods[neighborhood];
		if (applyMonopolyBonus)
		{
			num += 0.29999998f;
		}
		return ItemsGetter.GetByName(itemName).DefaultMarketPrice * num;
	}

	public static float GetMarketReferencePrice(string itemName, string neighborhood)
	{
		return Math.Min(GetDefaultMarketPrice(itemName), GetLowestMarketPrice(itemName, neighborhood));
	}

	public static float CalculateMaxAcceptablePrice(string itemName, string neighborhood, SocialClass socialClass)
	{
		float num = CitizenHelper.MaxAcceptableRelativePrice(socialClass, neighborhood);
		if (ProductMarketHelper.CanNeighborhoodHaveItemDemand(neighborhood, itemName) && HasPlayerMonopoly(itemName, neighborhood))
		{
			num += 0.29999998f;
		}
		return GetMarketReferencePrice(itemName, neighborhood) * num;
	}

	public static float CalculateMaxAcceptablePriceByNeighborhood(string itemName, string neighborhood)
	{
		float num = float.MaxValue;
		SocialClass[] socialClasses = SocialClasses;
		foreach (SocialClass socialClass in socialClasses)
		{
			float num2 = CalculateMaxAcceptablePrice(itemName, neighborhood, socialClass);
			if (num2 < num)
			{
				num = num2;
			}
		}
		return num;
	}

	public static bool HasPlayerMonopoly(string itemName, string neighborhood)
	{
		return ProductMarketHelper.GetNeighborhoodDemand(itemName, neighborhood).hasPlayerMonopoly;
	}

	public static float CalculateMarketAveragePriceByNeighborhood(string itemName, string neighborhood)
	{
		if (MarketAverageCache.TryGetValue((itemName, neighborhood), out var value))
		{
			return value;
		}
		float num = 0f;
		int num2 = 0;
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.Neighborhood != neighborhood || buildingRegistration.temporarilyClosed || buildingRegistration.retailPrices == null)
			{
				continue;
			}
			foreach (RetailPrice retailPrice in buildingRegistration.retailPrices)
			{
				if (retailPrice.itemName != itemName || retailPrice.price <= 0f)
				{
					continue;
				}
				if (buildingRegistration.RentedByPlayer)
				{
					OrderHistoryEntry orderHistoryEntry = buildingRegistration.orderHistory.LastOrDefault();
					if (orderHistoryEntry == null)
					{
						break;
					}
					bool flag = false;
					foreach (OrderHistoryEntry.ItemReport itemSale in orderHistoryEntry.itemSales)
					{
						if (!(itemSale.itemName != retailPrice.itemName))
						{
							if (itemSale.amountSold > 0)
							{
								flag = true;
							}
							break;
						}
					}
					if (!flag)
					{
						break;
					}
				}
				num2++;
				if (buildingRegistration.businessOwnerRivalId.IsSpecialRival())
				{
					SpecialRivalState specialRivalState = RivalsHelper.GetSpecialRivalState(buildingRegistration.businessOwnerRivalId);
					if (specialRivalState?.defenseStates != null)
					{
						bool flag2 = false;
						foreach (DefenseState defenseState in specialRivalState.defenseStates)
						{
							if (defenseState.defensiveMechanic == DefensiveMechanic.PriceReduction && defenseState.affectedItems != null && defenseState.affectedItems.Contains(itemName))
							{
								flag2 = true;
								break;
							}
						}
						if (flag2)
						{
							num += CalculateOptimalPriceByNeighborhood(itemName, neighborhood);
							break;
						}
					}
				}
				num += retailPrice.price;
				break;
			}
		}
		float num3 = ((num2 > 0) ? (num / (float)num2) : CalculateOptimalPriceByNeighborhood(itemName, neighborhood));
		MarketAverageCache.Add((itemName, neighborhood), num3);
		return num3;
	}

	public static void UpdateStockTodoTasks(this ItemInstance itemInstance)
	{
		if (!itemInstance.todoTasksUpdateIsRunning)
		{
			if (ItemsToUpdateToDoTasks.Count == 0)
			{
				CoroutineUtility.RunAfterOneFrame(RunUpdateStockTodoTasks);
			}
			itemInstance.todoTasksUpdateIsRunning = true;
			ItemsToUpdateToDoTasks.Add(itemInstance);
		}
	}

	private static void RunUpdateStockTodoTasks()
	{
		try
		{
			List<TodoTask> lowStockTasks = SaveGameManager.Current.TodoTasks.FindAll((TodoTask x) => x.type == TodoTaskType.LowStock);
			List<TodoTask> emptyStockTasks = SaveGameManager.Current.TodoTasks.FindAll((TodoTask x) => x.type == TodoTaskType.EmptyStock);
			foreach (ItemInstance itemsToUpdateToDoTask in ItemsToUpdateToDoTasks)
			{
				RunUpdateStockTodoTasks(itemsToUpdateToDoTask, lowStockTasks, emptyStockTasks);
			}
			ItemsToUpdateToDoTasks.Clear();
			GameEvent.Invoke("ba:gameevent_itemstockedup");
		}
		catch (Exception ex)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine(ex.Message);
			stringBuilder.AppendLine("RunUpdateStockTodoTasks failed");
			if (ItemsToUpdateToDoTasks.Exists((ItemInstance x) => x == null))
			{
				stringBuilder.AppendLine("There is a null item in ItemsToUpdateToDoTasks");
				ItemInstance itemInstance = ItemsToUpdateToDoTasks.Find((ItemInstance x) => x != null);
				if (itemInstance != null)
				{
					stringBuilder.AppendLine($"Address {itemInstance.AddressCached}");
				}
			}
			else
			{
				stringBuilder.AppendLine($"Address: {ItemsToUpdateToDoTasks[0].streetName} {ItemsToUpdateToDoTasks[0].streetNumber}");
				stringBuilder.AppendLine("Items that were going to be updated:");
				foreach (ItemInstance itemsToUpdateToDoTask2 in ItemsToUpdateToDoTasks)
				{
					stringBuilder.AppendLine(itemsToUpdateToDoTask2.itemName);
				}
			}
			Debug.LogError(stringBuilder);
		}
	}

	private static void RunUpdateStockTodoTasks(ItemInstance itemInstance, List<TodoTask> lowStockTasks, List<TodoTask> emptyStockTasks)
	{
		itemInstance.todoTasksUpdateIsRunning = false;
		if (InstanceBehavior<UIs>.Instance.tasksUI != null)
		{
			InstanceBehavior<UIs>.Instance.tasksUI.forceCheckForCompletedTodoTasks = true;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(itemInstance.AddressCached);
		if (buildingRegistration == null || ((itemInstance.ItemCached.type & ItemType.PointOfSale) != 0 && buildingRegistration.businessTypeName != "ba:businesstype_empty" && !BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.customersneedpaperbags)) || !buildingRegistration.RentedByPlayer || !(buildingRegistration.businessTypeName != "ba:businesstype_empty"))
		{
			return;
		}
		CargoInstance stockInstance = itemInstance.GetStockInstance();
		if (string.IsNullOrEmpty(stockInstance.itemName))
		{
			return;
		}
		int num = stockInstance.amount + BuildingHelper.CountTotalResourcesInStockCached(buildingRegistration, stockInstance.itemName, includeProducers: false, includePalletShelves: false);
		if ((stockInstance.ItemCached.type & ItemType.ServiceProduct) != 0)
		{
			return;
		}
		int maxStockCapacity = stockInstance.GetMaxStockCapacity(itemInstance);
		if (num <= 0)
		{
			TodoTask todoTask = lowStockTasks.Find((TodoTask x) => x.itemInstanceId == itemInstance.id);
			if (todoTask != null)
			{
				InstanceBehavior<UIs>.Instance.tasksUI.InstantlyCompleteTodoTask(todoTask);
			}
			if (!TasksUI.ExistsAlready(TodoTaskType.EmptyStock, itemInstance.AddressCached, stockInstance.itemName, itemInstance.id, null, emptyStockTasks))
			{
				InstanceBehavior<UIs>.Instance.tasksUI.CreateTodoTask(TodoTaskType.EmptyStock, itemInstance.AddressCached, stockInstance.itemName, itemInstance.id, null, Priority.High, 0, 0, null, skipExistsCheck: true);
			}
		}
		else if ((float)num <= (float)maxStockCapacity * 0.25f && !TasksUI.ExistsAlready(TodoTaskType.LowStock, itemInstance.AddressCached, stockInstance.itemName, itemInstance.id, null, lowStockTasks))
		{
			InstanceBehavior<UIs>.Instance.tasksUI.CreateTodoTask(TodoTaskType.LowStock, itemInstance.AddressCached, stockInstance.itemName, itemInstance.id, null, Priority.Medium, 0, 0, null, skipExistsCheck: true);
		}
	}

	public static void FillUpShowcaseShelfOrPointOfSale(this ItemInstance itemInstance)
	{
		if (PauseAutoFillShelves)
		{
			return;
		}
		CargoInstance stockInstance = itemInstance.GetStockInstance();
		if (string.IsNullOrEmpty(stockInstance.itemName))
		{
			return;
		}
		int maxStockCapacity = stockInstance.GetMaxStockCapacity(itemInstance);
		if (stockInstance.amount >= maxStockCapacity)
		{
			return;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(itemInstance.AddressCached);
		if (buildingRegistration == null)
		{
			return;
		}
		bool flag = false;
		ItemInstancesToRemove.Clear();
		foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
		{
			if (!value.ItemCached.HasTag(TagRef.Itemtag.isstockcontainer))
			{
				continue;
			}
			for (int num = value.cargoInstances.Count - 1; num >= 0; num--)
			{
				CargoInstance cargoInstance = value.cargoInstances[num];
				if (!(cargoInstance.itemName != stockInstance.itemName) && cargoInstance.nestedCargoInstances.Count <= 0)
				{
					int amountToMerge = Math.Min(maxStockCapacity - stockInstance.amount, cargoInstance.amount);
					stockInstance.MergeAmount(cargoInstance, amountToMerge);
					value.OnItemsInCargoUpdated()?.Invoke();
					itemInstance.OnItemsInCargoUpdated()?.Invoke();
					flag = true;
					if (cargoInstance.amount == 0)
					{
						value.RemoveFromCargo(cargoInstance);
						if (value.cargoInstances.Count == 0 && value.ItemCached.HasTag(TagRef.Itemtag.discardcontainerwhenempty))
						{
							ItemInstancesToRemove.Add(value);
						}
					}
					if (stockInstance.amount >= maxStockCapacity)
					{
						GameEvent.Invoke("ba:gameevent_itemstockedup");
						foreach (ItemInstance item in ItemInstancesToRemove)
						{
							buildingRegistration.RemoveItemInstanceFromBuilding(item);
						}
						ItemInstancesToRemove.Clear();
						return;
					}
				}
			}
		}
		foreach (ItemInstance item2 in ItemInstancesToRemove)
		{
			buildingRegistration.RemoveItemInstanceFromBuilding(item2);
		}
		ItemInstancesToRemove.Clear();
		foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances)
		{
			if (vehicleInstance.Address != itemInstance.AddressCached || vehicleInstance.id == SaveGameManager.Current.ActiveVehicleId)
			{
				continue;
			}
			for (int num2 = vehicleInstance.cargoInstances.Count - 1; num2 >= 0; num2--)
			{
				CargoInstance cargoInstance2 = vehicleInstance.cargoInstances[num2];
				if (!(cargoInstance2.itemName != stockInstance.itemName) && cargoInstance2.nestedCargoInstances.Count <= 0)
				{
					int amountToMerge2 = Math.Min(maxStockCapacity - stockInstance.amount, cargoInstance2.amount);
					stockInstance.MergeAmount(cargoInstance2, amountToMerge2);
					itemInstance.OnItemsInCargoUpdated()?.Invoke();
					if (cargoInstance2.amount == 0)
					{
						vehicleInstance.RemoveFromCargo(cargoInstance2);
					}
					if (stockInstance.amount >= maxStockCapacity)
					{
						GameEvent.Invoke("ba:gameevent_itemstockedup");
						return;
					}
				}
			}
		}
		if (flag)
		{
			GameEvent.Invoke("ba:gameevent_itemstockedup");
		}
	}

	public static bool ReturnToAShelf(this CargoInstance cargoInstanceToReturn, Address address, ItemInstance currentItemInstance = null)
	{
		ItemInstance itemInstance = null;
		foreach (ItemInstance value in GetItemsByAddress(address).Values)
		{
			if ((currentItemInstance != null && value == currentItemInstance) || (value.ItemCached.type & (ItemType.PointOfSale | ItemType.StorageShelf | ItemType.ShowcaseShelf)) == 0)
			{
				continue;
			}
			foreach (CargoInstance cargoInstance in value.cargoInstances)
			{
				value.MergeCargo(cargoInstanceToReturn, cargoInstance);
				if (cargoInstanceToReturn.amount <= 0)
				{
					return true;
				}
			}
			if (itemInstance == null && value.cargoInstances.Count < value.ItemCached.cargoCapacity)
			{
				itemInstance = value;
			}
		}
		if (itemInstance == null)
		{
			return false;
		}
		itemInstance.AddToCargo(cargoInstanceToReturn);
		return true;
	}

	public static void RemoveFromWorkShifts(this ItemInstance itemInstance, Address initialAddress)
	{
		if (string.IsNullOrEmpty(itemInstance.itemName) || !ItemsGetter.GetByName(itemInstance.itemName).assignable)
		{
			return;
		}
		BuildingRegistration buildingRegistration = SaveGameManager.Current?.BuildingRegistrations?.Find((BuildingRegistration x) => x.Address == initialAddress && x.RentedByPlayer);
		if (buildingRegistration == null)
		{
			return;
		}
		foreach (ScheduleDay scheduleDay in buildingRegistration.scheduleDays)
		{
			scheduleDay.RemoveAllWorkShiftsThatMatchPredicate((WorkShift x) => x.itemInstanceId == itemInstance.id);
		}
	}

	public static List<WorkShift> GetTodayWorkShifts(this ItemInstance itemInstance)
	{
		if (string.IsNullOrEmpty(itemInstance.itemName) || !ItemsGetter.GetByName(itemInstance.itemName).assignable)
		{
			return null;
		}
		BuildingRegistration buildingRegistration = itemInstance.GetBuildingRegistration();
		if (buildingRegistration == null)
		{
			return null;
		}
		List<WorkShift> list = new List<WorkShift>();
		foreach (ScheduleDay scheduleDay in buildingRegistration.scheduleDays)
		{
			if (scheduleDay.day != TimeHelper.GetDayOfWeek())
			{
				continue;
			}
			foreach (WorkShift workShift in scheduleDay.workShifts)
			{
				if (workShift.itemInstanceId == itemInstance.id)
				{
					list.Add(workShift);
				}
			}
		}
		return list;
	}

	public static bool SubtractFromStock(this ItemInstance itemInstance, int amount = 1)
	{
		if (!itemInstance.ItemCached.producerSettings.ResourcesRequired)
		{
			return true;
		}
		CargoInstance stockInstance = itemInstance.GetStockInstance();
		if (stockInstance == null || string.IsNullOrEmpty(stockInstance.itemName))
		{
			return false;
		}
		if (stockInstance.amount >= amount)
		{
			stockInstance.amount -= amount;
			if (stockInstance.amount <= 0)
			{
				ReStockingHelper.RefillSingleItemWithLimit(itemInstance);
			}
			else
			{
				itemInstance.OnItemsInCargoUpdated()?.Invoke();
			}
			return true;
		}
		return false;
	}

	public static Sprite GetIcon(string itemName)
	{
		if (ItemIconsCache.TryGetValue(itemName, out var value))
		{
			return value;
		}
		Sprite sprite = AddressableResolver.LoadAssetAsync<Sprite>(PlayerHelper.GetItemNameIconPath(itemName)).WaitForCompletion();
		ItemIconsCache.Add(itemName, sprite);
		return sprite;
	}

	public static Sprite GetIconWithFallback(string itemName, string fallbackItemName = "ba:itemname_closedcardboardbox")
	{
		if (!AddressableResolver.IsValidAddressableKey(PlayerHelper.GetItemNameIconPath(itemName)))
		{
			return GetIcon(fallbackItemName);
		}
		Sprite icon = GetIcon(itemName);
		if (!icon)
		{
			return GetIcon(fallbackItemName);
		}
		return icon;
	}

	public static bool HasItemStorageForProduct(this ItemInstance itemInstance, string productName)
	{
		if (itemInstance.cargoInstances.Count < itemInstance.ItemCached.cargoCapacity)
		{
			return true;
		}
		foreach (CargoInstance cargoInstance in itemInstance.cargoInstances)
		{
			if (cargoInstance.itemName == productName && cargoInstance.amount < cargoInstance.ItemCached.boxSize)
			{
				return true;
			}
		}
		return false;
	}

	public static bool IsAvailableInCurrentSeason(this Item item)
	{
		if (item.season != SeasonName.None)
		{
			if (PlayerPrefSettings.SeasonalDecorations)
			{
				return item.season == SeasonHelper.CurrentSeasonName;
			}
			return false;
		}
		return true;
	}

	public static float GetWholesalePrice(this Item item)
	{
		return item.wholesalePrice * (SaveGameManager.Current?.productMarketEntries.FirstOrDefault((ProductMarketEntry x) => x.itemName == item.itemName)?.importPriceIndex ?? 1f) * (SaveGameManager.Current?.gameVariables.marketPriceMultiplier ?? 1f);
	}

	public static string GetConsumeActionLocalizationKey(this Item item)
	{
		return "action_" + item.consumeAnimation.ToStringFast();
	}

	public static void DeliverCargoToBuilding(CargoInstance cargoToDeliver, BuildingRegistration buildingRegistration, Func<ItemInstance, bool> filter = null)
	{
		PalletShelvesWithSpace.Clear();
		foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
		{
			if (filter != null && !filter(value))
			{
				continue;
			}
			foreach (CargoInstance cargoInstance2 in value.cargoInstances)
			{
				value.MergeCargo(cargoToDeliver, cargoInstance2);
				if (cargoToDeliver.amount <= 0)
				{
					return;
				}
			}
			if (value.cargoInstances.Count < value.ItemCached.cargoCapacity)
			{
				PalletShelvesWithSpace.Add(value);
			}
		}
		if (cargoToDeliver.amount <= 0)
		{
			return;
		}
		foreach (ItemInstance item in PalletShelvesWithSpace)
		{
			while (item.cargoInstances.Count < item.ItemCached.cargoCapacity)
			{
				int num = ((cargoToDeliver.amount > cargoToDeliver.ItemCached.boxSize) ? cargoToDeliver.ItemCached.boxSize : cargoToDeliver.amount);
				CargoInstance cargoInstance = new CargoInstance(cargoToDeliver.itemName, num, cargoToDeliver.pricePerUnit);
				item.AddToCargo(cargoInstance);
				cargoToDeliver.amount -= num;
				if (cargoToDeliver.amount <= 0)
				{
					return;
				}
			}
		}
	}

	public static List<string> GetRetailProductItemNames()
	{
		List<string> list = new List<string>();
		foreach (Item allItem in ItemsGetter.AllItems)
		{
			if ((allItem.type & ItemType.RetailProduct) != 0)
			{
				list.Add(allItem.itemName);
			}
		}
		return list;
	}

	public static BuildingRegistration GetBuildingRegistration(this ItemInstance itemInstance)
	{
		return GetBuildingRegistrationByItemInstance?.Invoke(itemInstance);
	}

	public static bool IsAiShelfEmpty(ItemController itemController, BuildingRegistration buildingRegistration)
	{
		if (itemController == null || buildingRegistration.RentedByPlayer)
		{
			return false;
		}
		if (itemController is ShelfController shelfController)
		{
			return shelfController.fillState <= 0.0;
		}
		return false;
	}

	public static bool IsItemSoldInBuildingType(string itemName, string buildingType)
	{
		HashSet<string> value;
		if (ItemsSoldInBuildingType.Count != 0)
		{
			if (ItemsSoldInBuildingType.TryGetValue(buildingType, out value))
			{
				return value.Contains(itemName);
			}
			return false;
		}
		foreach (string key in BuildingTypeHelper.BuildingTypes.Keys)
		{
			FillItemsSoldInBusinessTypeList(key);
		}
		if (ItemsSoldInBuildingType.TryGetValue(buildingType, out value))
		{
			return value.Contains(itemName);
		}
		return false;
	}

	private static void FillItemsSoldInBusinessTypeList(string buildingType)
	{
		foreach (BusinessType allPlayerAvailableBusiness in BusinessTypeHelper.GetAllPlayerAvailableBusinesses())
		{
			if (allPlayerAvailableBusiness.suitableBuildingType != buildingType)
			{
				continue;
			}
			ItemsSoldInBuildingType.TryGetValue(buildingType, out var value);
			if (value == null)
			{
				value = new HashSet<string>();
				ItemsSoldInBuildingType.Add(buildingType, value);
			}
			foreach (string primaryProduct in allPlayerAvailableBusiness.GetPrimaryProducts())
			{
				value.Add(primaryProduct);
			}
		}
	}

	public static void Discard(this ItemInstance item)
	{
		if (!item.AddressCached.IsUndefined())
		{
			item.RemoveFromWorkShifts(item.AddressCached);
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RemoveItemInstanceFromBuilding(item);
		}
		else
		{
			PlayerHelper.ItemInstanceInHands = null;
		}
		if (BuildingManager.IsInsideBuilding)
		{
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.UpdateSecurityLevel();
			BusinessSecurityHelper.UpdateCamerasCoverage(InstanceBehavior<BuildingManager>.Instance.buildingRegistration?.Address);
		}
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			GlobalEvents.onItemDiscarded?.Invoke(item);
			if (PlayerHelper.IsUsingVehicle)
			{
				InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVehicle(VehicleHelper.GetCurrentVehicleBase());
			}
		});
	}

	public static string GetCargoName(this ICargoHolder cargoHolder, bool isKey = false)
	{
		if (cargoHolder is ItemInstance itemInstance)
		{
			if (!isKey)
			{
				return itemInstance.itemName.GetLocalization();
			}
			return itemInstance.itemName;
		}
		if (cargoHolder is VehicleInstance vehicleInstance)
		{
			if (!isKey)
			{
				return vehicleInstance.vehicleTypeName.GetLocalization();
			}
			return vehicleInstance.vehicleTypeName;
		}
		return string.Empty;
	}

	public static int GetCargoSpace(this ICargoHolder cargoHolder)
	{
		return cargoHolder.GetMaxCargoSize() - cargoHolder.GetCargoInstances().Count;
	}

	public static ItemInstance InitializeNewInstance(this CargoInstance cargoInstanceToPlace)
	{
		ItemInstance itemInstance = InitializeNewInstance(cargoInstanceToPlace.itemName);
		if (string.IsNullOrEmpty(itemInstance.worldSpaceTextValue) && !string.IsNullOrEmpty(itemInstance.ItemCached.defaultWorldSpaceKey))
		{
			itemInstance.worldSpaceTextValue = itemInstance.ItemCached.defaultWorldSpaceKey.GetLocalization();
		}
		return cargoInstanceToPlace.ParseIntoItemInstance(itemInstance);
	}

	public static bool CanAddCargo(this ItemInstance cargoInstance)
	{
		return cargoInstance.cargoInstances.Count < cargoInstance.ItemCached.cargoCapacity;
	}

	private static void UpdateItemAddresses()
	{
		if (SaveGameManager.Current == null)
		{
			return;
		}
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.itemInstances == null)
			{
				continue;
			}
			foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
			{
				value.streetName = buildingRegistration.StreetName;
				value.streetNumber = buildingRegistration.StreetNumber;
			}
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		ItemsToUpdateToDoTasks.Clear();
		PauseAutoFillShelves = false;
		ItemIconsCache.Clear();
		ItemsSoldInBuildingType.Clear();
		GetBuildingRegistrationByItemInstance = null;
	}
}
