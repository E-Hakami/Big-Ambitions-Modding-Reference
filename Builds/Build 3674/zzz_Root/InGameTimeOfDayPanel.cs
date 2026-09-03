using Buildings.Indoors.InteriorDesign;
using IngameDebugConsole;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameTimeOfDayPanel : MonoBehaviour
{
	private static InGameTimeOfDayPanel Instance;

	[SerializeField]
	private Slider timeOfDaySlider;

	[SerializeField]
	private TMP_Text timeOfDayText;

	[SerializeField]
	private int minMinute = 300;

	[SerializeField]
	private int maxMinute = 1440;

	public static float selectedTimeInHours;

	private void Awake()
	{
		Instance = this;
		base.gameObject.SetActive(value: false);
		timeOfDaySlider.minValue = minMinute;
		timeOfDaySlider.maxValue = maxMinute;
		timeOfDaySlider.onValueChanged.AddListener(OnTimeOfDayValueChanged);
	}

	public void OnEnable()
	{
		selectedTimeInHours = (float)(SaveGameManager.Current.Hour * 60) + SaveGameManager.Current.Minute;
		timeOfDaySlider.SetValueWithoutNotify(selectedTimeInHours);
		timeOfDayText.SetCurrentFormattedTime();
	}

	[ConsoleMethod("OpenTimeOfDayPanel", "Open the time of day panel.", new string[] { })]
	public static void Open()
	{
		Instance.gameObject.SetActive(value: true);
		InstanceBehavior<GameManager>.Instance.timeOfDayController.usingSliderDebugTool = true;
	}

	[ConsoleMethod("CloseTimeOfDayPanel", "Close the time of day panel.", new string[] { })]
	public static void Close()
	{
		InstanceBehavior<GameManager>.Instance.timeOfDayController.usingSliderDebugTool = false;
		Instance.gameObject.SetActive(value: false);
	}

	public void OnTimeOfDayValueChanged(float value)
	{
		float num = value / 60f;
		OnHourChanged(num);
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

	private static void OnHourChanged(float timeInHours)
	{
		TimeOfDayController timeOfDayController = InteriorDesignerHelper.TimeOfDayController;
		if ((bool)timeOfDayController)
		{
			timeOfDayController.SetEnvironmentSettings(timeInHours);
			timeOfDayController.UpdateHourlyValues(timeInHours);
			selectedTimeInHours = timeInHours;
		}
	}
}
