using System.Collections.Generic;
using System.Linq;
using AI.Customers.CustomerEntries;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Entities;
using Extensions;
using UnityEngine;

namespace Buildings.Retail.Simulation;

[CreateAssetMenu(menuName = "BusinessSimulator/Retail/FullService")]
public class FullServiceBusinessSimulator : RetailBusinessSimulator
{
	private readonly List<ItemInstance> _chairs = new List<ItemInstance>();

	private readonly List<ItemInstance> _trashBins = new List<ItemInstance>();

	protected override ItemType AvailableFurnitureItemTypes => ItemType.EmployeeWorkstation | ItemType.StorageShelf | ItemType.ShowcaseShelf | ItemType.Seat;

	protected override string[] AvailableFurnitureItemNames => ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.istrashbin);

	protected override bool IsItemNamePointOfSale(string itemName)
	{
		return ItemsGetter.GetByName(itemName).HasTag(TagRef.Itemtag.isfullservicecashregister);
	}

	public override void SetUp(BuildingRegistration registration, int hour)
	{
		base.SetUp(registration, hour);
		_chairs.Clear();
		_trashBins.Clear();
		foreach (ItemInstance item in availableFurniture)
		{
			if (item.ItemCached.HasTag(TagRef.Itemtag.istrashbin))
			{
				_trashBins.Add(item);
			}
			else if ((ItemsGetter.GetByName(item.itemName).type & ItemType.Seat) != 0)
			{
				_chairs.Add(item);
			}
		}
	}

	protected override void AddDirtToUsedFurniture(CustomerEntry spawnEntry)
	{
		if (!spawnEntry.order.entries.All((OrderEntry x) => !x.paid))
		{
			if (_chairs.Count != 0)
			{
				BuildingCleanlinessHelper.AddDirtBulkEntry(_chairs.GetRandom(), buildingRegistration);
			}
			if (_trashBins.Count != 0)
			{
				BuildingCleanlinessHelper.AddDirtBulkEntry(_trashBins.GetRandom(), buildingRegistration);
			}
		}
	}
}
