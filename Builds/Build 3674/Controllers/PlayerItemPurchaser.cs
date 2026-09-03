using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Rivals;
using BigAmbitions.SoundSystem;
using BigAmbitions.Tags;
using Buildings;
using Entities;
using Helpers;
using Localizor;
using UI.Notification;
using UnityEngine;

namespace Controllers;

public class PlayerItemPurchaser
{
	private float _pricePerUnit;

	public ItemController itemController;

	public float TotalPrice { get; private set; }

	public void UpdatePriceInfo()
	{
		if (itemController.playerItemPurchaserSettings == null)
		{
			return;
		}
		if (string.IsNullOrEmpty(itemController.playerItemPurchaserSettings.itemName) && itemController.playerItemPurchaserSettings.enabled)
		{
			itemController.playerItemPurchaserSettings.itemName = itemController.itemName;
		}
		BuildingRegistration registration = itemController.BuildingContext.Registration;
		BusinessType data = BusinessTypeHelper.GetData(registration);
		if ((object)data == null || data.customerType != CustomerType.None)
		{
			bool flag = registration.businessTypeName == "ba:businesstype_wholesalestore";
			int priceIndex = registration.GetPriceIndex();
			Item byName = ItemsGetter.GetByName(itemController.playerItemPurchaserSettings.itemName);
			_pricePerUnit = (flag ? (byName.GetWholesalePrice() * ProductMarketHelper.GetProductMarketEventMultiplier(byName.itemName, registration.Neighborhood)) : ItemHelper.GetPriceOnCurrentBusiness(byName.itemName)) * ((float)priceIndex / 100f);
			TotalPrice = _pricePerUnit * (float)((!itemController.playerItemPurchaserSettings.isQuantityItem) ? 1 : byName.boxSize);
			if (!registration.RentedByPlayer && itemController is ShelfController shelfController)
			{
				float shelfFillState = GetShelfFillState(byName.itemName, registration);
				shelfController.ShowItemVisuals(byName.itemName);
				shelfController.UpdateFillState(shelfFillState);
			}
		}
	}

	public static float GetShelfFillState(string itemName, BuildingRegistration registration)
	{
		if (registration.RentedByPlayer)
		{
			return 1f;
		}
		float result = 1f;
		if (registration.businessOwnerRivalId.IsSpecialRival())
		{
			return result;
		}
		if (registration.BuildingCached.SpecialService?.settings is WholesaleStoreSettings)
		{
			result = ((!ProductMarketHelper.IsProductInMarketEvent(itemName, MarketEventType.ProductBackorder, registration.Neighborhood, ignoreToday: true)) ? (ProductMarketHelper.IsProductInMarketEvent(itemName, MarketEventType.ProductShortage, registration.Neighborhood, ignoreToday: true) ? Random.Range(0.25f, 0.4f) : 1f) : 0f);
		}
		else
		{
			AiBusinessGoodsSource? aiBusinessGoodsSource = ((registration.BuildingCached.SpecialService != null) ? new AiBusinessGoodsSource?(registration.BuildingCached.SpecialService.businessGoodsSource) : CompetitionHelper.GetBusinessDefault(registration.BusinessName)?.goodsSource);
			if (aiBusinessGoodsSource == AiBusinessGoodsSource.Wholesale)
			{
				if (ProductMarketHelper.IsProductInMarketEvent(itemName, MarketEventType.ProductBackorder, registration.Neighborhood, ignoreToday: true))
				{
					result = 0f;
				}
				else
				{
					MarketEvent marketEvent = ProductMarketHelper.GetMarketEvent(itemName, MarketEventType.ProductShortage, registration.Neighborhood, ignoreToday: true);
					if (marketEvent == null)
					{
						return result;
					}
					result = Random.Range(0.25f, 0.4f);
					if (ProductMarketHelper.ShouldShortageMakeShelvesBeEmpty(marketEvent))
					{
						result = 0f;
					}
				}
			}
			else if (aiBusinessGoodsSource == AiBusinessGoodsSource.Import)
			{
				if (ProductMarketHelper.IsProductInMarketEvent(itemName, MarketEventType.ProductBackorder, string.Empty, ignoreToday: true))
				{
					result = 0f;
				}
				else
				{
					MarketEvent marketEvent2 = ProductMarketHelper.GetMarketEvent(itemName, MarketEventType.ProductShortage, string.Empty, ignoreToday: true);
					if (marketEvent2 == null)
					{
						return result;
					}
					result = Random.Range(0.25f, 0.4f);
					if (ProductMarketHelper.ShouldShortageMakeShelvesBeEmpty(marketEvent2))
					{
						result = 0f;
					}
				}
			}
		}
		return result;
	}

	public void GrabItem()
	{
		BusinessType data = BusinessTypeHelper.GetData(itemController.BuildingContext.Registration);
		if (data.customerType == CustomerType.None)
		{
			return;
		}
		if (IsShelfEmpty(itemController))
		{
			Notifications.ShowError("wholesale_store_item_out_of_stock", "itemOutOfStock");
			return;
		}
		CustomerType customerType = data.customerType;
		if (customerType == CustomerType.FullService || customerType == CustomerType.Hairdresser || customerType == CustomerType.Nightclub || customerType == CustomerType.Casino)
		{
			Notifications.ShowError("playeritempurchaser_notification_require_interaction_with_cashier", "InteractionWithCashierRequired");
			return;
		}
		Item byName = ItemsGetter.GetByName(itemController.playerItemPurchaserSettings.itemName);
		int amount = ((!itemController.playerItemPurchaserSettings.isQuantityItem) ? 1 : byName.boxSize);
		CargoInstance cargoInstance = new CargoInstance(byName.itemName, amount, _pricePerUnit, paid: false);
		if (itemController.itemName == cargoInstance.itemName)
		{
			cargoInstance.customColors = new List<CustomColor>();
			List<CustomColor> customColors = itemController.customColors;
			if (customColors != null && customColors.Count > 0)
			{
				cargoInstance.customColors.AddRange(itemController.customColors.Select((CustomColor x) => x.Copy()));
			}
		}
		if (data.HasTag(TagRef.Businesstag.customersneedshoppingcontainer))
		{
			if (!PlayerHelper.IsHoldingItem || !PlayerHelper.IsHoldingShoppingBasket)
			{
				Notifications.ShowError("playeritempurchaser_notification_require_shoppingbasket", "ShoppingContainerRequired");
				return;
			}
			PlayerHelper.ItemInstanceInHands.MergeIntoCargo(cargoInstance);
			if (cargoInstance.amount > 0 && !PlayerHelper.ItemInstanceInHands.TryToAddToCargo(cargoInstance))
			{
				Notifications.ShowError("playeritempurchaser_notification_full_shoppingbasket", "ShoppingContainerFull");
				return;
			}
		}
		else if (PlayerHelper.IsUsingVehicle)
		{
			VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
			if (!currentVehicle.TryToAddToCargo(cargoInstance))
			{
				string value = currentVehicle.vehicleTypeName.Localize().ToString();
				Dictionary<string, string> notificationData = new Dictionary<string, string> { { "type", value } };
				Notifications.Show(NotificationType.Error, "managecargo_notification_vehicle_full", notificationData, 4f, "VehicleIsFull");
				return;
			}
			currentVehicle.OnItemsInCargoUpdated()?.Invoke();
		}
		else if (data.customerType == CustomerType.Gym)
		{
			BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
			Dictionary<string, string> data2 = new Dictionary<string, string> { { "businessName", buildingRegistration.BusinessName } };
			bool num = !buildingRegistration.businessOwnerRivalId.IsSpecialRival() && (buildingRegistration.BuildingCached.SpecialService?.hasTaxDeductiblePurchases ?? false);
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_itempurchase", data2);
			if (num)
			{
				transactionInfo.SetTaxDeductibleName(buildingRegistration.BusinessName);
			}
			GameManager.ChangeMoneySafe(0f - TotalPrice, transactionInfo);
			cargoInstance.paid = true;
			if (PlayerHelper.IsHoldingItem)
			{
				if (!PlayerHelper.ItemInstanceInHands.TryToAddToCargo(cargoInstance))
				{
					Notifications.ShowError("playeritempurchaser_notification_hands_full", "HandsNotEmpty");
					return;
				}
				PlayerHelper.ItemInstanceInHands.OnItemsInCargoUpdated()?.Invoke();
			}
			else
			{
				PlayerHelper.ItemInstanceInHands = ItemHelper.InitializeItemInHandsWithCargo(cargoInstance, "ba:itemname_paperbag");
			}
		}
		else if (PlayerHelper.IsHoldingItem)
		{
			if (!PlayerHelper.ItemInstanceInHands.TryToAddToCargo(cargoInstance))
			{
				Notifications.ShowError("playeritempurchaser_notification_hands_full", "HandsNotEmpty");
				return;
			}
			PlayerHelper.ItemInstanceInHands.OnItemsInCargoUpdated()?.Invoke();
		}
		else
		{
			string businessTypeName = data.businessTypeName;
			string containerName = ((businessTypeName == "ba:businesstype_cinema" || businessTypeName == "ba:businesstype_theater") ? "ba:itemname_paperbag" : "ba:itemname_closedcardboardbox");
			PlayerHelper.ItemInstanceInHands = ItemHelper.InitializeItemInHandsWithCargo(cargoInstance, containerName);
		}
		InstanceBehavior<SfxManager>.Instance?.PlayAudio(SoundType.AddProductToBasket, PlayerHelper.GetPosition());
		EnergyHelper.SpentEnergyOnce(EnergyConsumption.Low);
	}

	private static bool IsShelfEmpty(ItemController itemController)
	{
		if (!(itemController is ShelfController shelfController))
		{
			return false;
		}
		return shelfController.fillState == 0.0;
	}
}
