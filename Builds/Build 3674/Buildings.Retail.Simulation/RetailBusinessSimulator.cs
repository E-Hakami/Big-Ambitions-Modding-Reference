using System;
using System.Collections.Generic;
using AI.Citizens;
using AI.Customers.CustomerEntries;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Entities;
using Extensions;
using Helpers;
using Helpers.BusinessSimulation;
using UI;
using UnityEngine;

namespace Buildings.Retail.Simulation;

[CreateAssetMenu(menuName = "BusinessSimulator/Retail")]
public class RetailBusinessSimulator : BusinessSimulator
{
	private const float ToiletChance = 0.5f;

	private const float SinkChance = 0.9f;

	protected BusinessType businessType;

	protected readonly List<ItemInstance> availableFurniture = new List<ItemInstance>();

	protected readonly List<ItemInstance> toilets = new List<ItemInstance>();

	protected readonly List<ItemInstance> sinks = new List<ItemInstance>();

	protected readonly List<PointOfSale> pointOfSales = new List<PointOfSale>();

	protected readonly Dictionary<string, List<ItemInstance>> availableProducers = new Dictionary<string, List<ItemInstance>>();

	protected float businessCleanliness;

	protected ItemInstance lastProducerUsed;

	private readonly List<PointOfSale> _stockedPointOfSales = new List<PointOfSale>();

	private readonly Dictionary<string, List<ItemInstance>> _restockableItemsByStockItemName = new Dictionary<string, List<ItemInstance>>();

	protected virtual ItemType AvailableFurnitureItemTypes => (ItemType)0;

	protected virtual string[] AvailableFurnitureItemNames => Array.Empty<string>();

	protected virtual bool IsItemNamePointOfSale(string itemName)
	{
		return false;
	}

	public override void SetUp(BuildingRegistration registration, int hour)
	{
		base.SetUp(registration, hour);
		businessType = BusinessTypeHelper.GetData(buildingRegistration.businessTypeName);
		LoadAvailableFurniture(buildingRegistration);
		LoadPointsOfSaleWithCustomerSatisfactionThisHour();
		SetUpAvailableShowcaseShelves();
		businessCleanliness = buildingRegistration.GetCleanliness();
	}

	private void LoadAvailableFurniture(BuildingRegistration registration)
	{
		availableFurniture.Clear();
		toilets.Clear();
		sinks.Clear();
		foreach (ItemInstance value in registration.itemInstances.Values)
		{
			SetToiletsAndSinks(value);
			if (ShouldAddToAvailableFurniture(value.itemName) && !ItemHelper.HasAnyMissingRequirements(value))
			{
				availableFurniture.Add(value);
			}
		}
	}

	private void SetToiletsAndSinks(ItemInstance itemInstance)
	{
		if ((itemInstance.ItemCached.type & ItemType.Sink) != 0)
		{
			sinks.Add(itemInstance);
		}
		else if ((itemInstance.ItemCached.type & ItemType.Toilet) != 0)
		{
			toilets.Add(itemInstance);
		}
	}

	private bool ShouldAddToAvailableFurniture(string itemName)
	{
		string[] availableFurnitureItemNames = AvailableFurnitureItemNames;
		for (int i = 0; i < availableFurnitureItemNames.Length; i++)
		{
			if (availableFurnitureItemNames[i] == itemName)
			{
				return true;
			}
		}
		return (ItemsGetter.GetByName(itemName).type & AvailableFurnitureItemTypes) != 0;
	}

	private void LoadPointsOfSaleWithCustomerSatisfactionThisHour()
	{
		pointOfSales.Clear();
		foreach (ItemInstance item in availableFurniture)
		{
			if (IsItemNamePointOfSale(item.itemName))
			{
				float num = EmployeeHelper.GetEmployeeAtStationAndHour(buildingRegistration, item.id, currentHour)?.GetCustomerSatisfaction(item) ?? 0f;
				if (!(num <= 0f))
				{
					pointOfSales.Add(new PointOfSale
					{
						instance = item,
						customerService = num
					});
				}
			}
		}
	}

	private void SetUpAvailableShowcaseShelves()
	{
		availableProducers.Clear();
		foreach (ItemInstance item in availableFurniture)
		{
			if ((item.ItemCached.type & ItemType.ShowcaseShelf) != 0)
			{
				CargoInstance stockInstance = item.GetStockInstance();
				if (!string.IsNullOrEmpty(stockInstance.itemName) && stockInstance.amount > 0)
				{
					if (availableProducers.TryGetValue(stockInstance.itemName, out var value))
					{
						value.Add(item);
						continue;
					}
					availableProducers[stockInstance.itemName] = new List<ItemInstance> { item };
				}
			}
			else
			{
				if ((item.ItemCached.type & ItemType.EmployeeWorkstation) == 0)
				{
					continue;
				}
				string[] itemsToProduce = item.ItemCached.producerSettings.itemsToProduce;
				foreach (string key in itemsToProduce)
				{
					if (availableProducers.TryGetValue(key, out var value2))
					{
						value2.Add(item);
						continue;
					}
					availableProducers[key] = new List<ItemInstance> { item };
				}
			}
		}
	}

	public override void SimulateCurrentHour()
	{
		ProcessAllCustomersFromThisHour();
		RestockShelvesIfItsTime();
		if (!InstanceBehavior<UIs>.Instance.timeMachine.isRunning)
		{
			UpdateStockTasksFromFurniture(buildingRegistration);
		}
	}

	private void ProcessAllCustomersFromThisHour()
	{
		int maxCustomerCapacityThisHour = GetMaxCustomerCapacityThisHour();
		List<CustomerEntry> list = CustomerEntriesHelper.GetEntriesByAddress(buildingRegistration.Address).FindAll((CustomerEntry x) => x.spawnTime.Hour == currentHour);
		int num = 0;
		foreach (CustomerEntry item in list)
		{
			if (num < maxCustomerCapacityThisHour)
			{
				lastProducerUsed = null;
				ProcessCustomer(item);
			}
			else
			{
				item.completed = true;
			}
			num++;
		}
	}

	private int GetMaxCustomerCapacityThisHour()
	{
		int customerCapacity = BuildingSizeHelper.GetData(buildingRegistration).GetCustomerCapacity(buildingRegistration.BuildingCached.BuildingType, buildingRegistration.BuildingCached.BuildingVersion);
		IEnumerable<Item.ItemCapacity> itemsSortedByCapacity = availableFurniture.GetItemsSortedByCapacity(buildingRegistration, requireEmployee: true, checkMissingRequirements: false);
		int num = customerCapacity;
		foreach (Item.ItemCapacity item in itemsSortedByCapacity)
		{
			if (item.CustomersLimit < num)
			{
				num = item.CustomersLimit;
			}
		}
		return num;
	}

	protected virtual void ProcessCustomer(CustomerEntry customerEntry)
	{
		CitizenData citizenData = CitizenHelper.CreateRandomCitizenDataNonAlloc(buildingRegistration.Neighborhood);
		Order order;
		if (!ProcessEntranceFee(customerEntry, citizenData))
		{
			customerEntry.order.completed = true;
			order = customerEntry.order;
			if (order.timestamp == null)
			{
				order.timestamp = new Timestamp(TimeHelper.CurrentDay, currentHour, 0f);
			}
			buildingRegistration.unprocessedCompletedOrders.Add(customerEntry.order);
			customerEntry.completed = true;
			return;
		}
		PointOfSale idealPointOfSale = GetIdealPointOfSale();
		int num = customerEntry.order.entries.Count;
		while (num > 0)
		{
			num--;
			if (!customerEntry.order.entries[num].processed)
			{
				OrderEntry orderEntry = ProcessItemInOrder(customerEntry.order.entries[num], citizenData, idealPointOfSale);
				if (orderEntry != null)
				{
					customerEntry.order.entries.Add(orderEntry);
				}
			}
		}
		AddDirtToToiletsAndSinksIfNeeded(customerEntry);
		AddDirtToUsedFurniture(customerEntry);
		if (idealPointOfSale != null)
		{
			ProcessPaperBag(customerEntry, idealPointOfSale);
			BuildingCleanlinessHelper.AddDirtBulkEntry(idealPointOfSale.instance, buildingRegistration);
		}
		order = customerEntry.order;
		if (order.timestamp == null)
		{
			order.timestamp = new Timestamp(TimeHelper.CurrentDay, currentHour, 0f);
		}
		customerEntry.order.Complete(buildingRegistration, GetCustomerService(idealPointOfSale), businessCleanliness);
		buildingRegistration.unprocessedCompletedOrders.Add(customerEntry.order);
		customerEntry.completed = true;
	}

	protected bool ProcessEntranceFee(CustomerEntry customerEntry, CitizenData citizenData)
	{
		if (!businessType.hasEntranceFee)
		{
			return true;
		}
		string entranceFeeNameForBusinessType = BusinessTypeHelper.GetEntranceFeeNameForBusinessType(businessType);
		if (string.IsNullOrEmpty(entranceFeeNameForBusinessType))
		{
			return true;
		}
		OrderEntry orderEntry = new OrderEntry
		{
			itemName = entranceFeeNameForBusinessType
		};
		CompleteOrderEntry(orderEntry, citizenData, null);
		customerEntry.order.entries.Add(orderEntry);
		return orderEntry.paid;
	}

	protected virtual OrderEntry ProcessItemInOrder(OrderEntry orderEntry, CitizenData citizenData, PointOfSale pointOfSale)
	{
		Item byName = ItemsGetter.GetByName(orderEntry.itemName);
		if (byName != null && (byName.type & ItemType.ServiceProduct) != 0)
		{
			lastProducerUsed = null;
			CompleteOrderEntry(orderEntry, citizenData, null);
			return null;
		}
		if (pointOfSale == null || (businessType.HasTag(TagRef.Businesstag.customersneedpaperbags) && pointOfSale.instance.GetStockInstance().amount <= 0))
		{
			orderEntry.processed = true;
			lastProducerUsed = null;
			return null;
		}
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
		if (orderEntry.paid)
		{
			random.SubtractFromStock();
			CargoInstance stockInstance = random.GetStockInstance();
			if (stockInstance.itemName == orderEntry.itemName && stockInstance.amount <= 0)
			{
				availableProducers[orderEntry.itemName].Remove(random);
			}
		}
		return null;
	}

	protected void CompleteOrderEntry(OrderEntry orderEntry, CitizenData citizenData, ItemInstance producerInstance)
	{
		orderEntry.available = true;
		orderEntry.price = ItemHelper.GetPrice(orderEntry.itemName, buildingRegistration);
		orderEntry.priceAccceptable = citizenData.IsPriceAcceptable(orderEntry.itemName, orderEntry.price);
		orderEntry.paid = orderEntry.priceAccceptable;
		orderEntry.processed = true;
		if (producerInstance != null)
		{
			if ((ItemsGetter.GetByName(orderEntry.itemName).type & ItemType.ServiceProduct) == 0)
			{
				orderEntry.wholesalePrice = producerInstance.GetStockInstance().pricePerUnit;
			}
			if (producerInstance != lastProducerUsed)
			{
				BuildingCleanlinessHelper.AddDirtBulkEntry(producerInstance, buildingRegistration);
			}
		}
	}

	protected void ProcessPaperBag(CustomerEntry customerEntry, PointOfSale pointOfSale)
	{
		if (businessType.HasTag(TagRef.Businesstag.customersneedpaperbags) && customerEntry.order.entries.Exists((OrderEntry x) => x.paid && (ItemsGetter.GetByName(x.itemName).type & ItemType.RetailProduct) != 0 && x.price > 0f))
		{
			pointOfSale.instance.SubtractFromStock();
			customerEntry.AddPaperBagEntryToOrder(pointOfSale.instance.GetStockInstance().pricePerUnit);
		}
	}

	protected virtual void AddDirtToUsedFurniture(CustomerEntry customerEntry)
	{
	}

	protected void AddDirtToToiletsAndSinksIfNeeded(CustomerEntry customerEntry)
	{
		bool flag = false;
		foreach (OrderEntry entry in customerEntry.order.entries)
		{
			if (entry.paid)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			AddDirtToToiletsAndSinks();
		}
	}

	protected void AddDirtToToiletsAndSinks()
	{
		if (0.5f.Probability() && toilets.Count > 0)
		{
			BuildingCleanlinessHelper.AddDirtBulkEntry(toilets.GetRandom(), buildingRegistration);
			if (0.9f.Probability() && sinks.Count > 0)
			{
				BuildingCleanlinessHelper.AddDirtBulkEntry(sinks.GetRandom(), buildingRegistration);
			}
		}
	}

	protected virtual PointOfSale GetIdealPointOfSale()
	{
		_stockedPointOfSales.Clear();
		foreach (PointOfSale pointOfSale in pointOfSales)
		{
			if (pointOfSale.instance.GetStockInstance().amount > 0)
			{
				_stockedPointOfSales.Add(pointOfSale);
			}
		}
		return _stockedPointOfSales.GetRandom();
	}

	protected virtual float GetCustomerService(PointOfSale pointOfSale)
	{
		return pointOfSale?.customerService ?? 0f;
	}

	public void RestockShelvesIfItsTime()
	{
		if (IsRestockTime(buildingRegistration))
		{
			RestockShelves();
		}
	}

	private bool IsRestockTime(BuildingRegistration registration)
	{
		if (registration.temporarilyClosed)
		{
			return false;
		}
		DayOfWeekOrdered dayOfWeek = TimeHelper.GetDayOfWeek();
		ScheduleDay scheduleDay = registration.scheduleDays.Find((ScheduleDay schedule) => schedule.day == dayOfWeek);
		if (scheduleDay?.workShifts == null || !scheduleDay.isOpen)
		{
			return false;
		}
		int num = -1;
		foreach (WorkShift workShift in scheduleDay.workShifts)
		{
			if (workShift.type == WorkShiftType.Default && workShift.endingHour > num)
			{
				num = workShift.endingHour;
			}
		}
		return num == currentHour + 1;
	}

	private void RestockShelves()
	{
		foreach (List<ItemInstance> value in GetRestockableItemsByStockItemName().Values)
		{
			ReStockingHelper.RedistributeStockByPercentage(buildingRegistration, value);
		}
	}

	private Dictionary<string, List<ItemInstance>> GetRestockableItemsByStockItemName()
	{
		_restockableItemsByStockItemName.Clear();
		foreach (ItemInstance item in availableFurniture)
		{
			if (item.CanRestockItem())
			{
				string itemName = item.GetStockInstance().itemName;
				if (!_restockableItemsByStockItemName.TryGetValue(itemName, out var value))
				{
					value = new List<ItemInstance>();
					_restockableItemsByStockItemName.Add(itemName, value);
				}
				value.Add(item);
			}
		}
		return _restockableItemsByStockItemName;
	}

	private void UpdateStockTasksFromFurniture(BuildingRegistration registration)
	{
		foreach (ItemInstance value in registration.itemInstances.Values)
		{
			if ((value.ItemCached.type & (ItemType.PointOfSale | ItemType.ShowcaseShelf)) != 0 && !string.IsNullOrEmpty(value.GetStockInstance().itemName) && !ItemHelper.HasAnyMissingRequirements(value))
			{
				value.UpdateStockTodoTasks();
			}
		}
	}

	public override void OnTimeMachineEnd(BuildingRegistration registration)
	{
		UpdateStockTasksFromFurniture(registration);
	}
}
