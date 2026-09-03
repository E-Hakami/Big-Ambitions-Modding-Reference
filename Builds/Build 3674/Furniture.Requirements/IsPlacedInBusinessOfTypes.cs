using System;
using BigAmbitions.Items;
using HGAttributes;
using Helpers;
using Streets;
using UnityEngine;

namespace Furniture.Requirements;

[CreateAssetMenu(menuName = "BigAmbitions/Furniture/Requirements/IsPlacedInBusinessOfTypes")]
public class IsPlacedInBusinessOfTypes : FurnitureRequirement
{
	[SerializeField]
	[AutocompleteDropdown("BusinessTypes")]
	private string[] businessTypeNames;

	[SerializeField]
	private bool invertResult;

	public override bool IsRequirementMet(ItemInstance itemInstance)
	{
		if (itemInstance.AddressCached.IsUndefined())
		{
			return true;
		}
		bool flag = false;
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(itemInstance.AddressCached);
		if (buildingRegistration != null)
		{
			flag = Array.IndexOf(businessTypeNames, buildingRegistration.businessTypeName) != -1;
		}
		else if (BuildingManager.IsInsideBuilding)
		{
			flag = Array.IndexOf(businessTypeNames, InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName) != -1;
		}
		if (!invertResult)
		{
			return flag;
		}
		return !flag;
	}
}
