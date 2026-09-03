using System.Collections.Generic;
using Buildings.Factory;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class AddFactoryExportsList : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			buildingRegistration.factoryExports = new List<FactoryExport>();
		}
	}
}
