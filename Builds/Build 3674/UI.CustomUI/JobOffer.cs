using System;
using System.Linq;
using BigAmbitions.InputSystem;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CustomUI;

public class JobOffer : MonoBehaviour
{
	[NonSerialized]
	public Job job;

	public Button acceptButton;

	private JobInstance _jobInstance;

	public TextLocalizationComponent workHoursLabel;

	public TextMeshProUGUI workDaysValue;

	public TextMeshProUGUI wagePerHourLabel;

	public TextLocalizationComponent title;

	public TextLocalizationComponent acceptLabel;

	public TextLocalizationComponent cancelLabel;

	[HideInInspector]
	public bool isPanelOpen;

	public string happinessModifierType;

	public void Start()
	{
		workHoursLabel.Arguments = new string[7] { "common_monday", "common_tuesday", "common_wednesday", "common_thursday", "common_friday", "common_saturday", "common_sunday" };
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool isOpen)
		{
			if (isPanelOpen)
			{
				base.gameObject.SetActive(!isOpen);
			}
		});
		SetUpKeysLabels();
		GlobalEvents.onBindingsChanged = (Action)Delegate.Combine(GlobalEvents.onBindingsChanged, new Action(SetUpKeysLabels));
	}

	private void SetUpKeysLabels()
	{
		acceptLabel.Suffix = PlayerAction.Interact.AsSuffix();
		cancelLabel.Suffix = PlayerAction.Cancel.AsSuffix();
	}

	private void OnEnable()
	{
		if (!isPanelOpen && job != null)
		{
			title.Key = job.localizeKey.GetLocalization();
			_jobInstance = JobHelper.GetJobInstance(InstanceBehavior<BuildingManager>.Instance.building.Address);
			GameObject obj = acceptButton.gameObject;
			JobInstance jobInstance = _jobInstance;
			obj.SetActive(jobInstance == null || !jobInstance.hired);
			workDaysValue.text = string.Join("\n", job.scheduleDays.Select((ScheduleDay x) => (!x.isOpen) ? "playerhud_currentjob_day_off".GetLocalization() : (x.workShifts[0].startingHour.GetFormattedTime() + " - " + x.workShifts[0].endingHour.GetFormattedTime())));
			wagePerHourLabel.text = job.hourlySalary.ToShortCurrencyFormat();
			isPanelOpen = true;
		}
	}

	public void Cancel()
	{
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.JobOfferPanel);
		base.gameObject.SetActive(value: false);
		isPanelOpen = false;
	}

	public void AcceptJob()
	{
		if (SaveGameManager.Current.JobInstances.Any((JobInstance x) => x.hired))
		{
			Notifications.ShowError("joboffer_notification_already_got_job");
			return;
		}
		JobInstance jobInstance = SaveGameManager.Current.JobInstances.Find((JobInstance x) => x.fired && x.address == InstanceBehavior<BuildingManager>.Instance.building.Address && SaveGameManager.Current.Day - x.firedDay < job.minDaysBeforeRehireAfterFiring);
		if (jobInstance != null)
		{
			Notifications.ShowError("joboffer_notification_rejected");
			GameManager.SendTextMessage(JobHelper.GetBossContact(jobInstance), "ba:messagetype_phone_boss_reject_job");
			Cancel();
			return;
		}
		if (_jobInstance == null)
		{
			_jobInstance = new JobInstance();
			_jobInstance.address = InstanceBehavior<BuildingManager>.Instance.building.Address;
			SaveGameManager.Current.JobInstances.Add(_jobInstance);
		}
		_jobInstance.hired = true;
		_jobInstance.hiringDay = SaveGameManager.Current.Day;
		_jobInstance.fired = false;
		_jobInstance.firedDay = 0;
		HappinessHelper.AddModifier(happinessModifierType);
		GlobalEvents.onJobChange?.Invoke();
		GameEvent.Invoke("ba:gameevent_newjob");
		Cancel();
	}
}
