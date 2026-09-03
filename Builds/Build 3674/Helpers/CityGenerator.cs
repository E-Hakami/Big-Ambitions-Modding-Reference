using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Rivals;
using BigAmbitions.Tags;
using Blueprints;
using Buildings;
using BusinessLayoutSets;
using Entities;
using Extensions;
using UnityEngine;

namespace Helpers;

public static class CityGenerator
{
	public const int RivalBusinessesPercentage = 40;

	public const int SpecialRivalsBuildingPercentage = 24;

	private const int ArtificialProvidersAmountToForceBusinesses = 1000;

	private const int RealEstateRivalsBuildingPercentage = 50;

	private static readonly Dictionary<string, int> SpecialRivalCyclers = new Dictionary<string, int>();

	public static void InitializeCity(string neighbourhoodName = null)
	{
		SpecialRivalCyclers.Clear();
		if (string.IsNullOrEmpty(neighbourhoodName))
		{
			foreach (string neighborhood in NeighborhoodHelper.Neighborhoods)
			{
				PopulateBuildings(neighborhood);
				SetupResidentialBuildings(neighborhood);
				SetupWarehouseBuildings(neighborhood);
			}
			DistributeBuildingsToRivals();
			SetupRivalFactories();
			if (SaveGameManager.Current.gameVariables.allContactsUnlocked)
			{
				ContactsHelper.UnlockAllContacts();
			}
			if (TutorialHelper.IsTutorialEnabled())
			{
				foreach (BuildingRegistration item in SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.Neighborhood == "ba:neighborhood_garmentdistrict" && x.businessTypeName == "ba:businesstype_giftshop").ToList())
				{
					item.ShutDownAIBusiness();
				}
			}
		}
		else
		{
			PopulateBuildings(neighbourhoodName);
			SetupResidentialBuildings(neighbourhoodName);
			SetupWarehouseBuildings(neighbourhoodName);
		}
		ProductMarketHelper.OnInitializeCity();
		if (string.IsNullOrEmpty(neighbourhoodName))
		{
			if (SaveGameManager.Current.gameVariables.difficulty == Difficulty.Custom)
			{
				ProductMarketHelper.GenerateHypeEventOnRandomWholesalerItems(neighbourhoodName);
			}
			RivalsHelper.FillData(SaveGameManager.Current.rivalStates.Select((RivalState x) => x.rivalId).ToList());
		}
	}

	private static void SetupWarehouseBuildings(string neighborhood)
	{
		var list = (from x in BuildingHelper.allBuildings
			where string.IsNullOrEmpty(neighborhood) || neighborhood == x.Neighbourhood
			select new
			{
				building = x,
				registration = BuildingHelper.GetBuildingRegistration(x.Address)
			} into x
			where !x.registration.RentedByPlayer && x.building.BuildingType == "ba:buildingtype_warehouse"
			select x).ToList().Shuffle();
		if (list.Count == 0)
		{
			return;
		}
		foreach (var item in list)
		{
			item.registration.AvailableForRent = true;
		}
		int idealPercentageOfEmptyBuildings = CompetitionHelper.GetIdealPercentageOfEmptyBuildings("ba:buildingtype_warehouse", neighborhood);
		foreach (var item2 in list.Shuffle().Take(Mathf.RoundToInt((float)list.Count * (100f - (float)idealPercentageOfEmptyBuildings) / 100f)))
		{
			item2.registration.AvailableForRent = false;
		}
	}

	private static void SetupResidentialBuildings(string neighborhood)
	{
		if (neighborhood == "ba:neighborhood_thehamptons")
		{
			SetupHamptonsResidentialBuildings();
			return;
		}
		var list = (from x in BuildingHelper.allBuildings
			where string.IsNullOrEmpty(neighborhood) || neighborhood == x.Neighbourhood
			select new
			{
				building = x,
				registration = BuildingHelper.GetBuildingRegistration(x.Address)
			} into x
			where !x.registration.RentedByPlayer && x.building.BuildingType == "ba:buildingtype_residential"
			select x).ToList().Shuffle();
		if (list.Count == 0)
		{
			return;
		}
		foreach (var item in list)
		{
			item.registration.AvailableForRent = true;
		}
		int idealPercentageOfEmptyBuildings = CompetitionHelper.GetIdealPercentageOfEmptyBuildings("ba:buildingtype_residential", neighborhood);
		foreach (var item2 in list.Shuffle().Take(Mathf.RoundToInt((float)list.Count * (100f - (float)idealPercentageOfEmptyBuildings) / 100f)))
		{
			item2.registration.AvailableForRent = false;
		}
		var anon = list.FirstOrDefault(x => x.building.Address.Equals(TutorialHelper.InitialApartment));
		if (anon != null)
		{
			anon.registration.AvailableForRent = true;
		}
	}

	private static void SetupHamptonsResidentialBuildings()
	{
		foreach (var item in (from x in BuildingHelper.allBuildings
			where x.Neighbourhood == "ba:neighborhood_thehamptons"
			select new
			{
				building = x,
				registration = BuildingHelper.GetBuildingRegistration(x.Address)
			} into x
			where !x.registration.RentedByPlayer && x.building.BuildingType == "ba:buildingtype_residential"
			select x).ToList())
		{
			if (!item.building.IsHamptonsAIVilla() && !BuildingHelper.IsHamptonsBuildingOwnedByRival(item.building))
			{
				RealEstateHelper.SetBuildingForSale(item.registration);
			}
		}
	}

	private static void PopulateBuildings(string neighborhood)
	{
		IList<(Building, BuildingRegistration)> availableBuildings = GetAvailableBuildings(neighborhood);
		EnsureTutorialBuildingsAreAvailable(availableBuildings);
		List<(Building, BuildingRegistration)> list = new List<(Building, BuildingRegistration)>(availableBuildings.Count);
		foreach (BuildingTypeData value in BuildingTypeHelper.BuildingTypes.Values)
		{
			if (value.HasTag(TagRef.Buildingtypetag.allowincitygeneration))
			{
				list.AddRange(MarkBuildingsForRentAndGetAvailableOnes(neighborhood, value.buildingType, availableBuildings));
			}
		}
		List<(string, int, bool)> demandedItemsWithOptimalProviders = GetDemandedItemsWithOptimalProviders();
		SetRivalBuildings(neighborhood, list, demandedItemsWithOptimalProviders);
	}

	private static void SetRivalBuildings(string neighborhood, IList<(Building building, BuildingRegistration registration)> availableBuildings, List<(string itemName, int optimalProviders, bool isSpecialRivalOnly)> items)
	{
		int remainingRivalBusinesses = Mathf.RoundToInt((float)availableBuildings.Count * 0.4f) + 1;
		Dictionary<string, int> optimalProvidersRemainingPerItem = new Dictionary<string, int>();
		foreach (var item3 in items)
		{
			if (item3.isSpecialRivalOnly)
			{
				optimalProvidersRemainingPerItem[item3.itemName] = 1000;
			}
			else
			{
				optimalProvidersRemainingPerItem[item3.itemName] = item3.optimalProviders;
			}
		}
		foreach (var item4 in availableBuildings.OrderByDescending(((Building building, BuildingRegistration registration) x) => x.building.GetCustomerCapacity).ToList())
		{
			Building item = item4.building;
			BuildingRegistration item2 = item4.registration;
			SpecialRival specialRival = RivalsHelper.GetSpecialRivalByNeighborhood(neighborhood);
			string rivalId = GetRivalId(item.BuildingType, new BuildingSizeInfo(item), ref specialRival, ref remainingRivalBusinesses);
			if (rivalId == null)
			{
				BusinessHelper.SetBuildingForRent(item4.registration);
				continue;
			}
			List<AiBusinessDefault> aiBusinessDefaultsSuitableWithRival = GetAiBusinessDefaultsSuitableWithRival(specialRival, rivalId);
			List<BusinessLayoutSet> layoutsSuitableWithBuildingAndBusinessDefaults = GetLayoutsSuitableWithBuildingAndBusinessDefaults(item4.building, aiBusinessDefaultsSuitableWithRival);
			if (layoutsSuitableWithBuildingAndBusinessDefaults.Count == 0)
			{
				Debug.LogError(GetErrorMessage(item, specialRival, rivalId));
				continue;
			}
			BusinessLayoutSet businessLayoutSet = layoutsSuitableWithBuildingAndBusinessDefaults.OrderBy((BusinessLayoutSet x) => GetAverageOptimalProviders(x, optimalProvidersRemainingPerItem)).Last();
			AiBusinessGoodsSource goodsSource = AiBusinessGoodsSource.Import;
			if (specialRival == null)
			{
				goodsSource = ((!SaveGameManager.Current.wholesaleRivalIds.Contains(rivalId)) ? AiBusinessGoodsSource.Import : AiBusinessGoodsSource.Wholesale);
			}
			CompetitionHelper.StartNewCompetitorBusiness(businessDefault: RivalsHelper.GetRivalBusinessDefault(businessLayoutSet, specialRival, goodsSource, item.BuildingType == "ba:buildingtype_retail"), businessTypeName: businessLayoutSet.BusinessType, registration: item2, impactMarket: false, rivalId: rivalId);
			HashSet<string> primaryProducts = BusinessTypeHelper.GetData(businessLayoutSet).GetPrimaryProducts();
			foreach (string itemName in CompetitionHelper.GetItemNamesByLayoutSet(businessLayoutSet))
			{
				if (!primaryProducts.Contains(itemName))
				{
					continue;
				}
				if (optimalProvidersRemainingPerItem[itemName] == 1000)
				{
					optimalProvidersRemainingPerItem[itemName] = items.First(((string itemName, int optimalProviders, bool isSpecialRivalOnly) x) => x.itemName == itemName).optimalProviders;
				}
				else
				{
					optimalProvidersRemainingPerItem[itemName]--;
				}
			}
		}
	}

	private static string GetRivalId(string buildingType, BuildingSizeInfo sizeInfo, ref SpecialRival specialRival, ref int remainingRivalBusinesses)
	{
		if (buildingType == "ba:buildingtype_cinema" || buildingType == "ba:buildingtype_theater")
		{
			string text = CycleNextSpecialRival(buildingType);
			specialRival = RivalsHelper.GetSpecialRival(text);
			return text;
		}
		bool num = specialRival != null && remainingRivalBusinesses > 0 && RivalsHelper.IsBuildingSuitableForSpecialRival(buildingType, sizeInfo);
		bool canBeImporter = RivalsHelper.IsBuildingSuitableForImporterRival(buildingType, sizeInfo);
		bool canBeWholesaler = RivalsHelper.IsBuildingSuitableForWholesalerRival(buildingType, sizeInfo);
		string result;
		if (num)
		{
			result = specialRival.rivalData.id;
			remainingRivalBusinesses--;
		}
		else
		{
			result = GetNonSpecialRivalId(canBeImporter, canBeWholesaler);
			specialRival = null;
		}
		return result;
	}

	public static string CycleNextSpecialRival(string buildingType)
	{
		SpecialRival[] array = (from x in RivalsHelper.GetSpecialRivals()
			where !RivalsHelper.IsRivalDefeated(x.rivalData.id)
			select x).ToArray();
		if (array.Length == 0)
		{
			return null;
		}
		SpecialRivalCyclers.TryGetValue(buildingType, out var value);
		string id = array[value % array.Length].rivalData.id;
		SpecialRivalCyclers[buildingType] = (value + 1) % array.Length;
		return id;
	}

	public static string GetNonSpecialRivalId(bool canBeImporter, bool canBeWholesaler)
	{
		if (!canBeImporter && !canBeWholesaler)
		{
			return null;
		}
		if (canBeImporter && !canBeWholesaler)
		{
			return SaveGameManager.Current.importRivalIds.GetRandom();
		}
		if (!canBeImporter)
		{
			return SaveGameManager.Current.wholesaleRivalIds.GetRandom();
		}
		if (!RngHelper.Chance(50))
		{
			return SaveGameManager.Current.wholesaleRivalIds.GetRandom();
		}
		return SaveGameManager.Current.importRivalIds.GetRandom();
	}

	private static List<(string, int, bool)> GetDemandedItemsWithOptimalProviders()
	{
		List<(string, int, bool)> list = new List<(string, int, bool)>();
		foreach (BigAmbitions.Items.Item allItem in ItemsGetter.AllItems)
		{
			if (allItem.isADemandedProduct)
			{
				int optimalProviders = allItem.GetOptimalProviders();
				list.Add((allItem.itemName, optimalProviders, allItem.isSpecialRivalOnly));
			}
		}
		return list;
	}

	private static IEnumerable<(Building, BuildingRegistration)> MarkBuildingsForRentAndGetAvailableOnes(string neighborhood, string buildingType, IList<(Building building, BuildingRegistration registration)> availableBuildings)
	{
		int idealPercentageOfEmptyBuildings = CompetitionHelper.GetIdealPercentageOfEmptyBuildings(buildingType, neighborhood);
		List<(Building, BuildingRegistration)> list = availableBuildings.Where(((Building building, BuildingRegistration registration) x) => x.building.BuildingType == buildingType).ToList();
		int count = ((!(buildingType == "ba:buildingtype_cinema") && !(buildingType == "ba:buildingtype_theater")) ? Mathf.RoundToInt((float)(list.Count * idealPercentageOfEmptyBuildings) / 100f) : ((!IsAnyBuildingAvailableToPlayer(buildingType)) ? 1 : 0));
		foreach (var item in list.Take(count))
		{
			BusinessHelper.SetBuildingForRent(item.Item2);
		}
		return list.Skip(count);
	}

	public static bool IsAnyBuildingAvailableToPlayer(string buildingType, bool alreadyRentedOnly = false)
	{
		foreach (Building allBuilding in BuildingHelper.allBuildings)
		{
			if (!(allBuilding.BuildingType != buildingType))
			{
				BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(allBuilding.Address);
				if (buildingRegistration.RentedByPlayer || (buildingRegistration.AvailableForRent && !alreadyRentedOnly))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static IList<(Building building, BuildingRegistration registration)> GetAvailableBuildings(string neighborhood)
	{
		List<(Building, BuildingRegistration)> list = new List<(Building, BuildingRegistration)>();
		foreach (Building allBuilding in BuildingHelper.allBuildings)
		{
			if (!string.IsNullOrEmpty(neighborhood) && neighborhood != allBuilding.Neighbourhood)
			{
				continue;
			}
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(allBuilding.Address);
			if (!buildingRegistration.RentedByPlayer && allBuilding.SpecialService == null)
			{
				switch (allBuilding.BuildingType)
				{
				case "ba:buildingtype_office":
				case "ba:buildingtype_retail":
				case "ba:buildingtype_cinema":
				case "ba:buildingtype_theater":
					list.Add((allBuilding, buildingRegistration));
					break;
				}
			}
		}
		list.Shuffle();
		return list;
	}

	private static void EnsureTutorialBuildingsAreAvailable(IList<(Building building, BuildingRegistration registration)> availableBuildings)
	{
		if (SaveGameManager.Current.gameVariables.tutorialEnabled)
		{
			(Building, BuildingRegistration) random = availableBuildings.Where(((Building building, BuildingRegistration registration) x) => x.building.BuildingSize == TutorialHelper.TutorialSizeInfo.buildingSize && x.building.BuildingType == "ba:buildingtype_retail" && x.building.Neighbourhood == "ba:neighborhood_garmentdistrict").GetRandom();
			if (random.Item1 != null)
			{
				availableBuildings.Remove(random);
				BusinessHelper.SetBuildingForRent(random.Item2);
			}
			(Building, BuildingRegistration) random2 = availableBuildings.Where(((Building building, BuildingRegistration registration) x) => x.building.BuildingSize == TutorialHelper.TutorialSizeInfo.buildingSize && x.building.BuildingVersion == TutorialHelper.TutorialSizeInfo.buildingVersion && x.building.Neighbourhood == "ba:neighborhood_garmentdistrict").GetRandom();
			if (random2.Item1 != null)
			{
				availableBuildings.Remove(random2);
				BusinessHelper.SetBuildingForRent(random2.Item2);
			}
			(Building, BuildingRegistration) random3 = availableBuildings.Where(((Building building, BuildingRegistration registration) x) => x.building.BuildingSize == TutorialHelper.TutorialSizeInfo.buildingSize && x.building.BuildingType == "ba:buildingtype_retail" && x.building.trafficIndex >= 30).GetRandom();
			if (!(random3.Item1 == null))
			{
				availableBuildings.Remove(random3);
				BusinessHelper.SetBuildingForRent(random3.Item2);
			}
		}
	}

	private static List<AiBusinessDefault> GetAiBusinessDefaultsSuitableWithRival(SpecialRival specialRival, string rivalId)
	{
		if (specialRival != null)
		{
			return (from x in CompetitionHelper.GetAllBusinessDefaults()
				where x.corporationRivalId == rivalId || x.corporationRivalId == "*"
				select x).ToList();
		}
		AiBusinessGoodsSource goodsSource = ((!SaveGameManager.Current.wholesaleRivalIds.Contains(rivalId)) ? AiBusinessGoodsSource.Import : AiBusinessGoodsSource.Wholesale);
		return (from x in CompetitionHelper.GetAllBusinessDefaults()
			where string.IsNullOrEmpty(x.corporationRivalId) && x.goodsSource == goodsSource
			select x).ToList();
	}

	private static List<BusinessLayoutSet> GetLayoutsSuitableWithBuildingAndBusinessDefaults(Building building, List<AiBusinessDefault> aiBusinessDefaults)
	{
		List<BusinessLayoutSet> list = new List<BusinessLayoutSet>();
		foreach (KeyValuePair<string, BusinessLayoutSet> allBusinessLayoutSet in BusinessLayoutSetHelper.GetAllBusinessLayoutSets())
		{
			var (_, set) = allBusinessLayoutSet;
			BusinessType data = BusinessTypeHelper.GetData(set);
			if (!data)
			{
				Debug.LogError($"Set is null {set}");
				Debug.LogError($"BusinessTypes loaded={BusinessTypeHelper.BusinessTypeNames.Count()})");
				Debug.LogError("BusinessLayoutSet " + set.LayoutName + " has no BusinessTypeData (businessType='" + set.BusinessType + "'");
			}
			if (data.HasTag(TagRef.Businesstag.allowplayercreation) && set.BuildingSize == building.BuildingSize && set.BuildingVersion == building.BuildingVersion && data.suitableBuildingType == building.BuildingType && aiBusinessDefaults.Any((AiBusinessDefault y) => y.buildingLayout == set.LayoutName))
			{
				list.Add(set);
			}
		}
		return list;
	}

	private static string GetErrorMessage(Building building, SpecialRival specialRival, string rivalId)
	{
		if (specialRival != null)
		{
			return $"Couldn't find any layout for Building {building.BuildingSize}{building.BuildingVersion}: " + $"{building.Address} and rival {specialRival.rivalData.rivalName}";
		}
		AiBusinessGoodsSource aiBusinessGoodsSource = ((!SaveGameManager.Current.wholesaleRivalIds.Contains(rivalId)) ? AiBusinessGoodsSource.Import : AiBusinessGoodsSource.Wholesale);
		return $"Couldn't find any layout for Building {building.BuildingSize}{building.BuildingVersion}: " + $"{building.Address} with goodsSource: {aiBusinessGoodsSource}";
	}

	private static float GetAverageOptimalProviders(BusinessLayoutSet layoutSet, Dictionary<string, int> providersPerItem)
	{
		IReadOnlyCollection<string> itemNamesByLayoutSet = CompetitionHelper.GetItemNamesByLayoutSet(layoutSet);
		HashSet<string> primaryProducts = BusinessTypeHelper.GetData(layoutSet).GetPrimaryProducts();
		float num = 0f;
		int num2 = 0;
		foreach (string item in itemNamesByLayoutSet)
		{
			if (primaryProducts.Contains(item))
			{
				num += (float)providersPerItem[item];
				num2++;
			}
		}
		if (num2 <= 0)
		{
			return 0f;
		}
		return num / (float)num2;
	}

	public static void DistributeBuildingsToRivals(string neighborhood = null)
	{
		List<Building> list = BuildingHelper.allBuildings.Where((Building x) => !x.GetRegistration().BuildingOwnedByPlayer && (string.IsNullOrEmpty(neighborhood) || x.Neighbourhood == neighborhood) && x.SpecialService == null && !x.IsHamptonsHouse() && !RivalsHelper.IgnoredAddresses.Contains(x.Address)).ToList();
		int count = list.Count;
		IReadOnlyCollection<SpecialRival> specialRivals = RivalsHelper.GetSpecialRivals();
		List<Building> second = AssignBuildingsToRivals(count, list, specialRivals.Select((SpecialRival x) => x.rivalData).ToList(), 24f);
		list = list.Except(second).ToList();
		List<RivalData> list2 = RivalsHelper.GetNonSpecialRivals().Take(Mathf.RoundToInt(4.75f)).ToList();
		List<Building> second2 = AssignBuildingsToRivals(count, list, list2, 50f);
		list = list.Except(second2).ToList();
		AssignBuildingsToRivals(count, list, RivalsHelper.GetNonSpecialRivals().Except(list2).ToList());
		AssignSpecialRivalsHamptonsHouses(specialRivals);
		AssignRemainingHamptonsHousesToNonSpecialRivals();
	}

	private static void AssignRemainingHamptonsHousesToNonSpecialRivals()
	{
		List<Building> list = new List<Building>();
		foreach (Building allBuilding in BuildingHelper.allBuildings)
		{
			if (allBuilding.IsHamptonsHouse() && !allBuilding.IsHamptonsAIVilla() && !BuildingHelper.IsHamptonsBuildingOwnedByRival(allBuilding))
			{
				list.Add(allBuilding);
			}
		}
		AssignBuildingsToRivals(list.Count, list, RivalsHelper.GetNonSpecialRivals().ToList());
	}

	private static void AssignSpecialRivalsHamptonsHouses(IReadOnlyCollection<SpecialRival> specialRivals)
	{
		foreach (SpecialRival specialRival in specialRivals)
		{
			specialRival.hamptonsBuilding.GetRegistration().buildingOwnerRivalId = specialRival.rivalData.id;
		}
	}

	private static List<Building> AssignBuildingsToRivals(int totalBuildings, List<Building> buildings, List<RivalData> rivals, float percentage = -1f)
	{
		int count = ((percentage < 0f) ? buildings.Count : Mathf.RoundToInt((float)totalBuildings * (percentage / 100f)));
		List<Building> list = buildings.Take(count).ToList();
		int num = ((rivals.Count > 0) ? (list.Count / rivals.Count) : 0);
		int num2 = ((rivals.Count > 0) ? (list.Count % rivals.Count) : 0);
		for (int i = 0; i < rivals.Count; i++)
		{
			RivalData rivalData = rivals[i];
			List<Building> list2 = list.Skip(i * num).Take(num).ToList();
			if (num2 > 0)
			{
				list2.Add(list.Skip(rivals.Count * num + num2 - 1).First());
				num2--;
			}
			foreach (Building item in list2)
			{
				item.GetRegistration().buildingOwnerRivalId = rivalData.id;
			}
		}
		foreach (Building item2 in list)
		{
			BuildingRegistration registration = item2.GetRegistration();
			if (string.IsNullOrEmpty(registration.buildingOwnerRivalId))
			{
				if (rivals.Count > 0)
				{
					RivalData rivalData2 = rivals.Last();
					registration.buildingOwnerRivalId = rivalData2.id;
				}
				else if (!registration.IsOnSale())
				{
					RealEstateHelper.SetBuildingForSale(registration);
				}
			}
		}
		return list;
	}

	private static void SetupRivalFactories()
	{
		List<string> list = SaveGameManager.Current.specialRivalStates.Select((SpecialRivalState x) => x.rivalId).ToList();
		List<BuildingRegistration> list2 = SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.GetBuildingType() == "ba:buildingtype_warehouse" && !x.RentedByPlayer).ToList().Shuffle()
			.Take(list.Count)
			.ToList();
		for (int num = 0; num < list.Count; num++)
		{
			BuildingRegistration buildingRegistration = list2[num];
			buildingRegistration.AvailableForRent = false;
			buildingRegistration.businessTypeName = "ba:businesstype_factory";
			buildingRegistration.businessOwnerRivalId = list[num];
			buildingRegistration.GenerateAiBusinessEmployees();
		}
	}
}
