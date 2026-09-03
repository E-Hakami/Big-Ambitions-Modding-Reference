using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Blueprints;
using Entities;
using Enums;
using Extensions;
using Helpers;
using IngameDebugConsole;
using Localizor;
using UnityEngine;

namespace BigAmbitions.Rivals;

public static class RivalDefenseHelper
{
	public static readonly Queue<TextMessage> SpecialMessagesTmpQueue = new Queue<TextMessage>();

	private const float PriceReductionPercentageCap = 90f;

	private const int PriceReductionDurationCap = 12;

	private const int LowDemandNewBusinessesCap = 10;

	private const int HireBestEmployeesNumberCap = 40;

	private const int HireBestEmployeesWagePercentageIncreaseCap = 33;

	private static float GetRivalsDifficultyMultiplier()
	{
		return SaveGameManager.Current.gameVariables.rivalsDifficultyMultiplier;
	}

	public static void RunHourly()
	{
		foreach (SpecialRivalState rivalState in SaveGameManager.Current.specialRivalStates)
		{
			rivalState?.defenseStates?.RemoveAll(delegate(DefenseState defenseState)
			{
				if (defenseState.timestamp.IsInThePast())
				{
					HandleDefenseStateEnd(rivalState.rivalId, defenseState.defensiveMechanic);
					return true;
				}
				return false;
			});
		}
	}

	private static void HandleDefenseStateEnd(string rivalId, DefensiveMechanic defensiveMechanic)
	{
		if (defensiveMechanic == DefensiveMechanic.PriceReduction)
		{
			foreach (BuildingRegistration item in SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.businessOwnerRivalId == rivalId))
			{
				CompetitionHelper.RecalculateRetailPrices(item);
			}
		}
		ItemHelper.ClearPriceCaches();
	}

	[ConsoleMethod("ActivatePriceReduction", "Activates a price reduction for your top selling items in rival stores", new string[] { }, AutoCompleteMap = new string[] { "neighborhood=Neighborhoods" })]
	public static bool ActivatePriceReduction(string neighborhood, Priority aggression)
	{
		SpecialRival rival = RivalsHelper.GetSpecialRivalByNeighborhood(neighborhood);
		List<string> bestSellers = GetTopSellingProducts(UnityEngine.Random.Range(3, 5), neighborhood);
		SpecialRivalState rivalState = RivalsHelper.GetSpecialRivalState(rival.rivalData.id);
		if (rivalState.defenseStates != null)
		{
			bestSellers.RemoveAll((string x) => rivalState.defenseStates.Where((DefenseState d) => d.defensiveMechanic == DefensiveMechanic.PriceReduction).Any((DefenseState d) => d.affectedItems.Contains(x)));
		}
		if (bestSellers.Count < 1)
		{
			return false;
		}
		List<RetailPrice> list = (from business in SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.businessOwnerRivalId == rival.rivalData.id).ToList()
			select business.retailPrices.Where((RetailPrice x) => bestSellers.Contains(x.itemName)).ToList()).SelectMany((List<RetailPrice> retailPrices) => retailPrices).ToList();
		var (num, durationInDays) = GetPriceReductionValues(aggression);
		foreach (RetailPrice item in list)
		{
			item.price = Mathf.Max(item.price * num, CompetitionHelper.GetMinimumRivalPrice(item.itemName));
		}
		ItemHelper.ClearPriceCaches();
		AddDefenseState(rival.rivalData.id, DefensiveMechanic.PriceReduction, aggression, durationInDays, bestSellers);
		CreateImpactedProductsSpecialMessage(list.Select((RetailPrice x) => x.itemName).Distinct());
		return true;
	}

	private static void CreateImpactedProductsSpecialMessage(IEnumerable<string> items)
	{
		IEnumerable<string> values = items.Select((string x) => x.GetLocalization());
		Dictionary<string, string> messageData = new Dictionary<string, string> { 
		{
			"products",
			string.Join(", ", values)
		} };
		SpecialMessagesTmpQueue.Enqueue(new TextMessage("ba:messagetype_impacted_products", messageData, read: false, isNewInteraction: false, isSpecialMessage: true));
	}

	[ConsoleMethod("ActivateLowDemand", "The rival of this neighborhood will start new businesses of your most profitable business type", new string[] { }, AutoCompleteMap = new string[] { "neighborhood=Neighborhoods" })]
	public static bool ActivateLowDemand(string neighborhood, Priority aggression)
	{
		int lowDemandValues = GetLowDemandValues(aggression);
		SpecialRival rival = RivalsHelper.GetSpecialRivalByNeighborhood(neighborhood);
		BuildingRegistration bestBusiness = GetTopIncomeBusinesses(1, neighborhood)[0];
		List<BuildingRegistration> list = SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.AvailableForRent && x.Neighborhood == neighborhood && x.GetBuildingType() == bestBusiness.GetBuildingType() && RivalsHelper.IsBuildingSuitableForSpecialRival(x.GetBuildingType(), new BuildingSizeInfo(x))).ToList().Shuffle()
			.Take(lowDemandValues)
			.ToList();
		if (list.Count < lowDemandValues)
		{
			int count = lowDemandValues - list.Count;
			list.AddRange(SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.Neighborhood == neighborhood && !x.RentedByPlayer && x.businessOwnerRivalId != rival.rivalData.id && !RivalsHelper.IgnoredAddresses.Contains(x.Address) && BusinessTypeHelper.GetData(x).HasTag(TagRef.Businesstag.allowplayercreation) && x.GetBuildingType() == bestBusiness.GetBuildingType() && RivalsHelper.IsBuildingSuitableForSpecialRival(x.GetBuildingType(), new BuildingSizeInfo(x))).ToList().Shuffle()
				.Take(count)
				.ToList());
		}
		if (list.Count < lowDemandValues)
		{
			return false;
		}
		AiBusinessDefault businessDefault = RivalsHelper.GetBusinessDefault(rival.rivalData.id, bestBusiness.businessTypeName);
		string text = CompetitionHelper.GetRivalIdForBusinessDefault(businessDefault);
		if (text == "*")
		{
			text = RivalsHelper.GetRandomSpecialRivalId(canFallbackToImport: true, canFallbackToWholesale: false);
		}
		foreach (BuildingRegistration item in list)
		{
			CompetitionHelper.StartNewCompetitorBusiness(bestBusiness.businessTypeName, item, impactMarket: true, businessDefault, text);
		}
		AddDefenseState(rival.rivalData.id, DefensiveMechanic.LowDemand, aggression, 10);
		CreateOpenedBusinessesSpecialMessage(list.Count, bestBusiness.businessTypeName);
		return true;
	}

	private static void CreateOpenedBusinessesSpecialMessage(int numberOfBusinesses, string businessTypeName)
	{
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{
				"amount",
				numberOfBusinesses.ToString()
			},
			{
				"businessType",
				businessTypeName.GetLocalization()
			}
		};
		SpecialMessagesTmpQueue.Enqueue(new TextMessage("ba:messagetype_rivals_businesses_opened", messageData, read: false, isNewInteraction: false, isSpecialMessage: true));
	}

	[ConsoleMethod("ActivateHireEmployees", "The rival of this neighborhood will try to hire your best employees", new string[] { }, AutoCompleteMap = new string[] { "neighborhood=Neighborhoods" })]
	public static bool ActivateHireEmployees(string neighborhood, Priority aggression)
	{
		(int, int, int) hireBestEmployeesValues = GetHireBestEmployeesValues(aggression);
		int item = hireBestEmployeesValues.Item1;
		int item2 = hireBestEmployeesValues.Item2;
		int item3 = hireBestEmployeesValues.Item3;
		SpecialRival specialRivalByNeighborhood = RivalsHelper.GetSpecialRivalByNeighborhood(neighborhood);
		List<string> skills = specialRivalByNeighborhood.rivalData.ownedBusinesses.SelectMany((BuildingRegistration x) => BusinessTypeHelper.GetData(x).employeePrimarySkills).Distinct().ToList();
		List<string> rivalBusinessTypes = BusinessTypeHelper.GetPossibleBusinessTypes(skills);
		List<Address> businessAddresses = (from x in SaveGameManager.Current.BuildingRegistrations
			where x.RentedByPlayer && x.Neighborhood == neighborhood && rivalBusinessTypes.Contains(x.businessTypeName)
			select x.Address).ToList();
		List<EmployeeInstance> list = (from x in EmployeeHelper.GetEmployeeInstances()
			where businessAddresses.Contains(x.assignedAddress) && !x.isTrainingDay && x.IsPoachable && !x.isBeingReplaced
			orderby x.GetSkillValue(x.GetPrimarySkill()) + x.satisfaction descending
			select x).Take(item).ToList();
		if (list.Count < item / 2)
		{
			return false;
		}
		foreach (EmployeeInstance item4 in list)
		{
			item4.PoachByRival(specialRivalByNeighborhood.rivalData.id, item2, item3);
		}
		AddDefenseState(specialRivalByNeighborhood.rivalData.id, DefensiveMechanic.HireBestEmployees, aggression, item2, null, list.Select((EmployeeInstance x) => x.id).ToList());
		CreateAttemptingToPoachSpecialMessage(list.Count);
		return true;
	}

	private static void CreateAttemptingToPoachSpecialMessage(int numberOfEmployees)
	{
		Dictionary<string, string> messageData = new Dictionary<string, string> { 
		{
			"amount",
			numberOfEmployees.ToString()
		} };
		SpecialMessagesTmpQueue.Enqueue(new TextMessage("ba:messagetype_rivals_attempting_to_poach", messageData, read: false, isNewInteraction: false, isSpecialMessage: true));
	}

	private static void AddDefenseState(string rivalId, DefensiveMechanic defensiveMechanic, Priority aggression, int durationInDays, List<string> affectedItems = null, List<string> affectedEmployeeIds = null)
	{
		SpecialRivalState specialRivalState = RivalsHelper.GetSpecialRivalState(rivalId);
		Timestamp timestamp = TimeHelper.Now();
		timestamp.AddDays(durationInDays);
		SpecialRivalState specialRivalState2 = specialRivalState;
		if (specialRivalState2.defenseStates == null)
		{
			specialRivalState2.defenseStates = new List<DefenseState>();
		}
		specialRivalState.defenseStates.Add(new DefenseState
		{
			defensiveMechanic = defensiveMechanic,
			aggression = aggression,
			timestamp = timestamp,
			affectedItems = affectedItems,
			affectedEmployeeIds = affectedEmployeeIds
		});
	}

	private static List<string> GetTopSellingProducts(int topNumber, string neighborhood)
	{
		SpecialRival rival = RivalsHelper.GetSpecialRivalByNeighborhood(neighborhood);
		List<string> itemsSoldByRival = SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.Neighborhood == neighborhood && x.businessOwnerRivalId == rival.rivalData.id).SelectMany((BuildingRegistration reg) => reg.GetListOfItemsForSale()).ToList();
		return (from x in (from x in (from x in SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.RentedByPlayer && x.Neighborhood == neighborhood).SelectMany((BuildingRegistration buildingRegistration) => buildingRegistration.orderHistory)
					where x.dayNumber.InRange(SaveGameManager.Current.Day - 7, SaveGameManager.Current.Day)
					select x).SelectMany((OrderHistoryEntry x) => x.itemSales)
				where x.amountSold > 0 && itemsSoldByRival.Contains(x.itemName)
				group x by x.itemName into g
				orderby g.Sum((OrderHistoryEntry.ItemReport x) => x.amountSold) descending
				select g.Key).Select(ItemsGetter.GetByName)
			where (x.type & ItemType.RetailProduct) != 0
			select x.itemName).Take(topNumber).ToList();
	}

	private static List<BuildingRegistration> GetTopIncomeBusinesses(int topNumber, string neighborhood)
	{
		return (from x in SaveGameManager.Current.BuildingRegistrations
			where x.RentedByPlayer && x.Neighborhood == neighborhood && x.businessTypeName != "ba:businesstype_headquarters"
			orderby x.GetAvgDailyIncome(7) descending
			select x).Take(topNumber).ToList();
	}

	public unsafe static (float, int) GetPriceReductionValues(Priority aggression)
	{
		object obj = aggression switch
		{
			Priority.Low => (45, 5), 
			Priority.Medium => (55, 6), 
			Priority.High => (65, 8), 
			_ => (45, 5), 
		};
		int item = ((ValueTuple<int, int>*)(&obj))->Item1;
		int item2 = ((ValueTuple<int, int>*)(&obj))->Item2;
		item = Mathf.RoundToInt(Mathf.Clamp((float)item * GetRivalsDifficultyMultiplier(), 0f, 90f));
		item2 = Mathf.RoundToInt(Mathf.Clamp((float)item2 * GetRivalsDifficultyMultiplier(), 0f, 12f));
		return (1f - (float)item / 100f, item2);
	}

	public static int GetLowDemandValues(Priority aggression)
	{
		return Mathf.RoundToInt(Mathf.Clamp((float)(aggression switch
		{
			Priority.Low => 3, 
			Priority.Medium => 5, 
			Priority.High => 7, 
			_ => 3, 
		}) * GetRivalsDifficultyMultiplier(), 0f, 10f));
	}

	private unsafe static (int, int, int) GetHireBestEmployeesValues(Priority aggression)
	{
		object obj = aggression switch
		{
			Priority.Low => (10, 3, 8), 
			Priority.Medium => (15, 2, 16), 
			Priority.High => (24, 2, 24), 
			_ => (10, 3, 8), 
		};
		int item = ((ValueTuple<int, int, int>*)(&obj))->Item1;
		int item2 = ((ValueTuple<int, int, int>*)(&obj))->Item2;
		int item3 = ((ValueTuple<int, int, int>*)(&obj))->Item3;
		item = Mathf.RoundToInt(Mathf.Clamp((float)item * GetRivalsDifficultyMultiplier(), 0f, 40f));
		item3 = Mathf.RoundToInt(Mathf.Clamp((float)item3 * GetRivalsDifficultyMultiplier(), 0f, 33f));
		return (item, item2, item3);
	}
}
