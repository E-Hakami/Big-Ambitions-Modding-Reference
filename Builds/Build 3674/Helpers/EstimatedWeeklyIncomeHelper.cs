using System.Collections.Generic;
using System.Linq;
using AI.Customers.CustomerEntries;
using BigAmbitions.Items;
using BigAmbitions.Rivals;
using BigAmbitions.Tags;
using Blueprints;
using Buildings;
using Buildings.BuildingTypes.Shared;
using BusinessLayoutSets;
using Entities;
using Streets;
using UnityEngine;

namespace Helpers;

public static class EstimatedWeeklyIncomeHelper
{
	private class Employee
	{
		public string PrimarySkillName { get; set; }

		public float PrimarySkillValue { get; set; }
	}

	public static float GetEstimatedWeeklyIncome(this BuildingRegistration registration, BusinessLayoutSet layoutSet = null, int currentDay = -1)
	{
		Building building = BuildingHelper.GetBuilding(registration.Address);
		if (layoutSet == null)
		{
			layoutSet = BusinessLayoutSetHelper.GetOrLoadBusinessLayoutSet(registration.businessTypeName, new BuildingSizeInfo(building), registration.Layout, warnIfNotFound: false);
		}
		if (layoutSet == null)
		{
			Debug.LogError("Couldn't calculate estimated weekly income for " + registration.Address.ToFormattedString());
			return 0f;
		}
		int hoursOpenPerWeek = CalculateHoursOpenPerWeek(registration);
		if (currentDay == -1)
		{
			currentDay = TimeHelper.CurrentDay;
		}
		float num = CalculateProductsIncome(registration, building, layoutSet, currentDay);
		int num2 = building.GetBuildingDailyMarketRent() * 7;
		float num3 = CalculateWeeklyWages(registration, layoutSet, hoursOpenPerWeek);
		return num - (float)num2 - num3;
	}

	private static int CalculateHoursOpenPerWeek(BuildingRegistration registration)
	{
		int num = 0;
		foreach (ScheduleDay scheduleDay in registration.scheduleDays)
		{
			if (!scheduleDay.isOpen)
			{
				continue;
			}
			foreach (OpeningHourSlot openingHourSlot in scheduleDay.openingHourSlots)
			{
				num += openingHourSlot.endingHour - openingHourSlot.startingHour;
			}
		}
		return num;
	}

	private static int CalculateCustomersPerWeek(BuildingRegistration registration)
	{
		CustomerEntriesCalculator customerEntriesCalculator = CustomerEntriesCalculatorFactory.GetCustomerEntriesCalculator(registration);
		float initialCustomers = customerEntriesCalculator.GetInitialCustomers();
		if (initialCustomers == 0f)
		{
			return 0;
		}
		int num = 0;
		foreach (ScheduleDay scheduleDay in registration.scheduleDays)
		{
			if (!scheduleDay.isOpen)
			{
				continue;
			}
			foreach (OpeningHourSlot openingHourSlot in scheduleDay.openingHourSlots)
			{
				for (int i = openingHourSlot.startingHour; i < openingHourSlot.endingHour; i++)
				{
					int customersByHour = customerEntriesCalculator.GetCustomersByHour(i, scheduleDay.day, initialCustomers);
					if (customersByHour > 0)
					{
						num += customersByHour;
					}
				}
			}
		}
		return num;
	}

	private static float CalculateProductsIncome(BuildingRegistration registration, Building building, BusinessLayoutSet layoutSet, int currentDay)
	{
		List<string> list = registration.retailPrices.Select((RetailPrice x) => x.itemName).ToList();
		float num = 0f;
		foreach (string item in list)
		{
			num += GetProductBenefit(registration, building, layoutSet, item, currentDay);
		}
		int num2 = CalculateCustomersPerWeek(registration);
		return num * (float)num2;
	}

	private static float GetProductBenefit(BuildingRegistration registration, Building building, BusinessLayoutSet layoutSet, string productName, int currentDay)
	{
		float result = 0f;
		BigAmbitions.Items.Item byName = ItemsGetter.GetByName(productName);
		bool flag = (byName.type & ItemType.ServiceProduct) != 0;
		bool flag2 = (byName.type & ItemType.RetailProduct) != 0;
		bool flag3 = false;
		if (flag2)
		{
			foreach (BusinessLayoutSets.Item item in layoutSet.Items)
			{
				if (!(item.playerItemPurchaserSettings.itemName != byName.itemName))
				{
					flag3 = true;
					break;
				}
			}
		}
		if (flag || (flag2 & flag3))
		{
			float num = 1f;
			if (flag2)
			{
				int num2 = DaysFromLastWeekThatTheItemWasNotSoldDueToMarketEvents(productName, registration, currentDay);
				num = 1f - (float)num2 / 7f;
			}
			float num3 = ((byName.isADemandedProduct && BuildingTypeHelper.GetData(building).HasTag(TagRef.Buildingtypetag.sellsphysicalproducts)) ? ((float)ProductMarketHelper.GetNeighborhoodDemand(byName.itemName, building.Neighbourhood).demand / 100f) : 1f);
			float num4 = 0f;
			foreach (RetailPrice retailPrice in registration.retailPrices)
			{
				if (!(retailPrice.itemName != productName))
				{
					num4 = retailPrice.price;
					break;
				}
			}
			float wholesalePrice = byName.GetWholesalePrice();
			BusinessType data = BusinessTypeHelper.GetData(registration);
			float productImpact = data.GetProductImpact(productName);
			float num5 = ((!(data.maxAmountPerProduct > 1f)) ? data.maxAmountPerProduct : (data.IsPrimaryProduct(productName) ? ((1f + data.maxAmountPerProduct) * 0.5f) : 1f));
			result = (num4 - wholesalePrice) * productImpact * num5 * byName.productSalesRatio * num3 * num;
		}
		return result;
	}

	private static int DaysFromLastWeekThatTheItemWasNotSoldDueToMarketEvents(string itemName, BuildingRegistration registration, int currentDay)
	{
		if (registration.buildingOwnerRivalId.IsSpecialRival())
		{
			return 0;
		}
		AiBusinessGoodsSource goodsSource = CompetitionHelper.GetBusinessDefault(registration.BusinessName).goodsSource;
		int num = 0;
		int num2 = currentDay - 6;
		foreach (MarketEvent marketEvent in SaveGameManager.Current.marketEvents)
		{
			if (marketEvent.itemName != itemName || (marketEvent.type != MarketEventType.ProductBackorder && marketEvent.type != MarketEventType.ProductShortage) || (goodsSource == AiBusinessGoodsSource.Import && !string.IsNullOrEmpty(marketEvent.neighbourhood)) || (goodsSource == AiBusinessGoodsSource.Wholesale && marketEvent.neighbourhood != registration.Neighborhood))
			{
				continue;
			}
			int num3 = ((marketEvent.type == MarketEventType.ProductBackorder) ? (marketEvent.startDay + 1) : (marketEvent.startDay + ProductMarketHelper.ProductMarketSettings.startDayToEmptyShelvesWhenThereIsAShortage));
			int num4 = marketEvent.startDay + marketEvent.durationInDays - 1;
			if (marketEvent.stopped)
			{
				num4 = ((marketEvent.dayStopped >= num4) ? num4 : marketEvent.dayStopped);
			}
			if (num2 <= num4)
			{
				num = ((num2 > num3) ? (num + (num4 + 1 - num2)) : (num + (num4 + 1 - num3)));
				if (currentDay < num4)
				{
					num = Mathf.Max(num - (num4 - currentDay), 0);
				}
			}
		}
		return num;
	}

	private static float CalculateWeeklyWages(BuildingRegistration registration, BusinessLayoutSet layoutSet, int hoursOpenPerWeek)
	{
		List<Employee> list = GatherEmployees(registration);
		Dictionary<string, float> dictionary = new Dictionary<string, float>();
		Dictionary<string, int> dictionary2 = new Dictionary<string, int>();
		foreach (Employee item in list)
		{
			if (dictionary.TryAdd(item.PrimarySkillName, 0f))
			{
				dictionary2[item.PrimarySkillName] = 0;
			}
			dictionary[item.PrimarySkillName] += EmployeeHelper.CalculateHourlyWageForSkill(item.PrimarySkillName, item.PrimarySkillValue);
			dictionary2[item.PrimarySkillName]++;
		}
		foreach (string item2 in dictionary.Keys.ToList())
		{
			dictionary[item2] /= dictionary2[item2];
		}
		float num = 0f;
		foreach (BusinessLayoutSets.Item item3 in layoutSet.Items)
		{
			BigAmbitions.Items.Item byName = ItemsGetter.GetByName(item3.itemName);
			if ((byName.type & ItemType.EmployeeWorkstation) == 0)
			{
				continue;
			}
			string[] suitableSkills = byName.suitableSkills;
			foreach (string key in suitableSkills)
			{
				if (dictionary.TryGetValue(key, out var value))
				{
					num += value * (float)hoursOpenPerWeek;
				}
			}
		}
		return num;
	}

	private static List<Employee> GatherEmployees(BuildingRegistration registration)
	{
		if (registration.aiEmployees.Count == 0)
		{
			registration.GenerateAiBusinessEmployees();
		}
		List<Employee> list = new List<Employee>();
		foreach (AiBusinessEmployeeData aiEmployee in registration.aiEmployees)
		{
			list.Add(new Employee
			{
				PrimarySkillName = aiEmployee.primarySkillName,
				PrimarySkillValue = aiEmployee.primarySkillValue
			});
		}
		List<EmployeeInstance> poachedEmployees = registration.poachedEmployees;
		if (poachedEmployees != null && poachedEmployees.Count > 0)
		{
			list.AddRange(registration.poachedEmployees.Select((EmployeeInstance e) => new Employee
			{
				PrimarySkillName = e.characterData.skills[0].name,
				PrimarySkillValue = e.characterData.skills[0].value
			}));
		}
		return list;
	}
}
