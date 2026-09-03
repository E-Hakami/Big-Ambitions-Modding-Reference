using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class BusinessTypeLegacyMap : LegacyMapperBase
{
	private static readonly HashSet<string> _legacyEnumTypeNames = new HashSet<string> { "Enums.BusinessTypeName, BigAmbitions" };

	public override List<string> Keys => new List<string>
	{
		"BusinessType.businessTypeName", "BuildingRegistration.businessTypeName", "BuildingRegistration.businessType", "BuildingInteriorSound.businessTypes", "BuildingEnterSound.businessTypes", "MarketEvent.businessTypeName", "MarketNews.businessTypeName", "IsBusinessType.businessTypeName", "AiBusinessDefault.businessTypeName", "InteriorInstallationFirmContract.businessTypeName",
		"SpecialService.businessTypeName", "BuildingTypeData.availableBusinessTypes", "BuildingTypeData.availableDevBusinessTypes", "BusinessLayoutSet.BusinessType", "CityMapFilterData.businessTypeName", "IsPlacedInBusinessOfType.businessTypeName", "IsPlacedInBusinessOfTypes.businessTypeNames", "HasPlacedItem.businessTypeName", "HasRentedBuildings.businessTypeName", "HasRunBusinessForTime.businessTypeName",
		"HasStartedBusiness.businessTypeNames", "HasStock.businessTypeName", "IsWorkingAtStation.businessTypeName", "CustomBuildingTarget.businessTypeName", "businessType"
	};

	public override HashSet<string> LegacyEnumTypeNames => _legacyEnumTypeNames;

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 0, "ba:businesstype_empty" },
		{ 1, "ba:businesstype_coffeeshop" },
		{ 2, "ba:businesstype_bank" },
		{ 3, "ba:businesstype_fastfoodrestaurant" },
		{ 4, "ba:businesstype_giftshop" },
		{ 5, "ba:businesstype_supermarket" },
		{ 6, "ba:businesstype_school" },
		{ 7, "ba:businesstype_jewelrystore" },
		{ 8, "ba:businesstype_webdevelopmentagency" },
		{ 9, "ba:businesstype_clothingstore" },
		{ 10, "ba:businesstype_cardealership" },
		{ 11, "ba:businesstype_appliancestore" },
		{ 12, "ba:businesstype_wholesalestore" },
		{ 13, "ba:businesstype_recruitmentagency" },
		{ 14, "ba:businesstype_furniturestore" },
		{ 15, "ba:businesstype_liquorstore" },
		{ 16, "ba:businesstype_marketingagency" },
		{ 17, "ba:businesstype_officesupplystore" },
		{ 18, "ba:businesstype_lawfirm" },
		{ 19, "ba:businesstype_headquarters" },
		{ 20, "ba:businesstype_importexport" },
		{ 21, "ba:businesstype_warehouse" },
		{ 22, "ba:businesstype_florist" },
		{ 23, "ba:businesstype_casino" },
		{ 24, "ba:businesstype_hospital" },
		{ 25, "ba:businesstype_fruitandvegetablestore" },
		{ 26, "ba:businesstype_graphicdesigner" },
		{ 27, "ba:businesstype_nightclub" },
		{ 28, "ba:businesstype_hairdresser" },
		{ 29, "ba:businesstype_interiorinstallationfirm" },
		{ 30, "ba:businesstype_gym" },
		{ 31, "ba:businesstype_irs" },
		{ 32, "ba:businesstype_gasstation" },
		{ 33, "ba:businesstype_truckgarage" },
		{ 34, "ba:businesstype_electronicsstore" },
		{ 35, "ba:businesstype_bookstore" },
		{ 37, "ba:businesstype_movingservice" },
		{ 38, "ba:businesstype_factory" },
		{ 39, "ba:businesstype_clinic" },
		{ 40, "ba:businesstype_cinema" },
		{ 41, "ba:businesstype_theater" },
		{ 42, "ba:businesstype_travelagency" },
		{ 43, "ba:businesstype_eventplanningagency" }
	};
}
