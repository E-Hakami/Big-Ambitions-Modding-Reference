using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings;
using Helpers;
using Seasons;
using UnityEngine;

namespace AI.Customers.CustomerEntries;

public static class CustomerEntriesHelper
{
	private static readonly Dictionary<Address, List<CustomerEntry>> BusinessCustomerEntries = new Dictionary<Address, List<CustomerEntry>>();

	public static void Init()
	{
		DayOfWeekOrdered dayOfWeek = TimeHelper.GetDayOfWeek();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			UpdateCustomerEntriesForPlayerBusiness(buildingRegistration, dayOfWeek);
		}
	}

	public static void UpdateCustomerEntriesForAllPlayerBusinesses()
	{
		BusinessCustomerEntries.Clear();
		DayOfWeekOrdered dayOfWeek = TimeHelper.GetDayOfWeek();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			UpdateCustomerEntriesForPlayerBusiness(buildingRegistration, dayOfWeek);
		}
	}

	public static void UpdateCustomerEntriesForPlayerBusiness(BuildingRegistration registration, DayOfWeekOrdered day)
	{
		if (!registration.RentedByPlayer)
		{
			return;
		}
		List<CustomerEntry> businessCustomerEntries = GetBusinessCustomerEntries(registration);
		if (!ShouldEntriesBeCreated(registration))
		{
			BusinessCustomerEntries[registration.Address] = businessCustomerEntries;
			return;
		}
		CustomerEntriesCalculator customerEntriesCalculator = CustomerEntriesCalculatorFactory.GetCustomerEntriesCalculator(registration);
		float initialCustomers = customerEntriesCalculator.GetInitialCustomers();
		if (initialCustomers == 0f)
		{
			return;
		}
		float customerDemandsWeight = NeighborhoodHelper.GetData(registration.Neighborhood).customerDemandsWeight;
		int hour;
		for (hour = SaveGameManager.Current.Hour; hour < 24; hour++)
		{
			if (BusinessHelper.IsBusinessOpen(registration, hour))
			{
				int customersByHour = customerEntriesCalculator.GetCustomersByHour(hour, day, initialCustomers);
				customersByHour -= businessCustomerEntries.Count((CustomerEntry x) => x.spawnTime.Hour == hour);
				if (customersByHour > 0)
				{
					businessCustomerEntries.AddRange(customerEntriesCalculator.GetCustomersEntriesForHour(hour, customersByHour, customerDemandsWeight));
				}
			}
		}
		BusinessCustomerEntries[registration.Address] = businessCustomerEntries;
	}

	private static List<CustomerEntry> GetBusinessCustomerEntries(BuildingRegistration registration)
	{
		if (BusinessCustomerEntries.TryGetValue(registration.Address, out var value))
		{
			value.RemoveAll((CustomerEntry x) => !x.completed);
			return value;
		}
		return new List<CustomerEntry>();
	}

	public static List<CustomerEntry> GetEntriesByAddress(Address address)
	{
		if (BusinessCustomerEntries.TryGetValue(address, out var value))
		{
			return value;
		}
		return GenerateAiEntries(address);
	}

	private static List<CustomerEntry> GenerateAiEntries(Address address)
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(address);
		List<CustomerEntry> list = new List<CustomerEntry>();
		List<string> list2 = buildingRegistration.cachedAvailableProducts;
		if (list2 == null || list2.Count == 0)
		{
			list2 = buildingRegistration.GetListOfItemsForSale();
			if (list2 == null || list2.Count == 0)
			{
				BusinessCustomerEntries[address] = list;
				return list;
			}
			UpdateCachedProductsForAiBusiness(buildingRegistration, list2);
		}
		RemoveSeasonalItems(list2);
		float num = ((BusinessTypeHelper.GetPrimaryProducts(buildingRegistration.businessTypeName).Count != 0) ? (BusinessHelper.GetMaxHcpsqmForRegistration(buildingRegistration) * (float)BuildingHelper.GetBuildingSquareMeters(buildingRegistration.Address)) : ((float)BuildingSizeHelper.GetData(buildingRegistration).GetCustomerCapacity(buildingRegistration.BuildingCached.BuildingType, buildingRegistration.BuildingCached.BuildingVersion)));
		if (num > 0f)
		{
			CustomerEntriesCalculator customerEntriesCalculator = CustomerEntriesCalculatorFactory.GetCustomerEntriesCalculator(buildingRegistration);
			DayOfWeekOrdered dayOfWeek = TimeHelper.GetDayOfWeek();
			float customerDemandsWeight = NeighborhoodHelper.GetData(buildingRegistration.Neighborhood).customerDemandsWeight;
			for (int i = 0; i < 24; i++)
			{
				if (BusinessHelper.IsBusinessOpen(buildingRegistration, i))
				{
					int customersByHour = customerEntriesCalculator.GetCustomersByHour(i, dayOfWeek, num);
					customersByHour = Random.Range(0, customersByHour);
					list.AddRange(customerEntriesCalculator.GetCustomersEntriesForHour(i, customersByHour, customerDemandsWeight));
				}
			}
		}
		BusinessCustomerEntries[address] = list;
		return list;
	}

	private static void UpdateCachedProductsForAiBusiness(BuildingRegistration registration, List<string> itemsForSale)
	{
		if (!registration.RentedByPlayer)
		{
			if (CompetitionHelper.ShouldRecalculateRetailPrices(registration))
			{
				CompetitionHelper.RecalculateRetailPrices(registration, null, itemsForSale);
			}
			else
			{
				registration.cachedAvailableProducts = itemsForSale;
			}
		}
	}

	private static void RemoveSeasonalItems(List<string> itemsForSale)
	{
		itemsForSale.RemoveAll(delegate(string x)
		{
			SeasonName season = ItemsGetter.GetByName(x).season;
			return season != SeasonName.None && (!PlayerPrefSettings.SeasonalDecorations || season != SeasonHelper.CurrentSeasonName);
		});
	}

	private static bool ShouldEntriesBeCreated(BuildingRegistration registration)
	{
		if (BusinessTypeHelper.GetData(registration).HasTag(TagRef.Businesstag.generatesrevenue))
		{
			return registration.AreAllRequirementsMet();
		}
		return false;
	}
}
