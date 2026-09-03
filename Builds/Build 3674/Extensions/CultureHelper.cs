using System.Globalization;

namespace Extensions;

public static class CultureHelper
{
	private const string DefaultNumberFormat = ",|.";

	public static CultureInfo CultureInfo { get; private set; }

	public static void UpdateStoredCultureInfo()
	{
		SetCultureInfo(PlayerPrefSettings.NumberFormat);
	}

	private static void SetCultureInfo(string numberFormat)
	{
		if (string.IsNullOrEmpty(numberFormat))
		{
			numberFormat = ",|.";
		}
		int num = numberFormat.IndexOf('|');
		string text = ((num >= 0) ? numberFormat.Substring(0, num) : ",");
		object obj;
		if (num < 0)
		{
			obj = ".";
		}
		else
		{
			string text2 = numberFormat;
			int num2 = num + 1;
			obj = text2.Substring(num2, text2.Length - num2);
		}
		string text3 = (string)obj;
		CultureInfo = CultureInfo.CreateSpecificCulture("en-US");
		NumberFormatInfo numberFormat2 = CultureInfo.NumberFormat;
		numberFormat2.NumberGroupSeparator = text;
		numberFormat2.NumberDecimalSeparator = text3;
		numberFormat2.CurrencyGroupSeparator = text;
		numberFormat2.CurrencyDecimalSeparator = text3;
		numberFormat2.CurrencyNegativePattern = 1;
		numberFormat2.CurrencyPositivePattern = 0;
		numberFormat2.NegativeSign = "-";
		numberFormat2.CurrencySymbol = "$";
		CultureInfo.CurrentCulture = CultureInfo;
		GlobalEvents.onCultureInfoChanged?.Invoke();
	}
}
