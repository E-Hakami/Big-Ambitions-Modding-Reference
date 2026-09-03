using System.Collections.Generic;
using AI.Citizens;
using AI.Customers.CustomerEntries;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Entities;
using Extensions;
using UnityEngine;

namespace Buildings.Retail.Simulation;

[CreateAssetMenu(menuName = "BusinessSimulator/Retail/SelfService")]
public class SelfServiceBusinessSimulator : RetailBusinessSimulator
{
	private readonly List<ItemInstance> _shoppingContainers = new List<ItemInstance>();

	private bool _areThereScalesAvailable;

	private string[] _cachedFurnitureItemNames;

	protected override ItemType AvailableFurnitureItemTypes => ItemType.EmployeeWorkstation | ItemType.StorageShelf | ItemType.ShowcaseShelf;

	protected override string[] AvailableFurnitureItemNames
	{
		get
		{
			string[] cachedFurnitureItemNames = _cachedFurnitureItemNames;
			if (cachedFurnitureItemNames != null && cachedFurnitureItemNames.Length > 0)
			{
				return _cachedFurnitureItemNames;
			}
			string[] allItemNamesByTag = ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.isweighingscale);
			string[] allItemNamesByTag2 = ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.isshoppingcontainerprovider);
			_cachedFurnitureItemNames = new string[allItemNamesByTag.Length + allItemNamesByTag2.Length];
			allItemNamesByTag.CopyTo(_cachedFurnitureItemNames, 0);
			allItemNamesByTag2.CopyTo(_cachedFurnitureItemNames, allItemNamesByTag.Length);
			return _cachedFurnitureItemNames;
		}
	}

	protected override bool IsItemNamePointOfSale(string itemName)
	{
		return (ItemsGetter.GetByName(itemName).type & ItemType.PointOfSale) != 0;
	}

	public override void SetUp(BuildingRegistration registration, int hour)
	{
		base.SetUp(registration, hour);
		_shoppingContainers.Clear();
		_areThereScalesAvailable = false;
		foreach (ItemInstance item in availableFurniture)
		{
			if (item.ItemCached.HasTag(TagRef.Itemtag.isshoppingcontainerprovider))
			{
				_shoppingContainers.Add(item);
			}
			else if (item.ItemCached.HasTag(TagRef.Itemtag.isweighingscale))
			{
				_areThereScalesAvailable = true;
			}
		}
	}

	protected override void ProcessCustomer(CustomerEntry customerEntry)
	{
		if (businessType.CustomersNeedShoppingContainer)
		{
			if (_shoppingContainers.Count == 0)
			{
				customerEntry.completed = true;
				return;
			}
			BuildingCleanlinessHelper.AddDirtBulkEntry(_shoppingContainers.GetRandom(), buildingRegistration);
		}
		base.ProcessCustomer(customerEntry);
	}

	protected override OrderEntry ProcessItemInOrder(OrderEntry orderEntry, CitizenData citizenData, PointOfSale pointOfSale)
	{
		if (ItemsGetter.GetByName(orderEntry.itemName).requiresWeighing && !_areThereScalesAvailable)
		{
			orderEntry.processed = true;
			return null;
		}
		return base.ProcessItemInOrder(orderEntry, citizenData, pointOfSale);
	}
}
