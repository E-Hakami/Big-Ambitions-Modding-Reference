using System.Linq;
using BigAmbitions.Characters.Skills;
using Entities;
using Helpers;
using NaughtyAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Employees/HasEmployeeAssigned")]
public class HasEmployeeAssigned : QuestRequirement
{
	public string skillName;

	public bool anyWorkShift = true;

	[HideIf("anyWorkShift")]
	public WorkShiftType workShiftType;

	public override bool CheckIfCompleted()
	{
		return EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			excludeBeingReplaced = true
		}).Any((EmployeeInstance x) => x.characterData.skills.Any((Skill skill) => skill.name == skillName && skill.value > 0f) && ((!anyWorkShift) ? x.IsAssignedToSpecificWorkShift(workShiftType) : x.IsAssignedToAnyWorkShift()));
	}
}
