using System;
using System.Collections.Generic;
using System.Linq;
using AI.Citizens;
using BigAmbitions.Items;
using BigAmbitions.Neighborhoods;
using BigAmbitions.Rivals;
using BigAmbitions.Tags;
using Blueprints;
using Buildings;
using BusinessLayoutSets;
using Entities;
using Extensions;
using IngameDebugConsole;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Helpers;

public static class CompetitionHelper
{
	public const int PlayerMonopolyExtraPriceMultiplierPercentage = 30;

	private const string AddressableLabel = "AiBusinessDefaults";

	private const int MaxStoredDailyIncomes = 20;

	private const int YearsToRecoverFromAIBusinessTakeOver = 3;

	private const int MinimumInGameDaysToShutDownBusiness = 21;

	private const int MinimumDaysBeforeShuttingDownBusiness = 40;

	private const int LowIncomeDaysToShutDownBusiness = 14;

	private const float LenientMaxDailyIncomeToShutDownBusiness = 2857.1428f;

	private const int RetailPriceRecalculationBatchSize = 10;

	private const int DailyValuationUpdateBatchSize = 10;

	private const int TopDemandedItemsToTakeOneRandom = 3;

	private const float CompatFixMinimumOptimalPriceFraction = 0.25f;

	private const float CompatFixPriceMultiplierMin = 0.982f;

	private const float CompatFixPriceMultiplierMax = 1.018f;

	private static AiBusinessDefault[] BusinessDefaultsCached;

	private static readonly List<string> NonRivalPriceMatchingBusinessNames = new List<string> { "El Gato Food Market", "IKA Coffee Shop" };

	private static readonly Dictionary<string, AiBusinessDefault[]> BusinessDefaultsByType = new Dictionary<string, AiBusinessDefault[]>();

	private static readonly Dictionary<string, List<string>> itemsToIgnoreByRival = new Dictionary<string, List<string>>();

	private static readonly Dictionary<string, float> MinimumRivalPrices = new Dictionary<string, float>();

	private static readonly List<string> SortedDemandedItems = new List<string>();

	private static readonly List<ProductMarketEntry> SortedMarketEntries = new List<ProductMarketEntry>();

	private static readonly Dictionary<BusinessLayoutSet, HashSet<string>> ItemNamesByLayoutSet = new Dictionary<BusinessLayoutSet, HashSet<string>>();

	private static bool StartedCinemaBusinessToday;

	private static bool StartedTheaterBusinessToday;

	public static void RunDaily()
	{
		StartedCinemaBusinessToday = false;
		StartedTheaterBusinessToday = false;
		foreach (NeighbourhoodStats neighbourhoodStat in SaveGameManager.Current.NeighbourhoodStats)
		{
			if (string.IsNullOrEmpty(neighbourhoodStat.name) || neighbourhoodStat.name == "ba:neighborhood_global")
			{
				continue;
			}
			bool flag = neighbourhoodStat.nextNewBusinessDay <= SaveGameManager.Current.Day;
			bool flag2 = neighbourhoodStat.nextResidentialSwapDay <= SaveGameManager.Current.Day;
			bool flag3 = neighbourhoodStat.nextWarehouseSwapDay <= SaveGameManager.Current.Day;
			List<Building> list = BuildingHelper.AllNeighbourhoodBuildings[neighbourhoodStat.name];
			if (list.Count != 0)
			{
				List<Building> list2 = list.Where((Building x) => BuildingTypeHelper.GetData(x).HasTag(TagRef.Buildingtypetag.showinbusinesslist)).ToList();
				if (flag)
				{
					neighbourhoodStat.nextNewBusinessDay = StartNewBusiness(list2, neighbourhoodStat.name);
				}
				if (TimeHelper.CurrentDay >= 21)
				{
					ShutdownBusinesses(list2, neighbourhoodStat);
				}
				if (flag2 && neighbourhoodStat.name != "ba:neighborhood_thehamptons")
				{
					neighbourhoodStat.nextResidentialSwapDay = SwapBuilding(list, "ba:buildingtype_residential", GetIdealPercentageOfEmptyBuildings("ba:buildingtype_residential", neighbourhoodStat.name));
				}
				if (flag3)
				{
					neighbourhoodStat.nextWarehouseSwapDay = SwapBuilding(list, "ba:buildingtype_warehouse", GetIdealPercentageOfEmptyBuildings("ba:buildingtype_warehouse", neighbourhoodStat.name));
				}
			}
		}
		ProductMarketHelper.FillProvidersDictionary();
		ProductMarketHelper.UpdateMarketDemands();
		SetItemsToIgnoreByRival();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (ShouldRecalculateRetailPrices(buildingRegistration))
			{
				InstanceBehavior<GameManager>.Instance.pendingRetailPriceRecalculations.Enqueue(buildingRegistration);
			}
		}
		foreach (BuildingRegistration buildingRegistration2 in SaveGameManager.Current.BuildingRegistrations)
		{
			if (ShouldUpdateDailyValuation(buildingRegistration2))
			{
				InstanceBehavior<GameManager>.Instance.pendingDailyValuationUpdates.Enqueue(buildingRegistration2);
			}
		}
		SaveGameManager.Current.marketEvents.RemoveAll((MarketEvent x) => x.startDay + x.durationInDays + 65 < SaveGameManager.Current.Day);
	}

	public static bool ShouldRecalculateRetailPrices(BuildingRegistration buildingRegistration)
	{
		if (buildingRegistration.RentedByPlayer || buildingRegistration.AvailableForRent || buildingRegistration.businessTypeName == "ba:businesstype_empty")
		{
			return false;
		}
		BusinessType data = BusinessTypeHelper.GetData(buildingRegistration);
		if (data != null)
		{
			return data.HasTag(TagRef.Businesstag.allowplayercreation);
		}
		return false;
	}

	private static bool ShouldUpdateDailyValuation(BuildingRegistration buildingRegistration)
	{
		if (string.IsNullOrEmpty(buildingRegistration.businessOwnerRivalId))
		{
			return false;
		}
		return !BuildingTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Buildingtypetag.skipdailyvaluation);
	}

	public static void ProcessPendingRetailPriceRecalculations()
	{
		Queue<BuildingRegistration> pendingRetailPriceRecalculations = InstanceBehavior<GameManager>.Instance.pendingRetailPriceRecalculations;
		int num = 0;
		while (pendingRetailPriceRecalculations.Count > 0 && num < 10)
		{
			BuildingRegistration buildingRegistration = pendingRetailPriceRecalculations.Dequeue();
			if (ShouldRecalculateRetailPrices(buildingRegistration))
			{
				List<string> value = null;
				if (!string.IsNullOrEmpty(buildingRegistration.businessOwnerRivalId))
				{
					itemsToIgnoreByRival.TryGetValue(buildingRegistration.businessOwnerRivalId, out value);
				}
				RecalculateRetailPrices(buildingRegistration, value);
				num++;
			}
		}
	}

	public static void ProcessPendingDailyValuationUpdates()
	{
		if (InstanceBehavior<GameManager>.Instance.pendingRetailPriceRecalculations.Count > 0)
		{
			return;
		}
		Queue<BuildingRegistration> pendingDailyValuationUpdates = InstanceBehavior<GameManager>.Instance.pendingDailyValuationUpdates;
		int num = 0;
		while (pendingDailyValuationUpdates.Count > 0 && num < 10)
		{
			BuildingRegistration buildingRegistration = pendingDailyValuationUpdates.Dequeue();
			if (ShouldUpdateDailyValuation(buildingRegistration))
			{
				UpdateDailyValuation(buildingRegistration);
				num++;
			}
		}
	}

	public static bool AreRecalculationsPending()
	{
		if (!InstanceBehavior<GameManager>.Instance)
		{
			return false;
		}
		if (InstanceBehavior<GameManager>.Instance.pendingRetailPriceRecalculations.Count <= 0)
		{
			return InstanceBehavior<GameManager>.Instance.pendingDailyValuationUpdates.Count > 0;
		}
		return true;
	}

	private static void SetItemsToIgnoreByRival()
	{
		itemsToIgnoreByRival.Clear();
		foreach (SpecialRivalState specialRivalState in SaveGameManager.Current.specialRivalStates)
		{
			if (specialRivalState.defenseStates == null)
			{
				continue;
			}
			foreach (DefenseState defenseState in specialRivalState.defenseStates)
			{
				if (defenseState.defensiveMechanic == DefensiveMechanic.PriceReduction)
				{
					if (itemsToIgnoreByRival.TryGetValue(specialRivalState.rivalId, out var value))
					{
						value.AddRange(defenseState.affectedItems);
					}
					else
					{
						itemsToIgnoreByRival.Add(specialRivalState.rivalId, defenseState.affectedItems.ToList());
					}
				}
			}
		}
	}

	public static void UpdateDailyValuation(BuildingRegistration buildingRegistration, int day = -1)
	{
		if (day == -1)
		{
			day = TimeHelper.CurrentDay;
		}
		float item = buildingRegistration.GetEstimatedWeeklyIncome(null, day) / 7f;
		buildingRegistration.dailyIncomes.Add(item);
		if (buildingRegistration.dailyIncomes.Count > 20)
		{
			buildingRegistration.dailyIncomes.RemoveAt(0);
		}
	}

	private static int SwapBuilding(List<Building> neighborhoodBuildings, string buildingType, int minPercentageFree)
	{
		List<BuildingRegistration> list = (from x in neighborhoodBuildings
			where x.BuildingType == buildingType
			select BuildingHelper.GetBuildingRegistration(x.Address) into x
			where x != null
			select x).ToList();
		List<BuildingRegistration> list2 = list.Where((BuildingRegistration x) => x.AvailableForRent).ToList();
		if (!SaveGameManager.Current.CompletedQuestEntries.Contains("tutorial_quest_get_some_sleep_objective_4"))
		{
			list2 = list2.Where((BuildingRegistration x) => x.Address != TutorialHelper.InitialApartment).ToList();
		}
		int maxDaysToBusinessSwap = BuildingTypeHelper.GetData(buildingType).maxDaysToBusinessSwap;
		if (list.Count == 0)
		{
			return SaveGameManager.Current.Day + maxDaysToBusinessSwap;
		}
		if (list2.Count / list.Count * 100 > minPercentageFree)
		{
			list2.GetRandom().AvailableForRent = false;
		}
		else
		{
			BuildingRegistration random = list.Where((BuildingRegistration x) => !x.AvailableForRent && !x.RentedByPlayer).GetRandom();
			if (random != null)
			{
				random.AvailableForRent = true;
			}
		}
		return SaveGameManager.Current.Day + UnityEngine.Random.Range(5, maxDaysToBusinessSwap);
	}

	public static int GetIdealPercentageOfEmptyBuildings(string buildingType, string neighborhood = null)
	{
		BuildingTypeData data = BuildingTypeHelper.GetData(buildingType);
		IdealAvailableBuildingsInNeighborhood idealAvailableBuildingsInNeighborhood = data.idealAvailableBuildingsInNeighborhood.FirstOrDefault((IdealAvailableBuildingsInNeighborhood x) => x.neighbourhood == neighborhood);
		if (idealAvailableBuildingsInNeighborhood == null)
		{
			idealAvailableBuildingsInNeighborhood = data.idealAvailableBuildingsInNeighborhood.FirstOrDefault((IdealAvailableBuildingsInNeighborhood x) => x.neighbourhood == "ba:neighborhood_global");
		}
		if (idealAvailableBuildingsInNeighborhood == null)
		{
			idealAvailableBuildingsInNeighborhood = data.idealAvailableBuildingsInNeighborhood.FirstOrDefault((IdealAvailableBuildingsInNeighborhood x) => string.IsNullOrEmpty(x.neighbourhood));
		}
		return idealAvailableBuildingsInNeighborhood?.idealAvailableBuildingsPercentage ?? 0;
	}

	private static int StartNewBusiness(List<Building> neighborhoodBusinessBuildings, string neighborhood)
	{
		List<Building> emptyBuildings = GetEmptyBuildings(neighborhoodBusinessBuildings);
		RemoveTutorialNeededBuildings(emptyBuildings);
		List<string> itemsOrderedByDemand = GetItemsOrderedByDemand(neighborhood);
		int num = UnityEngine.Random.Range(0, 3);
		List<string> list = itemsOrderedByDemand;
		List<string> list2 = itemsOrderedByDemand;
		int index = num;
		string text = itemsOrderedByDemand[num];
		string text2 = itemsOrderedByDemand[0];
		string text3 = (list[0] = text);
		text3 = (list2[index] = text2);
		foreach (string item in itemsOrderedByDemand)
		{
			if (TryCreateCompetitorBusiness(neighborhoodBusinessBuildings, neighborhood, item, emptyBuildings))
			{
				return SaveGameManager.Current.Day + UnityEngine.Random.Range(2, 10);
			}
		}
		return SaveGameManager.Current.Day;
	}

	private static bool TryCreateCompetitorBusiness(List<Building> neighborhoodBusinessBuildings, string neighborhood, string itemName, List<Building> emptyBuildings)
	{
		BigAmbitions.Items.Item byName = ItemsGetter.GetByName(itemName);
		if (!TryGetBusinessForItem(neighborhoodBusinessBuildings, neighborhood, byName, emptyBuildings, out var businessDefault, out var buildingRegistration))
		{
			return false;
		}
		string rivalIdForBusinessDefault = GetRivalIdForBusinessDefault(businessDefault);
		StartNewCompetitorBusiness(businessDefault.businessTypeName, buildingRegistration, impactMarket: true, businessDefault, rivalIdForBusinessDefault, setUpFirstDailyIncome: false);
		if (buildingRegistration.businessTypeName == "ba:businesstype_cinema")
		{
			StartedCinemaBusinessToday = true;
		}
		else if (buildingRegistration.businessTypeName == "ba:businesstype_theater")
		{
			StartedTheaterBusinessToday = true;
		}
		return true;
	}

	private static bool TryGetBusinessForItem(List<Building> neighborhoodBusinessBuildings, string neighborhood, BigAmbitions.Items.Item item, List<Building> emptyBuildings, out AiBusinessDefault businessDefault, out BuildingRegistration buildingRegistration)
	{
		businessDefault = null;
		buildingRegistration = null;
		string suitableBuildingType = GetSuitableBuildingTypeForItem(item);
		if (suitableBuildingType == "ba:buildingtype_cinema" || suitableBuildingType == "ba:buildingtype_theater")
		{
			if (suitableBuildingType == "ba:buildingtype_cinema" && StartedCinemaBusinessToday)
			{
				return false;
			}
			if (suitableBuildingType == "ba:buildingtype_theater" && StartedTheaterBusinessToday)
			{
				return false;
			}
			if (emptyBuildings.Count((Building x) => x.BuildingType == suitableBuildingType) <= 1 && !CityGenerator.IsAnyBuildingAvailableToPlayer(suitableBuildingType, alreadyRentedOnly: true))
			{
				return false;
			}
		}
		else if (GetEmptyBuildingsPercentage(neighborhoodBusinessBuildings, suitableBuildingType, emptyBuildings) < GetIdealPercentageOfEmptyBuildings(suitableBuildingType, neighborhood))
		{
			return false;
		}
		businessDefault = FindSuitableBusinessDefaultForItem(emptyBuildings, item, out buildingRegistration);
		return businessDefault != null;
	}

	private static AiBusinessDefault FindSuitableBusinessDefaultForItem(List<Building> emptyBuildings, BigAmbitions.Items.Item item, out BuildingRegistration buildingRegistration)
	{
		buildingRegistration = null;
		List<BusinessLayoutSet> allSuitableLayouts = GetAllSuitableLayouts(emptyBuildings, item);
		if (allSuitableLayouts.Count == 0)
		{
			return null;
		}
		SpecialRival specialRival = RivalsHelper.GetSpecialRivalByNeighborhood(emptyBuildings[0].Neighbourhood);
		if (specialRival != null && RivalsHelper.IsRivalDefeated(specialRival.rivalData.id))
		{
			specialRival = null;
		}
		AiBusinessDefault aiBusinessDefault = GetBusinessDefaultsFromLayouts(allSuitableLayouts).ChooseRandomBusinessDefault(specialRival);
		if (aiBusinessDefault == null)
		{
			return null;
		}
		string layoutName = aiBusinessDefault.buildingLayout;
		BusinessLayoutSet random = allSuitableLayouts.Where((BusinessLayoutSet x) => x.LayoutName == layoutName).GetRandom();
		emptyBuildings.Shuffle();
		Building buildingMatchingLayout = GetBuildingMatchingLayout(emptyBuildings, random);
		if (!buildingMatchingLayout)
		{
			Debug.LogError("Could not find matching building for layout " + layoutName);
			return null;
		}
		buildingRegistration = BuildingHelper.GetBuildingRegistration(buildingMatchingLayout.Address);
		return aiBusinessDefault;
	}

	public static AiBusinessDefault ChooseRandomBusinessDefault(this ICollection<AiBusinessDefault> businessDefaults, SpecialRival specialRival)
	{
		AiBusinessDefault result = null;
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		foreach (AiBusinessDefault businessDefault in businessDefaults)
		{
			if (flag & flag2 & flag3)
			{
				break;
			}
			if (!string.IsNullOrEmpty(businessDefault.corporationRivalId))
			{
				flag = true;
			}
			else if (businessDefault.goodsSource == AiBusinessGoodsSource.Wholesale)
			{
				flag2 = true;
			}
			else if (businessDefault.goodsSource == AiBusinessGoodsSource.Import)
			{
				flag3 = true;
			}
		}
		flag = flag && specialRival != null;
		if ((flag && !flag2 && !flag3) || (flag && new System.Random().PercentageChance(40)))
		{
			result = businessDefaults.GetRandomBusinessDefault(specialRival);
		}
		else if (flag3 && !flag2)
		{
			result = businessDefaults.GetRandomBusinessDefault(null, AiBusinessGoodsSource.Import);
		}
		else if (!flag3 & flag2)
		{
			result = businessDefaults.GetRandomBusinessDefault(null, AiBusinessGoodsSource.Wholesale);
		}
		else if (flag3)
		{
			result = businessDefaults.GetRandomBusinessDefault(null, (!RngHelper.Chance(50)) ? AiBusinessGoodsSource.Import : AiBusinessGoodsSource.Wholesale);
		}
		return result;
	}

	public static HashSet<AiBusinessDefault> GetBusinessDefaultsFromLayouts(List<BusinessLayoutSet> suitableLayouts)
	{
		HashSet<AiBusinessDefault> hashSet = new HashSet<AiBusinessDefault>();
		foreach (BusinessLayoutSet suitableLayout in suitableLayouts)
		{
			AiBusinessDefault[] businessDefaultsByType = GetBusinessDefaultsByType(suitableLayout.BusinessType);
			foreach (AiBusinessDefault aiBusinessDefault in businessDefaultsByType)
			{
				if (!(aiBusinessDefault.buildingLayout != suitableLayout.LayoutName))
				{
					hashSet.Add(aiBusinessDefault);
				}
			}
		}
		return hashSet;
	}

	private static List<BusinessLayoutSet> GetAllSuitableLayouts(List<Building> emptyBuildings, BigAmbitions.Items.Item item)
	{
		List<BusinessLayoutSet> list = new List<BusinessLayoutSet>();
		foreach (KeyValuePair<string, BusinessLayoutSet> allBusinessLayoutSet in BusinessLayoutSetHelper.GetAllBusinessLayoutSets())
		{
			if (!BusinessTypeHelper.GetData(allBusinessLayoutSet.Value.BusinessType).HasTag(TagRef.Businesstag.allowplayercreation) || !(GetBuildingMatchingLayout(emptyBuildings, allBusinessLayoutSet.Value) != null))
			{
				continue;
			}
			if (allBusinessLayoutSet.Value.BusinessType == "ba:businesstype_cinema")
			{
				if (item.itemName == "ba:itemname_cinematicket")
				{
					list.Add(allBusinessLayoutSet.Value);
				}
			}
			else if (allBusinessLayoutSet.Value.BusinessType == "ba:businesstype_theater")
			{
				if (item.itemName == "ba:itemname_theaterticket")
				{
					list.Add(allBusinessLayoutSet.Value);
				}
			}
			else if (GetItemNamesByLayoutSet(allBusinessLayoutSet.Value).Contains(item.itemName))
			{
				list.Add(allBusinessLayoutSet.Value);
			}
		}
		return list;
	}

	private static Building GetBuildingMatchingLayout(List<Building> emptyBuildings, BusinessLayoutSet layout)
	{
		foreach (Building emptyBuilding in emptyBuildings)
		{
			if (emptyBuilding.BuildingSize == layout.BuildingSize && emptyBuilding.BuildingVersion == layout.BuildingVersion && emptyBuilding.BuildingType == BusinessTypeHelper.GetSuitableBuildingType(layout.BusinessType))
			{
				return emptyBuilding;
			}
		}
		return null;
	}

	private static int GetEmptyBuildingsPercentage(List<Building> neighborhoodBusinessBuildings, string suitableBuildingType, List<Building> emptyBuildings)
	{
		int num = 0;
		foreach (Building neighborhoodBusinessBuilding in neighborhoodBusinessBuildings)
		{
			if (neighborhoodBusinessBuilding.BuildingType == suitableBuildingType)
			{
				num++;
			}
		}
		int num2 = 0;
		foreach (Building emptyBuilding in emptyBuildings)
		{
			if (emptyBuilding.BuildingType == suitableBuildingType)
			{
				num2++;
			}
		}
		if (num != 0)
		{
			return num2 * 100 / num;
		}
		return 0;
	}

	public static string GetSuitableBuildingTypeForItem(BigAmbitions.Items.Item item)
	{
		if (item.itemName == "ba:itemname_cinematicket")
		{
			return "ba:buildingtype_cinema";
		}
		if (item.itemName == "ba:itemname_theaterticket")
		{
			return "ba:buildingtype_theater";
		}
		foreach (BusinessType allPlayerAvailableBusiness in BusinessTypeHelper.GetAllPlayerAvailableBusinesses())
		{
			string suitableBuildingType = allPlayerAvailableBusiness.suitableBuildingType;
			if (!(suitableBuildingType == "ba:buildingtype_cinema") && !(suitableBuildingType == "ba:buildingtype_theater") && allPlayerAvailableBusiness.IsPrimaryProduct(item.itemName))
			{
				return allPlayerAvailableBusiness.suitableBuildingType;
			}
		}
		return "ba:buildingtype_retail";
	}

	private static List<string> GetItemsOrderedByDemand(string neighborhood)
	{
		SortedMarketEntries.Clear();
		foreach (ProductMarketEntry productMarketEntry in SaveGameManager.Current.productMarketEntries)
		{
			SortedMarketEntries.Add(productMarketEntry);
		}
		SortedMarketEntries.Sort(delegate(ProductMarketEntry a, ProductMarketEntry b)
		{
			float itemDemand = GetItemDemand(neighborhood, a);
			return GetItemDemand(neighborhood, b).CompareTo(itemDemand);
		});
		SortedDemandedItems.Clear();
		foreach (ProductMarketEntry sortedMarketEntry in SortedMarketEntries)
		{
			SortedDemandedItems.Add(sortedMarketEntry.itemName);
		}
		return SortedDemandedItems;
	}

	private static float GetItemDemand(string neighborhood, ProductMarketEntry productMarketEntry)
	{
		float result = -1f;
		foreach (NeighborhoodDemand demandValue in productMarketEntry.demandValues)
		{
			if (!(demandValue.neighborhood != neighborhood))
			{
				result = demandValue.demand;
				break;
			}
		}
		return result;
	}

	private static List<Building> GetEmptyBuildings(List<Building> neighborhoodBusinessBuildings)
	{
		List<Building> list = new List<Building>();
		foreach (Building neighborhoodBusinessBuilding in neighborhoodBusinessBuildings)
		{
			if (!RivalsHelper.IgnoredAddresses.Contains(neighborhoodBusinessBuilding.Address))
			{
				BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(neighborhoodBusinessBuilding.Address);
				if (buildingRegistration != null && buildingRegistration.businessTypeName == "ba:businesstype_empty" && !buildingRegistration.RentedByPlayer)
				{
					list.Add(neighborhoodBusinessBuilding);
				}
			}
		}
		return list;
	}

	public static string GetRivalIdForBusinessDefault(AiBusinessDefault businessDefault, bool autoselectIfWildcard = true)
	{
		string text = businessDefault.corporationRivalId;
		if (string.IsNullOrEmpty(text))
		{
			text = ((businessDefault.goodsSource == AiBusinessGoodsSource.Wholesale) ? RivalsHelper.GetWholesaleRivalIds().GetRandom() : RivalsHelper.GetImportRivalIds().GetRandom());
		}
		if (autoselectIfWildcard && text == "*")
		{
			text = RivalsHelper.GetRandomSpecialRivalId(canFallbackToImport: true, canFallbackToWholesale: false);
		}
		return text;
	}

	private static void RemoveTutorialNeededBuildings(List<Building> emptyBuildings)
	{
		if (!TutorialHelper.HasCompletedObjective("tutorial_quest_establish_first_business_objective_2"))
		{
			Building random = emptyBuildings.Where((Building x) => x.BuildingSize == TutorialHelper.TutorialSizeInfo.buildingSize && x.BuildingType == "ba:buildingtype_retail" && x.Neighbourhood == "ba:neighborhood_garmentdistrict").GetRandom();
			if (random != null)
			{
				emptyBuildings.Remove(random);
			}
		}
		if (!TutorialHelper.HasCompletedObjective("tutorial_quest_first_hq_objective_2"))
		{
			Building random2 = emptyBuildings.Where((Building x) => x.BuildingSize == TutorialHelper.TutorialSizeInfo.buildingSize && x.BuildingVersion == TutorialHelper.TutorialSizeInfo.buildingVersion && x.Neighbourhood == "ba:neighborhood_garmentdistrict").GetRandom();
			if (random2 != null)
			{
				emptyBuildings.Remove(random2);
			}
		}
		if (!TutorialHelper.HasCompletedObjective("tutorial_quest_another_business_objective_2"))
		{
			Building random3 = emptyBuildings.Where((Building x) => x.BuildingSize == TutorialHelper.TutorialSizeInfo.buildingSize && x.BuildingType == "ba:buildingtype_retail" && x.trafficIndex >= 30).GetRandom();
			if (random3 != null)
			{
				emptyBuildings.Remove(random3);
			}
		}
	}

	public static void ShutdownBusinessesImmediate(IEnumerable<BuildingRegistration> registrations)
	{
		foreach (BuildingRegistration registration in registrations)
		{
			SaveGameManager.Current.marketEvents.Add(new MarketEvent(MarketEventType.BusinessClosed, SaveGameManager.Current.Day, registration.BusinessName, registration.Address, registration.businessOwnerRivalId.GetRivalName(), registration.businessTypeName, registration.Neighborhood));
			registration.ShutDownAIBusiness();
		}
	}

	private static void ShutdownBusinesses(List<Building> businessBuildings, NeighbourhoodStats neighbourhoodStats)
	{
		int maxBusinessesToKill = UnityEngine.Random.Range(1, 3);
		int num = TryShutdownBusinesses(businessBuildings, maxBusinessesToKill);
		if (num == 0)
		{
			if (neighbourhoodStats.nextForceShutdownDay <= SaveGameManager.Current.Day)
			{
				num = TryShutdownBusinesses(businessBuildings, 1, lenient: true);
				neighbourhoodStats.nextForceShutdownDay = SaveGameManager.Current.Day + UnityEngine.Random.Range(2, 10);
			}
		}
		else
		{
			neighbourhoodStats.nextForceShutdownDay = SaveGameManager.Current.Day + UnityEngine.Random.Range(2, 10);
		}
		if (num > 0)
		{
			RivalsHelper.CheckRivalTimeline(neighbourhoodStats.name);
		}
	}

	private static int TryShutdownBusinesses(List<Building> businessBuildings, int maxBusinessesToKill, bool lenient = false)
	{
		int num = 0;
		int num2 = SaveGameManager.Current.Day - 40;
		foreach (Building businessBuilding in businessBuildings)
		{
			if ((bool)businessBuilding.SpecialService)
			{
				continue;
			}
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(businessBuilding.Address);
			if (buildingRegistration == null || buildingRegistration.RentedByPlayer)
			{
				continue;
			}
			switch (buildingRegistration.businessTypeName)
			{
			case null:
			case "":
			case "ba:businesstype_empty":
				continue;
			}
			List<float> dailyIncomes = buildingRegistration.dailyIncomes;
			if (dailyIncomes == null || dailyIncomes.Count < 14 || (buildingRegistration.creationDay != 1 && buildingRegistration.creationDay > num2))
			{
				continue;
			}
			float num3 = buildingRegistration.dailyIncomes.TakeLast(14).Average();
			float maxDailyIncomeToShutDownBusiness = NeighborhoodHelper.GetData(buildingRegistration.Neighborhood).maxDailyIncomeToShutDownBusiness;
			float num4 = (lenient ? 2857.1428f : maxDailyIncomeToShutDownBusiness);
			if (!(num3 >= num4))
			{
				SaveGameManager.Current.marketEvents.Add(new MarketEvent(MarketEventType.BusinessClosed, SaveGameManager.Current.Day, buildingRegistration.BusinessName, buildingRegistration.Address, buildingRegistration.businessOwnerRivalId.GetRivalName(), buildingRegistration.businessTypeName, buildingRegistration.Neighborhood));
				buildingRegistration.ShutDownAIBusiness();
				num++;
				if (num >= maxBusinessesToKill)
				{
					break;
				}
			}
		}
		return num;
	}

	public static void StartNewCompetitorBusiness(string businessTypeName, BuildingRegistration registration, bool impactMarket, AiBusinessDefault businessDefault, string rivalId, bool setUpFirstDailyIncome = true)
	{
		registration.creationDay = SaveGameManager.Current.Day;
		registration.businessTypeName = businessTypeName;
		registration.scheduleDays = businessDefault.schedule;
		registration.BusinessName = businessDefault.businessName;
		registration.Layout = businessDefault.buildingLayout;
		registration.AvailableForRent = false;
		registration.signAppearanceSettings = businessDefault.signAppearanceSettings;
		registration.businessOwnerRivalId = rivalId;
		Building building = BuildingHelper.GetBuilding(registration.Address);
		registration.customerCapacity = GetAiBusinessCustomerCapacity(new BuildingSizeInfo(building), building.BuildingType);
		registration.logoSettings = businessDefault.logoSettings;
		CityBuildingController cityBuildingController = InstanceBehavior<CityManager>.Instance?.FindCityBuildingController(registration.Address);
		if (cityBuildingController != null)
		{
			cityBuildingController.UpdateSign();
		}
		registration.cachedAvailableProducts = registration.GetListOfItemsForSale().ToList();
		RecalculateRetailPrices(registration, null, registration.cachedAvailableProducts);
		registration.GenerateAiBusinessEmployees();
		if (impactMarket)
		{
			SaveGameManager.Current.marketEvents.Add(new MarketEvent(MarketEventType.BusinessOpened, SaveGameManager.Current.Day, registration.BusinessName, registration.Address, registration.businessOwnerRivalId.GetRivalName(), registration.businessTypeName, registration.Neighborhood));
		}
		if (setUpFirstDailyIncome && registration.dailyIncomes != null)
		{
			float item = registration.GetEstimatedWeeklyIncome() / 7f;
			registration.dailyIncomes.Add(item);
		}
	}

	public static IReadOnlyCollection<string> GetItemNamesByLayoutSet(BusinessLayoutSet set)
	{
		if (ItemNamesByLayoutSet.TryGetValue(set, out var value))
		{
			return value;
		}
		value = new HashSet<string>();
		BusinessType data = BusinessTypeHelper.GetData(set);
		if (data != null)
		{
			foreach (string primaryProduct in data.GetPrimaryProducts())
			{
				if ((ItemsGetter.GetByName(primaryProduct).type & (ItemType.RetailProduct | ItemType.ServiceProduct)) != 0)
				{
					value.Add(primaryProduct);
				}
			}
		}
		if (data?.suitableBuildingType == "ba:buildingtype_office")
		{
			return value;
		}
		foreach (BusinessLayoutSets.Item item in set.Items)
		{
			string itemName = item.playerItemPurchaserSettings.itemName;
			if (!string.IsNullOrEmpty(itemName) && (ItemsGetter.GetByName(itemName).type & (ItemType.RetailProduct | ItemType.ServiceProduct)) != 0)
			{
				value.Add(itemName);
			}
		}
		ItemNamesByLayoutSet.Add(set, value);
		return value;
	}

	public static AiBusinessDefault[] GetBusinessDefaultsByType(string businessTypeName)
	{
		if (BusinessDefaultsByType.TryGetValue(businessTypeName, out var value))
		{
			return value;
		}
		FillBusinessDefaultsCacheIfNeeded();
		List<AiBusinessDefault> list = new List<AiBusinessDefault>();
		AiBusinessDefault[] businessDefaultsCached = BusinessDefaultsCached;
		foreach (AiBusinessDefault aiBusinessDefault in businessDefaultsCached)
		{
			if (aiBusinessDefault.businessTypeName == businessTypeName)
			{
				list.Add(aiBusinessDefault);
			}
		}
		value = list.ToArray();
		BusinessDefaultsByType.Add(businessTypeName, value);
		return value;
	}

	private static void FillBusinessDefaultsCacheIfNeeded()
	{
		if (BusinessDefaultsCached == null)
		{
			BusinessDefaultsCached = Addressables.LoadAssetsAsync<AiBusinessDefault>("AiBusinessDefaults", null).WaitForCompletion().ToArray();
		}
	}

	public static void ClearBusinessDefaults()
	{
		BusinessDefaultsCached = null;
		BusinessDefaultsByType.Clear();
	}

	public static AiBusinessDefault GetBusinessDefault(string businessName)
	{
		FillBusinessDefaultsCacheIfNeeded();
		AiBusinessDefault[] businessDefaultsCached = BusinessDefaultsCached;
		foreach (AiBusinessDefault aiBusinessDefault in businessDefaultsCached)
		{
			if (aiBusinessDefault.businessName == businessName)
			{
				return aiBusinessDefault;
			}
		}
		return null;
	}

	public static AiBusinessDefault[] GetAllBusinessDefaults()
	{
		FillBusinessDefaultsCacheIfNeeded();
		return BusinessDefaultsCached;
	}

	public static float CalculateAiOwnedValuation(BuildingRegistration registration)
	{
		Building building = BuildingHelper.GetBuilding(registration.Address);
		if (!BuildingTypeHelper.GetData(registration).HasTag(TagRef.Buildingtypetag.calculateaiownedvaluation))
		{
			return 0f;
		}
		float num = Mathf.Max(registration.dailyIncomes.Average() * (float)SaveGameManager.Current.gameVariables.daysPerYear * 3f, 0f);
		if (num < 0f)
		{
			num = 0f;
		}
		return num + (BusinessLayoutSetHelper.GetOrLoadBusinessLayoutSet(registration.businessTypeName, new BuildingSizeInfo(building), registration.Layout)?.GetValuation() ?? 0f);
	}

	public static int GetAiBusinessCustomerCapacity(BuildingSizeInfo sizeInfo, string buildingType)
	{
		return Mathf.RoundToInt((float)BuildingSizeHelper.GetData(sizeInfo.buildingSize).GetCustomerCapacity(buildingType, sizeInfo.buildingVersion) * UnityEngine.Random.Range(0.5f, 1f));
	}

	public static void ClearMinimumRivalPrices()
	{
		MinimumRivalPrices.Clear();
	}

	public static float GetMinimumRivalPrice(string itemName)
	{
		if (MinimumRivalPrices.TryGetValue(itemName, out var value))
		{
			return value;
		}
		BigAmbitions.Items.Item byName = ItemsGetter.GetByName(itemName, suppressError: true);
		value = ((byName == null) ? 0f : (byName.GetWholesalePrice() * ProductMarketHelper.ProductMarketSettings.minimumRivalPriceOverWholesale));
		MinimumRivalPrices.Add(itemName, value);
		return value;
	}

	public static void RecalculateRetailPrices(BuildingRegistration registration, List<string> itemsToIgnore = null, List<string> freshProducts = null)
	{
		RefreshCachedAvailableProducts(registration, freshProducts);
		bool flag = IsNonRivalPriceMatchingStore(registration);
		bool flag2 = false;
		foreach (string cachedAvailableProduct in registration.cachedAvailableProducts)
		{
			if (itemsToIgnore != null && itemsToIgnore.Contains(cachedAvailableProduct))
			{
				continue;
			}
			float playerPrice = 0f;
			bool flag3 = flag && TryGetLowestPlayerRetailPrice(cachedAvailableProduct, registration.Neighborhood, out playerPrice);
			float num;
			if (flag3)
			{
				float b = ItemHelper.GetDefaultMarketPrice(cachedAvailableProduct) * CitizenHelper.averagePriceIndicesInNeighborhoods[registration.Neighborhood];
				num = Mathf.Min(playerPrice, b);
				flag2 = true;
			}
			else
			{
				float num2 = ItemHelper.CalculateMarketAveragePriceByNeighborhood(cachedAvailableProduct, registration.Neighborhood);
				num = Mathf.Max(num2 * UnityEngine.Random.Range(0.982f, 1.018f), num2 * 0.25f);
				if (num > num2)
				{
					num = num2;
				}
				float lowestMarketPrice = ItemHelper.GetLowestMarketPrice(cachedAvailableProduct, registration.Neighborhood);
				if (num < lowestMarketPrice)
				{
					num = lowestMarketPrice;
				}
			}
			num = Mathf.Max(num, GetMinimumRivalPrice(cachedAvailableProduct));
			RetailPrice retailPrice = null;
			foreach (RetailPrice retailPrice2 in registration.retailPrices)
			{
				if (!(retailPrice2.itemName != cachedAvailableProduct))
				{
					retailPrice = retailPrice2;
					break;
				}
			}
			if (retailPrice != null && retailPrice.price > 0f)
			{
				float price = retailPrice.price;
				double num3 = CalculateRetailPricesThreshold(price);
				if (flag3 ? Mathf.Approximately(price, num) : ((double)price.Difference(num) < num3))
				{
					continue;
				}
				registration.retailPrices.Remove(retailPrice);
			}
			registration.retailPrices.Add(new RetailPrice
			{
				itemName = cachedAvailableProduct,
				price = num
			});
		}
		for (int num4 = registration.retailPrices.Count - 1; num4 >= 0; num4--)
		{
			if (!registration.cachedAvailableProducts.Contains(registration.retailPrices[num4].itemName))
			{
				registration.retailPrices.RemoveAt(num4);
			}
		}
		if (flag2)
		{
			ItemHelper.ClearPriceCaches();
		}
	}

	private static void RefreshCachedAvailableProducts(BuildingRegistration registration, List<string> freshProducts)
	{
		if (freshProducts == null)
		{
			freshProducts = registration.GetListOfItemsForSale();
		}
		if (freshProducts.Count > 0 || registration.cachedAvailableProducts == null)
		{
			registration.cachedAvailableProducts = freshProducts;
		}
	}

	private static bool IsNonRivalPriceMatchingStore(BuildingRegistration registration)
	{
		string item = registration.BuildingCached?.SpecialService?.businessName ?? registration.BusinessName;
		return NonRivalPriceMatchingBusinessNames.Contains(item);
	}

	private static bool TryGetLowestPlayerRetailPrice(string itemName, string neighborhood, out float playerPrice)
	{
		playerPrice = 0f;
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer || buildingRegistration.Neighborhood != neighborhood || buildingRegistration.retailPrices == null)
			{
				continue;
			}
			foreach (RetailPrice retailPrice in buildingRegistration.retailPrices)
			{
				if (!(retailPrice.itemName != itemName) && !(retailPrice.price <= 0f))
				{
					if (playerPrice <= 0f || retailPrice.price < playerPrice)
					{
						playerPrice = retailPrice.price;
					}
					break;
				}
			}
		}
		return playerPrice > 0f;
	}

	private static double CalculateRetailPricesThreshold(double price)
	{
		return 0.005 * price + 0.09;
	}

	[ConsoleMethod("TestPrintProductPriceInNewStore", "Calculate product price in new store.", new string[] { }, AutoCompleteMap = new string[] { "itemName=Items" })]
	public static void Command_CalculateProductPriceInNewStore(string itemName, string neighborhood)
	{
		float val = Mathf.Max(ItemHelper.CalculateMarketAveragePriceByNeighborhood(itemName, neighborhood) * UnityEngine.Random.Range(0.982f, 1.018f), ItemHelper.CalculateOptimalPriceByNeighborhood(itemName, neighborhood) * 0.25f);
		Debug.Log("For " + itemName + " calculated price is " + val.ToCurrencyFormat() + " in " + neighborhood);
	}

	[ConsoleMethod("TestRecalculateRetailPricesForAll", "Recalculate retail prices for all buildings.", new string[] { })]
	public static void RecalculateRetailPricesForAll()
	{
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer && buildingRegistration.cachedAvailableProducts != null && buildingRegistration.cachedAvailableProducts.Count != 0 && BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.allowplayercreation))
			{
				RecalculateRetailPrices(buildingRegistration);
			}
		}
	}

	[ConsoleMethod("TestDoCompatFixForRetailPrices", "Do compatibility fix for retail prices.", new string[] { })]
	public static void DoCompatFixForRetailPrices()
	{
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer || buildingRegistration.cachedAvailableProducts == null || buildingRegistration.cachedAvailableProducts.Count == 0 || !BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.allowplayercreation))
			{
				continue;
			}
			buildingRegistration.retailPrices.Clear();
			foreach (string cachedAvailableProduct in buildingRegistration.cachedAvailableProducts)
			{
				float num = ItemHelper.CalculateOptimalPriceByNeighborhood(cachedAvailableProduct, buildingRegistration.Neighborhood);
				buildingRegistration.retailPrices.Add(new RetailPrice
				{
					itemName = cachedAvailableProduct,
					price = Mathf.Max(Mathf.Max(num * UnityEngine.Random.Range(0.982f, 1.018f), num * 0.25f), GetMinimumRivalPrice(cachedAvailableProduct))
				});
			}
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		BusinessDefaultsCached = null;
		BusinessDefaultsByType.Clear();
		ItemNamesByLayoutSet.Clear();
		MinimumRivalPrices.Clear();
	}
}
