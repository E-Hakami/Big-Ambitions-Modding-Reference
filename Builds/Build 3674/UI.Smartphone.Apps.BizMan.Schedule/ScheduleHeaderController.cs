using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.Schedule;

public class ScheduleHeaderController : MonoBehaviour
{
	[SerializeField]
	private TMP_InputField searchField;

	[SerializeField]
	private ScheduleHourToggleGroup hourToggleGroup;

	[SerializeField]
	private Image headerImage;

	[SerializeField]
	private Color disabledHeaderColor;

	private Color _defaultHeaderColor;

	private void Awake()
	{
		_defaultHeaderColor = headerImage.color;
	}

	public void SetUp(Action<string> onSearchFieldChanged)
	{
		searchField.onValueChanged.RemoveAllListeners();
		searchField.onValueChanged.AddListener(onSearchFieldChanged.Invoke);
		hourToggleGroup.SetUp();
		ScheduleHelper.OnOpeningHourChanged.AddListener(delegate
		{
			ScheduleHelper.GenerateAddEmployeeVisibilityLookup();
		});
	}

	public void UpdateState()
	{
		searchField.SetTextWithoutNotify(string.Empty);
		ScheduleHelper.CalculateOpeningHoursState();
		ScheduleHelper.GenerateAddEmployeeVisibilityLookup();
		hourToggleGroup.UpdateState();
		headerImage.color = (ScheduleHelper.CurrentScheduleDay.isOpen ? _defaultHeaderColor : disabledHeaderColor);
	}
}
