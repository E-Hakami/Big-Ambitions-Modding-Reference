using AwesomeCharts;
using UnityEngine;

namespace UI.Smartphone.Apps.Rivals;

public class NearestIntAxisFormatter : AxisValueFormatter
{
	public string FormatAxisValue(int index, float value, float minValue, float maxValue)
	{
		return Mathf.RoundToInt(value).ToString();
	}
}
