using System;
using System.Collections.Generic;
using BaTable;
using EnhancedUI.EnhancedScroller;
using Entities;
using Entities.Employee.JobDemands;
using Entities.Employee.JobDemands.Requirements;
using JimmysUnityUtilities;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class ScheduleEmployeeScrollerController : BaTable<ScheduleEmployeeCellView, ScheduleEmployeeModel>
{
	[SerializeField]
	private float cellSize = 300f;

	[SerializeField]
	private ScheduleEmployeeFilterController filterController;

	[SerializeField]
	private TextLocalizationComponent noEmployeesLabel;

	private readonly List<ScheduleEmployeeModel> _allEmployeeModels = new List<ScheduleEmployeeModel>();

	private string _workstationId;

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return cellSize;
	}

	private void Awake()
	{
		filterController.SetUp(SetEmployeeData);
	}

	public void LoadList(Action<string> onEmployeeSelected, int hour, string workstationId)
	{
		_workstationId = workstationId;
		_allEmployeeModels.Clear();
		data.Clear();
		for (int i = 0; i < ScheduleHelper.Employees.Count; i++)
		{
			EmployeeInstance employee = ScheduleHelper.Employees[i];
			_allEmployeeModels.Add(LoadEmployee(employee, onEmployeeSelected, hour, workstationId));
		}
		_allEmployeeModels.Sort(delegate(ScheduleEmployeeModel x, ScheduleEmployeeModel y)
		{
			if (x.perfectMatch && !y.perfectMatch)
			{
				return -1;
			}
			if (!x.perfectMatch && y.perfectMatch)
			{
				return 1;
			}
			if (x.isAvailable && !y.isAvailable)
			{
				return -1;
			}
			return (!x.isAvailable && y.isAvailable) ? 1 : 0;
		});
		SetEmployeeData();
	}

	private void SetEmployeeData()
	{
		List<ScheduleEmployeeModel> items = new List<ScheduleEmployeeModel>(_allEmployeeModels);
		filterController.ApplyFilters(ref items);
		filterController.SortItems(ref items, _workstationId);
		data.Clear();
		foreach (ScheduleEmployeeModel item in items)
		{
			data.Add(item);
		}
		scroller.ReloadData();
		bool flag = items.Count > 0;
		noEmployeesLabel.gameObject.SetActive(!flag);
		if (noEmployeesLabel.gameObject.activeSelf)
		{
			bool flag2 = ScheduleHelper.Employees.Count > 0;
			noEmployeesLabel.Key = ((!flag2) ? "bizman_schedule_employee_no_employees" : "bizman_schedule_employee_no_employees_found_filter");
		}
	}

	private static ScheduleEmployeeModel LoadEmployee(EmployeeInstance employee, Action<string> onEmployeeSelected, int hour, string workstationId)
	{
		bool flag = workstationId.IsNullOrEmpty();
		bool flag2 = ((hour == -1) | flag) || ScheduleHelper.IsEmployeeAvailable(workstationId, employee.id, hour);
		bool num = flag2 && hour != -1 && !flag;
		bool perfectMatch = false;
		JobDemand jobDemand = null;
		if (num)
		{
			jobDemand = GetNotMetScheduleDemand(employee, hour, workstationId, out perfectMatch);
		}
		return new ScheduleEmployeeModel(employee, flag2, perfectMatch, (jobDemand == null) ? string.Empty : jobDemand.demandName, ScheduleHelper.GetOverworkedDays(employee), ScheduleHelper.GetEmployeeColor(employee.id), flag, onEmployeeSelected);
	}

	private static JobDemand GetNotMetScheduleDemand(EmployeeInstance employee, int hour, string workstationId, out bool perfectMatch)
	{
		perfectMatch = true;
		List<ScheduleDay> simulatedScheduleDays = ScheduleHelper.GetSimulatedScheduleDays(workstationId, hour, employee);
		foreach (string demand in employee.demands)
		{
			JobDemand byName = JobDemandHelper.GetByName(demand);
			if (!(byName is IScheduleDemand))
			{
				continue;
			}
			if (byName is HoursWorkingPerWeek hoursWorkingPerWeek)
			{
				if (hoursWorkingPerWeek.IsAboveMax(employee))
				{
					perfectMatch = false;
					if (employee.assignedWeeklyHours > hoursWorkingPerWeek.MaxHours)
					{
						return byName;
					}
				}
			}
			else if (byName is DaysWorkingPerWeek daysWorkingPerWeek)
			{
				if (daysWorkingPerWeek.IsAboveMax(employee))
				{
					perfectMatch = false;
					return byName;
				}
			}
			else if (!byName.Fulfilled(employee, simulatedScheduleDays))
			{
				perfectMatch = false;
				if (!byName.Fulfilled(employee, ScheduleHelper.ScheduleDays))
				{
					return byName;
				}
			}
		}
		return null;
	}
}
