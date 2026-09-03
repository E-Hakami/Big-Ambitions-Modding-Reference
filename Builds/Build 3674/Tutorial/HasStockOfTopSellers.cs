using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Entities;
using Extensions;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasStockOfTopSellers")]
public class HasStockOfTopSellers : QuestRequirement
{
	[SerializeField]
	private int numberOfBestSellers;

	[SerializeField]
	private int minimumAmountOutsideOfProducersPerItem;

	[NonSerialized]
	private readonly List<(Item item, int amount)> _bestSellersAndAmounts = new List<(Item, int)>();

	[NonSerialized]
	private int _maxPossibleBestSellers;

	[SerializeField]
	protected QuestEntryTarget businessTarget;

	[SerializeField]
	private bool includeProducers;

	public override bool CheckIfCompleted()
	{
		LoadBestSellersAndAmounts(out var hadSalesInLastWeek);
		if (!hadSalesInLastWeek)
		{
			return false;
		}
		if (_bestSellersAndAmounts.Count < Mathf.Min(_maxPossibleBestSellers, numberOfBestSellers))
		{
			return false;
		}
		for (int i = 0; i < Mathf.Min(_maxPossibleBestSellers, numberOfBestSellers); i++)
		{
			if (_bestSellersAndAmounts[i].amount < minimumAmountOutsideOfProducersPerItem)
			{
				return false;
			}
		}
		return true;
	}

	private void LoadBestSellersAndAmounts(out bool hadSalesInLastWeek)
	{
		hadSalesInLastWeek = false;
		_maxPossibleBestSellers = 0;
		_bestSellersAndAmounts.Clear();
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(businessTarget.GetAddress());
		if (buildingRegistration == null)
		{
			return;
		}
		List<OrderHistoryEntry> source = buildingRegistration.orderHistory.Where((OrderHistoryEntry x) => x.dayNumber.InRange(SaveGameManager.Current.Day - 7, SaveGameManager.Current.Day)).ToList();
		hadSalesInLastWeek = source.Any((OrderHistoryEntry x) => x.itemSales.Count > 0);
		List<(Item, int)> list = (from x in (from x in (from x in source.SelectMany((OrderHistoryEntry x) => x.itemSales)
					group x by x.itemName).Where(delegate(IGrouping<string, OrderHistoryEntry.ItemReport> x)
				{
					Item byName = ItemsGetter.GetByName(x.Key);
					return byName != null && !byName.HasTag(TagRef.Itemtag.isbag) && byName.type != ItemType.ServiceProduct;
				})
				select (ItemsGetter.GetByName(x.Key), x.Sum((OrderHistoryEntry.ItemReport c) => c.amountSold))).ToList()
			orderby x.Item2 descending
			select x).Take(numberOfBestSellers).ToList();
		_maxPossibleBestSellers = list.Count;
		for (int num = 0; num < Mathf.Min(_maxPossibleBestSellers, numberOfBestSellers); num++)
		{
			int item = BuildingHelper.CountTotalResourcesInStock(buildingRegistration, list[num].Item1.itemName, includeProducers, includePallets: true, includeBoxItemInstances: false);
			_bestSellersAndAmounts.Add((list[num].Item1, item));
		}
	}
}
