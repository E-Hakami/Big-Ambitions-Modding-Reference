using System.Collections.Generic;
using System.Linq;
using BaTable;
using Buildings.BuildingTypes.Shared;
using EnhancedUI.EnhancedScroller;
using JimmysUnityUtilities;
using UI.Notification;

namespace UI.Smartphone.Apps.BizMan;

public class RivalEmployeeScrollerController : BaTable<RivalEmployeeCellView, RivalEmployeeModel>
{
	public void LoadList(IEnumerable<AiBusinessEmployeeData> aiEmployees)
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			data.Clear();
			data = aiEmployees.Select((AiBusinessEmployeeData employee) => new RivalEmployeeModel(employee, delegate(AiBusinessEmployeeData e)
			{
				OnNegotiate(employee, e);
			})).ToList();
			ResetFilters();
			scroller.ReloadData();
		});
	}

	private static void OnNegotiate(AiBusinessEmployeeData employee, AiBusinessEmployeeData e)
	{
		if (!employee.isNegotiationFinished || TryReenableNegotiation(employee))
		{
			InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.NegotiateWithCandidate(e.GetEmployeeInstance(), isRival: true, e.isPoached);
			InstanceBehavior<UIs>.Instance.rivalEmployeesUi.Toggle(newState: false);
		}
	}

	private static bool TryReenableNegotiation(AiBusinessEmployeeData employee)
	{
		if (employee.reenableNegotiationAtDay > TimeHelper.CurrentDay)
		{
			int num = employee.reenableNegotiationAtDay - TimeHelper.CurrentDay;
			Dictionary<string, string> notificationData = new Dictionary<string, string> { 
			{
				"amount",
				num.ToString()
			} };
			Notifications.Show(NotificationType.Error, "notification_cannot_poach_employee_yet", notificationData);
			return false;
		}
		employee.isNegotiationFinished = false;
		return true;
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return 100f;
	}
}
