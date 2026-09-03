using System.Linq;
using Buildings.BuildingTypes.Shared;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class RemovePoachedEmployeesFromAiEmployeesList : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration item in gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => !string.IsNullOrEmpty(x.businessOwnerRivalId)))
		{
			item.aiEmployees.RemoveAll((AiBusinessEmployeeData x) => x.isPoached);
		}
	}
}
