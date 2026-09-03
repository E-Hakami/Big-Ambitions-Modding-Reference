using System.Collections.Generic;
using System.Linq;
using BaTable;
using Buildings.Office.Headquarters;
using EnhancedUI.EnhancedScroller;
using Entities;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.HRManagers;

public class HeadhunterEmployeesScrollerController : BaTable<HeadhunterEmployeeCellView, HeadhunterEmployeeModel>
{
	[SerializeField]
	private TextLocalizationComponent amountOfEmployeesLabel;

	public void Load(HeadhunterPlan plan)
	{
		data.Clear();
		List<EmployeeInstance> list = (from x in EmployeeHelper.GetEmployeeInstances()
			where !string.IsNullOrEmpty(x.assignedHrManagerPlanId) && plan.assignedHrPlans.Contains(x.assignedHrManagerPlanId)
			select x).ToList();
		data = (from employeeInstance in list
			where employeeInstance.satisfaction <= 20f || employeeInstance.IsRetiringInLessThanDays(7)
			select new HeadhunterEmployeeModel(employeeInstance.id, employeeInstance.isBeingReplaced ? EmployeeHelper.GetAwaitingReplacementText() : employeeInstance.characterData.name, employeeInstance.IsAssignedToAnyBusiness() ? BuildingHelper.GetBuildingRegistration(employeeInstance.assignedAddress).BusinessName : "-", employeeInstance.GetPrimarySkill().GetLocalization(), employeeInstance.GetReplacementReasonLocalized())).ToList();
		amountOfEmployeesLabel.Arguments = new
		{
			amount = data.Count,
			maxAmount = list.Count
		};
		ResetFilters();
		scroller.ReloadData();
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return 100f;
	}
}
