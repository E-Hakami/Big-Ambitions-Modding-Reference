using Entities;

namespace AI.Employees;

public class NoTaskAssignedComplaint : Complaint
{
	public NoTaskAssignedComplaint()
	{
		complaintMessageType = "ba:messagetype_employee_contact_complaint_low_no_task";
		complaintMessageNoRivalType = "ba:messagetype_employee_contact_complaint_low_no_task_no_rival";
		hoursToHandleComplaint = 24;
	}

	public override bool ConditionToComplainMet(EmployeeInstance employeeInstance)
	{
		bool num = employeeInstance.IsAssignedToAnyWorkShift();
		bool flag = employeeInstance.isTrainingDay || employeeInstance.IsTraining;
		if (!num)
		{
			return !flag;
		}
		return false;
	}

	public override bool ComplaintHandled(EmployeeInstance employeeInstance)
	{
		if (!employeeInstance.IsAssignedToAnyWorkShift())
		{
			return employeeInstance.IsTraining;
		}
		return true;
	}
}
