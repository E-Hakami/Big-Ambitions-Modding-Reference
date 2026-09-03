using System.Collections.Generic;
using System.Linq;
using AI.Citizens;
using BigAmbitions.InteriorDesigner.InteriorElements;
using BigAmbitions.Rivals;
using Blueprints;
using Buildings;
using Entities;
using Extensions;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class InitializeSpecialBusinesses : ICompatibilityFix
{
	private static readonly Address NewRecruitmentAgencyAddress = new Address("ba:street_broadwaystreet", 5);

	private static readonly Address NewFurnitureStoreAddress = new Address("ba:street_broadwaystreet", 6);

	private static readonly Address[] NewCinemaAddresses = new Address[4]
	{
		new Address("ba:street_broadwaystreet", 4),
		new Address("ba:street_broadwaystreet", 14),
		new Address("ba:street_sixthstreet", 5),
		new Address("ba:street_thirdavenue", 15)
	};

	private static readonly Address[] NewTheaterAddresses = new Address[4]
	{
		new Address("ba:street_broadwaystreet", 8),
		new Address("ba:street_broadwaystreet", 3),
		new Address("ba:street_broadwaystreet", 10),
		new Address("ba:street_fifthstreet", 6)
	};

	private static readonly BuildingSizeInfo[] OldCinemaSizes = new BuildingSizeInfo[4]
	{
		new BuildingSizeInfo("ba:buildingsize_l", 1),
		new BuildingSizeInfo("ba:buildingsize_m", 1),
		new BuildingSizeInfo("ba:buildingsize_l", 1),
		new BuildingSizeInfo("ba:buildingsize_d", 2)
	};

	private static readonly BuildingSizeInfo[] OldTheaterSizes = new BuildingSizeInfo[4]
	{
		new BuildingSizeInfo("ba:buildingsize_m", 1),
		new BuildingSizeInfo("ba:buildingsize_k", 1),
		new BuildingSizeInfo("ba:buildingsize_k", 1),
		new BuildingSizeInfo("ba:buildingsize_m", 1)
	};

	private static readonly Dictionary<string, int> BuildingTypeConvertedCount = new Dictionary<string, int>
	{
		{ "ba:buildingtype_cinema", 0 },
		{ "ba:buildingtype_theater", 0 }
	};

	public void Apply(GameInstance gameInstance)
	{
		GameInstance gameInstance2 = gameInstance;
		if (gameInstance2.disabledLicensingFees == null)
		{
			gameInstance2.disabledLicensingFees = new List<GameInstance.DisabledLicensingFee>();
		}
		gameInstance2 = gameInstance;
		if (gameInstance2.paidLicensingFeesToday == null)
		{
			gameInstance2.paidLicensingFeesToday = new List<(Address, string)>();
		}
		CitizenHelper.Init();
		InteriorElementsHelper.Init();
		RivalsHelper.FillData(gameInstance.rivalStates.Select((RivalState x) => x.rivalId).ToList());
		ProductMarketHelper.FillProvidersDictionary();
		ProductMarketHelper.UpdateMarketDemands();
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(NewRecruitmentAgencyAddress);
		BuildingRegistration buildingRegistration2 = BuildingHelper.GetBuildingRegistration(NewFurnitureStoreAddress);
		string text = BusinessTypeHelper.GetData(buildingRegistration).suitableBuildingType;
		string text2 = BusinessTypeHelper.GetData(buildingRegistration2).suitableBuildingType;
		BuildingSizeInfo buildingSizeInfo = new BuildingSizeInfo("ba:buildingsize_c", 1);
		BuildingSizeInfo buildingSizeInfo2 = new BuildingSizeInfo("ba:buildingsize_d", 2);
		if (text == "ba:buildingtype_special")
		{
			text = BuildingSizeHelper.GetBuildingTypeBySizeInfo(buildingSizeInfo);
		}
		if (text2 == "ba:buildingtype_special")
		{
			text2 = BuildingSizeHelper.GetBuildingTypeBySizeInfo(buildingSizeInfo2);
		}
		CompatibilityHelper.EvictPlayerFromAddressAndUpdateOccupant(NewRecruitmentAgencyAddress, buildingSizeInfo, text);
		CompatibilityHelper.EvictPlayerFromAddressAndUpdateOccupant(NewFurnitureStoreAddress, buildingSizeInfo2, text2);
		for (int num = 0; num < NewCinemaAddresses.Length; num++)
		{
			ApplyFix((NewCinemaAddresses[num], OldCinemaSizes[num]), "ba:businesstype_cinema", gameInstance);
		}
		for (int num2 = 0; num2 < NewTheaterAddresses.Length; num2++)
		{
			ApplyFix((NewTheaterAddresses[num2], OldTheaterSizes[num2]), "ba:businesstype_theater", gameInstance);
		}
	}

	private static void ApplyFix((Address, BuildingSizeInfo) info, string businessType, GameInstance gameInstance)
	{
		BuildingRegistration buildingRegistration = gameInstance.BuildingRegistrations.Find((BuildingRegistration x) => x.Address == info.Item1);
		if (buildingRegistration == null)
		{
			return;
		}
		string text = BusinessTypeHelper.GetData(buildingRegistration).suitableBuildingType;
		if (string.IsNullOrEmpty(text) || text == "ba:buildingtype_special")
		{
			text = BuildingSizeHelper.GetBuildingTypeBySizeInfo(info.Item2);
		}
		CompatibilityHelper.EvictPlayerFromAddressAndUpdateOccupant(info.Item1, info.Item2, text);
		BuildingRegistration buildingRegistration2 = BuildingHelper.GetBuildingRegistration(info.Item1);
		string text2 = businessType;
		string text3 = ((text2 == "ba:businesstype_cinema") ? "ba:buildingtype_cinema" : ((!(text2 == "ba:businesstype_theater")) ? string.Empty : "ba:buildingtype_theater"));
		string text4 = text3;
		if (string.IsNullOrEmpty(text4))
		{
			return;
		}
		int num = BuildingTypeConvertedCount[text4];
		BuildingTypeConvertedCount[text4] = num + 1;
		string rivalId = CityGenerator.CycleNextSpecialRival(text4);
		if (string.IsNullOrEmpty(rivalId))
		{
			rivalId = RivalsHelper.GetRandomSpecialRivalId(canFallbackToImport: true, canFallbackToWholesale: false);
		}
		if (string.IsNullOrEmpty(rivalId))
		{
			return;
		}
		AiBusinessDefault random;
		if (businessType == "ba:businesstype_cinema" || businessType == "ba:businesstype_theater")
		{
			random = (from x in CompetitionHelper.GetAllBusinessDefaults()
				where x.corporationRivalId == rivalId && x.businessTypeName == businessType
				select x).GetRandom();
			if (!random)
			{
				random = (from x in CompetitionHelper.GetAllBusinessDefaults()
					where x.businessTypeName == businessType
					select x).GetRandom();
			}
		}
		else
		{
			random = CompetitionHelper.GetBusinessDefaultsByType(businessType).GetRandom();
		}
		if (num > 0)
		{
			CompetitionHelper.StartNewCompetitorBusiness(businessType, buildingRegistration2, impactMarket: false, random, rivalId);
		}
		else
		{
			buildingRegistration2.ShutDownAIBusiness();
		}
		buildingRegistration2.buildingOwnerRivalId = rivalId;
		SaveGameManager.Current.realEstate.RemoveAll((RealEstate x) => x.address == info.Item1);
		SaveGameManager.Current.buildingsForSale.RemoveAll((BuildingForSale x) => x.address == info.Item1);
	}
}
