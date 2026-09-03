using System.Linq;
using Entities;
using Extensions;
using Helpers;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class FixTooLowSalariesInOldSaveGames : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (EmployeeInstance employeeInstance in gameInstance.EmployeeInstances)
		{
			if (!(employeeInstance.hourlyWage > 1f))
			{
				float hourlyWage = employeeInstance.hourlyWage;
				if (employeeInstance.hourlyWage <= 1f)
				{
					employeeInstance.hourlyWage = EmployeeHelper.CalculateHourlyWageForSkill(employeeInstance.characterData.skills.First()) * 0.5f;
				}
				Debug.Log(employeeInstance.characterData.name + ", " + employeeInstance.characterData.skills[0].name + ", wage: from " + hourlyWage.ToCurrencyFormat() + " to " + employeeInstance.hourlyWage.ToCurrencyFormat());
			}
		}
	}
}
