using BigAmbitions.Items;
using Helpers;
using UnityEngine;

namespace Furniture.Requirements;

[CreateAssetMenu(menuName = "BigAmbitions/Furniture/Requirements/IsPlacedAtHeight")]
public class IsPlacedAtHeight : FurnitureRequirement
{
	[SerializeField]
	private float minHeight;

	[SerializeField]
	private float maxHeight;

	public override bool IsRequirementMet(ItemInstance itemInstance)
	{
		if (!BuildingManager.IsInsideBuilding)
		{
			return true;
		}
		float num = itemInstance.position.y;
		if (Physics.Raycast(itemInstance.position, Vector3.down, out var hitInfo, 100f, LayerHelper.groundLayerMask))
		{
			num -= hitInfo.point.y;
		}
		if (num >= minHeight)
		{
			return num <= maxHeight;
		}
		return false;
	}
}
