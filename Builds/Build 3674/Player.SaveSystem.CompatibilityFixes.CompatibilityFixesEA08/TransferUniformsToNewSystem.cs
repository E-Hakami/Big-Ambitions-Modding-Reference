using System.Collections.Generic;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class TransferUniformsToNewSystem : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration2 in gameInstance.BuildingRegistrations)
		{
			BuildingRegistration buildingRegistration = buildingRegistration2;
			if (buildingRegistration.uniformsBySkill == null)
			{
				buildingRegistration.uniformsBySkill = new Dictionary<string, string>();
			}
			if (!buildingRegistration2.RentedByPlayer)
			{
				continue;
			}
			foreach (EmployeeInstance employeeInstance in gameInstance.EmployeeInstances)
			{
				if (!(employeeInstance.assignedAddress != buildingRegistration2.Address) && !buildingRegistration2.uniformsBySkill.ContainsKey(employeeInstance.GetPrimarySkill()) && employeeInstance.presetId != null)
				{
					buildingRegistration2.uniformsBySkill[employeeInstance.GetPrimarySkill()] = employeeInstance.presetId;
				}
			}
		}
	}
}
