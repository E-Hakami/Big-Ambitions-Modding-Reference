using System.Linq;
using BigAmbitions.Rivals;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class Change48ThAvenueToI3 : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		Address address = new Address("ba:street_eighthavenue", 4);
		BuildingRegistration buildingRegistration = gameInstance.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.Address == address);
		if (buildingRegistration != null)
		{
			if (buildingRegistration.RentedByPlayer)
			{
				BuildingHelper.SellBuilding(address, $"{address} was sold (caused by compatibility support)");
			}
			if (buildingRegistration.BuildingOwnedByPlayer)
			{
				RealEstateHelper.SellBuildingForCompat(buildingRegistration);
			}
			buildingRegistration.Reset();
			BuildingHelper.GetBuildingRegistration(address);
			buildingRegistration.AvailableForRent = true;
			RivalsHelper.FillData(gameInstance.rivalStates.Select((RivalState x) => x.rivalId).ToList());
			buildingRegistration.buildingOwnerRivalId = RivalsHelper.GetRandomRivalForBuilding(buildingRegistration.Neighborhood);
		}
	}
}
