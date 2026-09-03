using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class UpdateDeliveryDriversHoursAndDays : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (EmployeeInstance employeeInstance in gameInstance.EmployeeInstances)
		{
			if (employeeInstance.HasSkill("ba:skill_deliverydriver"))
			{
				employeeInstance.UpdateWeeklyHoursAndDays();
			}
		}
	}
}
