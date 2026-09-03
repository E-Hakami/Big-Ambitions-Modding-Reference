using AI.Citizens;
using AI.Customers.CustomerEntries;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Entities;
using Extensions;
using UnityEngine;

namespace Buildings.Retail.Simulation;

[CreateAssetMenu(menuName = "BusinessSimulator/Retail/CinemaTheater")]
public class CinemaTheaterBusinessSimulator : SelfServiceBusinessSimulator
{
	protected override string[] AvailableFurnitureItemNames => ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.iscinemaworkstation);

	protected override void ProcessCustomer(CustomerEntry customerEntry)
	{
		CitizenData citizenData = CitizenHelper.CreateRandomCitizenDataNonAlloc(buildingRegistration.Neighborhood);
		PointOfSale idealPointOfSale = GetIdealPointOfSale();
		bool flag = false;
		foreach (OrderEntry entry in customerEntry.order.entries)
		{
			if (entry.processed)
			{
				continue;
			}
			if (flag)
			{
				entry.processed = true;
				entry.priceAccceptable = false;
				continue;
			}
			ProcessItemInOrder(entry, citizenData, idealPointOfSale);
			if (!entry.priceAccceptable && ItemsGetter.GetByName(entry.itemName).HasTag(TagRef.Itemtag.isticket))
			{
				flag = true;
			}
		}
		AddDirtToToiletsAndSinksIfNeeded(customerEntry);
		AddDirtToUsedFurniture(customerEntry);
		if (idealPointOfSale != null)
		{
			ProcessPaperBag(customerEntry, idealPointOfSale);
			BuildingCleanlinessHelper.AddDirtBulkEntry(idealPointOfSale.instance, buildingRegistration);
		}
		Order order = customerEntry.order;
		if (order.timestamp == null)
		{
			order.timestamp = new Timestamp(TimeHelper.CurrentDay, currentHour, 0f);
		}
		customerEntry.order.Complete(buildingRegistration, idealPointOfSale?.customerService ?? 0f, businessCleanliness);
		buildingRegistration.unprocessedCompletedOrders.Add(customerEntry.order);
		customerEntry.completed = true;
	}

	protected override PointOfSale GetIdealPointOfSale()
	{
		return pointOfSales.GetRandom();
	}
}
