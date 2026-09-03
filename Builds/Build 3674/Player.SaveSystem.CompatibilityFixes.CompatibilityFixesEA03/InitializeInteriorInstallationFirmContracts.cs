using System.Collections.Generic;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class InitializeInteriorInstallationFirmContracts : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.interiorInstallationFirmContracts = new List<InteriorInstallationFirmContract>();
	}
}
