using BigAmbitions.Items;
using HGAttributes;
using Helpers;
using Items.SpecialItems;
using Localizor;
using UnityEngine;

namespace Furniture.Requirements;

[CreateAssetMenu(menuName = "BigAmbitions/Furniture/Requirements/IsAttachedToFactoryWorkstation")]
public class IsAttachedToFactoryWorkstation : FurnitureRequirement
{
	[SerializeField]
	[AutocompleteDropdown("Items")]
	private string workstationItemName;

	public override bool IsRequirementMet(ItemInstance itemInstance)
	{
		localizationArguments = new
		{
			itemName = workstationItemName.GetLocalization()
		};
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(itemInstance.AddressCached);
		if (buildingRegistration != null && !buildingRegistration.RentedByPlayer)
		{
			return true;
		}
		if (string.IsNullOrEmpty(itemInstance.parentId))
		{
			return false;
		}
		ItemController itemControllerByID = ItemHelper.GetItemControllerByID(itemInstance.parentId);
		if (itemControllerByID is FactoryAssemblyMachineController)
		{
			return itemControllerByID.itemName == workstationItemName;
		}
		return false;
	}
}
