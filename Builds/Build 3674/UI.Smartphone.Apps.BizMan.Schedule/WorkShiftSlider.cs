using System;
using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using Entities;
using Entities.Employee.JobDemands;
using Entities.Employee.JobDemands.Requirements;
using Enums;
using Extensions;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class WorkShiftSlider : MonoBehaviour, IPointerDownHandler, IEventSystemHandler
{
	[SerializeField]
	private RectTransform rectTransform;

	[SerializeField]
	private EmployeeTooltip employeeTooltip;

	[SerializeField]
	private TMP_Text employeeNameLabel;

	[SerializeField]
	private TextLocalizationComponent hoursLabel;

	[SerializeField]
	private Image background;

	[Header("Sick overlay")]
	[SerializeField]
	private GameObject sickOverlayObj;

	[SerializeField]
	private Image sickIcon;

	[SerializeField]
	private Image sickAndReplacedIcon;

	[SerializeField]
	private Image awaitingReplacementIcon;

	[Header("Dragging")]
	[SerializeField]
	private WorkShiftDrag drag;

	[SerializeField]
	private WorkShiftSliderHandle leftHandle;

	[SerializeField]
	private WorkShiftSliderHandle rightHandle;

	[Header("Warning icon")]
	[SerializeField]
	private GameObject warningObj;

	[SerializeField]
	private Image warningIcon;

	[SerializeField]
	private Image warningStrip;

	private float _hourWidth;

	private int _originalStartingHour;

	private float _parentWidth;

	private RectTransform _parentRectTransform;

	private WorkShift _workShift;

	private Action _updateCellView;

	private void OnDisable()
	{
		employeeTooltip.Hide();
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			RemoveWorkShift();
		}
	}

	public void SetUp(Action updateCellView, RectTransform parentRectTransform, WorkShift workShift)
	{
		_updateCellView = updateCellView;
		_parentRectTransform = parentRectTransform;
		UpdateHourWidth();
		_workShift = workShift;
		leftHandle.SetUp(isLeft: true, rectTransform, _workShift, OnWorkShiftChanged, RemoveWorkShift, OnVisualHourChange);
		rightHandle.SetUp(isLeft: false, rectTransform, _workShift, OnWorkShiftChanged, RemoveWorkShift, OnVisualHourChange);
		drag.SetUp(parentRectTransform, workShift, OnWorkShiftChanged);
		background.color = ScheduleHelper.GetEmployeeColor(_workShift.employeeId);
	}

	private void OnRectTransformDimensionsChange()
	{
		if ((bool)_parentRectTransform && _workShift != null)
		{
			float parentWidth = _parentWidth;
			UpdateHourWidth();
			if (!Mathf.Approximately(parentWidth, _parentWidth))
			{
				WorkShiftHelper.SetUp(_parentWidth);
				drag.SetUp(_parentRectTransform, _workShift, OnWorkShiftChanged);
				UpdateState(_workShift);
			}
		}
	}

	private void OnWorkShiftChanged()
	{
		_updateCellView();
		UpdateWarning(ScheduleHelper.GetScheduleEmployeeById(_workShift.employeeId));
		hoursLabel.Arguments = new
		{
			hours = _workShift.DurationInHours
		};
		_originalStartingHour = _workShift.startingHour;
	}

	private void OnVisualHourChange(int duration)
	{
		hoursLabel.Arguments = new
		{
			hours = duration
		};
	}

	public void UpdateState(WorkShift workShift)
	{
		_originalStartingHour = workShift.startingHour;
		rectTransform.offsetMin = new Vector2((float)workShift.startingHour * _hourWidth, rectTransform.offsetMin.y);
		rectTransform.offsetMax = new Vector2(0f - (float)(24 - workShift.endingHour) * _hourWidth, rectTransform.offsetMax.y);
		hoursLabel.Arguments = new
		{
			hours = workShift.DurationInHours
		};
		EmployeeInstance scheduleEmployeeById = ScheduleHelper.GetScheduleEmployeeById(workShift.employeeId);
		employeeNameLabel.text = scheduleEmployeeById.characterData.name;
		employeeTooltip.employeeInstance = scheduleEmployeeById;
		UpdateSickOverlay(scheduleEmployeeById);
		UpdateWarning(scheduleEmployeeById);
	}

	private void UpdateHourWidth()
	{
		_parentWidth = _parentRectTransform.rect.width;
		_hourWidth = _parentWidth / 24f;
	}

	private void UpdateWarning(EmployeeInstance employee)
	{
		List<JobDemand> list = new List<JobDemand>();
		foreach (string demand in employee.demands)
		{
			JobDemand byName = JobDemandHelper.GetByName(demand);
			if (byName is IScheduleDemand && !byName.Fulfilled(employee, ScheduleHelper.ScheduleDays))
			{
				list.Add(byName);
			}
		}
		List<DayOfWeekOrdered> overworkedDays = ScheduleHelper.GetOverworkedDays(employee);
		employeeTooltip.overworkedDays = overworkedDays;
		if (list.Count == 0)
		{
			bool flag = overworkedDays.Count > 0;
			warningObj.SetActive(flag);
			if (flag)
			{
				warningIcon.color = InstanceBehavior<GlobalReferences>.Instance.colors.lightGrey;
				warningStrip.color = InstanceBehavior<GlobalReferences>.Instance.colors.lightGrey;
			}
			return;
		}
		Color32 color = list.GetMax((JobDemand a, JobDemand b) => a.priority.CompareTo(b.priority)).priority switch
		{
			Priority.Low => InstanceBehavior<GlobalReferences>.Instance.colors.green, 
			Priority.Medium => InstanceBehavior<GlobalReferences>.Instance.colors.yellow, 
			Priority.High => InstanceBehavior<GlobalReferences>.Instance.colors.red, 
			_ => InstanceBehavior<GlobalReferences>.Instance.colors.white, 
		};
		warningObj.SetActive(value: true);
		warningIcon.color = color;
		warningStrip.color = color;
	}

	private void UpdateSickOverlay(EmployeeInstance employee)
	{
		bool isAbsent = employee.isAbsent;
		bool flag = isAbsent && employee.isReplaced;
		bool flag2 = employee.isBeingReplaced && !flag;
		bool active = isAbsent | flag2;
		sickOverlayObj.SetActive(active);
		sickIcon.gameObject.SetActive(isAbsent && !employee.isReplaced && !flag2);
		sickAndReplacedIcon.gameObject.SetActive(flag);
		awaitingReplacementIcon.gameObject.SetActive(flag2);
	}

	private void RemoveWorkShift()
	{
		employeeTooltip.Hide();
		ScheduleHelper.RemoveWorkShift(_workShift.itemInstanceId, _workShift.employeeId, _originalStartingHour);
	}

	public void OnManageEmployeeClick()
	{
		InstanceBehavior<UIs>.Instance.smartphoneUI.OpenApp(AppName.MyEmployees);
		InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.DelayShowEmployee(ScheduleHelper.GetScheduleEmployeeById(_workShift.employeeId));
	}
}
