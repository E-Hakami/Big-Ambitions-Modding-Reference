using System;
using Extensions;
using UI.Notification;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class ScheduleHourToggle : HoverMonoBehaviour, IPointerDownHandler, IEventSystemHandler
{
	[SerializeField]
	private GameObject isOpenIcon;

	[SerializeField]
	private GameObject isShiftClickedIcon;

	[SerializeField]
	private GameObject isNotInteractableIcon;

	private Action<bool, int> _onValueChanged;

	private Action<int> _onShiftClick;

	private bool _isOpen;

	private ScheduleHourToggleGroup _group;

	[HideInInspector]
	public int hour;

	public void SetUp(ScheduleHourToggleGroup group, int newHour, Action<bool, int> onValueChanged, Action<int> onLastShiftClick)
	{
		_group = group;
		hour = newHour;
		_onValueChanged = onValueChanged;
		_onShiftClick = onLastShiftClick;
	}

	public void SetOpen(bool open, bool notify = false)
	{
		isShiftClickedIcon.SetActive(value: false);
		isOpenIcon.SetActive(open);
		_isOpen = open;
		if (notify)
		{
			_onValueChanged?.Invoke(open, hour);
		}
	}

	public void SetShiftOverlay(bool active)
	{
		isShiftClickedIcon.SetActive(active);
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		base.OnPointerEnter(eventData);
		if (_group.isMultiSelecting)
		{
			if (!Input.GetMouseButton(0))
			{
				_group.isMultiSelecting = false;
				return;
			}
			SetOpen(_group.multiSelectValue, notify: true);
			_group.lastClickedHour = hour;
		}
	}

	public void OnPointerDown(PointerEventData eventData)
	{
		if (ScheduleHelper.IsHeadquarters)
		{
			Notifications.ShowError("bizman_schedule_cannot_toggle_open", null, trackOnSaveGame: false);
			return;
		}
		if (!_group.isMultiSelecting)
		{
			_group.isMultiSelecting = true;
			_group.multiSelectValue = !_isOpen;
		}
		if (_group.lastClickedHour != -1 && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)))
		{
			_onShiftClick?.Invoke(hour);
			return;
		}
		SetOpen(!_isOpen, notify: true);
		_group.lastClickedHour = hour;
	}
}
