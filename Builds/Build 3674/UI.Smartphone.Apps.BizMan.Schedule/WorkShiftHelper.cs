using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public static class WorkShiftHelper
{
	private static float ParentWidth;

	private static float HourWidth;

	private static ScrollRect _scrollRect;

	public static RectTransform MovableArea { get; private set; }

	public static ScrollRect ScrollRect
	{
		get
		{
			return _scrollRect;
		}
		set
		{
			_scrollRect = value;
			MovableArea = value.transform.parent.GetComponent<RectTransform>();
		}
	}

	public static void SetUp(float parentWidth)
	{
		ParentWidth = parentWidth;
		HourWidth = parentWidth / 24f;
	}

	public static Vector2 CalculateHandleRange(bool isLeft, ref RectTransform rectTransform, WorkShift workShift)
	{
		Vector2 zero = Vector2.zero;
		int num = (isLeft ? Mathf.RoundToInt(rectTransform.offsetMin.x / HourWidth) : Mathf.RoundToInt((ParentWidth + rectTransform.offsetMax.x) / HourWidth));
		List<WorkShift> list = ScheduleHelper.GetWorkShiftsByWorkstationId(workShift.itemInstanceId).Copy();
		list.AddRange(ScheduleHelper.GetWorkShiftsByEmployeeId(workShift.employeeId));
		foreach (WorkShift item in list)
		{
			if (isLeft)
			{
				if (item.startingHour != num && item.endingHour <= num)
				{
					zero.x = Mathf.Max(zero.x, (float)item.endingHour * HourWidth);
				}
			}
			else if (item.endingHour != num && item.startingHour >= num)
			{
				zero.y = Mathf.Min(zero.y, 0f - ParentWidth + (float)item.startingHour * HourWidth);
			}
		}
		if (isLeft)
		{
			zero.y = ParentWidth + rectTransform.offsetMax.x - HourWidth;
		}
		else
		{
			zero.x = 0f - ParentWidth + rectTransform.offsetMin.x + HourWidth;
		}
		return zero;
	}

	public static void SnapSliderToHour(ref RectTransform rectTransform, out int startingHour, out int endingHour)
	{
		startingHour = Mathf.RoundToInt(rectTransform.offsetMin.x / HourWidth);
		endingHour = Mathf.RoundToInt((ParentWidth + rectTransform.offsetMax.x) / HourWidth);
		rectTransform.offsetMin = new Vector2((float)startingHour * HourWidth, rectTransform.offsetMin.y);
		rectTransform.offsetMax = new Vector2(0f - ParentWidth + (float)endingHour * HourWidth, rectTransform.offsetMax.y);
	}

	public static int CalculateHourDifference(bool isLeft, RectTransform rectTransform, WorkShift workShift)
	{
		if (isLeft)
		{
			return Mathf.RoundToInt(rectTransform.offsetMin.x / HourWidth) - workShift.startingHour;
		}
		return Mathf.RoundToInt((ParentWidth + rectTransform.offsetMax.x) / HourWidth) - workShift.endingHour;
	}

	public static IEnumerator HandleAutoScrollOnEdges(float verticalScrollThreshold, Vector2 scrollSpeedRange)
	{
		float halfHeight = MovableArea.rect.height * 0.5f;
		while (Input.GetMouseButton(0))
		{
			Vector3 mousePosition = Input.mousePosition;
			if (RectTransformUtility.ScreenPointToLocalPointInRectangle(MovableArea, mousePosition, null, out var localPoint))
			{
				if (halfHeight - localPoint.y < verticalScrollThreshold)
				{
					float t = 1f - (halfHeight - localPoint.y) / verticalScrollThreshold;
					float num = Mathf.Lerp(scrollSpeedRange.x, scrollSpeedRange.y, t);
					ScrollRect.verticalNormalizedPosition += num;
				}
				else if (halfHeight + localPoint.y < verticalScrollThreshold)
				{
					float t2 = 1f - (halfHeight + localPoint.y) / verticalScrollThreshold;
					float num2 = Mathf.Lerp(scrollSpeedRange.x, scrollSpeedRange.y, t2);
					ScrollRect.verticalNormalizedPosition -= num2;
				}
			}
			else if (mousePosition.y > MovableArea.position.y + halfHeight)
			{
				ScrollRect.verticalNormalizedPosition -= scrollSpeedRange.y;
			}
			else if (mousePosition.y < MovableArea.position.y - halfHeight)
			{
				ScrollRect.verticalNormalizedPosition += scrollSpeedRange.y;
			}
			yield return null;
		}
	}
}
