using System;
using BigAmbitions.DayNightCycle;
using Entities;
using Extensions;
using UnityEngine;

namespace AI.Customers.CustomerEntries;

public class CustomerEntriesCalculatorCasino : CustomerEntriesCalculator
{
	public override int GetCustomersByHour(int hour, DayOfWeekOrdered day, float initialCustomers)
	{
		return Mathf.CeilToInt(initialCustomers);
	}

	public override int GetCustomerSpawnMinute(int hour)
	{
		return 0;
	}

	protected override void AddProductsToCustomersEntriesList()
	{
		foreach (CustomerEntry customersEntry in customersEntryList)
		{
			int count = UnityEngine.Random.Range(1, Math.Min(registration.cachedAvailableProducts.Count, 3));
			foreach (string item in registration.cachedAvailableProducts.GetRandom(count))
			{
				customersEntry.order.entries.Add(new OrderEntry
				{
					itemName = item
				});
			}
		}
	}
}
