using System.Linq;
using Entities;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Employees/HasAssignedOfficeEmployeeDynamic")]
public class HasAssignedOfficeEmployeeDynamic : QuestRequirement
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
		return buildingRegistration.scheduleDays.Any((ScheduleDay x) => x.workShifts.Select((WorkShift y) => EmployeeHelper.GetEmployeeById(y.employeeId)).Any((EmployeeInstance y) => y.HasSkill(officeSkill)));
	}
}
