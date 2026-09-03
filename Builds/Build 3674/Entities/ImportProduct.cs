using System;
using System.Runtime.Serialization;
using BigAmbitions.Items;
using Helpers;

namespace Entities;

[Serializable]
public class ImportProduct
{
	public string itemName;

	public int amount;

	public int amountOrderedLastWeek;

	public int amountOrderedThisWeek;

	public Address assignedWarehouse;

	[NonSerialized]
	private Item _itemCached;

	[IgnoreDataMember]
	public Item ItemCached
	{
		get
		{
			if (_itemCached == null)
			{
				_itemCached = ItemsGetter.GetByName(itemName);
			}
			return _itemCached;
		}
	}

	public float Price => ItemCached.GetWholesalePrice() * ProductMarketHelper.GetProductMarketEventMultiplier(itemName);

	public int GetAmountToBuy(bool isTargetAmount)
	{
		if (assignedWarehouse == null || !isTargetAmount)
		{
			return amount;
		}
		int num = amount - BuildingHelper.CountResourcesInPallets(assignedWarehouse, itemName);
		if (num >= 0)
		{
			return num;
		}
		return 0;
	}
}
