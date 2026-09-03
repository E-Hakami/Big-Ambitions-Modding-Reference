using System.Linq;
using BigAmbitions.Items;
using HGAttributes;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/WorldItemToGrab")]
public class TutorialPointerDataWorldItemToGrab : TutorialPointerDataWorldItem
{
	[SerializeField]
	[AutocompleteDropdown("Items")]
	private string[] itemNamesToGrab;

	public override bool ShouldBeEnabled()
	{
		if (base.ShouldBeEnabled())
		{
			return !HasGrabbedItems();
		}
		return false;
	}

	public override void FindEntityController()
	{
		Vector3 playerPosition = PlayerHelper.GetPosition();
		ItemController itemController = (from x in InstanceBehavior<BuildingManager>.Instance.allItemControllers
			where itemNamesToGrab.Contains(x.GetProducedItemName())
			orderby Vector3.SqrMagnitude(x.transform.position - playerPosition)
			select x).FirstOrDefault();
		if (!(itemController == null))
		{
			entityControllerTarget = itemController;
			while (itemController.parentItemController != null)
			{
				itemController = (ItemController)(entityControllerTarget = itemController.parentItemController);
			}
		}
	}

	private bool HasGrabbedItems()
	{
		if (PlayerHelper.IsHoldingItem && itemNamesToGrab.Contains(PlayerHelper.ItemInstanceInHands.itemName))
		{
			return true;
		}
		ICargoHolder cargoHolder2;
		if (!PlayerHelper.IsHoldingItem)
		{
			ICargoHolder cargoHolder = (PlayerHelper.IsUsingVehicle ? VehicleHelper.GetCurrentVehicle() : null);
			cargoHolder2 = cargoHolder;
		}
		else
		{
			ICargoHolder cargoHolder = PlayerHelper.ItemInstanceInHands;
			cargoHolder2 = cargoHolder;
		}
		ICargoHolder cargoHolder3 = cargoHolder2;
		if (cargoHolder3 == null)
		{
			return false;
		}
		if (cargoHolder3.GetCargoInstances().Any((CargoInstance x) => itemNamesToGrab.Contains(x.itemName)))
		{
			return true;
		}
		return cargoHolder3.GetCargoInstances().SelectMany((CargoInstance x) => x.nestedCargoInstances).Any((NestedCargoInstance x) => itemNamesToGrab.Contains(x.itemName));
	}
}
