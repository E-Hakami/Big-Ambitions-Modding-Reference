using System.Collections.Generic;
using System.Linq;
using BaTable;
using EnhancedUI.EnhancedScroller;
using Entities;
using Entities.Employee.JobDemands;
using Entities.Employee.JobDemands.Requirements;
using Helpers;
using UI.Smartphone.Apps.BizMan.HrManagers;

namespace UI.Smartphone.Apps.BizMan.HRManagers;

public sealed class EmployeesScrollerController : BaTable<EmployeeCellView, HrManagerEmployeeModel>
{
	public void Load(List<EmployeeInstance> employees, bool includeUnassignedEmployees, string managerId)
	{
		data.Clear();
		data = employees.Select((EmployeeInstance instance) => new HrManagerEmployeeModel(instance, GetInsuranceSortValue(instance), assigned: true)).ToList();
		if (includeUnassignedEmployees)
		{
			data.AddRange(from instance in EmployeeHelper.GetEmployeeInstances()
				where string.IsNullOrEmpty(instance.assignedHrManagerPlanId) && instance.id != managerId
				select new HrManagerEmployeeModel(instance, GetInsuranceSortValue(instance), assigned: false));
		}
		ResetFilters();
		scroller.ReloadData();
	}

	private static int GetInsuranceSortValue(EmployeeInstance instance)
	{
		HasHealthInsurance hasHealthInsurance = null;
		foreach (string demand in instance.demands)
		{
			if (JobDemandHelper.GetByName(demand) is HasHealthInsurance hasHealthInsurance2)
			{
				hasHealthInsurance = hasHealthInsurance2;
				break;
			}
		}
		if (hasHealthInsurance == null)
		{
			return -1;
		}
		return (int)hasHealthInsurance.HealthInsurancePlan;
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return 100f;
	}
}
