using BigAmbitions.Rivals;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;

public class SetUpTheHamptonsNeighborhood : ICompatibilityFix
{
	private const string Neighborhood = "ba:neighborhood_thehamptons";

	public void Apply(GameInstance gameInstance)
	{
		CityGenerator.InitializeCity("ba:neighborhood_thehamptons");
		CityGenerator.DistributeBuildingsToRivals("ba:neighborhood_thehamptons");
		ReleaseDefeatedRivalsHamptonsHouses(gameInstance);
	}

	private static void ReleaseDefeatedRivalsHamptonsHouses(GameInstance gameInstance)
	{
		foreach (SpecialRivalState specialRivalState in gameInstance.specialRivalStates)
		{
			if (specialRivalState.isDefeated)
			{
				RealEstateHelper.SetBuildingForSale(BuildingHelper.GetBuildingRegistration(RivalsHelper.GetSpecialRival(specialRivalState.rivalId).hamptonsBuilding.Address));
			}
		}
	}
}
