using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Helpers;
using Localizor;
using Player.HUD.ItemInfoOverlays;
using UI.Notification;

namespace Items.SpecialItems;

public class ShowcaseShelfController : ShelfController
{
	public override bool IsFull()
	{
		CargoInstance stockInstance = base.ItemInstance.GetStockInstance();
		if (string.IsNullOrEmpty(stockInstance.itemName))
		{
			return false;
		}
		return stockInstance.amount >= stockInstance.GetMaxStockCapacity(base.ItemInstance);
	}

	public override void UpdateVisuals()
	{
		CargoInstance stockInstance = base.ItemInstance.GetStockInstance();
		if (!string.IsNullOrEmpty(stockInstance.itemName))
		{
			if (base.BuildingContext.IsPlayerOwnedBusiness && stockInstance.amount <= 0)
			{
				base.ItemInstance.FillUpShowcaseShelfOrPointOfSale();
			}
			ShowItemVisuals(stockInstance.itemName, showDefault: false);
			UpdateFillState((double)stockInstance.amount / (double)stockInstance.GetMaxStockCapacity(base.ItemInstance));
		}
		else
		{
			UpdateFillState(0.0);
		}
		SetStockAmount();
	}

	protected override void GrabItem()
	{
		CargoInstance stockInstance = base.ItemInstance.GetStockInstance();
		CargoInstance cargoInstance = new CargoInstance(stockInstance.itemName, 1, base.BuildingContext.IsPlayerOwnedBusiness ? 0f : stockInstance.pricePerUnit, paid: false, customColors?.Select((CustomColor x) => x.Copy()).ToList());
		PlayerHelper.ItemInstanceInHands.AddToCargo(cargoInstance);
		EnergyHelper.SpentEnergyOnce(EnergyConsumption.Low);
	}

	public void WalkToTakeItem(string itemToBuy)
	{
		InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
		if (VehicleHelper.IsInsideVehicle())
		{
			Notifications.ShowError("notification_must_exit_vehicle_before_action");
			return;
		}
		CargoInstance stockInstance = base.ItemInstance.GetStockInstance();
		if (stockInstance == null || stockInstance.itemName != itemToBuy || stockInstance.amount < 1)
		{
			Notifications.ShowError("wholesale_store_item_out_of_stock");
			return;
		}
		MoveTowardsEntity(delegate
		{
			TakeItem(itemToBuy);
		});
	}

	private void TakeItem(string itemToBuy)
	{
		if (PlayerHelper.IsHoldingItem)
		{
			Notifications.ShowError("notification_need_empty_hands_to_interact");
			return;
		}
		CargoInstance stockInstance = base.ItemInstance.GetStockInstance();
		if (stockInstance == null || stockInstance.itemName != itemToBuy || stockInstance.amount < 1)
		{
			Notifications.ShowError("wholesale_store_item_out_of_stock");
			return;
		}
		float num = (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness ? 0f : ItemHelper.GetDefaultMarketPrice(itemToBuy));
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			Dictionary<string, string> data = new Dictionary<string, string> { 
			{
				"itemName",
				itemToBuy.GetLocalization()
			} };
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_purchasefromsellerstand", data);
			if (!GameManager.ChangeMoneySafe(0f - num, transactionInfo, null, null, force: false, showNotification: true))
			{
				return;
			}
		}
		if (!base.ItemInstance.SubtractFromStock())
		{
			Notifications.ShowError("wholesale_store_item_out_of_stock");
			return;
		}
		HandTruck componentInChildren = InstanceBehavior<GameManager>.Instance.playerController.GetComponentInChildren<HandTruck>();
		if ((bool)componentInChildren)
		{
			componentInChildren.ExitVehicle();
		}
		ItemInstance itemInstance = new ItemInstance(ItemsGetter.GetRandomBag());
		CargoInstance cargoInstance = new CargoInstance(itemToBuy, 1, num);
		itemInstance.AddToCargo(cargoInstance);
		PlayerHelper.ItemInstanceInHands = itemInstance;
		GameEvent.Invoke("ba:gameevent_purchasecompleted");
	}
}
