using BigAmbitions.Items;
using Helpers;
using Streets;
using UnityEngine;

namespace Furniture.Requirements;

[CreateAssetMenu(menuName = "BigAmbitions/Furniture/Requirements/IsPlacedInBusinessWithCustomerType")]
public class IsPlacedInBusinessWithCustomerType : FurnitureRequirement
{
	[SerializeField]
	private CustomerType customerType;

	public override bool IsRequirementMet(ItemInstance itemInstance)
	{
		if (itemInstance.AddressCached.IsUndefined())
		{
			return true;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(itemInstance.AddressCached);
		if (buildingRegistration != null)
		{
			return BusinessTypeHelper.GetData(buildingRegistration).customerType == customerType;
		}
		if (BuildingManager.IsInsideBuilding)
		{
			return BusinessTypeHelper.GetData(InstanceBehavior<BuildingManager>.Instance.buildingRegistration).customerType == customerType;
		}
		return false;
	}
}
