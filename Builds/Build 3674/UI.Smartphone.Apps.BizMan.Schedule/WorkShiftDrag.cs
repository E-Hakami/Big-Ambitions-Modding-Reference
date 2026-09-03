using System;
using System.Collections.Generic;
using System.Linq;
using NaughtyAttributes;
using Tooltip;
using UI.Notification;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class WorkShiftDrag : MonoBehaviour, IDragHandler, IEventSystemHandler, IBeginDragHandler, IEndDragHandler
{
	[SerializeField]
	private RectTransform workShiftRectTransform;

	[SerializeField]
	private Image image;

	[Header("Scrolling")]
	[SerializeField]
	private float verticalScrollThreshold = 30f;

	[SerializeField]
	[MinMaxSlider(0.005f, 0.03f)]
	private Vector2 scrollSpeedRange = new Vector2(0.005f, 0.03f);

	private float _hourWidth;

	private Vector2 _initialOffset;

	private Vector2 _initialPosition;

	private Action _onWorkShiftChanged;

	private float _originalInverseX = -1f;

	private Vector3 _originalLocalScale;

	private Vector2 _originalOffsetMax;

	private Vector2 _originalOffsetMin;

	private RectTransform _originalParent;

	private Vector2 _originalPivot;

	private WorkShift _workShift;

	private RectTransform _workShiftsParentRect;

	private bool _isDragging;

	public static WorkShiftDrag CurrentDraggedWorkShift { get; private set; }

	public string WorkstationId => _workShift.itemInstanceId;

	public void OnBeginDrag(PointerEventData eventData)
	{
		_isDragging = true;
		CursorHoverChangeEvent.FreezeCursorType = true;
		TooltipSystem.PauseTooltips(pause: true);
		ScheduleHelper.RemoveShiftFromCache(_workShift);
		image.raycastTarget = false;
		SaveOriginalRectValues(eventData);
		SetRectLoose();
		CurrentDraggedWorkShift = this;
		StartCoroutine(WorkShiftHelper.HandleAutoScrollOnEdges(verticalScrollThreshold, scrollSpeedRange));
	}

	public void OnDrag(PointerEventData eventData)
	{
		if (_isDragging)
		{
			Vector3 worldPoint;
			if (_workShiftsParentRect != null && RectTransformUtility.RectangleContainsScreenPoint(_workShiftsParentRect, eventData.position))
			{
				Vector2 vector = (Vector2)workShiftRectTransform.position + new Vector2(eventData.delta.x, 0f) / WorkShiftHelper.MovableArea.localScale.x;
				vector.y = _workShiftsParentRect.position.y;
				workShiftRectTransform.position = vector;
			}
			else if (RectTransformUtility.ScreenPointToWorldPointInRectangle(WorkShiftHelper.MovableArea, eventData.position, eventData.pressEventCamera, out worldPoint))
			{
				workShiftRectTransform.position = (Vector2)worldPoint + _initialOffset;
			}
		}
	}

	public void OnEndDrag(PointerEventData eventData)
	{
		if (!_isDragging)
		{
			return;
		}
		_isDragging = false;
		CurrentDraggedWorkShift = null;
		CursorHoverChangeEvent.FreezeCursorType = false;
		TooltipSystem.PauseTooltips(pause: false);
		GameObject gameObject = eventData?.pointerCurrentRaycast.gameObject;
		if (gameObject == null)
		{
			ResetPosition();
			return;
		}
		ScheduleCellHour component = gameObject.GetComponent<ScheduleCellHour>();
		if (component == null)
		{
			ResetPosition();
			return;
		}
		_workShiftsParentRect = component.WorkShiftParent;
		int hourDelta = Mathf.RoundToInt((_workShiftsParentRect.InverseTransformPoint(eventData.position).x - _originalInverseX) / _hourWidth);
		if (!SetPosition(component, hourDelta))
		{
			ResetPosition();
			return;
		}
		image.raycastTarget = true;
		_originalInverseX = -1f;
	}

	public void SetUp(RectTransform workShiftsParentRect, WorkShift workShift, Action onWorkShiftChanged)
	{
		_workShiftsParentRect = workShiftsParentRect;
		_workShift = workShift;
		_hourWidth = workShiftsParentRect.rect.width / 24f;
		_onWorkShiftChanged = onWorkShiftChanged;
	}

	public void SetWorkstationParent(RectTransform workShiftsParentRect)
	{
		_workShiftsParentRect = workShiftsParentRect;
	}

	private bool SetPosition(ScheduleCellHour releasedCellHour, int hourDelta)
	{
		if (!ScheduleHelper.HasSkillForWorkstation(releasedCellHour.WorkstationId, _workShift.employeeId))
		{
			Notifications.ShowError("bizman_schedule_employee_doesnt_have_required_skill", null, trackOnSaveGame: false);
			return false;
		}
		List<WorkShift> list = ScheduleHelper.GetWorkShiftsByWorkstationId(releasedCellHour.WorkstationId).Copy();
		list.AddRange(ScheduleHelper.GetWorkShiftsByEmployeeId(_workShift.employeeId));
		int newStartHour;
		int newEndHour;
		if (ScheduleHelper.IsHeadquarters && _workShift.type != WorkShiftType.Cleaning)
		{
			OpeningHourSlot openingHourSlot = ScheduleHelper.CurrentScheduleDay.openingHourSlots[0];
			newStartHour = openingHourSlot.startingHour;
			newEndHour = openingHourSlot.endingHour;
			if (list.Any((WorkShift x) => x.GetFirstOverlapHour(newStartHour, newEndHour) != -1))
			{
				return false;
			}
		}
		else
		{
			newStartHour = Mathf.Clamp(_workShift.startingHour + hourDelta, 0, 24);
			newEndHour = Mathf.Clamp(_workShift.endingHour + hourDelta, 0, 24);
			AdjustHoursForOverlap(list, releasedCellHour, ref newStartHour, ref newEndHour);
		}
		if (newStartHour >= newEndHour)
		{
			return false;
		}
		_workShiftsParentRect = releasedCellHour.WorkShiftParent;
		workShiftRectTransform.anchoredPosition = _initialPosition;
		workShiftRectTransform.SetParent(_workShiftsParentRect, worldPositionStays: false);
		workShiftRectTransform.anchorMin = Vector2.zero;
		workShiftRectTransform.anchorMax = Vector2.one;
		workShiftRectTransform.pivot = _originalPivot;
		workShiftRectTransform.offsetMin = new Vector2((float)newStartHour * _hourWidth, 0f);
		workShiftRectTransform.offsetMax = new Vector2((_workShiftsParentRect.rect.width - (float)newEndHour * _hourWidth) * -1f, 0f);
		workShiftRectTransform.localScale = _originalLocalScale;
		ScheduleHelper.AddShiftToCache(_workShift);
		ScheduleHelper.MoveWorkShift(_workShift, newStartHour, newEndHour, releasedCellHour.WorkstationId);
		_onWorkShiftChanged?.Invoke();
		return true;
	}

	private void AdjustHoursForOverlap(List<WorkShift> workShifts, ScheduleCellHour releasedCellHour, ref int newStartHour, ref int newEndHour)
	{
		foreach (WorkShift workShift in workShifts)
		{
			if (workShift == _workShift)
			{
				continue;
			}
			if (workShift.IsHourInShift(newStartHour))
			{
				newStartHour = workShift.endingHour;
			}
			if (workShift.IsHourInShift(newEndHour))
			{
				newEndHour = workShift.startingHour;
			}
			int firstOverlapHour = workShift.GetFirstOverlapHour(newStartHour, newEndHour);
			if (firstOverlapHour != -1)
			{
				int lastOverlapHour = workShift.GetLastOverlapHour(newStartHour, newEndHour);
				if (releasedCellHour.Hour < workShift.startingHour)
				{
					newEndHour = firstOverlapHour;
				}
				else
				{
					newStartHour = lastOverlapHour + 1;
				}
			}
		}
	}

	private void SaveOriginalRectValues(PointerEventData eventData)
	{
		_originalInverseX = _workShiftsParentRect.InverseTransformPoint(eventData.position).x;
		_initialPosition = workShiftRectTransform.anchoredPosition;
		_initialOffset = (Vector2)workShiftRectTransform.position - eventData.position / WorkShiftHelper.MovableArea.localScale;
		if (!RectTransformUtility.RectangleContainsScreenPoint(workShiftRectTransform, eventData.position))
		{
			_initialOffset = Vector3.zero;
		}
		_originalParent = workShiftRectTransform.parent as RectTransform;
		_originalPivot = workShiftRectTransform.pivot;
		_originalOffsetMin = workShiftRectTransform.offsetMin;
		_originalOffsetMax = workShiftRectTransform.offsetMax;
		_originalLocalScale = workShiftRectTransform.localScale;
	}

	private void ResetPosition()
	{
		ScheduleCellView component = _originalParent.parent.GetComponent<ScheduleCellView>();
		if (component == null || component.WorkstationId != _workShift.itemInstanceId)
		{
			ScheduleHelper.AddShiftToCache(_workShift);
			UnityEngine.Object.Destroy(workShiftRectTransform.gameObject);
			return;
		}
		_workShiftsParentRect = component.WorkShiftParent;
		ResetParent();
		ScheduleHelper.AddShiftToCache(_workShift);
		_onWorkShiftChanged?.Invoke();
		image.raycastTarget = true;
	}

	private void ResetParent()
	{
		workShiftRectTransform.anchoredPosition = _initialPosition;
		workShiftRectTransform.SetParent(_originalParent, worldPositionStays: false);
		workShiftRectTransform.anchorMin = Vector2.zero;
		workShiftRectTransform.anchorMax = Vector2.one;
		workShiftRectTransform.pivot = _originalPivot;
		workShiftRectTransform.offsetMin = _originalOffsetMin;
		workShiftRectTransform.offsetMax = _originalOffsetMax;
		workShiftRectTransform.localScale = _originalLocalScale;
	}

	private void SetRectLoose()
	{
		Vector3 position = workShiftRectTransform.position;
		float size = _workShiftsParentRect.rect.width + workShiftRectTransform.offsetMax.x - workShiftRectTransform.offsetMin.x;
		float height = _workShiftsParentRect.rect.height;
		workShiftRectTransform.SetParent(WorkShiftHelper.MovableArea, worldPositionStays: true);
		workShiftRectTransform.anchorMin = new Vector2(0.5f, 0.5f);
		workShiftRectTransform.anchorMax = new Vector2(0.5f, 0.5f);
		workShiftRectTransform.pivot = new Vector2(0.5f, 0.5f);
		workShiftRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, size);
		workShiftRectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
		workShiftRectTransform.position = position;
	}
}
