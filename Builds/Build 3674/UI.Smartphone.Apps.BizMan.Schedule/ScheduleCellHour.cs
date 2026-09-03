using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class ScheduleCellHour : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler
{
	public GameObject isOpenOverlay;

	public Button button;

	public GameObject addEmployeeIcon;

	private Func<RectTransform> _getWorkShiftParent;

	public string WorkstationId { get; private set; }

	public RectTransform WorkShiftParent => _getWorkShiftParent?.Invoke();

	public int Hour { get; private set; }

	public void OnPointerEnter(PointerEventData eventData)
	{
		if (!(WorkShiftDrag.CurrentDraggedWorkShift == null))
		{
			WorkShiftDrag.CurrentDraggedWorkShift.SetWorkstationParent((WorkShiftDrag.CurrentDraggedWorkShift.WorkstationId == WorkstationId) ? WorkShiftParent : null);
		}
	}

	public void SetUp(int hour, string workstationId, Func<RectTransform> getWorkShiftParent)
	{
		_getWorkShiftParent = getWorkShiftParent;
		Hour = hour;
		WorkstationId = workstationId;
		button.onClick.RemoveAllListeners();
		button.onClick.AddListener(delegate
		{
			ScheduleHelper.OnRequestEmployeeSelection.Invoke(Hour, workstationId);
		});
	}

	public void SetOpen(bool isOpen)
	{
		isOpenOverlay.SetActive(isOpen);
		button.interactable = isOpen;
	}

	public void UpdateIconVisibility(bool visible)
	{
		addEmployeeIcon.SetActive(visible);
	}
}
