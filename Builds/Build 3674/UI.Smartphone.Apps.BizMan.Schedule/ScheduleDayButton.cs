using System;
using BigAmbitions.DayNightCycle;
using Entities;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using TMPro;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class ScheduleDayButton : MonoBehaviour
{
	private static ScheduleDay CopiedScheduleDay;

	[SerializeField]
	private TextLocalizationComponent dayText;

	[SerializeField]
	private TMP_Text dayLabel;

	[SerializeField]
	private Toggle openToggle;

	[SerializeField]
	private Button selectDayButton;

	[BoxGroup("Background")]
	[SerializeField]
	private GameObject openBackground;

	[BoxGroup("Background")]
	[SerializeField]
	private GameObject closedBackground;

	[BoxGroup("Background")]
	[SerializeField]
	private GameObject selectedBackground;

	[BoxGroup("Buttons")]
	[SerializeField]
	private Button copyScheduleButton;

	[BoxGroup("Buttons")]
	[SerializeField]
	private Button pasteScheduleButton;

	[BoxGroup("Buttons")]
	[SerializeField]
	private Button autoScheduleButton;

	[BoxGroup("Buttons")]
	[SerializeField]
	private Button clearScheduleButton;

	[BoxGroup("Buttons")]
	[SerializeField]
	private Button pasteToAllDaysButton;

	[HideInInspector]
	public int dayIndex;

	private Action<int> _onDaySelected;

	private Action _updatePasteButtonVisibility;

	private Action<int> _pasteToAllDays;

	private Action _reloadSelectedDay;

	private bool _hasHrManager;

	public void SetUp(DayOfWeekOrdered day, Action<int> onDaySelected, Action updatePasteButtonVisibility, bool hasHrManager, Action<int> pasteToAllDays, Action reloadSelectedDay)
	{
		_onDaySelected = onDaySelected;
		_updatePasteButtonVisibility = updatePasteButtonVisibility;
		_hasHrManager = hasHrManager;
		_pasteToAllDays = pasteToAllDays;
		_reloadSelectedDay = reloadSelectedDay;
		openToggle.onValueChanged.RemoveAllListeners();
		selectDayButton.onClick.RemoveAllListeners();
		openToggle.onValueChanged.AddListener(OnOpenToggleChange);
		selectDayButton.onClick.AddListener(delegate
		{
			onDaySelected((int)day);
		});
		dayIndex = (int)day;
		dayText.Key = day.GetLocalizeKey();
		ScheduleDay scheduleDay = ScheduleHelper.GetScheduleDay(dayIndex);
		autoScheduleButton.gameObject.SetActive(scheduleDay.isOpen && !ScheduleHelper.IsHeadquarters && _hasHrManager && selectedBackground.activeSelf);
		UpdateButtonsVisibility();
	}

	private void OnDisable()
	{
		CopiedScheduleDay = null;
	}

	public void UpdateDayButton(bool isOpen)
	{
		openToggle.SetIsOnWithoutNotify(isOpen);
		autoScheduleButton.gameObject.SetActive(isOpen && !ScheduleHelper.IsHeadquarters && _hasHrManager && selectedBackground.activeSelf);
		ScheduleHelper.GetScheduleDay(dayIndex).isOpen = isOpen;
		if (!selectedBackground.activeSelf)
		{
			openBackground.SetActive(isOpen);
			closedBackground.SetActive(!isOpen);
		}
	}

	public void Select(bool isSelected)
	{
		if (isSelected)
		{
			selectedBackground.SetActive(value: true);
			openBackground.SetActive(value: false);
			closedBackground.SetActive(value: false);
		}
		else
		{
			selectedBackground.SetActive(value: false);
			openBackground.SetActive(openToggle.isOn);
			closedBackground.SetActive(!openToggle.isOn);
		}
		dayLabel.color = (isSelected ? InstanceBehavior<GlobalReferences>.Instance.colors.black : InstanceBehavior<GlobalReferences>.Instance.colors.white);
		UpdateButtonsVisibility();
	}

	public void UpdateButtonsVisibility()
	{
		bool activeSelf = selectedBackground.activeSelf;
		bool isOpen = ScheduleHelper.GetScheduleDay(dayIndex).isOpen;
		DayOfWeekOrdered dayOfWeekOrdered = (DayOfWeekOrdered)dayIndex;
		bool flag = CopiedScheduleDay != null;
		clearScheduleButton.gameObject.SetActive(activeSelf && !ScheduleHelper.IsHeadquarters);
		autoScheduleButton.gameObject.SetActive(((activeSelf && !ScheduleHelper.IsHeadquarters) & isOpen) && _hasHrManager && ScheduleHelper.Employees.Count > 0);
		copyScheduleButton.gameObject.SetActive((activeSelf && !ScheduleHelper.IsHeadquarters) & isOpen);
		pasteScheduleButton.gameObject.SetActive(!ScheduleHelper.IsHeadquarters & isOpen & flag);
		pasteToAllDaysButton.gameObject.SetActive(((activeSelf && !ScheduleHelper.IsHeadquarters) & flag) && CopiedScheduleDay.day == dayOfWeekOrdered);
	}

	public void OnCopyScheduleClick()
	{
		CopiedScheduleDay = ScheduleHelper.GetScheduleDay(dayIndex);
		_updatePasteButtonVisibility?.Invoke();
	}

	public void OnPasteScheduleClick(bool partOfPasteToAll)
	{
		if (CopiedScheduleDay == null || !ScheduleHelper.GetScheduleDay(dayIndex).isOpen)
		{
			return;
		}
		ScheduleDay scheduleDay = ScheduleHelper.GetScheduleDay(dayIndex);
		scheduleDay.isOpen = CopiedScheduleDay.isOpen;
		scheduleDay.openingHourSlots = CopiedScheduleDay.openingHourSlots.Copy();
		scheduleDay.workShifts = CopiedScheduleDay.workShifts.Copy();
		CopyDisabledLicensingFeesBetweenDays(CopiedScheduleDay, scheduleDay);
		if (!partOfPasteToAll)
		{
			foreach (EmployeeInstance employee in ScheduleHelper.Employees)
			{
				employee.UpdateWeeklyHoursAndDays(ScheduleHelper.ScheduleDays);
			}
			ScheduleHelper.UpdateHQPlans();
		}
		UpdateDayButton(scheduleDay.isOpen);
		if (!partOfPasteToAll)
		{
			_onDaySelected(dayIndex);
		}
	}

	private void CopyDisabledLicensingFeesBetweenDays(ScheduleDay fromDay, ScheduleDay toDay)
	{
		ClearDisabledLicensingFeesOfDay(toDay);
		for (int num = SaveGameManager.Current.disabledLicensingFees.Count - 1; num >= 0; num--)
		{
			GameInstance.DisabledLicensingFee disabledLicensingFee = SaveGameManager.Current.disabledLicensingFees[num];
			if (!(disabledLicensingFee.address != ScheduleHelper.Business.address) && disabledLicensingFee.day == fromDay.day)
			{
				SaveGameManager.Current.disabledLicensingFees.Add(new GameInstance.DisabledLicensingFee
				{
					address = disabledLicensingFee.address,
					itemId = disabledLicensingFee.itemId,
					day = toDay.day
				});
			}
		}
	}

	private void ClearDisabledLicensingFeesOfDay(ScheduleDay scheduleDay)
	{
		for (int num = SaveGameManager.Current.disabledLicensingFees.Count - 1; num >= 0; num--)
		{
			GameInstance.DisabledLicensingFee disabledLicensingFee = SaveGameManager.Current.disabledLicensingFees[num];
			if (!(disabledLicensingFee.address != ScheduleHelper.Business.address) && disabledLicensingFee.day == scheduleDay.day)
			{
				SaveGameManager.Current.disabledLicensingFees.RemoveAt(num);
			}
		}
	}

	public void OnAutoScheduleClick()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.schedule.scheduleConfirm.ShowConfirm("hud_confirm_autofill_schedule_day", delegate(bool fast)
		{
			ScheduleDay scheduleDay = ScheduleHelper.GetScheduleDay(dayIndex);
			ScheduleHelper.Business.buildingRegistration.AutoFillSchedule(null, scheduleDay, warnIfUnassigned: true, fast);
		});
	}

	public void OnClearScheduleClick()
	{
		ScheduleHelper.ClearScheduleDay(dayIndex, delegate
		{
			if (ScheduleHelper.CurrentScheduleDay.day == (DayOfWeekOrdered)dayIndex)
			{
				_onDaySelected(dayIndex);
			}
		});
	}

	public void OnPasteToAllDaysClick()
	{
		LanguageChangeEventDataHolder bodyData = "bizman_schedule_paste_to_all_days_confirm".Localize();
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			_pasteToAllDays(dayIndex);
			foreach (EmployeeInstance employee in ScheduleHelper.Employees)
			{
				employee.UpdateWeeklyHoursAndDays(ScheduleHelper.ScheduleDays);
			}
			ScheduleHelper.UpdateHQPlans();
			_onDaySelected(dayIndex);
		});
	}

	public void UpdateHrManagerAvailability(bool hasHrManager)
	{
		_hasHrManager = hasHrManager;
		autoScheduleButton.gameObject.SetActive(hasHrManager && !ScheduleHelper.IsHeadquarters && ScheduleHelper.GetScheduleDay(dayIndex).isOpen && selectedBackground.activeSelf);
	}

	private void OnOpenToggleChange(bool isOpen)
	{
		if (ScheduleHelper.IsHeadquarters)
		{
			openToggle.SetIsOnWithoutNotify(!isOpen);
			Notifications.ShowError("bizman_schedule_cannot_toggle_open", null, trackOnSaveGame: false);
			return;
		}
		ScheduleHelper.GetScheduleDay(dayIndex).isOpen = isOpen;
		ScheduleHelper.UpdateEmployeesAfterWorkShiftChange(ScheduleHelper.GetEmployeesForDay(dayIndex));
		UpdateButtonsVisibility();
		if (!selectedBackground.activeSelf)
		{
			openBackground.SetActive(isOpen);
			closedBackground.SetActive(!isOpen);
		}
		_reloadSelectedDay();
	}
}
