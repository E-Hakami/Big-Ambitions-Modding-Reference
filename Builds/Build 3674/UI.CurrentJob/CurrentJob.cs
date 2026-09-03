using System;
using System.Linq;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using TMPro;
using UI.Guiders;
using UI.Notification;
using UnityEngine;

namespace UI.CurrentJob;

public class CurrentJob : MonoBehaviour
{
	public TextLocalizationComponent title;

	public TextMeshProUGUI businessName;

	public TextLocalizationComponent address;

	public TextLocalizationComponent todayWorkStatus;

	public TextLocalizationComponent tomorrowWorkStatus;

	public GameObject panel;

	private JobInstance _jobInstance;

	public bool HasCurrentJob => _jobInstance != null;

	private void Start()
	{
		ReloadUI();
		GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(ReloadUI));
		GlobalEvents.onJobChange = (Action)Delegate.Combine(GlobalEvents.onJobChange, new Action(ReloadUI));
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool isOpen)
		{
			if (HasCurrentJob)
			{
				base.gameObject.SetActive(!isOpen);
			}
		});
	}

	public void ReloadUI()
	{
		_jobInstance = JobHelper.GetCurrentJobs().FirstOrDefault();
		panel.SetActive(_jobInstance != null && !CityMap.IsOpen);
		if (_jobInstance == null)
		{
			return;
		}
		Job job = JobHelper.GetJob(_jobInstance.address);
		address.SetValue(_jobInstance.address.ToFormattedString(), clearKey: true);
		title.Key = job.localizeKey.GetLocalization();
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(_jobInstance.address);
		businessName.text = buildingRegistration.BusinessName;
		WorkShift workShift = job.scheduleDays.SingleOrDefault((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek() && x.isOpen)?.workShifts.SingleOrDefault();
		if (workShift != null)
		{
			if (workShift.endingHour <= SaveGameManager.Current.Hour)
			{
				todayWorkStatus.Arguments = new string[2] { "common_today", "playerhud_currentjob_finished" };
				todayWorkStatus.TextContainer.color = InstanceBehavior<GlobalReferences>.Instance.colors.green;
			}
			else
			{
				bool flag = JobHelper.IsWorkingTime(_jobInstance);
				todayWorkStatus.Arguments = new string[2]
				{
					"common_today",
					workShift.startingHour.GetFormattedTime() + " - " + workShift.endingHour.GetFormattedTime()
				};
				todayWorkStatus.TextContainer.color = ((flag && (!(SaveGameManager.Current.CurrentStreetName == _jobInstance.address.streetName) || SaveGameManager.Current.CurrentStreetNumber != _jobInstance.address.streetNumber)) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : InstanceBehavior<GlobalReferences>.Instance.colors.white);
			}
		}
		else
		{
			todayWorkStatus.Arguments = new string[2] { "common_today", "playerhud_currentjob_day_off" };
			todayWorkStatus.TextContainer.color = InstanceBehavior<GlobalReferences>.Instance.colors.lightGrey;
		}
		WorkShift workShift2 = job.scheduleDays.SingleOrDefault((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek(SaveGameManager.Current.Day + 1) && x.isOpen)?.workShifts.SingleOrDefault();
		if (workShift2 != null)
		{
			tomorrowWorkStatus.Arguments = new string[2]
			{
				"common_tomorrow",
				workShift2.startingHour.GetFormattedTime() + " - " + workShift2.endingHour.GetFormattedTime()
			};
			tomorrowWorkStatus.TextContainer.color = InstanceBehavior<GlobalReferences>.Instance.colors.white;
		}
		else
		{
			tomorrowWorkStatus.Arguments = new string[2] { "common_tomorrow", "playerhud_currentjob_day_off" };
			tomorrowWorkStatus.TextContainer.color = InstanceBehavior<GlobalReferences>.Instance.colors.lightGrey;
		}
	}

	public void SetDestination()
	{
		GuidersManager.SetGuiderTarget(_jobInstance.address, DirectionGuiderType.Destination);
	}

	public void QuitJob()
	{
		if (JobHelper.IsPlayerWorking())
		{
			Notifications.ShowError("playerhud_currentjob_notification_cant_quit_working");
			return;
		}
		_jobInstance.hired = false;
		_jobInstance.fired = false;
		_jobInstance.firedDay = 0;
		GameEvent.Invoke("ba:gameevent_quitjob");
		GameEvent.Invoke(string.Empty);
		ReloadUI();
	}

	public void Hide(bool hide)
	{
		if (HasCurrentJob)
		{
			panel.SetActive(!hide);
		}
	}
}
