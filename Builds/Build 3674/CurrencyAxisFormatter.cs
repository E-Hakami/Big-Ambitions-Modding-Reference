using AwesomeCharts;
using Extensions;

public class CurrencyAxisFormatter : AxisValueFormatter
{
	public string FormatAxisValue(int index, float value, float minValue, float maxValue)
	{
		return value.ToShortCurrencyFormat(abbreviated: true);
	}
}
