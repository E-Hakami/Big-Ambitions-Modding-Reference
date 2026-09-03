using System;
using System.Linq;
using BigAmbitions.Characters.Skills;
using Entities;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class SetEmployeeInitialSkillAmount : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (EmployeeInstance employeeInstance in gameInstance.EmployeeInstances)
		{
			float num = employeeInstance.characterData.skills.Sum((Skill x) => x.value);
			employeeInstance.initialCombinedSkillAmount = UnityEngine.Random.Range(Math.Min(20f, num - 1f), num);
		}
	}
}
