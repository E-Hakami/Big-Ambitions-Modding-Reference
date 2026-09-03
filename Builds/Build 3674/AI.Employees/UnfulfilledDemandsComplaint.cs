using System;
using System.Collections.Generic;
using Entities;
using Entities.Employee.JobDemands;
using UnityEngine;

namespace AI.Employees;

[Serializable]
public class UnfulfilledDemandsComplaint : Complaint
{
	[SerializeField]
	private string demandName;

	public UnfulfilledDemandsComplaint()
	{
		complaintMessageType = "ba:messagetype_employee_contact_complaint_unfulfilled_demands";
		complaintMessageNoRivalType = "ba:messagetype_employee_contact_complaint_unfulfilled_demands_no_rival";
		hoursToHandleComplaint = 72;
	}

	public override bool ConditionToComplainMet(EmployeeInstance employeeInstance)
	{
		bool result = false;
		foreach (string demand in employeeInstance.demands)
		{
			if (!JobDemandHelper.GetByName(demand).Fulfilled(employeeInstance, null))
			{
				result = true;
				demandName = demand;
				break;
			}
		}
		return result;
	}

	public override bool ComplaintHandled(EmployeeInstance employeeInstance)
	{
		bool num = JobDemandHelper.GetByName(demandName).Fulfilled(employeeInstance, null);
		bool flag = employeeInstance.GetLastBonusDay() == SaveGameManager.Current.Day;
		return num | flag;
	}

	protected override void AddComplaintMessageData(EmployeeInstance employeeInstance, Dictionary<string, string> messageData)
	{
		messageData.Add("jobDemandName", demandName);
	}
}
