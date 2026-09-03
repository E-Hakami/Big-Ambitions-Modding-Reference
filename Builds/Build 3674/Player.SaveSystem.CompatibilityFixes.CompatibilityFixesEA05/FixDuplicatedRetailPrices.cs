using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class FixDuplicatedRetailPrices : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			buildingRegistration.retailPrices = (from x in buildingRegistration.retailPrices
				group x by x.itemName into x
				select x.First()).ToList();
		}
	}
}
