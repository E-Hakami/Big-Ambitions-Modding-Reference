using System.Collections.Generic;
using JimmysUnityUtilities;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class ScheduleHourToggleGroup : MonoBehaviour
{
	[SerializeField]
	private RectTransform headerRect;

	[SerializeField]
	private RectTransform hourLabelsRect;

	[SerializeField]
	private List<ScheduleHourToggle> hourToggles;

	private int _lastHoveredHour = -1;

	private bool _hadShift;

	[HideInInspector]
	public int lastClickedHour;

	[HideInInspector]
	public bool isMultiSelecting;

	[HideInInspector]
	public bool multiSelectValue;

	private void Start()
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			float num = headerRect.rect.width / 48f;
			hourLabelsRect.offsetMin = new Vector2(num, hourLabelsRect.offsetMin.y);
			hourLabelsRect.offsetMax = new Vector2(0f - num, hourLabelsRect.offsetMax.y);
		});
	}

	private void OnDisable()
	{
		lastClickedHour = -1;
		isMultiSelecting = false;
	}

	public void SetUp()
	{
		for (int i = 0; i < hourToggles.Count; i++)
		{
			hourToggles[i].SetUp(this, i, OnHourToggleChange, OnShiftClick);
		}
	}

	private void Update()
	{
		HandleHoverEffect();
	}

	public void UpdateState()
	{
		lastClickedHour = -1;
		for (int i = 0; i < 24; i++)
		{
			hourToggles[i].SetOpen(ScheduleHelper.OpeningHoursState[i]);
		}
	}

	private void OnShiftClick(int hour)
	{
		int num = Mathf.Min(lastClickedHour, hour);
		int num2 = Mathf.Max(lastClickedHour, hour);
		bool open = ScheduleHelper.OpeningHoursState[lastClickedHour];
		for (int i = num; i <= num2; i++)
		{
			hourToggles[i].SetOpen(open, notify: true);
		}
		lastClickedHour = -1;
		_lastHoveredHour = -1;
	}

	private void HandleHoverEffect()
	{
		ScheduleHourToggle scheduleHourToggle = hourToggles.Find((ScheduleHourToggle x) => x.IsHovered);
		int num = (scheduleHourToggle ? scheduleHourToggle.hour : (-1));
		bool flag = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
		if (_lastHoveredHour == num && _hadShift == flag)
		{
			return;
		}
		if (lastClickedHour == -1 || num == -1 || !flag)
		{
			for (int num2 = 0; num2 < 24; num2++)
			{
				hourToggles[num2].SetShiftOverlay(active: false);
			}
		}
		else
		{
			int num3 = Mathf.Min(lastClickedHour, num);
			int num4 = Mathf.Max(lastClickedHour, num);
			for (int num5 = 0; num5 < 24; num5++)
			{
				hourToggles[num5].SetShiftOverlay(num5 >= num3 && num5 <= num4);
			}
		}
		_lastHoveredHour = num;
		_hadShift = flag;
	}

	private static void OnHourToggleChange(bool isOpen, int hour)
	{
		ScheduleDay currentScheduleDay = ScheduleHelper.CurrentScheduleDay;
		if (!currentScheduleDay.isOpen & isOpen)
		{
			currentScheduleDay.openingHourSlots.Clear();
		}
		List<OpeningHourSlot> openingHourSlots = currentScheduleDay.openingHourSlots;
		if (isOpen)
		{
			int num = hour;
			int num2 = hour + 1;
			for (int num3 = openingHourSlots.Count - 1; num3 >= 0; num3--)
			{
				OpeningHourSlot openingHourSlot = openingHourSlots[num3];
				if (openingHourSlot.endingHour >= num && openingHourSlot.startingHour <= num2)
				{
					num = Mathf.Min(num, openingHourSlot.startingHour);
					num2 = Mathf.Max(num2, openingHourSlot.endingHour);
					openingHourSlots.RemoveAt(num3);
				}
			}
			openingHourSlots.Add(new OpeningHourSlot
			{
				startingHour = num,
				endingHour = num2
			});
		}
		else
		{
			for (int num4 = openingHourSlots.Count - 1; num4 >= 0; num4--)
			{
				OpeningHourSlot openingHourSlot2 = openingHourSlots[num4];
				if (hour >= openingHourSlot2.startingHour && hour < openingHourSlot2.endingHour)
				{
					openingHourSlots.RemoveAt(num4);
					if (hour > openingHourSlot2.startingHour)
					{
						openingHourSlots.Add(new OpeningHourSlot
						{
							startingHour = openingHourSlot2.startingHour,
							endingHour = hour
						});
					}
					if (hour + 1 < openingHourSlot2.endingHour)
					{
						openingHourSlots.Add(new OpeningHourSlot
						{
							startingHour = hour + 1,
							endingHour = openingHourSlot2.endingHour
						});
					}
				}
			}
		}
		openingHourSlots.Sort((OpeningHourSlot a, OpeningHourSlot b) => a.startingHour.CompareTo(b.startingHour));
		ScheduleHelper.OpeningHoursState[hour] = isOpen;
		ScheduleHelper.OnOpeningHourChanged.Invoke(hour, isOpen);
	}
}
