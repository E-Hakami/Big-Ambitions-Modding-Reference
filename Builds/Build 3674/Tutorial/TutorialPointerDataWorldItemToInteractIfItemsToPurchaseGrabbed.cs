using System.Linq;
using BigAmbitions.Items;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/WorldItemToInteractIfItemsToPurchaseGrabbed")]
public class TutorialPointerDataWorldItemToInteractIfItemsToPurchaseGrabbed : TutorialPointerDataWorldItemToInteract
{
	[SerializeField]
	private HasPurchasedItem questRequirement;

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
		int num = questRequirement.minimumQuantity;
		foreach (CargoInstance cargoInstance in currentCargoHolder.GetCargoInstances())
		{
			if (questRequirement.MatchesItem(cargoInstance.itemName))
			{
				num -= cargoInstance.amount;
				if (num <= 0)
				{
					return true;
				}
			}
			foreach (NestedCargoInstance nestedCargoInstance in cargoInstance.nestedCargoInstances)
			{
				if (questRequirement.MatchesItem(nestedCargoInstance.itemName))
				{
					num -= nestedCargoInstance.amount;
					if (num <= 0)
					{
						return true;
					}
				}
			}
		}
		return false;
	}
}
