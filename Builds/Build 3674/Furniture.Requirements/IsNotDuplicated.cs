using BigAmbitions.Items;
using UnityEngine;

namespace Furniture.Requirements;

[CreateAssetMenu(menuName = "BigAmbitions/Furniture/Requirements/IsNotDuplicated")]
public class IsNotDuplicated : FurnitureRequirement
{
	public override bool IsRequirementMet(ItemInstance itemInstance)
	{
		BuildingRegistration buildingRegistration = itemInstance.GetBuildingRegistration();
		if (buildingRegistration == null)
		{
			return true;
		}
		ItemInstance value = itemInstance;
		if (!string.IsNullOrEmpty(itemInstance.parentId) && !buildingRegistration.itemInstances.TryGetValue(value.parentId, out value))
		{
			Debug.LogError("Item instance " + itemInstance.itemName + " has a non-existent parent id");
			value = itemInstance;
		}
		int num = 0;
		foreach (AttachableChild stackedItem in value.stackedItems)
		{
			if (ItemsGetter.GetByName(stackedItem.childItemName).type == itemInstance.ItemCached.type)
			{
				if (num > 0)
				{
					return false;
				}
				num++;
			}
		}
		return true;
	}
}
