using System;
using Entities;

namespace AI.Employees;

[Serializable]
public class LowSatisfactionComplaint : Complaint
{
	public LowSatisfactionComplaint()
	{
		complaintMessageType = "ba:messagetype_employee_contact_complaint_low_satisfaction";
		complaintMessageNoRivalType = "ba:messagetype_employee_contact_complaint_low_satisfaction_no_rival";
		hoursToHandleComplaint = 0;
	}

	public override bool ConditionToComplainMet(EmployeeInstance employeeInstance)
	{
		return employeeInstance.satisfaction < 10f;
	}

	public override bool ComplaintHandled(EmployeeInstance employeeInstance)
	{
		return false;
	}
}
