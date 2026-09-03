using System.Linq;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class FixWrongInstallationFirms : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => !x.RentedByPlayer && x.AvailableForRent && x.businessTypeName == "ba:businesstype_interiorinstallationfirm"))
		{
			BusinessHelper.SetBuildingForRent(item);
		}
	}
}
