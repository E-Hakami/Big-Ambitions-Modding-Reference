using System;
using System.Linq;
using BigAmbitions.Items;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/WorldDynamicItemsToSetStock")]
public class TutorialPointerDataWorldDynamicItemsToSetStock : TutorialPointerDataWorldItem
{
	[SerializeField]
	private HasStockOfDynamicInventory questRequirement;

	[NonSerialized]
	private string _itemToSetStock;

	[NonSerialized]
	private string _oldItemToSetStock;

	public override bool ShouldBeEnabled()
	{
		SetItemToSetStock();
		if (!base.ShouldBeEnabled() || string.IsNullOrEmpty(_itemToSetStock))
		{
			return false;
		}
		FindSuitableControllerToSetStock();
		return entityControllerTarget != null;
	}

	public override void Relocate(TutorialPointer tutorialPointer)
	{
		if (_oldItemToSetStock != _itemToSetStock)
		{
			OnShow(tutorialPointer);
		}
		base.Relocate(tutorialPointer);
	}

	private void FindSuitableControllerToSetStock()
	{
		if (string.IsNullOrEmpty(_itemToSetStock))
		{
			entityControllerTarget = null;
			return;
		}
		ItemController itemController = GetClosestSuitableItem();
		if (!(itemController == null))
		{
			entityControllerTarget = itemController;
			while (itemController.parentItemController != null)
			{
				itemController = (ItemController)(entityControllerTarget = itemController.parentItemController);
			}
		}
	}

	private ItemController GetClosestSuitableItem()
	{
		Vector3 position = PlayerHelper.GetPosition();
		ItemController result = null;
		float num = float.MaxValue;
		foreach (ItemController allItemController in InstanceBehavior<BuildingManager>.Instance.allItemControllers)
		{
			if (!allItemController.Item.itemsThatCanShowcase.Contains(_itemToSetStock))
			{
				continue;
			}
			CargoInstance stockInstance = allItemController.ItemInstance.GetStockInstance();
			if ((!(stockInstance.itemName != _itemToSetStock) || string.IsNullOrEmpty(stockInstance.itemName)) && (string.IsNullOrEmpty(stockInstance.itemName) || stockInstance.amount < stockInstance.GetMaxStockCapacity(allItemController.ItemInstance)))
			{
				float sqrMagnitude = (allItemController.transform.position - position).sqrMagnitude;
				if (sqrMagnitude < num)
				{
					num = sqrMagnitude;
					result = allItemController;
				}
			}
		}
		return result;
	}

	private void SetItemToSetStock()
	{
		if (!PlayerHelper.IsHoldingItem)
		{
			SetItemToSetStock(null);
			return;
		}
		TutorialDynamicItems remainingDynamicItems = questRequirement.GetRemainingDynamicItems();
		if (remainingDynamicItems == null)
		{
			SetItemToSetStock(null);
			return;
		}
		CargoInstance cargoInstance = PlayerHelper.ItemInstanceInHands.GetCargoInstances()[0];
		SetItemToSetStock(remainingDynamicItems.ContainsUnfulfilled(cargoInstance.itemName) ? cargoInstance.itemName : null);
	}

	private void SetItemToSetStock(string itemName)
	{
		_oldItemToSetStock = _itemToSetStock;
		_itemToSetStock = itemName;
	}
}
