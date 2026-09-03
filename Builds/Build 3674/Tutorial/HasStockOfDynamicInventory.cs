using BigAmbitions.Items;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasStockOfDynamicInventory")]
public class HasStockOfDynamicInventory : HasDynamicItemsInTarget
{
	[SerializeField]
	private int minimumAmount;

	public override TutorialDynamicItems GetRemainingDynamicItems()
	{
		TutorialDynamicItems dynamicItems = hasPurchasedDynamicItemsRequirement.GetDynamicItems();
		if (dynamicItems.invalid)
		{
			return null;
		}
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		if (buildingRegistration == null)
		{
			return null;
		}
		dynamicItems.SetMinimumAmount(minimumAmount);
		foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
		{
			if ((value.ItemCached.type & (ItemType.PointOfSale | ItemType.ShowcaseShelf)) != 0)
			{
				CargoInstance stockInstance = value.GetStockInstance();
				dynamicItems.CheckItem(stockInstance.itemName, stockInstance.amount);
			}
		}
		return dynamicItems;
	}

	public override TutorialDynamicItems GetRemainingDynamicItemsForTutorialPointers()
	{
		TutorialDynamicItems dynamicItemsForTutorialPointers = hasPurchasedDynamicItemsRequirement.GetDynamicItemsForTutorialPointers();
		if (dynamicItemsForTutorialPointers.invalid)
		{
			return null;
		}
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		if (buildingRegistration == null)
		{
			return null;
		}
		dynamicItemsForTutorialPointers.SetMinimumAmount(minimumAmount);
		foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
		{
			if ((value.ItemCached.type & (ItemType.PointOfSale | ItemType.ShowcaseShelf)) != 0)
			{
				CargoInstance stockInstance = value.GetStockInstance();
				dynamicItemsForTutorialPointers.CheckItem(stockInstance.itemName, stockInstance.amount);
			}
		}
		return dynamicItemsForTutorialPointers;
	}
}
