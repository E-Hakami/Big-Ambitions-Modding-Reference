using System.Linq;
using BigAmbitions.Items;
using HGAttributes;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/IsNotHoldingSpecificItem")]
public class TutorialPointerHideConditionIfIsNotHoldingSpecificItem : TutorialPointerHideCondition
{
	[SerializeField]
	[AutocompleteDropdown("Items")]
	private string itemToHold;

	protected override bool ConditionMetInternal()
	{
		if (PlayerHelper.IsHoldingItem)
		{
			if (PlayerHelper.ItemInstanceInHands.itemName != itemToHold)
			{
				return PlayerHelper.ItemInstanceInHands.cargoInstances.All((CargoInstance x) => x.itemName != itemToHold);
			}
			return false;
		}
		return true;
	}
}
