using System.Linq;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA02;

public class Change28FourthAvenueBuildingSettings : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		Address address = new Address("ba:street_fourthavenue", 28);
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
			if (buildingRegistration.businessTypeName != "ba:businesstype_casino")
			{
				buildingRegistration.Reset();
			}
			BuildingHelper.GetBuildingRegistration(address);
		}
	}
}
