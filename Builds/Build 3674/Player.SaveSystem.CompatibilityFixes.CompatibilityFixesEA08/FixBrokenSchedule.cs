using System.Linq;
using Buildings.Office.Headquarters;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class FixBrokenSchedule : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (gameInstance.buildNumberAtLastSave < 2900)
		{
			return;
		}
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer)
			{
				continue;
			}
			if (buildingRegistration.businessTypeName == "ba:businesstype_headquarters")
			{
				if (!buildingRegistration.scheduleDays.Any((ScheduleDay x) => x.workShifts.Any((WorkShift y) => !string.IsNullOrEmpty(y.employeeId))))
				{
					continue;
				}
				foreach (WorkShift workShift in buildingRegistration.scheduleDays[0].workShifts)
				{
					if (string.IsNullOrEmpty(workShift.employeeId))
					{
						continue;
					}
					HeadhunterPlan assignedPlanForHeadhunter = HeadhunterHelper.GetAssignedPlanForHeadhunter(workShift.employeeId);
					if (assignedPlanForHeadhunter != null)
					{
						assignedPlanForHeadhunter.isRecruiting = false;
						assignedPlanForHeadhunter.UnAssignEmployee();
						continue;
					}
					HrManagerPlan assignedPlanForHrManager = HrManagerHelper.GetAssignedPlanForHrManager(workShift.employeeId);
					if (assignedPlanForHrManager != null)
					{
						assignedPlanForHrManager.UnAssignEmployee();
						continue;
					}
					ImportPartnership assignedPlanForPurchasingAgent = PurchasingAgentHelper.GetAssignedPlanForPurchasingAgent(workShift.employeeId);
					if (assignedPlanForPurchasingAgent != null)
					{
						assignedPlanForPurchasingAgent.UnAssignEmployee();
						assignedPlanForPurchasingAgent.isActive = false;
					}
					else
					{
						LogisticsManagerHelper.GetAssignedPlanForEmployee(workShift.employeeId)?.UnAssignEmployee();
					}
				}
				buildingRegistration.scheduleDays.ForEach(delegate(ScheduleDay x)
				{
					x.workShifts.Clear();
				});
				continue;
			}
			foreach (ScheduleDay scheduleDay in buildingRegistration.scheduleDays)
			{
				for (int num = scheduleDay.workShifts.Count - 1; num >= 0; num--)
				{
					if (string.IsNullOrEmpty(scheduleDay.workShifts[num].employeeId))
					{
						scheduleDay.workShifts.RemoveAt(num);
					}
				}
			}
		}
	}
}
