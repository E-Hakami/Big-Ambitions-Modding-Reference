using BigAmbitions.InteriorDesigner;
using BigAmbitions.InteriorDesigner.Tools;
using Buildings.Indoors.InteriorDesign;
using IngameDebugConsole;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InteriorDesigner;

public class TimeOfDayActionPanelUI : ActionPanelUI
{
	[SerializeField]
	private GameObject timeOfDayPanel;

	[SerializeField]
	private Slider timeOfDaySlider;

	[SerializeField]
	private TMP_Text timeOfDayText;

	[SerializeField]
	private int minMinute = 300;

	[SerializeField]
	private int maxMinute = 1440;

	public override ToolName[] ToolNames => new ToolName[1] { ToolName.TimeOfDay };

	private void Awake()
	{
		TimeOfDayTool.onEnvironmentPeriodChanged = delegate(float timeInHours)
		{
			InteriorDesignerHelper.TimeOfDayController.SetEnvironmentSettings(timeInHours);
			InteriorDesignerHelper.TimeOfDayController.UpdateHourlyValues(timeInHours);
		};
	}

	private void Start()
	{
		timeOfDaySlider.minValue = minMinute;
		timeOfDaySlider.maxValue = maxMinute;
	}

	public override void OnOpen()
	{
		timeOfDayPanel.SetActive(value: true);
	}

	public override void OnClose()
	{
		timeOfDayPanel.SetActive(value: false);
	}

	public override void OnEnterInteriorDesignerMode()
	{
		float valueWithoutNotify = ((SaveGameManager.Current != null) ? ((float)(SaveGameManager.Current.Hour * 60) + SaveGameManager.Current.Minute) : 720f);
		timeOfDaySlider.SetValueWithoutNotify(valueWithoutNotify);
		timeOfDayText.SetCurrentFormattedTime();
	}

	public void OnTimeOfDayValueChanged(float value)
	{
		float num = value / 60f;
		TimeOfDayTool.OnHourChanged(num);
		int hour = Mathf.FloorToInt(num);
		int num2 = Mathf.FloorToInt(value % 60f);
		timeOfDayText.SetFormattedTime(hour, num2);
	}

	public void IncreaseTimeButton()
	{
		ChangeTimeByMinutes(10);
	}

	public void DecreaseTimeButton()
	{
		ChangeTimeByMinutes(-10);
	}

	private void ChangeTimeByMinutes(int minutes)
	{
		float num = timeOfDaySlider.value + (float)minutes;
		num = Mathf.RoundToInt(num / 10f) * 10;
		if (num > (float)maxMinute)
		{
			num = maxMinute;
		}
		else if (num < (float)minMinute)
		{
			num = minMinute;
		}
		timeOfDaySlider.value = num;
	}

	[ConsoleMethod("SetTimeOfDayPercentage", "Between 0 and 100", new string[] { })]
	public static void Command_SetTimeOfDayPercentage(float value)
	{
		float num = value / 100f * 24f;
		TimeOfDayTool.OnHourChanged(num);
		int num2 = Mathf.FloorToInt(num);
		int num3 = Mathf.FloorToInt((num - (float)num2) * 60f);
		Debug.Log($"Environment period set to {num2:00}:{num3:00}");
	}
}
