using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class StreetLegacyMap : LegacyMapperBase
{
	public override List<string> Keys => new List<string>
	{
		"VehicleInstance.streetName", "Address.streetName", "BuildingRegistration.StreetName", "ItemInstance.streetName", "Building.StreetName", "GameInstance.CurrentStreetName", "Intersection.horizontalStreetName", "Intersection.verticalStreetName", "Road.streetName", "RoadNameLabel.streetName",
		"UnlockBuilding.buildingStreetName", "Contact.streetName", "Warehouse.StreetName", "Warehouse.streetName"
	};

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 1, "ba:street_firstavenue" },
		{ 2, "ba:street_secondavenue" },
		{ 3, "ba:street_thirdavenue" },
		{ 4, "ba:street_fourthavenue" },
		{ 5, "ba:street_fifthavenue" },
		{ 6, "ba:street_sixthavenue" },
		{ 7, "ba:street_firststreet" },
		{ 8, "ba:street_secondstreet" },
		{ 9, "ba:street_thirdstreet" },
		{ 10, "ba:street_fourthstreet" },
		{ 11, "ba:street_fifthstreet" },
		{ 12, "ba:street_sixthstreet" },
		{ 13, "ba:street_seventhstreet" },
		{ 14, "ba:street_eighthstreet" },
		{ 15, "ba:street_ninthstreet" },
		{ 16, "ba:street_tenthstreet" },
		{ 17, "ba:street_broadwaystreet" },
		{ 18, "ba:street_pier" },
		{ 19, "ba:street_thirdandahalfavenue" },
		{ 20, "ba:street_eleventhstreet" },
		{ 21, "ba:street_parking" },
		{ 22, "ba:street_twelfthstreet" },
		{ 23, "ba:street_thirteenthstreet" },
		{ 24, "ba:street_fourteenthstreet" },
		{ 25, "ba:street_seventhavenue" },
		{ 26, "ba:street_eighthavenue" },
		{ 27, "ba:street_ninthavenue" },
		{ 28, "ba:street_tenthavenue" },
		{ 29, "ba:street_twentiethstreet" },
		{ 30, "ba:street_twentyfirststreet" },
		{ 31, "ba:street_twentysecondstreet" },
		{ 32, "ba:street_twentythirdstreet" },
		{ 33, "ba:street_twentyfourthstreet" },
		{ 34, "ba:street_twentyfifthstreet" },
		{ 35, "ba:street_twentysixthstreet" },
		{ 36, "ba:street_airportavenue" },
		{ 37, "ba:street_oceancrestroad" },
		{ 38, "ba:street_harborstreet" },
		{ 39, "ba:street_oldmerchantroad" }
	};
}
