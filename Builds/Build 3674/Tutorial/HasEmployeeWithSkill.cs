using System.Linq;
using BigAmbitions.Characters.Skills;
using Entities;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Employees/HasEmployeeWithSkill")]
public class HasEmployeeWithSkill : QuestRequirement
{
	public string skillName;

	public int minSkillValue;

	public override bool CheckIfCompleted()
	{
		return EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			excludeBeingReplaced = true
		}).Any((EmployeeInstance x) => x.characterData.skills.Exists((Skill skill) => (string.IsNullOrEmpty(skillName) || skill.name == skillName) && skill.value >= (float)minSkillValue));
	}
}
