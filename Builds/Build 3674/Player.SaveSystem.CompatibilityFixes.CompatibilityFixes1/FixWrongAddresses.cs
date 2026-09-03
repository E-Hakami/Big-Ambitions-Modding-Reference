using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;

public class FixWrongAddresses : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.StreetName == "ba:street_hovgaardLane")
			{
				buildingRegistration.StreetName = "ba:street_hovgaardlane";
			}
		}
		foreach (RealEstate item in gameInstance.realEstate)
		{
			if (item.address.streetName == "ba:street_hovgaardLane")
			{
				item.address.streetName = "ba:street_hovgaardlane";
			}
		}
		foreach (BuildingForSale item2 in gameInstance.buildingsForSale)
		{
			if (item2.address.streetName == "ba:street_hovgaardLane")
			{
				item2.address.streetName = "ba:street_hovgaardlane";
			}
		}
	}
}
