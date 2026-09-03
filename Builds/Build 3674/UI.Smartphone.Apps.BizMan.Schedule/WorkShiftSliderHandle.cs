using System;
using System.Collections;
using Tooltip;
using UI.Notification;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class WorkShiftSliderHandle : MonoBehaviour, IPointerDownHandler, IEventSystemHandler
{
	private float _initialOffset;

	private bool _isLeft;

	private Action _onRightClick;

	private int _originalStartingHour;

	private CanvasScaler _parentScaler;

	private Vector2 _range;

	private RectTransform _sliderRectTransform;

	private Action _updateCell;

	private WorkShift _workShift;

	private Action<int> _onVisualHourChange;

	private int _originalWorkShiftLength;

	public void OnPointerDown(PointerEventData eventData)
	{
		if (eventData.button == PointerEventData.InputButton.Right)
		{
			_onRightClick?.Invoke();
		}
		else if (ScheduleHelper.IsHeadquarters && _workShift.type != WorkShiftType.Cleaning)
		{
			Notifications.ShowError("bizman_schedule_cannot_change_working_hours", null, trackOnSaveGame: false);
		}
		else
		{
			StartCoroutine(DraggingCoroutine());
		}
	}

	public void SetUp(bool isLeft, RectTransform sliderRectTransform, WorkShift workShift, Action updateCell, Action onRightClick, Action<int> onVisualHourChange)
	{
		_isLeft = isLeft;
		_sliderRectTransform = sliderRectTransform;
		_workShift = workShift;
		_updateCell = updateCell;
		_onRightClick = onRightClick;
		_parentScaler = GetComponentInParent<CanvasScaler>();
		_originalStartingHour = workShift.startingHour;
		_onVisualHourChange = onVisualHourChange;
	}

	private IEnumerator DraggingCoroutine()
	{
		OnSlideStart();
		Vector2 originalMousePosition = Input.mousePosition;
		while (Input.GetMouseButton(0))
		{
			float x = Mathf.Clamp((((Vector2)Input.mousePosition).x - originalMousePosition.x) / _parentScaler.transform.localScale.x + _initialOffset, _range.x, _range.y);
			if (_isLeft)
			{
				_sliderRectTransform.offsetMin = new Vector2(x, _sliderRectTransform.offsetMin.y);
			}
			else
			{
				_sliderRectTransform.offsetMax = new Vector2(x, _sliderRectTransform.offsetMax.y);
			}
			int num = WorkShiftHelper.CalculateHourDifference(_isLeft, _sliderRectTransform, _workShift);
			_onVisualHourChange?.Invoke(_isLeft ? (_originalWorkShiftLength - num) : (_originalWorkShiftLength + num));
			yield return null;
		}
		OnSlideEnd();
	}

	private void OnSlideStart()
	{
		CursorHoverChangeEvent.FreezeCursorType = true;
		TooltipSystem.PauseTooltips(pause: true);
		_initialOffset = (_isLeft ? _sliderRectTransform.offsetMin.x : _sliderRectTransform.offsetMax.x);
		_range = WorkShiftHelper.CalculateHandleRange(_isLeft, ref _sliderRectTransform, _workShift);
		ScheduleHelper.PauseScheduleScroll.Invoke(arg0: true);
		_originalStartingHour = _workShift.startingHour;
		_originalWorkShiftLength = _workShift.DurationInHours;
	}

	private void OnSlideEnd()
	{
		WorkShiftHelper.SnapSliderToHour(ref _sliderRectTransform, out var startingHour, out var endingHour);
		ScheduleHelper.EditWorkShift(_workShift.itemInstanceId, _workShift.employeeId, _originalStartingHour, startingHour, endingHour);
		ScheduleHelper.PauseScheduleScroll.Invoke(arg0: false);
		_updateCell?.Invoke();
		CursorHoverChangeEvent.FreezeCursorType = false;
		TooltipSystem.PauseTooltips(pause: false);
	}
}
