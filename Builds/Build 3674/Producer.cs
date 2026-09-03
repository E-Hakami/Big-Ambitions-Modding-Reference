using System.Collections.Generic;
using System.Linq;
using AI.Customers.CustomerEntries;
using BigAmbitions.Items;
using BigAmbitions.SoundSystem;
using BigAmbitions.Tags;
using Helpers;
using Player.HUD.ItemWarningIcons;
using UI.Notification;
using UnityEngine;

public class Producer : ItemController
{
	public bool CanAddAnyToInventory(IEnumerable<CargoInstance> cargoInstancesToAdd)
	{
		foreach (CargoInstance item in cargoInstancesToAdd)
		{
			if (CanAddToInventory(item))
			{
				return true;
			}
		}
		return false;
	}

	public bool CanAddToInventory(CargoInstance cargoInstance)
	{
		if (string.IsNullOrEmpty(cargoInstance.itemName))
		{
			return false;
		}
		if (this is DeliverySpot)
		{
			return false;
		}
		if (this is PalletController palletController)
		{
			return !palletController.IsFull();
		}
		if (this is StorageShelfController storageShelfController)
		{
			return !storageShelfController.IsFull();
		}
		if (!base.Item.itemsThatCanShowcase.Contains(cargoInstance.itemName))
		{
			return false;
		}
		CargoInstance stockInstance = base.ItemInstance.GetStockInstance();
		if (string.IsNullOrEmpty(stockInstance.itemName))
		{
			return true;
		}
		if (stockInstance.itemName != cargoInstance.itemName)
		{
			return false;
		}
		return stockInstance.amount < stockInstance.GetMaxStockCapacity(base.ItemInstance);
	}

	public override void Start()
	{
		base.Start();
		PlayerItemPurchaserSettings obj = playerItemPurchaserSettings;
		if ((obj == null || !obj.enabled) && !(base.BuildingContext.Building.BuildingType == "ba:buildingtype_residential"))
		{
			InstanceBehavior<BuildingManager>.Instance.ScheduleUpdateAvailableProducers();
			itemWarningIconOffset = new Vector3(0f, 0.8f, 0f);
		}
	}

	public override bool Interact()
	{
		if (base.Interact())
		{
			return true;
		}
		PlayerItemPurchaserSettings obj = playerItemPurchaserSettings;
		if (obj != null && obj.enabled)
		{
			return false;
		}
		if (!base.BuildingContext.Registration.RentedByPlayer)
		{
			if (!(base.BuildingContext.BusinessType == null))
			{
				CustomerType customerType = base.BuildingContext.BusinessType.customerType;
				if (customerType == CustomerType.SelfService || customerType == CustomerType.CinemaTheater)
				{
					goto IL_0060;
				}
			}
			return false;
		}
		goto IL_0060;
		IL_0060:
		if (base.BuildingContext.Registration.RentedByPlayer && ((base.ItemInstance.ItemCached.type & ItemType.PointOfSale) != 0 || !PlayerHelper.IsHoldingItem || !PlayerHelper.IsHoldingShoppingBasket))
		{
			ICargoHolder cargoHolder;
			if (!PlayerHelper.IsHoldingItem)
			{
				ICargoHolder currentVehicle = VehicleHelper.GetCurrentVehicle();
				cargoHolder = currentVehicle;
			}
			else
			{
				ICargoHolder currentVehicle = PlayerHelper.ItemInstanceInHands;
				cargoHolder = currentVehicle;
			}
			ICargoHolder cargoHolder2 = cargoHolder;
			if (cargoHolder2 != null && cargoHolder2.GetCargoInstances().Count > 0)
			{
				if ((base.ItemInstance.ItemCached.type & ItemType.PointOfSale) != 0 && !BuildingHelper.CustomersNeedPaperBagsInCurrentBuilding())
				{
					Notifications.Show(NotificationType.Error, "cash_register_notifications_no_paperbags_needed_for_businesstype");
					return true;
				}
				CargoInstance stockInstance = base.ItemInstance.GetStockInstance();
				List<CargoInstance> cargoInstances = cargoHolder2.GetCargoInstances();
				if (!string.IsNullOrEmpty(stockInstance.itemName))
				{
					if (cargoInstances.All((CargoInstance x) => x.itemName != stockInstance.itemName))
					{
						Dictionary<string, string> notificationData = new Dictionary<string, string> { 
						{
							"name",
							LocalizationHelper.GetItemLabel(stockInstance.itemName).ToString()
						} };
						Notifications.Show(NotificationType.Error, "producer_notification_already_holding", notificationData);
						return true;
					}
					int maxStockCapacity = stockInstance.GetMaxStockCapacity(base.ItemInstance);
					if (stockInstance.amount >= maxStockCapacity)
					{
						Dictionary<string, string> notificationData2 = new Dictionary<string, string> { 
						{
							"name",
							LocalizationHelper.GetItemLabel(itemName).ToString()
						} };
						Notifications.Show(NotificationType.Error, "producer_notification_already_full", notificationData2);
						return true;
					}
				}
				else
				{
					CargoInstance cargoInstance = cargoInstances.FirstOrDefault((CargoInstance x) => base.Item.itemsThatCanShowcase.Any((string y) => y == x.itemName));
					if (cargoInstance == null)
					{
						Notifications.ShowError("producer_notification_resource_not_fitting");
						return true;
					}
					stockInstance.itemName = cargoInstance.itemName;
					stockInstance.ResetItemCached();
				}
				ICargoHolder.TryToMergeAndMoveCargoBetweenHolders(cargoHolder2, base.ItemInstance);
				GameEvent.Invoke("ba:gameevent_itemstockedup");
				base.ItemInstance.OnItemsInCargoUpdated()?.Invoke();
				BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(base.ItemInstance.AddressCached);
				BusinessHelper.UpdateCustomerCapacity(buildingRegistration);
				BusinessHelper.UpdatePromotion(buildingRegistration);
				CustomerEntriesHelper.UpdateCustomerEntriesForPlayerBusiness(buildingRegistration, TimeHelper.GetDayOfWeek());
				buildingRegistration.UpdateSecurityLevel();
				GlobalEvents.onBuildingRegistrationChange?.Invoke(buildingRegistration.Address);
				return true;
			}
		}
		string producedItemName = GetProducedItemName();
		if (string.IsNullOrEmpty(producedItemName))
		{
			return false;
		}
		Item byName = ItemsGetter.GetByName(producedItemName);
		bool flag = byName?.HasTag(TagRef.Itemtag.isshoppingcontainer) ?? false;
		if (!string.IsNullOrEmpty(SaveGameManager.Current.ActiveVehicleId) & flag)
		{
			return false;
		}
		if ((base.Item.type & ItemType.EmployeeWorkstation) == 0)
		{
			bool isHoldingShoppingBasket = PlayerHelper.IsHoldingShoppingBasket;
			if (byName != null && (byName.type & ItemType.RetailProduct) != 0 && !isHoldingShoppingBasket)
			{
				return false;
			}
			if (!flag && (PlayerHelper.ItemInstanceInHands == null || (PlayerHelper.ItemInstanceInHands != null && !isHoldingShoppingBasket)))
			{
				return false;
			}
			if (PlayerHelper.ItemInstanceInHands != null && byName != null)
			{
				if (flag)
				{
					Notifications.ShowError("playeritempurchaser_notification_hands_full");
					return false;
				}
				Item itemInHands = PlayerHelper.ItemInHands;
				if ((object)itemInHands != null && itemInHands.HasTag(TagRef.Itemtag.isbag))
				{
					Notifications.ShowError("itempanelui_notification_cant_add_to_temp_container");
					return false;
				}
				if (isHoldingShoppingBasket && (byName.type & ItemType.RetailProduct) == 0)
				{
					Notifications.ShowError("itempanelui_notification_cant_put_non_quanitity_items_into_temp_container");
					return false;
				}
				if (isHoldingShoppingBasket)
				{
					if (PlayerHelper.ItemInstanceInHands.ItemCached.cargoCapacity <= PlayerHelper.ItemInstanceInHands.cargoInstances.Count)
					{
						return false;
					}
					InstanceBehavior<SfxManager>.Instance?.PlayAudio(SoundType.AddProductToBasket, InstanceBehavior<GameManager>.Instance.playerController.transform.position);
				}
			}
			if (!base.BuildingContext.IsPlayerOwnedBusiness || base.ItemInstance.SubtractFromStock())
			{
				GrabItem();
				return true;
			}
		}
		return false;
	}

	protected virtual void GrabItem()
	{
		ItemInstance itemInstance = ItemHelper.InitializeNewInstance(GetProducedItemName());
		itemInstance.customColors = customColors;
		PlayerHelper.ItemInstanceInHands = itemInstance;
		EnergyHelper.SpentEnergyOnce(EnergyConsumption.Low);
	}

	public void SetStockAmount()
	{
		if (base.BuildingContext.IsPlayerOwnedBusiness && !base.Item.itemsThatCanShowcase.All(delegate(string x)
		{
			Item byName = ItemsGetter.GetByName(x);
			return byName != null && !byName.HasTag(TagRef.Itemtag.isbag) && (byName.type & ItemType.RetailProduct) == 0;
		}) && ((base.ItemInstance.ItemCached.type & ItemType.PointOfSale) == 0 || BuildingHelper.CustomersNeedPaperBagsInCurrentBuilding()) && ShouldUpdateStockState())
		{
			UpdateSelectedStockOverlay();
		}
	}

	protected bool ShouldUpdateStockState()
	{
		return (base.Item.type & (ItemType.EmployeeWorkstation | ItemType.ShowcaseShelf)) != 0;
	}

	public override WarningIconType GetWarningIconType()
	{
		WarningIconType warningIconType = base.GetWarningIconType();
		if (warningIconType != WarningIconType.None)
		{
			return warningIconType;
		}
		if (base.Item.itemsThatCanShowcase.Length == 0)
		{
			return WarningIconType.None;
		}
		if ((base.Item.type & ItemType.PointOfSale) != 0 && (base.BuildingContext.BusinessType == null || !InstanceBehavior<BuildingManager>.Instance.businessType.HasTag(TagRef.Businesstag.customersneedpaperbags)))
		{
			return WarningIconType.None;
		}
		if (base.ItemInstance == null)
		{
			return WarningIconType.None;
		}
		CargoInstance stockInstance = base.ItemInstance.GetStockInstance();
		if (string.IsNullOrEmpty(stockInstance.itemName))
		{
			return WarningIconType.VeryLowStock;
		}
		int maxStockCapacity = stockInstance.GetMaxStockCapacity(base.ItemInstance);
		if (stockInstance.amount <= maxStockCapacity / 2)
		{
			if (stockInstance.amount > maxStockCapacity / 4)
			{
				return WarningIconType.LowStock;
			}
			return WarningIconType.VeryLowStock;
		}
		return WarningIconType.None;
	}
}
