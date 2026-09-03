using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using Furniture.Requirements;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasPurchasedRequiredItemForSecondProductInBusiness")]
public class HasPurchasedRequiredItemForSecondProductInBusiness : HasPurchasedDynamicItems
{
	[SerializeField]
	private QuestEntryTarget wholesaleStoreTarget;

	[NonSerialized]
	private readonly HashSet<string> _requiredItems = new HashSet<string>();

	[NonSerialized]
	private readonly List<string> _primaryItemsForSaleWithStockOrSales = new List<string>();

	protected override void SetDynamicItems()
	{
		dynamicItems.Reset();
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		dynamicItems.AddCollection(GetRequiredItemsForTutorial(buildingRegistration));
	}

	protected override void SetDynamicItemsForTutorialPointers()
	{
		dynamicItemsForTutorialPointers.Reset();
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		string text = GetRequiredItemsForTutorial(buildingRegistration)[0];
		dynamicItemsForTutorialPointers.AddCollection(new string[1] { text });
		foreach (FurnitureRequirement furnitureRequirement in ItemsGetter.GetByName(text).furnitureRequirements)
		{
			if (furnitureRequirement is HasItemTypeAttached)
			{
				dynamicItemsForTutorialPointers.AddCollection(new string[1] { "ba:itemname_counter1" });
				break;
			}
		}
	}

	protected override void CheckItemsInBuilding(TutorialDynamicItems dynamicItemsToCheck)
	{
	}

	private string[] GetRequiredItemsForTutorial(BuildingRegistration registration)
	{
		if (registration == null)
		{
			return null;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(wholesaleStoreTarget.GetAddress());
		if (buildingRegistration == null)
		{
			return null;
		}
		List<string> listOfItemsForSale = buildingRegistration.GetListOfItemsForSale();
		List<string> primaryRetailProducts = BusinessTypeHelper.GetData(registration).GetPrimaryRetailProducts();
		BuildingHelper.GetPrimaryItemsForSaleWithStockOrSales(registration, primaryRetailProducts, _primaryItemsForSaleWithStockOrSales);
		_requiredItems.Clear();
		foreach (string item in primaryRetailProducts)
		{
			if (_primaryItemsForSaleWithStockOrSales.Contains(item) || !listOfItemsForSale.Contains(item))
			{
				continue;
			}
			foreach (Item allItem in ItemsGetter.AllItems)
			{
				if (allItem.itemsThatCanShowcase.Contains(item))
				{
					_requiredItems.Add(allItem.itemName);
				}
			}
		}
		return _requiredItems.ToArray();
	}
}
