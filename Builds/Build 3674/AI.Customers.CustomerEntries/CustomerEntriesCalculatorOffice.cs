using System.Linq;
using BigAmbitions.DayNightCycle;
using Entities;
using Helpers;
using UnityEngine;

namespace AI.Customers.CustomerEntries;

public class CustomerEntriesCalculatorOffice : CustomerEntriesCalculator
{
	private const float DecimalValueToStartRoundingUp = 0.2f;

	public override int GetCustomersByHour(int hour, DayOfWeekOrdered day, float initialCustomers)
	{
		string text = registration.cachedAvailableProducts.FirstOrDefault();
		if (string.IsNullOrEmpty(text))
		{
			return 0;
		}
		float num = (float)ProductMarketHelper.GetNeighborhoodDemand(text, registration.Neighborhood).demand / 100f;
		if (!registration.RentedByPlayer)
		{
			return Mathf.CeilToInt(initialCustomers * GetPromotionMultiplier() * GetDayAndHourMultiplier(hour, day) * num);
		}
		int a = Mathf.CeilToInt(initialCustomers * GetCustomerSatisfactionMultiplier() * GetPromotionMultiplier() * GetDayAndHourMultiplier(hour, day) * num - 0.2f);
		int availableWorkersOnHourAndDay = GetAvailableWorkersOnHourAndDay(hour, day);
		return Mathf.Min(a, availableWorkersOnHourAndDay);
	}

	private int GetAvailableWorkersOnHourAndDay(int hour, DayOfWeekOrdered day)
	{
		if (registration.scheduleDays == null)
		{
			return 0;
		}
		int num = 0;
		foreach (ScheduleDay scheduleDay in registration.scheduleDays)
		{
			if (scheduleDay.day != day)
			{
				continue;
			}
			foreach (WorkShift workShift in scheduleDay.workShifts)
			{
				if (!string.IsNullOrEmpty(workShift.employeeId) && workShift.startingHour <= hour && workShift.endingHour > hour && registration.itemInstances.TryGetValue(workShift.itemInstanceId, out var value))
				{
					num += value.ItemCached.addedCustomersPerHour;
				}
			}
		}
		return num;
	}

	protected override void AddProductsToCustomersEntriesList()
	{
		string text = registration.cachedAvailableProducts.FirstOrDefault();
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		foreach (CustomerEntry customersEntry in customersEntryList)
		{
			customersEntry.order.entries.Add(new OrderEntry
			{
				itemName = text
			});
		}
	}
}
