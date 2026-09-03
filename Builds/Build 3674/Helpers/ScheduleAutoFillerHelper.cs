using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Buildings.Schedule;
using Entities;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI;
using UI.Notification;
using UI.Smartphone.Apps.BizMan.Schedule;
using UnityEngine;

namespace Helpers;

public static class ScheduleAutoFillerHelper
{
	public static void AutoFillSchedule(this BuildingRegistration business, List<EmployeeInstance> employees = null, ScheduleDay day = null, bool warnIfUnassigned = true, bool fast = false, bool inhibitSuccessNotification = false)
	{
		if (employees == null)
		{
			employees = EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
			{
				withAssignedAddress = business.Address,
				withoutSkills = ((business.businessTypeName == "ba:businesstype_warehouse") ? null : new string[1] { "ba:skill_deliverydriver" })
			});
		}
		if (business.businessTypeName == "ba:businesstype_warehouse")
		{
			AutoFillScheduleWarehouse(employees, (Warehouse)business);
			InstanceBehavior<UIs>.Instance.tasksUI.forceCheckForCompletedTodoTasks = true;
			return;
		}
		ScheduleAutoFiller scheduleAutoFiller = new ScheduleAutoFiller(employees, business, day);
		if (warnIfUnassigned && scheduleAutoFiller.UnassignedEmployees.Count > 0)
		{
			LanguageChangeEventDataHolder bodyData = "bizman_schedule_auto_fill_unassigned".Localize();
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
			{
				business.AutoFillSchedule(employees, day, warnIfUnassigned: false, fast, inhibitSuccessNotification);
			});
		}
		else
		{
			scheduleAutoFiller.fast = fast;
			scheduleAutoFiller.inhibitSuccessNotification = inhibitSuccessNotification;
			scheduleAutoFiller.onCompleted.AddListener(OnCompleted);
			InstanceBehavior<UIs>.Instance.fullMenu.schedule.RegisterAutoFiller(scheduleAutoFiller);
			new Thread(scheduleAutoFiller.FillWithEmployees).Start();
		}
	}

	private static void OnCompleted(ScheduleAutoFiller scheduleAutoFiller, bool success)
	{
		BuildingRegistration business = scheduleAutoFiller.Registration;
		if (scheduleAutoFiller.Invalidated)
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { { "businessName", business.BusinessName } };
			Notifications.Show(NotificationType.Warning, "bizman_schedule_auto_fill_notify_cancel", notificationData);
		}
		if (!success)
		{
			return;
		}
		foreach (EmployeeInstance employee in scheduleAutoFiller.Employees)
		{
			if (!employee.IsAssignedToAnyWorkShift())
			{
				employee.UnAssignWork();
				employee.AddTodoTask(TodoTaskType.EmployeeIdle);
			}
		}
		InstanceBehavior<UIs>.Instance.tasksUI.forceCheckForCompletedTodoTasks = true;
		business.UpdateSecurityLevel();
		if (business.businessTypeName == "ba:businesstype_headquarters")
		{
			ScheduleHelper.UpdateHQPlans();
		}
		BizManBusiness business2 = InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business;
		bool flag = !scheduleAutoFiller.inhibitSuccessNotification;
		if (business2.isActiveAndEnabled && business2.buildingRegistration == business)
		{
			business2.UpdateSecurityInfo();
			BizManSchedule schedule = InstanceBehavior<UIs>.Instance.fullMenu.schedule;
			if (schedule.isActiveAndEnabled)
			{
				flag = false;
				schedule.LoadScheduler();
			}
		}
		if (flag)
		{
			string headerKey = (scheduleAutoFiller.isOptimal ? "bizman_schedule_auto_fill_notify" : "bizman_schedule_auto_fill_notify_partial");
			Dictionary<string, string> notificationData2 = new Dictionary<string, string> { { "businessName", business.BusinessName } };
			Notifications.Show(NotificationType.Success, headerKey, notificationData2, 4f, null, delegate
			{
				OnClickShowSchedule(business);
			});
		}
		ScheduleHelper.OnWorkShiftChanged?.Invoke();
	}

	private static void OnClickShowSchedule(BuildingRegistration buildingRegistration)
	{
		if (!InstanceBehavior<UIs>.Instance.timeMachine.canvas.isActiveAndEnabled)
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(buildingRegistration.Address, "Schedule");
		}
	}

	private static void AutoFillScheduleWarehouse(IEnumerable<EmployeeInstance> employees, Warehouse warehouse)
	{
		List<EmployeeInstance> list = employees.Where((EmployeeInstance x) => x.GetPrimarySkill() == "ba:skill_deliverydriver").ToList();
		foreach (VehicleSlot vehicleSlot in warehouse.vehicleSlots)
		{
			vehicleSlot.employeeDriverId = null;
		}
		for (int num = 0; num < Mathf.Min(warehouse.vehicleSlots.Count, list.Count); num++)
		{
			warehouse.vehicleSlots[num].employeeDriverId = list[num].id;
		}
	}
}
