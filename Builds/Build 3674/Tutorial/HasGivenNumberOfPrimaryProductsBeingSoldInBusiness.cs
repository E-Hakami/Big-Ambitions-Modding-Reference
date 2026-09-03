using System;
using System.Collections.Generic;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasGivenNumberOfPrimaryProductsBeingSoldInBusiness")]
public class HasGivenNumberOfPrimaryProductsBeingSoldInBusiness : QuestRequirement
{
	[SerializeField]
	private CustomBuildingTarget customBuildingTarget;

	[SerializeField]
	private int numberOfPrimaryProducts;

	[NonSerialized]
	private readonly List<string> _primaryItemsForSaleWithStockOrSales = new List<string>();

	public override bool CheckIfCompleted()
	{
		BuildingRegistration buildingRegistration = customBuildingTarget.GetBuildingRegistration();
		if (buildingRegistration == null)
		{
			return false;
		}
		List<string> primaryRetailProducts = BusinessTypeHelper.GetData(buildingRegistration).GetPrimaryRetailProducts();
		int num = Mathf.Min(numberOfPrimaryProducts, primaryRetailProducts.Count);
		if (buildingRegistration.GetListOfItemsForSale().Count < num)
		{
			return false;
		}
		BuildingHelper.GetPrimaryItemsForSaleWithStockOrSales(buildingRegistration, primaryRetailProducts, _primaryItemsForSaleWithStockOrSales);
		return _primaryItemsForSaleWithStockOrSales.Count >= num;
	}
}
