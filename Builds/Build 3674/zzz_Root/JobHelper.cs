using System.Collections.Generic;
using System.Linq;
using Entities;
using Helpers;
using PlayerActivity;
using UI;
using UI.Smartphone.Apps.Contacts;

public static class JobHelper
{
	public static Job GetJob(Address address)
	{
		return BuildingHelper.GetBuilding(address)?.SpecialService?.playerJob;
	}

	public static JobInstance GetJobInstance(Address address)
	{
		return SaveGameManager.Current.JobInstances.SingleOrDefault((JobInstance x) => x.address == address);
	}

	public static void RunHourly()
	{
		foreach (JobInstance jobInstance in SaveGameManager.Current.JobInstances)
		{
			if (jobInstance.hired && jobInstance.lastWarningDay < SaveGameManager.Current.Day)
			{
				WorkShift workShift = GetJob(jobInstance.address).scheduleDays.Find((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek()).workShifts.FirstOrDefault();
				if (IsWorkingTime(jobInstance) && jobInstance.hiringDay < SaveGameManager.Current.Day && (InstanceBehavior<BuildingManager>.Instance?.building == null || InstanceBehavior<BuildingManager>.Instance?.building.Address != jobInstance.address))
				{
					SendWarning(jobInstance, workShift);
				}
			}
		}
	}

	private static void SendWarning(JobInstance jobInstance, WorkShift workShift)
	{
		jobInstance.warnings++;
		jobInstance.lastWarningDay = SaveGameManager.Current.Day;
		Contact bossContact = GetBossContact(jobInstance);
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		string messageKey;
		if (jobInstance.warnings >= 3)
		{
			messageKey = "ba:messagetype_phone_boss_fire_message";
			jobInstance.hired = false;
			jobInstance.fired = true;
			jobInstance.firedDay = SaveGameManager.Current.Day;
			GameEvent.Invoke("ba:gameevent_quitjob");
			InstanceBehavior<UIs>.Instance.playerHUD.currentJobUI.ReloadUI();
		}
		else
		{
			int startingHour = workShift.startingHour;
			int endingHour = workShift.endingHour;
			messageKey = "ba:messagetype_phone_boss_warning_onpremise";
			dictionary.Add("startingHour", startingHour.ToString());
			dictionary.Add("endingHour", endingHour.ToString());
		}
		GameManager.SendTextMessage(bossContact, messageKey, dictionary);
	}

	public static Contact GetBossContact(JobInstance jobInstance)
	{
		Job job = GetJob(jobInstance.address);
		return Contact.GetContact(description: BuildingHelper.GetBuildingRegistration(jobInstance.address).businessTypeName, name: job.employerName, category: ContactCategoryName.Employees, address: jobInstance.address);
	}

	public static bool IsWorkingTime(JobInstance jobInstance)
	{
		WorkShift workHoursToday = GetWorkHoursToday(jobInstance);
		if (workHoursToday == null)
		{
			return false;
		}
		if (SaveGameManager.Current.Hour >= workHoursToday.startingHour)
		{
			return SaveGameManager.Current.Hour < workHoursToday.endingHour;
		}
		return false;
	}

	public static WorkShift GetWorkHoursToday(JobInstance jobInstance)
	{
		ScheduleDay scheduleDay = GetJob(jobInstance.address).scheduleDays.Find((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek());
		if (!scheduleDay.isOpen)
		{
			return null;
		}
		return scheduleDay.workShifts.FirstOrDefault();
	}

	public static List<JobInstance> GetCurrentJobs()
	{
		return SaveGameManager.Current.JobInstances.Where((JobInstance x) => x.hired).ToList();
	}

	public static bool IsPlayerWorking()
	{
		if (InstanceBehavior<UIs>.Instance.playerActivityUI?.GetCurrentActivityType == typeof(WorkActivity))
		{
			return InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivityState == PlayerActivityState.Running;
		}
		return false;
	}
}
