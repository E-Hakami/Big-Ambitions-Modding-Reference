using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class FixWrongMidtownHospitalLayout : ICompatibilityFix
{
	private const string AffectedNeighborhood = "ba:neighborhood_midtown";

	public void Apply(GameInstance gameInstance)
	{
		BuildingRegistration buildingRegistration = gameInstance.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.businessTypeName == "ba:businesstype_hospital" && x.Neighborhood == "ba:neighborhood_midtown");
		if (buildingRegistration != null)
		{
			buildingRegistration.Layout = "MidtownHospital";
		}
	}
}
