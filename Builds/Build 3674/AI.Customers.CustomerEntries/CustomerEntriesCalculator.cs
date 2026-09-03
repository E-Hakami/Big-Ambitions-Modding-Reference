using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using Buildings;
using Entities;
using Extensions;
using Helpers;
using UnityEngine;

namespace AI.Customers.CustomerEntries;

public class CustomerEntriesCalculator
{
	protected BuildingRegistration registration;

	protected BusinessType businessType;

	protected readonly List<CustomerEntry> customersEntryList = new List<CustomerEntry>();

	public void Init(BuildingRegistration buildingRegistration)
	{
		registration = buildingRegistration;
		businessType = BusinessTypeHelper.GetData(registration);
	}

	public virtual float GetInitialCustomers()
	{
		return BuildingSizeHelper.GetData(registration).GetCustomerCapacity(registration.BuildingCached.BuildingType, registration.BuildingCached.BuildingVersion);
	}

	public virtual int GetCustomersByHour(int hour, DayOfWeekOrdered day, float initialCustomers)
	{
		return Mathf.CeilToInt(initialCustomers * GetPromotionMultiplier() * GetDayAndHourMultiplier(hour, day));
	}

	protected float GetPromotionMultiplier()
	{
		int num = (registration.RentedByPlayer ? registration.promotion.total : registration.BuildingCached.trafficIndex);
		return SaveGameManager.Current.gameVariables.baseCustomerPromotionMultiplier + 0.75f * ((float)num / 100f);
	}

	protected float GetCustomerSatisfactionMultiplier()
	{
		return (float)(registration.satisfaction.overall - 50) / 100f + 1f;
	}

	protected float GetDayAndHourMultiplier(int hour, DayOfWeekOrdered day)
	{
		return GetDayMultiplier(day) * GetHourMultiplier(hour);
	}

	private float GetDayMultiplier(DayOfWeekOrdered day)
	{
		foreach (DayFactorMultiplier dayFactorMultiplier in businessType.dayFactorMultipliers)
		{
			if (dayFactorMultiplier.dayOfWeekOrdered == day)
			{
				return dayFactorMultiplier.multiplier;
			}
		}
		return 0f;
	}

	private float GetHourMultiplier(int hour)
	{
		foreach (HourlyFactorMultiplier hourlyFactorMultiplier in businessType.hourlyFactorMultipliers)
		{
			if (hour.InRange(hourlyFactorMultiplier.startingHour, hourlyFactorMultiplier.endingHour))
			{
				return hourlyFactorMultiplier.multiplier;
			}
		}
		return 0f;
	}

	public List<CustomerEntry> GetCustomersEntriesForHour(int hour, int customersThisHour, float demandsWeight)
	{
		SetUpCustomersEntriesListForHour(hour, customersThisHour, demandsWeight);
		if (customersEntryList.Count == 0)
		{
			return customersEntryList;
		}
		AddProductsToCustomersEntriesList();
		if (!businessType.acceptCustomersWithoutOrderEntries)
		{
			for (int num = customersEntryList.Count - 1; num >= 0; num--)
			{
				if (customersEntryList[num].order.entries.Count == 0)
				{
					customersEntryList.RemoveAt(num);
				}
			}
		}
		return customersEntryList;
	}

	private void SetUpCustomersEntriesListForHour(int hour, int customersThisHour, float demandsWeight)
	{
		customersEntryList.Clear();
		int num = 0;
		foreach (Order unprocessedCompletedOrder in registration.unprocessedCompletedOrders)
		{
			if (unprocessedCompletedOrder != null)
			{
				Timestamp timestamp = unprocessedCompletedOrder.timestamp;
				if (timestamp != null && timestamp.Hour == hour)
				{
					num++;
				}
			}
		}
		if (customersThisHour - num <= 0)
		{
			return;
		}
		int day = SaveGameManager.Current.Day;
		int num2 = customersThisHour - num;
		for (int i = 0; i < num2; i++)
		{
			CustomerEntry customerEntry = new CustomerEntry
			{
				spawnTime = new Timestamp
				{
					Day = day,
					Hour = hour,
					Minute = GetCustomerSpawnMinute(hour)
				}
			};
			customerEntry.order.timestamp = customerEntry.spawnTime;
			foreach (CustomerDemandSet customerDemandSet in businessType.customerDemandSets)
			{
				if (Random.Range(0f, 1f) < demandsWeight * customerDemandSet.weight)
				{
					customerEntry.order.customerDemandTypes.Add(customerDemandSet.type);
				}
			}
			customersEntryList.Add(customerEntry);
		}
	}

	private static int GetMinMinute(int hour)
	{
		int result = 0;
		if (hour == SaveGameManager.Current.Hour && BuildingManager.IsInsideBuilding)
		{
			result = (int)SaveGameManager.Current.Minute;
		}
		return result;
	}

	public virtual int GetCustomerSpawnMinute(int hour)
	{
		return Random.Range(GetMinMinute(hour), 59);
	}

	protected virtual void AddProductsToCustomersEntriesList()
	{
	}
}
