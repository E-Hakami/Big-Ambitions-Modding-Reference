using System.Collections.Generic;
using AI.Citizens;
using AI.Customers.CustomerEntries;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Buildings.Retail.Simulation;
using Entities;
using Extensions;
using UnityEngine;

namespace Buildings.Retail.Businesses.Gym;

[CreateAssetMenu(menuName = "BusinessSimulator/Retail/Gym")]
public class GymBusinessSimulator : RetailBusinessSimulator
{
	private readonly List<ItemInstance> _workoutMachines = new List<ItemInstance>();

	protected override ItemType AvailableFurnitureItemTypes => ItemType.EmployeeWorkstation | ItemType.ShowcaseShelf | ItemType.WorkoutMachine;

	protected override string[] AvailableFurnitureItemNames => new string[2] { "ba:itemname_publicshower", "ba:itemname_gymlockers" };

	protected override bool IsItemNamePointOfSale(string itemName)
	{
		return ItemsGetter.GetByName(itemName).HasTag(TagRef.Itemtag.isgymtrainingstation);
	}

	public override void SetUp(BuildingRegistration registration, int hour)
	{
		base.SetUp(registration, hour);
		_workoutMachines.Clear();
		foreach (ItemInstance item in availableFurniture)
		{
			if ((ItemsGetter.GetByName(item.itemName).type & ItemType.WorkoutMachine) != 0)
			{
				_workoutMachines.Add(item);
			}
		}
	}

	protected override void ProcessCustomer(CustomerEntry customerEntry)
	{
		CitizenData citizenData = CitizenHelper.CreateRandomCitizenDataNonAlloc(buildingRegistration.Neighborhood);
		if (ProcessEntranceFee(customerEntry, citizenData))
		{
			int num = customerEntry.order.entries.Count;
			while (num > 0)
			{
				num--;
				if (!customerEntry.order.entries[num].processed)
				{
					OrderEntry orderEntry = ProcessItemInOrder(customerEntry.order.entries[num], citizenData, null);
					if (orderEntry != null)
					{
						customerEntry.order.entries.Add(orderEntry);
					}
				}
			}
			AddDirtToToiletsAndSinks();
			AddDirtToUsedFurniture(customerEntry);
		}
		float customerService = pointOfSales.GetRandom()?.customerService ?? 0f;
		Order order = customerEntry.order;
		if (order.timestamp == null)
		{
			order.timestamp = new Timestamp(TimeHelper.CurrentDay, currentHour, 0f);
		}
		customerEntry.order.Complete(buildingRegistration, customerService, businessCleanliness);
		buildingRegistration.unprocessedCompletedOrders.Add(customerEntry.order);
		customerEntry.completed = true;
	}

	protected override OrderEntry ProcessItemInOrder(OrderEntry orderEntry, CitizenData citizenData, PointOfSale pointOfSale)
	{
		if (!availableProducers.TryGetValue(orderEntry.itemName, out var value))
		{
			lastProducerUsed = null;
			return null;
		}
		ItemInstance random = value.GetRandom();
		if (random == null)
		{
			lastProducerUsed = null;
			return null;
		}
		CompleteOrderEntry(orderEntry, citizenData, random);
		lastProducerUsed = random;
		if (!orderEntry.paid)
		{
			return null;
		}
		random.SubtractFromStock();
		CargoInstance stockInstance = random.GetStockInstance();
		if (stockInstance.itemName == orderEntry.itemName && stockInstance.amount <= 0)
		{
			availableProducers[orderEntry.itemName].Remove(random);
		}
		return null;
	}

	protected override void AddDirtToUsedFurniture(CustomerEntry customerEntry)
	{
		int num = Random.Range(1, 4);
		for (int i = 0; i < num; i++)
		{
			BuildingCleanlinessHelper.AddDirtBulkEntry(_workoutMachines.GetRandom(), buildingRegistration);
		}
	}
}
