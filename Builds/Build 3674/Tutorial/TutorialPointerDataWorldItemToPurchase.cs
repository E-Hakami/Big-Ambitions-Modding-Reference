using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Helpers;
using NaughtyAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/WorldItemToBuy")]
public class TutorialPointerDataWorldItemToPurchase : TutorialPointerDataWorldItem
{
	[SerializeField]
	private HasPurchasedItem questRequirement;

	[SerializeField]
	[HideIf("pointToCheapestOne")]
	private bool pointOnlyToFirstItemName;

	[SerializeField]
	[HideIf("pointOnlyToFirstItemName")]
	private bool pointToCheapestOne;

	[SerializeField]
	private bool requiresShoppingBasket;

	[SerializeField]
	private bool requiresHandtruckOrFlatbed;

	private string[] _itemNamesToPointTo;

	public override bool ShouldBeEnabled()
	{
		if (base.ShouldBeEnabled() && !HasGrabbedItems())
		{
			return IsUsingCargoHolder();
		}
		return false;
	}

	public override void FindEntityController()
	{
		CacheItemNamesToPointTo();
		ItemController itemController = FindNearestPurchasableItemController();
		if (!(itemController == null))
		{
			entityControllerTarget = itemController;
			while (itemController.parentItemController != null)
			{
				itemController = (ItemController)(entityControllerTarget = itemController.parentItemController);
			}
		}
	}

	private void CacheItemNamesToPointTo()
	{
		if (_itemNamesToPointTo == null || _itemNamesToPointTo.Length == 0)
		{
			string[] resolvedItemNames = questRequirement.GetResolvedItemNames();
			if (pointToCheapestOne)
			{
				UpdateToCheapestItemName(resolvedItemNames);
				return;
			}
			_itemNamesToPointTo = ((!pointOnlyToFirstItemName) ? resolvedItemNames : new string[1] { resolvedItemNames[0] });
		}
	}

	private void UpdateToCheapestItemName(string[] resolved)
	{
		float num = float.MaxValue;
		string text = string.Empty;
		foreach (string text2 in resolved)
		{
			float defaultMarketPrice = ItemHelper.GetDefaultMarketPrice(text2);
			if (!(defaultMarketPrice >= num))
			{
				num = defaultMarketPrice;
				text = text2;
			}
		}
		_itemNamesToPointTo = new string[1] { text };
	}

	private ItemController FindNearestPurchasableItemController()
	{
		Vector3 position = PlayerHelper.GetPosition();
		ItemController result = null;
		float num = float.MaxValue;
		List<ItemController> allItemControllers = InstanceBehavior<BuildingManager>.Instance.allItemControllers;
		for (int i = 0; i < allItemControllers.Count; i++)
		{
			ItemController itemController = allItemControllers[i];
			if (itemController.playerItemPurchaserSettings.enabled && IsPointableItem(itemController))
			{
				float num2 = Vector3.SqrMagnitude(itemController.transform.position - position);
				if (!(num2 >= num))
				{
					num = num2;
					result = itemController;
				}
			}
		}
		return result;
	}

	private bool IsPointableItem(ItemController controller)
	{
		string producedItemName = controller.GetProducedItemName();
		for (int i = 0; i < _itemNamesToPointTo.Length; i++)
		{
			if (_itemNamesToPointTo[i] == producedItemName)
			{
				return true;
			}
		}
		return false;
	}

	private bool HasGrabbedItems()
	{
		ICargoHolder currentCargoHolder = GetCurrentCargoHolder();
		if (currentCargoHolder == null)
		{
			return false;
		}
		return CountGrabbedItems(currentCargoHolder) >= questRequirement.minimumQuantity;
	}

	private static ICargoHolder GetCurrentCargoHolder()
	{
		if (PlayerHelper.IsHoldingItem)
		{
			return PlayerHelper.ItemInstanceInHands;
		}
		if (PlayerHelper.IsUsingVehicle)
		{
			return VehicleHelper.GetCurrentVehicle();
		}
		return null;
	}

	private int CountGrabbedItems(ICargoHolder cargoHolder)
	{
		int num = 0;
		foreach (CargoInstance cargoInstance in cargoHolder.GetCargoInstances())
		{
			if (questRequirement.MatchesItem(cargoInstance.itemName))
			{
				num += cargoInstance.amount;
			}
			foreach (NestedCargoInstance nestedCargoInstance in cargoInstance.nestedCargoInstances)
			{
				if (questRequirement.MatchesItem(nestedCargoInstance.itemName))
				{
					num += nestedCargoInstance.amount;
				}
			}
			if (num >= questRequirement.minimumQuantity)
			{
				return num;
			}
		}
		return num;
	}

	private bool IsUsingCargoHolder()
	{
		if (requiresShoppingBasket)
		{
			return PlayerHelper.IsHoldingShoppingBasket;
		}
		if (!requiresHandtruckOrFlatbed)
		{
			return true;
		}
		if (!PlayerHelper.IsUsingVehicle)
		{
			return false;
		}
		return VehicleHelper.GetCurrentVehicleBase().vehicleType.HasTag(TagRef.Vehicletag.ishandvehicle);
	}
}
