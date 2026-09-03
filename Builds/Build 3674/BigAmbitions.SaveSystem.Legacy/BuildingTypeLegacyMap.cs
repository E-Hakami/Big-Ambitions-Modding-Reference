using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class BuildingTypeLegacyMap : LegacyMapperBase
{
	public override List<string> Keys => new List<string>
	{
		"Building.BuildingType", "BuildingTypeData.buildingType", "BuildingVersion.supportedBuildingTypes", "CustomerCapacity.buildingType", "BusinessType.suitableBuildingType", "BuildingInteriorSound.buildingTypes", "BuildingEnterSound.buildingTypes", "FurnitureStoreSettings.allowedDeliveryBuildingTypes", "InteriorInstallationFirmSettings.buildingTypesThatCanInstall", "DeliveryJobStartLocation.destinationBuildingTypes",
		"CustomBuildingTarget.buildingType", "TutorialPointerHideConditionSelectedBizManBuildingHasNoCharacteristics.buildingType", "HasOpenedAmountOfBusinesses.buildingType", "HasPlacedItem.buildingType", "HasPurchasedItem.buildingType", "HasRentedBuildings.buildingType", "HasRentedBuildingWithTrafficIndex.buildingType", "HasStock.buildingType", "HasVisitedBuilding.buildingType", "BlueprintMetadata.buildingType",
		"BlueprintCreatorUI.excludedBuildingTypes", "BuildingRegistrationsGoal.buildingType", "BuildingSizeGoal.buildingType", "ItemsInStockGoal.type"
	};

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 0, "ba:buildingtype_residential" },
		{ 1, "ba:buildingtype_retail" },
		{ 2, "ba:buildingtype_office" },
		{ 3, "ba:buildingtype_warehouse" },
		{ 4, "ba:buildingtype_special" },
		{ 5, "ba:buildingtype_cinema" },
		{ 6, "ba:buildingtype_theater" }
	};
}
