using System.Collections.Generic;
using System.Linq;
using AI.Citizens;
using AI.Customers.CustomerEntries;
using BigAmbitions.Items;
using Buildings.Retail.Simulation;
using Entities;
using Extensions;
using Helpers;
using UnityEngine;

namespace Buildings.Retail.Businesses.Hairdresser;

[CreateAssetMenu(menuName = "BusinessSimulator/Retail/FullServices/Hairdresser")]
public class HairdresserBusinessSimulator : FullServiceBusinessSimulator
{
	private readonly List<ItemInstance> _hairCareProductShelves = new List<ItemInstance>();

	private readonly List<ItemInstance> _stockedItemInstances = new List<ItemInstance>();

	private readonly List<PointOfSale> _stockedPointOfSales = new List<PointOfSale>();

	public override void SetUp(BuildingRegistration registration, int hour)
	{
		base.SetUp(registration, hour);
		_hairCareProductShelves.Clear();
		foreach (ItemInstance item in availableFurniture)
		{
			if ((item.ItemCached.type & ItemType.ShowcaseShelf) != 0 && item.GetStockInstance().itemName == "ba:itemname_haircareproduct")
			{
				_hairCareProductShelves.Add(item);
			}
		}
	}

	protected override PointOfSale GetIdealPointOfSale()
	{
		_stockedPointOfSales.Clear();
		_stockedPointOfSales.AddRange(pointOfSales.Where((PointOfSale x) => x.instance.GetStockInstance().amount > 0));
		return _stockedPointOfSales.GetRandom() ?? pointOfSales.GetRandom();
	}

	protected override void ProcessCustomer(CustomerEntry customerEntry)
	{
		if (pointOfSales.Count == 0 || _hairCareProductShelves.Count == 0)
		{
			customerEntry.completed = true;
		}
		else
		{
			base.ProcessCustomer(customerEntry);
		}
	}

	protected override OrderEntry ProcessItemInOrder(OrderEntry orderEntry, CitizenData citizenData, PointOfSale pointOfSale)
	{
		if ((ItemsGetter.GetByName(orderEntry.itemName).type & ItemType.ServiceProduct) == 0)
		{
			base.ProcessItemInOrder(orderEntry, citizenData, pointOfSale);
			return null;
		}
		_stockedItemInstances.Clear();
		_stockedItemInstances.AddRange(_hairCareProductShelves.Where((ItemInstance x) => x.GetStockInstance().amount > 0));
		ItemInstance random = _stockedItemInstances.GetRandom();
		if (random == null)
		{
			orderEntry.processed = true;
			return null;
		}
		if (!availableProducers.TryGetValue(orderEntry.itemName, out var value))
		{
			return null;
		}
		_stockedItemInstances.Clear();
		_stockedItemInstances.AddRange(value.Where((ItemInstance x) => EmployeeHelper.IsEmployeeStationEmployedAtHour(buildingRegistration, x.id, currentHour)));
		ItemInstance random2 = _stockedItemInstances.GetRandom();
		if (random2 == null)
		{
			return null;
		}
		CompleteOrderEntry(orderEntry, citizenData, random2);
		if (orderEntry.paid)
		{
			OrderEntry result = new OrderEntry
			{
				itemName = "ba:itemname_haircareproduct",
				processed = true,
				paid = true,
				available = true,
				priceAccceptable = true,
				price = 0f,
				wholesalePrice = random.GetStockInstance().pricePerUnit
			};
			random.SubtractFromStock();
			return result;
		}
		return null;
	}
}
