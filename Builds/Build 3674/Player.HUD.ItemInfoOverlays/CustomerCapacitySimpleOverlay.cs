using BigAmbitions.Items;
using Controllers;
using TMPro;
using UnityEngine;

namespace Player.HUD.ItemInfoOverlays;

public class CustomerCapacitySimpleOverlay : IOverlay
{
	[SerializeField]
	private TMP_Text capacityLabel;

	public override bool IsValid(EntityController entityController)
	{
		if (entityController is ItemController itemController)
		{
			Item itemWithCustomerCapacity = GetItemWithCustomerCapacity(itemController);
			if ((object)itemWithCustomerCapacity != null)
			{
				return HasCustomerCapacity(itemWithCustomerCapacity);
			}
		}
		return false;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		if (entityController is ItemController itemController && TryGetItemBeingSoldToPlayer(itemController, out var item))
		{
			return HasCustomerCapacity(item);
		}
		return false;
	}

	private static Item GetItemWithCustomerCapacity(ItemController itemController)
	{
		PlayerItemPurchaserSettings playerItemPurchaserSettings = itemController.playerItemPurchaserSettings;
		if (playerItemPurchaserSettings == null || !playerItemPurchaserSettings.enabled || string.IsNullOrEmpty(playerItemPurchaserSettings.itemName))
		{
			return itemController.Item;
		}
		return ItemsGetter.GetByName(playerItemPurchaserSettings.itemName);
	}

	private static bool TryGetItemBeingSoldToPlayer(ItemController itemController, out Item item)
	{
		item = null;
		PlayerItemPurchaserSettings playerItemPurchaserSettings = itemController.playerItemPurchaserSettings;
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && playerItemPurchaserSettings != null && playerItemPurchaserSettings.enabled && !string.IsNullOrEmpty(playerItemPurchaserSettings.itemName))
		{
			PlayerItemPurchaser playerItemPurchaser = itemController.PlayerItemPurchaser;
			if (playerItemPurchaser != null && playerItemPurchaser.TotalPrice > 0f)
			{
				item = GetItemWithCustomerCapacity(itemController);
				return item;
			}
		}
		return false;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		if (entityController is ItemController itemController && TryGetItemBeingSoldToPlayer(itemController, out var item))
		{
			capacityLabel.text = item.addedCustomersPerHour.ToString();
		}
	}

	private static bool HasCustomerCapacity(Item item)
	{
		if (item.isFurniture)
		{
			return item.addedCustomersPerHour > 1;
		}
		return false;
	}
}
