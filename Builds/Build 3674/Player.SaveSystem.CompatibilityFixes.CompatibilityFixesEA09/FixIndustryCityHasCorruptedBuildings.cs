using System.Linq;
using AI.Citizens;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class FixIndustryCityHasCorruptedBuildings : ICompatibilityFix
{
	private const string AffectedNeighborhood = "ba:neighborhood_industrycity";

	public void Apply(GameInstance gameInstance)
	{
		CitizenHelper.Init();
		if (gameInstance.BuildingRegistrations.Any((BuildingRegistration x) => x.Neighborhood == "ba:neighborhood_industrycity" && x.GetBuildingType() == "ba:buildingtype_retail" && !x.AvailableForRent && string.IsNullOrEmpty(x.businessOwnerRivalId) && string.IsNullOrEmpty(x.buildingOwnerRivalId)))
		{
			CityGenerator.InitializeCity("ba:neighborhood_industrycity");
			CityGenerator.DistributeBuildingsToRivals("ba:neighborhood_industrycity");
		}
	}
}
