using System.Linq;
using BigAmbitions.Items;
using HGAttributes;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/WorldItemToInteractIfItemsInHand")]
public class TutorialPointerDataWorldItemToInteractIfItemsInHand : TutorialPointerDataWorldItemToInteract
{
	[SerializeField]
	[AutocompleteDropdown("Items")]
	private string[] itemNames;

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
		if (!PlayerHelper.IsHoldingItem)
		{
			return false;
		}
		if (itemNames.Contains(PlayerHelper.ItemInstanceInHands.itemName))
		{
			return true;
		}
		foreach (CargoInstance cargoInstance in PlayerHelper.ItemInstanceInHands.GetCargoInstances())
		{
			if (itemNames.Contains(cargoInstance.itemName))
			{
				return true;
			}
		}
		return false;
	}
}
