using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Extensions;
using Helpers;
using NaughtyAttributes;
using UI;
using UI.Notification;
using UI.Purchase;
using UnityEngine;

public class StorageShelfController : ShelfController
{
	[Header("Storage shelf")]
	[SerializeField]
	private bool showRandomVisualsInAiStores = true;

	[ShowIf("showRandomVisualsInAiStores")]
	[SerializeField]
	[MinMaxSlider(0f, 1f)]
	private Vector2 randomFillStateRange = new Vector2(0.6f, 1f);

	public override bool IsFull()
	{
		return base.ItemInstance.cargoInstances.Count >= base.Item.cargoCapacity;
	}

	public override void Start()
	{
		GetOrCreateBuildingContext();
		bool num = !playerItemPurchaserSettings.enabled && showRandomVisualsInAiStores && !base.BuildingContext.IsPlayerOwnedBusiness;
		if (!playerItemPurchaserSettings.enabled)
		{
			ShowItemVisuals("ba:itemname_closedcardboardbox");
		}
		base.Start();
		if (num)
		{
			UpdateFillState(randomFillStateRange.RandomValue());
		}
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

	public void AddItemsToStorage()
	{
		InstanceBehavior<GameManager>.Instance.playerController.SetGoal(this, delegate
		{
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
			else if (PlayerHelper.IsUsingVehicle)
			{
				if (VehicleHelper.GetCurrentVehicle().GetCargoInstances().Any((CargoInstance x) => x.IsSealed))
				{
					Notifications.Show(NotificationType.Error, "notification_cant_tamper_sealed_box");
				}
				else if (ICargoHolder.TryToMergeAndMoveCargoBetweenHolders(VehicleHelper.GetCurrentVehicle(), base.ItemInstance))
				{
					GameEvent.Invoke("ba:gameevent_itemcargochanged");
				}
				else
				{
					Notifications.ShowError("storageshelf_notification_shelf_full");
				}
			}
			else if (PlayerHelper.IsHoldingItem)
			{
				if (PlayerHelper.ItemInstanceInHands.GetCargoInstances().Any((CargoInstance x) => x.IsSealed))
				{
					Notifications.Show(NotificationType.Error, "notification_cant_tamper_sealed_box");
				}
				else
				{
					Item itemInHands = PlayerHelper.ItemInHands;
					if ((object)itemInHands != null && itemInHands.HasTag(TagRef.Itemtag.isbox))
					{
						if (ICargoHolder.TryToMergeAndMoveCargoBetweenHolders(PlayerHelper.ItemInstanceInHands, base.ItemInstance))
						{
							GameEvent.Invoke("ba:gameevent_itemcargochanged");
						}
						else
						{
							Notifications.ShowError("storageshelf_notification_shelf_full");
						}
					}
					else if (base.ItemInstance.TryToAddToCargo(PlayerHelper.ItemInstanceInHands.ConvertToCargoInstance()))
					{
						PlayerHelper.ItemInstanceInHands = null;
						GameEvent.Invoke("ba:gameevent_itemcargochanged");
					}
					else
					{
						Notifications.ShowError("storageshelf_notification_shelf_full");
					}
				}
			}
			else
			{
				Debug.LogError("Nothing to add to the storage");
			}
		});
	}

	public override void UpdateVisuals()
	{
		UpdateFillState((base.Item.cargoCapacity > 0) ? ((double)base.ItemInstance.cargoInstances.Count / (double)base.Item.cargoCapacity) : 0.0);
	}
}
