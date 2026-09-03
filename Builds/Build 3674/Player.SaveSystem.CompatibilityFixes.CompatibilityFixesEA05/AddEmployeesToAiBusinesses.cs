using System.Collections.Generic;
using System.Linq;
using Buildings.BuildingTypes.Shared;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class AddEmployeesToAiBusinesses : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => !string.IsNullOrEmpty(x.businessOwnerRivalId)))
		{
			List<AiBusinessEmployeeData> aiEmployees = item.aiEmployees;
			if (aiEmployees == null || aiEmployees.Count <= 0)
			{
				item.GenerateAiBusinessEmployees();
			}
		}
	}
}
