using BigAmbitions.Items;
using UnityEngine;

namespace Furniture.Requirements;

[CreateAssetMenu(menuName = "BigAmbitions/Furniture/Requirements/IsPlacedInTheEntrance")]
public class IsPlacedInTheEntrance : FurnitureRequirement
{
	public override bool IsRequirementMet(ItemInstance itemInstance)
	{
		return itemInstance.isSecured;
	}
}
