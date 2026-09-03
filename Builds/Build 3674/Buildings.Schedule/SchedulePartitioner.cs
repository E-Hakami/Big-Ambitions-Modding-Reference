using System.Collections.Generic;
using System.Linq;
using Entities;
using Entities.Employee.JobDemands;
using Entities.Employee.JobDemands.Requirements;
using UnityEngine;

namespace Buildings.Schedule;

public class SchedulePartitioner
{
	public class SchedulePartition
	{
		private static readonly HashSet<EmployeeInstance> VisitedEmployees = new HashSet<EmployeeInstance>();

		public readonly List<WorkStationInfo> workStations = new List<WorkStationInfo>();

		public float expectedHourCoverage;

		public int maxCleaningHours;

		public void UpdateExpectedHourCoverage(Dictionary<WorkStationInfo, int> workStationHours, int maxHours)
		{
			int num = 0;
			foreach (WorkStationInfo workStation in workStations)
			{
				num += workStationHours[workStation];
			}
			expectedHourCoverage = (float)num / (float)maxHours;
		}

		public void UpdateMaxCleaningHours(Dictionary<WorkStationInfo, List<EmployeeInstance>> suitableEmployees, Dictionary<EmployeeInstance, int> employeeMaxHours)
		{
			maxCleaningHours = 0;
			VisitedEmployees.Clear();
			foreach (WorkStationInfo workStation in workStations)
			{
				if (workStation.workShiftType != WorkShiftType.Cleaning)
				{
					continue;
				}
				foreach (EmployeeInstance item in suitableEmployees[workStation])
				{
					if (VisitedEmployees.Add(item))
					{
						maxCleaningHours += employeeMaxHours[item];
					}
				}
			}
			VisitedEmployees.Clear();
		}
	}

	public readonly List<SchedulePartition> partitions = new List<SchedulePartition>();

	public readonly Dictionary<WorkStationInfo, List<EmployeeInstance>> suitableEmployees = new Dictionary<WorkStationInfo, List<EmployeeInstance>>();

	public readonly List<EmployeeInstance> unassignedEmployees = new List<EmployeeInstance>();

	public readonly List<EmployeeInstance> unsatisfiedEmployees = new List<EmployeeInstance>();

	private const int WorkStationsIdealGroupSize = 3;

	private const float EmployeeWorkStationsRatio = 0.5f;

	private static readonly Dictionary<EmployeeInstance, List<WorkStationInfo>> SuitableWorkStations = new Dictionary<EmployeeInstance, List<WorkStationInfo>>();

	private readonly List<EmployeeInstance> _employees;

	private readonly List<WorkStationInfo> _workStations;

	private readonly ScheduleDay _specificDayToFill;

	private readonly int _maxHours;

	private readonly int _openDays;

	private readonly Dictionary<WorkStationInfo, int> _workStationHours = new Dictionary<WorkStationInfo, int>();

	private readonly Dictionary<EmployeeInstance, int> _employeeMinHours = new Dictionary<EmployeeInstance, int>();

	private readonly Dictionary<EmployeeInstance, int> _employeeMaxHours = new Dictionary<EmployeeInstance, int>();

	private readonly Dictionary<WorkStationInfo, int> _workStationPotentials = new Dictionary<WorkStationInfo, int>();

	private readonly BuildingRegistration _buildingRegistration;

	private bool _isSecondPass;

	private readonly HashSet<EmployeeInstance> _firstPassEmployees = new HashSet<EmployeeInstance>();

	private int MaxWorkStationsPerEmployee => Mathf.CeilToInt((float)_workStations.Count * 0.5f);

	public SchedulePartitioner(List<EmployeeInstance> employees, List<WorkStationInfo> workStations, BuildingRegistration buildingRegistration, ScheduleDay specificDayToFill)
	{
		_employees = employees;
		_workStations = workStations;
		_specificDayToFill = specificDayToFill;
		_maxHours = GetMaxHours(buildingRegistration);
		_openDays = GetOpenDays(buildingRegistration);
		_buildingRegistration = buildingRegistration;
		Calculate();
	}

	private void Calculate()
	{
		DistributeEmployeesToWorkStations(_buildingRegistration);
		DistributeEmployeesToWorkStationsLenient();
		MakePartitions();
		SuitableWorkStations.Clear();
	}

	public void CalculateSecondPass()
	{
		_isSecondPass = true;
		partitions.Clear();
		unassignedEmployees.Clear();
		unsatisfiedEmployees.Clear();
		Calculate();
	}

	private int GetMaxHours(BuildingRegistration buildingRegistration)
	{
		if (_specificDayToFill != null)
		{
			return _specificDayToFill.GetHoursOpen;
		}
		int num = 0;
		foreach (ScheduleDay scheduleDay in buildingRegistration.scheduleDays)
		{
			if (!scheduleDay.isOpen)
			{
				continue;
			}
			foreach (OpeningHourSlot openingHourSlot in scheduleDay.openingHourSlots)
			{
				num += openingHourSlot.endingHour - openingHourSlot.startingHour;
			}
		}
		return num;
	}

	private static int GetOpenDays(BuildingRegistration buildingRegistration)
	{
		int num = 0;
		foreach (ScheduleDay scheduleDay in buildingRegistration.scheduleDays)
		{
			if (scheduleDay.isOpen)
			{
				num++;
			}
		}
		return num;
	}

	private void DistributeEmployeesToWorkStations(BuildingRegistration buildingRegistration)
	{
		foreach (WorkStationInfo workStation in _workStations)
		{
			suitableEmployees[workStation] = new List<EmployeeInstance>();
			_workStationHours[workStation] = 0;
			_workStationPotentials[workStation] = GetWorkStationPotential(workStation);
		}
		SuitableWorkStations.Clear();
		foreach (EmployeeInstance employee in _employees)
		{
			if (_isSecondPass && !_firstPassEmployees.Contains(employee))
			{
				continue;
			}
			unassignedEmployees.Add(employee);
			List<WorkStationInfo> list = new List<WorkStationInfo>();
			SuitableWorkStations[employee] = list;
			foreach (WorkStationInfo workStation2 in _workStations)
			{
				if (workStation2.skillsRequired.Contains(employee.GetPrimarySkill()) && WorkStationFulfillsEmployeeDemands(workStation2, employee, buildingRegistration))
				{
					list.Add(workStation2);
				}
			}
		}
		int num = ((!_isSecondPass) ? 1 : MaxWorkStationsPerEmployee);
		for (int i = 0; i < num; i++)
		{
			foreach (EmployeeInstance employee2 in _employees)
			{
				if (!_isSecondPass || _firstPassEmployees.Contains(employee2))
				{
					List<WorkStationInfo> list2 = SuitableWorkStations[employee2];
					SortWorkStations(list2);
					WorkStationInfo workStationInfo = list2.FirstOrDefault();
					if (workStationInfo != null && TryAssignEmployeeToWorkStation(workStationInfo, employee2, i == 0) && _isSecondPass)
					{
						list2.Remove(workStationInfo);
					}
				}
			}
		}
	}

	private void DistributeEmployeesToWorkStationsLenient()
	{
		if (unassignedEmployees.Count == 0)
		{
			return;
		}
		EmployeeInstance[] array = unassignedEmployees.ToArray();
		EmployeeInstance[] array2 = array;
		foreach (EmployeeInstance employeeInstance in array2)
		{
			List<WorkStationInfo> list = SuitableWorkStations[employeeInstance];
			list.Clear();
			foreach (WorkStationInfo workStation in _workStations)
			{
				if (workStation.skillsRequired.Contains(employeeInstance.GetPrimarySkill()))
				{
					list.Add(workStation);
				}
			}
		}
		int num = ((!_isSecondPass) ? 1 : MaxWorkStationsPerEmployee);
		for (int j = 0; j < num; j++)
		{
			array2 = array;
			foreach (EmployeeInstance employeeInstance2 in array2)
			{
				List<WorkStationInfo> list2 = SuitableWorkStations[employeeInstance2];
				SortWorkStations(list2);
				WorkStationInfo workStationInfo = list2.FirstOrDefault();
				if (workStationInfo != null && TryAssignEmployeeToWorkStation(workStationInfo, employeeInstance2, j == 0))
				{
					if (_isSecondPass)
					{
						list2.Remove(workStationInfo);
					}
					if (unassignedEmployees.Remove(employeeInstance2))
					{
						unsatisfiedEmployees.Add(employeeInstance2);
					}
				}
			}
		}
	}

	private bool TryAssignEmployeeToWorkStation(WorkStationInfo workStation, EmployeeInstance employee, bool checkHours)
	{
		_workStationHours.TryGetValue(workStation, out var value);
		if (!_employeeMinHours.TryGetValue(employee, out var value2))
		{
			int num = 0;
			foreach (string demand in employee.demands)
			{
				if (JobDemandHelper.GetByName(demand) is HoursWorkingPerWeek hoursWorkingPerWeek)
				{
					value2 = hoursWorkingPerWeek.MinHours;
					num = hoursWorkingPerWeek.MaxHours;
					break;
				}
			}
			if (_specificDayToFill != null && _openDays > 0)
			{
				value2 /= _openDays;
			}
			if (value2 == 0 && _buildingRegistration.businessTypeName == "ba:businesstype_headquarters")
			{
				value2 = GetMaxHours(_buildingRegistration);
				num = value2;
			}
			value2 = Mathf.Max(value2, 1);
			if (num == 0)
			{
				num = GetMaxHours(_buildingRegistration);
			}
			_employeeMinHours[employee] = value2;
			_employeeMaxHours[employee] = num;
		}
		if (checkHours && value + value2 > _maxHours)
		{
			return false;
		}
		unassignedEmployees.Remove(employee);
		suitableEmployees[workStation].Add(employee);
		if (!_isSecondPass)
		{
			_firstPassEmployees.Add(employee);
		}
		if (checkHours)
		{
			_workStationHours[workStation] = value + value2;
		}
		return true;
	}

	private void MakePartitions()
	{
		SchedulePartition schedulePartition = new SchedulePartition();
		for (int i = 0; i < _workStations.Count; i++)
		{
			WorkStationInfo item = _workStations[i];
			schedulePartition.workStations.Add(item);
			if (schedulePartition.workStations.Count >= 3 && i < _workStations.Count - 1)
			{
				partitions.Add(schedulePartition);
				schedulePartition = new SchedulePartition();
			}
		}
		if (schedulePartition.workStations.Count > 0)
		{
			partitions.Add(schedulePartition);
		}
		foreach (SchedulePartition partition in partitions)
		{
			partition.UpdateExpectedHourCoverage(_workStationHours, _maxHours);
			partition.UpdateMaxCleaningHours(suitableEmployees, _employeeMaxHours);
		}
	}

	private void LogPartitions()
	{
		for (int i = 0; i < partitions.Count; i++)
		{
			SchedulePartition schedulePartition = partitions[i];
			Debug.Log($"Partition {i}:");
			foreach (WorkStationInfo workStation in schedulePartition.workStations)
			{
				Debug.Log("  WorkStation: " + workStation.itemInstance.itemName + " " + workStation.itemInstance.id);
				if (!suitableEmployees.TryGetValue(workStation, out var value))
				{
					continue;
				}
				foreach (EmployeeInstance item in value)
				{
					Debug.Log($"    {item.characterData.name} {item.GetPrimarySkill()} {_employeeMinHours[item]}-{_employeeMaxHours[item]} hours");
				}
			}
		}
		Debug.Log("unassignedEmployees " + unassignedEmployees.Count);
		foreach (EmployeeInstance unassignedEmployee in unassignedEmployees)
		{
			Debug.Log(unassignedEmployee.characterData.name + " " + unassignedEmployee.GetPrimarySkill());
		}
		Debug.Log("unsatisfiedEmployees " + unsatisfiedEmployees.Count);
		foreach (EmployeeInstance unsatisfiedEmployee in unsatisfiedEmployees)
		{
			Debug.Log(unsatisfiedEmployee.characterData.name + " " + unsatisfiedEmployee.GetPrimarySkill());
		}
	}

	private static bool WorkStationFulfillsEmployeeDemands(WorkStationInfo workStation, EmployeeInstance employee, BuildingRegistration buildingRegistration)
	{
		foreach (string demand in employee.demands)
		{
			if (JobDemandHelper.GetByName(demand) is IWorkStationFilter workStationFilter && !workStationFilter.AcceptsWorkStation(workStation, buildingRegistration))
			{
				return false;
			}
		}
		return true;
	}

	private int GetWorkStationPotential(WorkStationInfo workStation)
	{
		int num = 0;
		foreach (EmployeeInstance employee in _employees)
		{
			foreach (string demand in employee.demands)
			{
				if (JobDemandHelper.GetByName(demand) is IWorkStationFilter workStationFilter && workStationFilter.AcceptsWorkStation(workStation, _buildingRegistration))
				{
					num++;
				}
			}
		}
		return num;
	}

	private void SortWorkStations(List<WorkStationInfo> workStations)
	{
		workStations.Sort(delegate(WorkStationInfo ws1, WorkStationInfo ws2)
		{
			int num = _workStationHours[ws1] + _workStationPotentials[ws1];
			int value = _workStationHours[ws2] + _workStationPotentials[ws2];
			return num.CompareTo(value);
		});
	}
}
