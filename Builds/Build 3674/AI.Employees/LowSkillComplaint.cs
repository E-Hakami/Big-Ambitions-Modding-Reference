using System;
using Buildings.Office.Headquarters;
using Entities;

namespace AI.Employees;

[Serializable]
public class LowSkillComplaint : Complaint
{
	private const int SkillValueToStopComplaining = 75;

	public LowSkillComplaint()
	{
		complaintMessageType = "ba:messagetype_employee_contact_complaint_low_skill";
		complaintMessageNoRivalType = "ba:messagetype_employee_contact_complaint_low_skill_no_rival";
		hoursToHandleComplaint = 24;
	}

	public override bool ConditionToComplainMet(EmployeeInstance employeeInstance)
	{
		float skillValue = employeeInstance.GetSkillValue(employeeInstance.GetPrimarySkill());
		if (skillValue >= 75f)
		{
			return false;
		}
		if (employeeInstance.IsTraining)
		{
			return false;
		}
		HrManagerPlan planFromId = HrManagerHelper.GetPlanFromId(employeeInstance.assignedHrManagerPlanId);
		if (planFromId != null)
		{
			return (float)planFromId.trainingTarget <= skillValue;
		}
		return true;
	}

	public override bool ComplaintHandled(EmployeeInstance employeeInstance)
	{
		return !ConditionToComplainMet(employeeInstance);
	}
}
