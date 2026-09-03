using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class UpdateEmployeeItemsAndHours : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (EmployeeInstance employeeInstance in gameInstance.EmployeeInstances)
		{
			employeeInstance.UpdateAssignedWorkStationItems();
			employeeInstance.UpdateWeeklyHoursAndDays();
		}
	}
}
