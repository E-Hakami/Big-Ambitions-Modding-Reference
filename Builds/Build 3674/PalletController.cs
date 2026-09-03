using System.Collections.Generic;
using Helpers;
using UI;
using UI.Notification;
using UI.Purchase;

public class PalletController : ShelfController
{
	public override void Start()
	{
		base.Start();
		if (!playerItemPurchaserSettings.enabled)
		{
			ShowItemVisuals("ba:itemname_closedcardboardbox");
		}
	}

	public override bool IsFull()
	{
		return base.ItemInstance.cargoInstances.Count >= base.Item.cargoCapacity;
	}

	public void ManageStorage()
	{
		if (PurchaseUI.IsPanelOpen)
		{
			return;
		}
		if (PlayerHelper.IsHoldingShoppingBasket)
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string>
			{
				{ "fromname", itemName },
				{
					"toname",
					PlayerHelper.ItemInstanceInHands.itemName
				}
			};
			Notifications.Show(NotificationType.Warning, "notification_cant_interact_with_item_in_hand", notificationData);
		}
		else
		{
			InstanceBehavior<GameManager>.Instance.playerController.SetGoal(this, delegate
			{
				InstanceBehavior<UIs>.Instance.playerHUD.manageCargoUI.Show(base.ItemInstance);
			});
		}
	}

	public override void UpdateVisuals()
	{
		UpdateFillState((base.Item.cargoCapacity > 0) ? ((double)base.ItemInstance.cargoInstances.Count / (double)base.Item.cargoCapacity) : 0.0);
	}
}
