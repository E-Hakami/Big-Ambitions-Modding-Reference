using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class NeighborhoodLegacyMap : LegacyMapperBase
{
	public override List<string> Keys => new List<string>
	{
		"NeighbourhoodStats.name", "NeighborhoodData.neighbourhood", "Building.Neighbourhood", "MarketEvent.neighbourhood", "NeighborhoodDemand.neighborhood", "CitizenData.Neighbourhood", "ParkingLaneGenerator.neighbourhood", "SubwayStation.neighbourhood", "CityMapNeighborhoodZone.neighbourhood", "SpecialRival.primaryNeighborhood",
		"CreateMarketEvent.neighbourhood", "HasRentedBuildings.neighbourhood", "VehicleInstance.parkingNeighbourhood", "IdealAvailableBuildingsInNeighborhood.neighbourhood", "Item.limitDemandToNeighbourhoods"
	};

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 0, "ba:neighborhood_garmentdistrict" },
		{ 1, "ba:neighborhood_hellskitchen" },
		{ 2, "ba:neighborhood_murrayhill" },
		{ 3, "ba:neighborhood_midtown" },
		{ 4, "ba:neighborhood_global" },
		{ 5, "ba:neighborhood_lowermanhattan" },
		{ 6, "ba:neighborhood_industrycity" },
		{ 7, "ba:neighborhood_thehamptons" }
	};
}
