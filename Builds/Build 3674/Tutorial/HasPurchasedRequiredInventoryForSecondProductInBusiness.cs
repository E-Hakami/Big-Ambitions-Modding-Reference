using System;
using System.Collections.Generic;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasPurchasedRequiredInventoryForSecondProductInBusiness")]
public class HasPurchasedRequiredInventoryForSecondProductInBusiness : HasPurchasedDynamicItems
{
	[SerializeField]
	private QuestEntryTarget wholesaleStoreTarget;

	[NonSerialized]
	private readonly List<string> _primaryItemsForSaleWithStockOrSales = new List<string>();

	protected override void SetDynamicItems()
	{
		dynamicItems.Reset();
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		string requiredItemForTutorial = GetRequiredItemForTutorial(buildingRegistration);
		if (!string.IsNullOrEmpty(requiredItemForTutorial))
		{
			dynamicItems.AddCollection(new string[1] { requiredItemForTutorial });
		}
	}

	protected override void SetDynamicItemsForTutorialPointers()
	{
		dynamicItemsForTutorialPointers.Reset();
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		string requiredItemForTutorial = GetRequiredItemForTutorial(buildingRegistration);
		if (!string.IsNullOrEmpty(requiredItemForTutorial))
		{
			dynamicItemsForTutorialPointers.AddCollection(new string[1] { requiredItemForTutorial });
		}
	}

	private string GetRequiredItemForTutorial(BuildingRegistration registration)
	{
		if (registration == null)
		{
			return null;
		}
		if (BuildingHelper.GetBuildingRegistration(wholesaleStoreTarget.GetAddress()) == null)
		{
			return null;
		}
		List<string> primaryRetailProducts = BusinessTypeHelper.GetData(registration).GetPrimaryRetailProducts();
		BuildingHelper.GetPrimaryItemsForSaleWithStockOrSales(registration, primaryRetailProducts, _primaryItemsForSaleWithStockOrSales);
		List<string> listOfItemsForSale = BuildingHelper.GetBuildingRegistration(wholesaleStoreTarget.GetAddress()).GetListOfItemsForSale();
		foreach (string item in primaryRetailProducts)
		{
			if (!_primaryItemsForSaleWithStockOrSales.Contains(item) && listOfItemsForSale.Contains(item))
			{
				return item;
			}
		}
		return null;
	}
}
