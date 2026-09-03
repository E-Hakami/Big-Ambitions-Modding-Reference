using System.Linq;
using BigAmbitions.Characters.Skills;
using Entities;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Employees/HasHiredOfficeEmployeeDynamic")]
public class HasHiredOfficeEmployeeDynamic : QuestRequirement
{
	[SerializeField]
	private CustomBuildingTarget officeTarget;

	public override bool CheckIfCompleted()
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(officeTarget.GetAddress());
		if (buildingRegistration == null)
		{
			return false;
		}
		string officeSkill = BusinessTypeHelper.GetData(buildingRegistration).employeePrimarySkills.First();
		return EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			excludeBeingReplaced = true
		}).Any((EmployeeInstance x) => x.characterData.skills.Exists((Skill skill) => skill.name == officeSkill));
	}
}
