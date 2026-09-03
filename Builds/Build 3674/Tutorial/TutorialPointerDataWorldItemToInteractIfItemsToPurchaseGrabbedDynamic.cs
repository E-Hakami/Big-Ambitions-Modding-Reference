using System;
using System.Linq;
using BigAmbitions.Items;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/WorldItemToInteractIfItemsGrabbedDynamic")]
public class TutorialPointerDataWorldItemToInteractIfItemsToPurchaseGrabbedDynamic : TutorialPointerDataWorldItemToInteract
{
	[SerializeField]
	private HasPurchasedDynamicItems questRequirement;

	[NonSerialized]
	private TutorialDynamicItems _dynamicItems;

	public override void FindEntityController()
	{
		ItemController itemController = InstanceBehavior<BuildingManager>.Instance.allItemControllers.FirstOrDefault((ItemController x) => itemNameToInteractWith.Contains(x.itemName) && !x.playerItemPurchaserSettings.enabled);
		if (!(itemController == null))
		{
			entityControllerTarget = itemController;
			while (itemController.parentItemController != null)
			{
				itemController = (ItemController)(entityControllerTarget = itemController.parentItemController);
			}
		}
	}

	public override bool ShouldBeEnabled()
	{
		if (base.ShouldBeEnabled())
		{
			return HasGrabbedItems();
		}
		return false;
	}

	private bool HasGrabbedItems()
	{
		ICargoHolder currentCargoHolder = PlayerHelper.GetCurrentCargoHolder();
		if (currentCargoHolder == null)
		{
			return false;
		}
		TutorialDynamicItems dynamicItems = GetDynamicItems();
		if (dynamicItems.invalid)
		{
			return false;
		}
		dynamicItems.ResetFulfilled();
		foreach (CargoInstance cargoInstance in currentCargoHolder.GetCargoInstances())
		{
			dynamicItems.CheckItem(cargoInstance.itemName);
			foreach (NestedCargoInstance nestedCargoInstance in cargoInstance.nestedCargoInstances)
			{
				dynamicItems.CheckItem(nestedCargoInstance.itemName);
			}
		}
		return dynamicItems.NoItemsRemaining();
	}

	private TutorialDynamicItems GetDynamicItems()
	{
		if (_dynamicItems != null)
		{
			return _dynamicItems;
		}
		_dynamicItems = questRequirement.GetDynamicItemsForTutorialPointers();
		return _dynamicItems;
	}
}
