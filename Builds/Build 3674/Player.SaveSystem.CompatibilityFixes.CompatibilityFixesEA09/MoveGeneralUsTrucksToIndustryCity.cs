using System.Linq;
using BigAmbitions.Rivals;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class MoveGeneralUsTrucksToIndustryCity : ICompatibilityFix
{
	private const string AffectedNeighborhood = "ba:neighborhood_garmentdistrict";

	public void Apply(GameInstance gameInstance)
	{
		Address address = new Address(gameInstance.CurrentStreetName, gameInstance.CurrentStreetNumber);
		Address oldAddress = new Address("ba:street_thirdstreet", 78);
		BuildingRegistration buildingRegistration = gameInstance.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.Address == oldAddress);
		if (buildingRegistration != null)
		{
			gameInstance.BuildingRegistrations.Remove(buildingRegistration);
			buildingRegistration = BuildingHelper.GetBuildingRegistration(oldAddress);
			buildingRegistration.AvailableForRent = true;
			RivalsHelper.FillData(gameInstance.rivalStates.Select((RivalState x) => x.rivalId).ToList());
			buildingRegistration.buildingOwnerRivalId = RivalsHelper.GetRandomRivalForBuilding("ba:neighborhood_garmentdistrict");
			if (address == oldAddress)
			{
				SaveGameCompatibilityFixes.forcePlayerExitForCompatibility = true;
			}
		}
	}
}
