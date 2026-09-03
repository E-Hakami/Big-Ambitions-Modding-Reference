using BigAmbitions.Items;
using HGAttributes;
using Helpers;
using Streets;
using UnityEngine;

namespace Furniture.Requirements;

[CreateAssetMenu(menuName = "BigAmbitions/Furniture/Requirements/IsPlacedInBusinessOfType")]
public class IsPlacedInBusinessOfType : FurnitureRequirement
{
	[SerializeField]
	[AutocompleteDropdown("BusinessTypes")]
	private string businessTypeName;

	public override bool IsRequirementMet(ItemInstance itemInstance)
	{
		if (itemInstance.AddressCached.IsUndefined())
		{
			return true;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(itemInstance.AddressCached);
		if (buildingRegistration != null)
		{
			return buildingRegistration.businessTypeName == businessTypeName;
		}
		if (BuildingManager.IsInsideBuilding)
		{
			return InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName == businessTypeName;
		}
		return false;
	}
}
