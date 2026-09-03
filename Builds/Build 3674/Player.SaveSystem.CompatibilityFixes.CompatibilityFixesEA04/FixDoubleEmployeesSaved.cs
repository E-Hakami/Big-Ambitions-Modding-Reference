using System.Linq;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class FixDoubleEmployeesSaved : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.EmployeeInstances = gameInstance.EmployeeInstances.Distinct().ToList();
	}
}
