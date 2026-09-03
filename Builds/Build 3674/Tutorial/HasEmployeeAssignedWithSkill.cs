using System.Linq;
using BigAmbitions.Characters.Skills;
using Entities;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Employees/HasEmployeeAssignedWithSkill")]
public class HasEmployeeAssignedWithSkill : QuestRequirement
{
	[SerializeField]
	private CustomBuildingTarget store;

	[SerializeField]
	private string[] skillNames;

	[SerializeField]
	private int minSkillValue;

	public override bool CheckIfCompleted()
	{
		if (store == null)
		{
			foreach (EmployeeInstance employeeInstance in EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
			{
				excludeBeingReplaced = true,
				isAssignedToAnyBusiness = true
			}))
			{
				foreach (Skill skill in employeeInstance.characterData.skills)
				{
					if (SkillMeetsRequirements(skill))
					{
						return true;
					}
				}
			}
			return false;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(store.GetAddress());
		if (buildingRegistration == null)
		{
			return false;
		}
		foreach (ScheduleDay scheduleDay in buildingRegistration.scheduleDays)
		{
			foreach (WorkShift workShift in scheduleDay.workShifts)
			{
				foreach (Skill skill2 in EmployeeHelper.GetEmployeeById(workShift.employeeId).characterData.skills)
				{
					if (SkillMeetsRequirements(skill2))
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private bool SkillMeetsRequirements(Skill skill)
	{
		if (skillNames == null || skillNames.Length == 0 || skillNames.Contains(skill.name))
		{
			return skill.value >= (float)minSkillValue;
		}
		return false;
	}
}
