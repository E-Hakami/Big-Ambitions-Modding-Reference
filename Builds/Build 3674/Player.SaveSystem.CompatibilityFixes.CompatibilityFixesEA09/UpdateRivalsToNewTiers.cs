using System.Collections.Generic;
using System.Linq;
using AI.Citizens;
using BigAmbitions.InteriorDesigner.InteriorElements;
using BigAmbitions.Items;
using BigAmbitions.Rivals;
using BigAmbitions.Tags;
using BusinessLayoutSets;
using Entities;
using Extensions;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class UpdateRivalsToNewTiers : ICompatibilityFix
{
	private const string AffectedBuildingSize = "ba:buildingsize_m";

	private static Dictionary<string, int> SpecialRivalBusinessTypeCount = new Dictionary<string, int>();

	private static readonly List<BuildingRegistration> RivalBuildingRegistrations = new List<BuildingRegistration>();

	private static readonly Dictionary<BuildingRegistration, int> RivalRegistrationCreationDays = new Dictionary<BuildingRegistration, int>();

	private static readonly List<BuildingRegistration> M1Registrations = new List<BuildingRegistration>();

	private static readonly List<BuildingRegistration> OtherRegistrations = new List<BuildingRegistration>();

	private static readonly List<AiBusinessDefault> BusinessDefaults = new List<AiBusinessDefault>();

	private static readonly List<KeyValuePair<string, BusinessLayoutSet>> Layouts = new List<KeyValuePair<string, BusinessLayoutSet>>();

	private static KeyValuePair<string, BusinessLayoutSet> Layout;

	private static readonly List<ProductMarketEntry> SortedMarketEntries = new List<ProductMarketEntry>();

	private static readonly List<BigAmbitions.Items.Item> SortedDemandedItems = new List<BigAmbitions.Items.Item>();

	private static readonly List<BusinessLayoutSet> SuitableLayouts = new List<BusinessLayoutSet>();

	private static Address CurrentPlayerAddress;

	public void Apply(GameInstance gameInstance)
	{
		CitizenHelper.Init();
		InteriorElementsHelper.Init();
		RivalsHelper.FillData(SaveGameManager.Current.rivalStates.Select((RivalState x) => x.rivalId).ToList());
		SetRivalTiers(gameInstance);
		CurrentPlayerAddress = new Address(gameInstance.CurrentStreetName, gameInstance.CurrentStreetNumber);
		RearrangeBusinesses(gameInstance);
		RivalsHelper.RefreshRivals();
	}

	private static void SetRivalTiers(GameInstance gameInstance)
	{
		string[] array = new string[7];
		string[] array2 = new string[8];
		int num = RivalsHelper.GetSpecialRivals().Count;
		for (int i = 0; i < 7; i++)
		{
			array[i] = gameInstance.rivalStates[num].rivalId;
			num++;
		}
		gameInstance.wholesaleRivalIds = array;
		for (int j = 0; j < 8; j++)
		{
			array2[j] = gameInstance.rivalStates[num].rivalId;
			num++;
		}
		gameInstance.importRivalIds = array2;
	}

	private static void RearrangeBusinesses(GameInstance gameInstance)
	{
		foreach (string neighborhood in NeighborhoodHelper.Neighborhoods)
		{
			RearrangeBusinessesInNeighborhood(gameInstance, neighborhood);
		}
	}

	private static void RearrangeBusinessesInNeighborhood(GameInstance gameInstance, string neighborhood)
	{
		SpecialRival specialRival = RivalsHelper.GetSpecialRivalByNeighborhood(neighborhood);
		if (specialRival != null && RivalsHelper.IsRivalDefeated(specialRival.rivalData.id))
		{
			specialRival = null;
		}
		if (specialRival != null)
		{
			SpecialRivalBusinessTypeCount.Clear();
			foreach (BuildingRegistration ownedRetailOfficeBusiness in specialRival.rivalData.ownedRetailOfficeBusinesses)
			{
				if (!SpecialRivalBusinessTypeCount.TryAdd(ownedRetailOfficeBusiness.businessTypeName, 1))
				{
					SpecialRivalBusinessTypeCount[ownedRetailOfficeBusiness.businessTypeName]++;
				}
			}
			SpecialRivalBusinessTypeCount = SpecialRivalBusinessTypeCount.OrderBy((KeyValuePair<string, int> x) => x.Value).ToDictionary((KeyValuePair<string, int> x) => x.Key, (KeyValuePair<string, int> x) => x.Value);
		}
		SetRivalBuildingRegistrations(gameInstance, neighborhood);
		SetBuildingsForRent(RivalBuildingRegistrations);
		if (specialRival != null)
		{
			OpenSpecialRivalBusinesses(specialRival);
			ProductMarketHelper.FillProvidersDictionary(checkFillState: false);
			ProductMarketHelper.UpdateMarketDemands(gameInstance);
		}
		foreach (BuildingRegistration rivalBuildingRegistration in RivalBuildingRegistrations)
		{
			if (rivalBuildingRegistration.AvailableForRent)
			{
				if (rivalBuildingRegistration.Address == CurrentPlayerAddress)
				{
					SaveGameCompatibilityFixes.forcePlayerExitForCompatibility = true;
				}
				StartNewBusiness(rivalBuildingRegistration);
			}
		}
	}

	private static void OpenSpecialRivalBusinesses(SpecialRival specialRival)
	{
		M1Registrations.Clear();
		foreach (BuildingRegistration rivalBuildingRegistration in RivalBuildingRegistrations)
		{
			if (rivalBuildingRegistration.BuildingCached.BuildingSize == "ba:buildingsize_m")
			{
				M1Registrations.Add(rivalBuildingRegistration);
			}
		}
		OtherRegistrations.Clear();
		foreach (BuildingRegistration rivalBuildingRegistration2 in RivalBuildingRegistrations)
		{
			if (rivalBuildingRegistration2.BuildingCached.BuildingSize != "ba:buildingsize_m")
			{
				OtherRegistrations.Add(rivalBuildingRegistration2);
			}
		}
		for (int i = 0; i < 2; i++)
		{
			foreach (KeyValuePair<string, int> item in SpecialRivalBusinessTypeCount)
			{
				BusinessDefaults.Clear();
				AiBusinessDefault[] businessDefaultsByType = CompetitionHelper.GetBusinessDefaultsByType(item.Key);
				foreach (AiBusinessDefault aiBusinessDefault in businessDefaultsByType)
				{
					if (aiBusinessDefault.corporationRivalId == specialRival.rivalData.id)
					{
						BusinessDefaults.Add(aiBusinessDefault);
					}
				}
				Layouts.Clear();
				foreach (KeyValuePair<string, BusinessLayoutSet> allBusinessLayoutSet in BusinessLayoutSetHelper.GetAllBusinessLayoutSets())
				{
					foreach (AiBusinessDefault businessDefault in BusinessDefaults)
					{
						if (businessDefault.buildingLayout == allBusinessLayoutSet.Value.LayoutName)
						{
							Layouts.Add(allBusinessLayoutSet);
							break;
						}
					}
				}
				int num = ((i == 0) ? 1 : (item.Value - 1));
				if (BusinessTypeHelper.GetData(item.Key).suitableBuildingType == "ba:buildingtype_retail")
				{
					num = OpenBusinesses(M1Registrations, num, Layouts, item.Key, specialRival);
					if (num <= 0)
					{
						continue;
					}
				}
				OpenBusinesses(OtherRegistrations, num, Layouts, item.Key, specialRival);
			}
		}
	}

	private static int OpenBusinesses(List<BuildingRegistration> registrations, int businessesToOpen, List<KeyValuePair<string, BusinessLayoutSet>> layouts, string businessTypeName, SpecialRival specialRival)
	{
		foreach (BuildingRegistration registration in registrations)
		{
			if (businessesToOpen <= 0)
			{
				break;
			}
			if (!registration.AvailableForRent)
			{
				continue;
			}
			Layout = default(KeyValuePair<string, BusinessLayoutSet>);
			foreach (KeyValuePair<string, BusinessLayoutSet> layout in layouts)
			{
				if (layout.Value.BuildingSize == registration.BuildingCached.BuildingSize && layout.Value.BuildingVersion == registration.BuildingCached.BuildingVersion && BusinessTypeHelper.GetData(layout.Value).suitableBuildingType == registration.BuildingCached.BuildingType)
				{
					Layout = layout;
					break;
				}
			}
			if (Layout.Value == null)
			{
				continue;
			}
			AiBusinessDefault aiBusinessDefault = null;
			foreach (AiBusinessDefault businessDefault in BusinessDefaults)
			{
				if (businessDefault.buildingLayout == Layout.Value.LayoutName)
				{
					aiBusinessDefault = businessDefault;
					break;
				}
			}
			if ((bool)aiBusinessDefault)
			{
				CompetitionHelper.StartNewCompetitorBusiness(businessTypeName, registration, impactMarket: false, aiBusinessDefault, specialRival.rivalData.id);
			}
			businessesToOpen--;
		}
		return businessesToOpen;
	}

	private static void SetBuildingsForRent(IList<BuildingRegistration> rivalBuildingRegistrations)
	{
		foreach (BuildingRegistration rivalBuildingRegistration in rivalBuildingRegistrations)
		{
			BusinessHelper.SetBuildingForRent(rivalBuildingRegistration, updateMarket: false, updateRivals: false);
		}
	}

	private static void SetRivalBuildingRegistrations(GameInstance gameInstance, string neighborhood)
	{
		RivalBuildingRegistrations.Clear();
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.Neighborhood == neighborhood)
			{
				string buildingType = buildingRegistration.GetBuildingType();
				if ((buildingType == "ba:buildingtype_retail" || buildingType == "ba:buildingtype_office") && !string.IsNullOrEmpty(buildingRegistration.businessOwnerRivalId))
				{
					RivalBuildingRegistrations.Add(buildingRegistration);
					RivalRegistrationCreationDays[buildingRegistration] = buildingRegistration.creationDay;
				}
			}
		}
		RivalBuildingRegistrations.Shuffle();
	}

	private static void StartNewBusiness(BuildingRegistration registration)
	{
		List<BigAmbitions.Items.Item> demandedItemsOrderedByDemand = GetDemandedItemsOrderedByDemand(registration, registration.Neighborhood);
		BigAmbitions.Items.Item randomFromRange = demandedItemsOrderedByDemand.GetRandomFromRange(0, 2);
		AiBusinessDefault aiBusinessDefault = FindSuitableBusinessDefaultForItemAndRegistration(registration, randomFromRange);
		if (aiBusinessDefault == null)
		{
			foreach (BigAmbitions.Items.Item item in demandedItemsOrderedByDemand)
			{
				if (!(item.itemName == randomFromRange.itemName))
				{
					aiBusinessDefault = FindSuitableBusinessDefaultForItemAndRegistration(registration, item);
					if (aiBusinessDefault != null)
					{
						break;
					}
				}
			}
		}
		if (!(aiBusinessDefault == null))
		{
			string rivalIdForBusinessDefault = CompetitionHelper.GetRivalIdForBusinessDefault(aiBusinessDefault);
			CompetitionHelper.StartNewCompetitorBusiness(aiBusinessDefault.businessTypeName, registration, impactMarket: false, aiBusinessDefault, rivalIdForBusinessDefault, setUpFirstDailyIncome: false);
			registration.creationDay = RivalRegistrationCreationDays[registration];
			ProductMarketHelper.FillProvidersDictionary(checkFillState: false);
			ProductMarketHelper.UpdateMarketDemands();
		}
	}

	private static AiBusinessDefault FindSuitableBusinessDefaultForItemAndRegistration(BuildingRegistration registration, BigAmbitions.Items.Item item)
	{
		List<BusinessLayoutSet> allSuitableLayouts = GetAllSuitableLayouts(registration, item);
		if (allSuitableLayouts.Count == 0)
		{
			return null;
		}
		AiBusinessDefault aiBusinessDefault = CompetitionHelper.GetBusinessDefaultsFromLayouts(allSuitableLayouts).ChooseRandomBusinessDefault(null);
		if (aiBusinessDefault == null)
		{
			return null;
		}
		return aiBusinessDefault;
	}

	private static List<BigAmbitions.Items.Item> GetDemandedItemsOrderedByDemand(BuildingRegistration registration, string neighborhood)
	{
		SortedMarketEntries.Clear();
		foreach (ProductMarketEntry productMarketEntry in SaveGameManager.Current.productMarketEntries)
		{
			SortedMarketEntries.Add(productMarketEntry);
		}
		SortedMarketEntries.Sort(delegate(ProductMarketEntry a, ProductMarketEntry b)
		{
			float value = -1f;
			foreach (NeighborhoodDemand demandValue in a.demandValues)
			{
				if (!(demandValue.neighborhood != neighborhood))
				{
					value = demandValue.demand;
					break;
				}
			}
			float num = -1f;
			foreach (NeighborhoodDemand demandValue2 in b.demandValues)
			{
				if (!(demandValue2.neighborhood != neighborhood))
				{
					num = demandValue2.demand;
					break;
				}
			}
			return num.CompareTo(value);
		});
		SortedDemandedItems.Clear();
		string buildingType = registration.GetBuildingType();
		foreach (ProductMarketEntry sortedMarketEntry in SortedMarketEntries)
		{
			BigAmbitions.Items.Item byName = ItemsGetter.GetByName(sortedMarketEntry.itemName);
			if (CompetitionHelper.GetSuitableBuildingTypeForItem(byName) == buildingType)
			{
				SortedDemandedItems.Add(byName);
			}
		}
		return SortedDemandedItems;
	}

	private static List<BusinessLayoutSet> GetAllSuitableLayouts(BuildingRegistration registration, BigAmbitions.Items.Item item)
	{
		SuitableLayouts.Clear();
		foreach (KeyValuePair<string, BusinessLayoutSet> allBusinessLayoutSet in BusinessLayoutSetHelper.GetAllBusinessLayoutSets())
		{
			BusinessType data = BusinessTypeHelper.GetData(allBusinessLayoutSet.Value);
			if (data.HasTag(TagRef.Businesstag.allowplayercreation) && allBusinessLayoutSet.Value.BuildingSize == registration.BuildingCached.BuildingSize && allBusinessLayoutSet.Value.BuildingVersion == registration.BuildingCached.BuildingVersion && data.suitableBuildingType == registration.GetBuildingType() && CompetitionHelper.GetItemNamesByLayoutSet(allBusinessLayoutSet.Value).Contains(item.itemName))
			{
				SuitableLayouts.Add(allBusinessLayoutSet.Value);
			}
		}
		return SuitableLayouts;
	}
}
