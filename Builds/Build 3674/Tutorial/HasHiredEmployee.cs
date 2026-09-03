using System.Linq;
using BigAmbitions.Characters.Skills;
using Entities;
using HGAttributes;
using Helpers;
using NaughtyAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Employees/HasHiredEmployee")]
public class HasHiredEmployee : QuestRequirement
{
	public int numberOfEmployees;

	public bool withSpecificSkills;

	[ShowIf("withSpecificSkills")]
	[AutocompleteDropdown("Skills")]
	public string[] skillNames;

	public override bool CheckIfCompleted()
	{
		if (withSpecificSkills)
		{
			return EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
			{
				excludeBeingReplaced = true
			}).Count((EmployeeInstance x) => x.characterData.skills.Exists((Skill s) => skillNames.Contains(s.name) && s.value > 0f)) >= numberOfEmployees;
		}
		return EmployeeHelper.GetEmployeeInstances().Count >= numberOfEmployees;
	}
}
