using System;
using System.Collections.Generic;
using System.Linq;
using Buildings.Schedule;
using Helpers;
using RoboRyanTron.SearchableEnum;
using UnityEngine;

namespace Entities.Employee.JobDemands.Requirements;

[Serializable]
[CreateAssetMenu(fileName = "NoSpecificShift", menuName = "BigAmbitions/Employees/JobDemand/NoSpecificShift")]
public class NoSpecificShift : JobDemand, IScheduleDemand, IWorkStationFilter
{
	[Header("No Specific Shift Demand")]
	[SerializeField]
	[SearchableEnum]
	private WorkShiftType[] workShiftTypes;

	public override bool Fulfilled(EmployeeInstance instance, List<ScheduleDay> scheduleDays)
	{
		return workShiftTypes.All((WorkShiftType workShift) => !EmployeeWorksSpecificShift(instance, scheduleDays, workShift));
	}

	private static bool EmployeeWorksSpecificShift(EmployeeInstance instance, List<ScheduleDay> scheduleDays, WorkShiftType workShiftType)
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(instance.assignedAddress);
		if (buildingRegistration == null || buildingRegistration.temporarilyClosed)
		{
			return false;
		}
		if (scheduleDays == null)
		{
			scheduleDays = buildingRegistration.scheduleDays;
		}
		return scheduleDays.Any((ScheduleDay x) => x.isOpen && x.workShifts.Any((WorkShift y) => y.employeeId == instance.id && y.type == workShiftType));
	}

	public bool AcceptsWorkStation(WorkStationInfo workStation, BuildingRegistration buildingRegistration)
	{
		return !workShiftTypes.Contains(workStation.workShiftType);
	}
}
