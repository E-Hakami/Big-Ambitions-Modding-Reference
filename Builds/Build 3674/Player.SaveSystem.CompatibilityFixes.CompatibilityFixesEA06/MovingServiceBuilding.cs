using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class MovingServiceBuilding : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		BuildingRegistration buildingRegistration = gameInstance.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => (bool)x.BuildingCached && x.Address == new Address("ba:street_twelfthstreet", 15));
		if (buildingRegistration != null)
		{
			buildingRegistration.businessTypeName = "ba:businesstype_movingservice";
		}
	}
}
