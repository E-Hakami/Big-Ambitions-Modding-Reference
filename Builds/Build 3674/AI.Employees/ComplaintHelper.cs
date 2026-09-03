using System.Collections.Generic;
using Entities;
using Helpers;
using IngameDebugConsole;
using UnityEngine;

namespace AI.Employees;

public static class ComplaintHelper
{
	private static bool _ignoreMaxNumberOfComplaints;

	public static readonly LowSatisfactionComplaint LowSatisfaction = new LowSatisfactionComplaint();

	public static readonly List<Complaint> Complaints = new List<Complaint>
	{
		LowSatisfaction,
		new LowSkillComplaint(),
		new NoTaskAssignedComplaint(),
		new UnfulfilledDemandsComplaint()
	};

	public static void UpdateDaysWhenEmployeesComplained()
	{
		Queue<int> daysWithComplaints = SaveGameManager.Current.daysWithComplaints;
		while (daysWithComplaints.Count > 0 && SaveGameManager.Current.Day - SaveGameManager.Current.daysWithComplaints.Peek() >= 7)
		{
			daysWithComplaints.Dequeue();
		}
	}

	public static bool CanComplain()
	{
		if (!_ignoreMaxNumberOfComplaints)
		{
			return SaveGameManager.Current.daysWithComplaints.Count < 2;
		}
		return true;
	}

	[ConsoleMethod("ForceEmployeeComplaints", "Make employees run complaints instantly", new string[] { })]
	public static void RunEmployeeComplaints()
	{
		foreach (EmployeeInstance employeeInstance in EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			excludeBeingReplaced = true
		}))
		{
			employeeInstance.RunComplaintIfNeeded(forceComplaint: true);
		}
	}

	[ConsoleMethod("ToggleMaxNumberOfComplaints", "Toggles if max number of complaints is taken into account", new string[] { })]
	public static void ToggleIgnoreMaxNumberOfComplaints()
	{
		_ignoreMaxNumberOfComplaints = !_ignoreMaxNumberOfComplaints;
		Debug.Log("Ignore max number of complaints: " + _ignoreMaxNumberOfComplaints);
	}

	[ConsoleMethod("ShowEmployeesHoursUntilNextComplaint", "Shows the employees' hours left until next complaint", new string[] { })]
	public static void ShowEmployeesHoursUntilNextComplaint()
	{
		foreach (EmployeeInstance employeeInstance in EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			excludeBeingReplaced = true
		}))
		{
			Debug.Log(employeeInstance.characterData.name + " hours until next complaint: " + employeeInstance.complaintData.hoursUntilNextComplaint);
		}
	}
}
