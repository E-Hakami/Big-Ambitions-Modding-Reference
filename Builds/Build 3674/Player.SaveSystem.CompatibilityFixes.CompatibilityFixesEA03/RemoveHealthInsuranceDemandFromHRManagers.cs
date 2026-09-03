using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class RemoveHealthInsuranceDemandFromHRManagers : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (EmployeeInstance employeeInstance in gameInstance.EmployeeInstances)
		{
			if (employeeInstance is HRManager)
			{
				if (employeeInstance.demands.Contains("ba:jobdemand_bronzehealthinsurance"))
				{
					employeeInstance.demands.Remove("ba:jobdemand_bronzehealthinsurance");
				}
				else if (employeeInstance.demands.Contains("ba:jobdemand_silverhealthinsurance"))
				{
					employeeInstance.demands.Remove("ba:jobdemand_silverhealthinsurance");
				}
				else if (employeeInstance.demands.Contains("ba:jobdemand_goldhealthinsurance"))
				{
					employeeInstance.demands.Remove("ba:jobdemand_goldhealthinsurance");
				}
			}
		}
	}
}
