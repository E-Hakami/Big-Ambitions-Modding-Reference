using System.Linq;
using BigAmbitions.Items;
using HGAttributes;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/WorldItemToInteractIfItemNotGrabbed")]
public class TutorialPointerDataWorldItemToInteractIfItemNotGrabbed : TutorialPointerDataWorldItemToInteract
{
	[SerializeField]
	[AutocompleteDropdown("Items")]
	private string itemNameToGrab;

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
			return !HasGrabbedItem();
		}
		return false;
	}

	private bool HasGrabbedItem()
	{
		if (PlayerHelper.IsHoldingItem && PlayerHelper.ItemInstanceInHands.itemName == itemNameToGrab)
		{
			return true;
		}
		ICargoHolder currentCargoHolder = PlayerHelper.GetCurrentCargoHolder();
		if (currentCargoHolder == null)
		{
			return false;
		}
		foreach (CargoInstance cargoInstance in currentCargoHolder.GetCargoInstances())
		{
			if (cargoInstance.itemName == itemNameToGrab)
			{
				return true;
			}
		}
		return false;
	}
}
