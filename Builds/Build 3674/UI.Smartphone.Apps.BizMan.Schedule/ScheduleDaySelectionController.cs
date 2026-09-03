using System;
using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using Extensions;
using Helpers;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class ScheduleDaySelectionController : MonoBehaviour
{
	[SerializeField]
	private Transform scheduleDayButtonTemplate;

	[SerializeField]
	private Button autoScheduleButton;

	[SerializeField]
	private BasicTooltip autoScheduleTooltip;

	[SerializeField]
	private Transform buttonsTransform;

	private readonly List<ScheduleDayButton> _scheduleDayButtons = new List<ScheduleDayButton>();

	private Action<ScheduleDay> _onDaySelected;

	private int _selectedDayIndex;

	private bool _hasHrManager;

	private void Start()
	{
		ScheduleHelper.OnWorkShiftChanged.AddListener(delegate
		{
			UpdateHrManagerAvailability();
		});
		ScheduleHelper.OnOpeningHourChanged.AddListener(OnOpeningHourChange);
	}

	private void OnDestroy()
	{
		ScheduleHelper.OnWorkShiftChanged.RemoveListener(delegate
		{
			UpdateHrManagerAvailability();
		});
		ScheduleHelper.OnOpeningHourChanged.RemoveListener(OnOpeningHourChange);
	}

	public void SetUp(Action<ScheduleDay> onDaySelected)
	{
		_onDaySelected = onDaySelected;
		_scheduleDayButtons.Clear();
		scheduleDayButtonTemplate.ResetTemplate();
		for (int i = 1; i < 8; i++)
		{
			ScheduleDayButton component = UnityEngine.Object.Instantiate(scheduleDayButtonTemplate, scheduleDayButtonTemplate.parent).GetComponent<ScheduleDayButton>();
			component.transform.SetSiblingIndex(i - 1);
			component.gameObject.SetActive(value: true);
			_scheduleDayButtons.Add(component);
			component.SetUp((DayOfWeekOrdered)i, OnDaySelected, UpdatePasteButton, _hasHrManager, PasteToAllDays, delegate
			{
				OnDaySelected(_selectedDayIndex);
			});
		}
		buttonsTransform.SetAsFirstSibling();
	}

	private void UpdatePasteButton()
	{
		foreach (ScheduleDayButton scheduleDayButton in _scheduleDayButtons)
		{
			scheduleDayButton.UpdateButtonsVisibility();
		}
	}

	private void PasteToAllDays(int dayIndex)
	{
		foreach (ScheduleDayButton scheduleDayButton in _scheduleDayButtons)
		{
			if (scheduleDayButton.dayIndex != dayIndex)
			{
				scheduleDayButton.OnPasteScheduleClick(partOfPasteToAll: true);
			}
		}
	}

	public void UpdateState()
	{
		UpdateHrManagerAvailability(force: true);
		List<ScheduleDay> scheduleDays = ScheduleHelper.ScheduleDays;
		for (int i = 0; i < scheduleDays.Count; i++)
		{
			_scheduleDayButtons[i].UpdateDayButton(scheduleDays[i].isOpen);
		}
		OnDaySelected((int)TimeHelper.GetDayOfWeek());
	}

	private void OnDaySelected(int dayIndex)
	{
		foreach (ScheduleDayButton scheduleDayButton in _scheduleDayButtons)
		{
			scheduleDayButton.Select(scheduleDayButton.dayIndex == dayIndex);
		}
		_selectedDayIndex = dayIndex;
		_onDaySelected?.Invoke(ScheduleHelper.GetScheduleDay(dayIndex));
	}

	public void OnClearScheduleClick()
	{
		ScheduleHelper.ClearAllSchedule(delegate
		{
			_onDaySelected?.Invoke(ScheduleHelper.GetScheduleDay(_selectedDayIndex));
		});
	}

	public void OnAutoFillScheduleClick()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.schedule.scheduleConfirm.ShowConfirm("hud_confirm_autofill_schedule_all", delegate(bool fast)
		{
			ScheduleHelper.Business.buildingRegistration.AutoFillSchedule(null, null, warnIfUnassigned: true, fast);
		});
	}

	public void OnEmployeesListButtonClick()
	{
		ScheduleHelper.OnRequestEmployeeSelection.Invoke(-1, string.Empty);
	}

	private void UpdateHrManagerAvailability(bool force = false)
	{
		if (!force && !ScheduleHelper.IsHeadquarters)
		{
			return;
		}
		_hasHrManager = EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
		{
			withSkills = new string[1] { "ba:skill_hrmanager" },
			excludeBeingReplaced = true,
			isAssignedToAnyWorkShift = true
		}).Count > 0;
		autoScheduleButton.interactable = _hasHrManager && ScheduleHelper.Employees.Count > 0;
		autoScheduleTooltip.descriptionKey = ((ScheduleHelper.Employees.Count == 0) ? "bizman_schedule_employee_no_employees" : (_hasHrManager ? "bizman_schedule_auto_scheduler_all_tooltip" : "bizman_schedule_auto_scheduler_not_available_tooltip"));
		foreach (ScheduleDayButton scheduleDayButton in _scheduleDayButtons)
		{
			scheduleDayButton.UpdateHrManagerAvailability(_hasHrManager);
		}
	}

	private void OnOpeningHourChange(int hour, bool isOpen)
	{
		if (!isOpen || ScheduleHelper.CurrentScheduleDay.isOpen)
		{
			return;
		}
		foreach (ScheduleDayButton scheduleDayButton in _scheduleDayButtons)
		{
			if (scheduleDayButton.dayIndex == (int)ScheduleHelper.CurrentScheduleDay.day)
			{
				scheduleDayButton.UpdateDayButton(isOpen: true);
				OnDaySelected(scheduleDayButton.dayIndex);
			}
		}
	}
}
