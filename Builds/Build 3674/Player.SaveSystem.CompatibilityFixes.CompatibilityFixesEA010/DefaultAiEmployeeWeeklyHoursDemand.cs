using Buildings.BuildingTypes.Shared;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class DefaultAiEmployeeWeeklyHoursDemand : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (string.IsNullOrEmpty(buildingRegistration.businessOwnerRivalId) || buildingRegistration.aiEmployees == null)
			{
				continue;
			}
			foreach (AiBusinessEmployeeData aiEmployee in buildingRegistration.aiEmployees)
			{
				aiEmployee.hoursPerWeekDemandName = "ba:jobdemand_fulltime";
			}
		}
	}
}
