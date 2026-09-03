using System;
using System.Collections.Generic;
using Buildings.Office.Headquarters;

namespace Entities;

[Serializable]
public class HRManager : EmployeeInstance
{
	public float experienceWithInsurancesInYears;

	[Obsolete]
	public List<string> assignedEmployees;

	[Obsolete]
	public bool replaceAbsentEmployees;

	[Obsolete]
	public int trainingTarget;

	[Obsolete]
	public HealthInsurancePlan healthInsurancePlan;

	public override void Reset()
	{
		base.Reset();
		UnAssignWork();
	}

	public override void WorkDaily()
	{
		HrManagerPlan assignedPlanForHrManager = HrManagerHelper.GetAssignedPlanForHrManager(id);
		if (assignedPlanForHrManager != null)
		{
			assignedPlanForHrManager.TrainEmployees();
			assignedPlanForHrManager.PayHealthInsurance();
		}
	}

	public override void UnAssignWork()
	{
		HrManagerHelper.GetAssignedPlanForHrManager(id)?.UnAssignEmployee();
	}
}
