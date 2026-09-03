using System;
using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using IngameDebugConsole;
using TMPro;
using UnityEngine;

public static class TimeHelper
{
	private const string TimeFormat = "{0:00}:{1:00}";

	private const string TimeFormatAm = "{0:00}:{1:00} AM";

	private const string TimeFormatPm = "{0:00}:{1:00} PM";

	public static bool use12h;

	private static Timestamp NowCached = new Timestamp();

	public static int CurrentDay => SaveGameManager.Current?.Day ?? 0;

	public static int CurrentHour => SaveGameManager.Current?.Hour ?? 0;

	public static float CurrentMinute => SaveGameManager.Current?.Minute ?? 0f;

	public static bool IsWeekend
	{
		get
		{
			DayOfWeekOrdered dayOfWeek = GetDayOfWeek();
			return dayOfWeek == DayOfWeekOrdered.Friday || dayOfWeek == DayOfWeekOrdered.Saturday || dayOfWeek == DayOfWeekOrdered.Sunday;
		}
	}

	public static Timestamp Now()
	{
		if (SaveGameManager.Current == null)
		{
			return NowCached;
		}
		return new Timestamp
		{
			Day = CurrentDay,
			Hour = CurrentHour,
			Minute = CurrentMinute
		};
	}

	public static void SetCurrentTime(this Timestamp timestamp)
	{
		if (timestamp == null)
		{
			Debug.LogError("Timestamp cannot be null");
			return;
		}
		timestamp.Day = CurrentDay;
		timestamp.Hour = CurrentHour;
		timestamp.Minute = CurrentMinute;
	}

	public static float NowInMinutes()
	{
		NowCached.Day = CurrentDay;
		NowCached.Hour = CurrentHour;
		NowCached.Minute = CurrentMinute;
		return NowCached.GetTotalMinutes();
	}

	public static bool IsInThePast(this Timestamp timestamp)
	{
		return timestamp.GetTotalMinutes() <= NowInMinutes();
	}

	public static bool IsInThePast(int day, int hour, float minute = 0f)
	{
		if (day != CurrentDay)
		{
			return day < CurrentDay;
		}
		if (hour != CurrentHour)
		{
			return hour < CurrentHour;
		}
		return minute <= CurrentMinute;
	}

	public static bool IsInTheFuture(this Timestamp timestamp)
	{
		return timestamp.GetTotalMinutes() >= NowInMinutes();
	}

	public static bool IsInTheFuture(int day, int hour, float minute = 0f)
	{
		if (day != CurrentDay)
		{
			return day > CurrentDay;
		}
		if (hour != CurrentHour)
		{
			return hour > CurrentHour;
		}
		return minute >= CurrentMinute;
	}

	public static bool DebugConsole_ParseTimestamp(string input, out object output)
	{
		string[] array = input.ToLower().Split(',');
		int result = 0;
		int result2 = 0;
		float result3 = 0f;
		output = null;
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (text.EndsWith("d"))
			{
				if (!int.TryParse(text.Remove(text.Length - 1, 1), out result))
				{
					return false;
				}
			}
			else if (text.EndsWith("h"))
			{
				if (!int.TryParse(text.Remove(text.Length - 1, 1), out result2))
				{
					return false;
				}
			}
			else if (text.EndsWith("m"))
			{
				if (!float.TryParse(text.Remove(text.Length - 1, 1), out result3))
				{
					return false;
				}
			}
			else if (text.EndsWith("w"))
			{
				if (!int.TryParse(text.Remove(text.Length - 1, 1), out var result4))
				{
					return false;
				}
				result += result4 * 7;
			}
		}
		Timestamp timestamp = Now();
		timestamp.Day += result;
		timestamp.AddHours(result2);
		timestamp.AddMinutes(result3);
		output = timestamp;
		return true;
	}

	public static DayOfWeekOrdered GetDayOfWeek()
	{
		return GetDayOfWeek(CurrentDay);
	}

	public static DayOfWeekOrdered GetNextDayOfWeek()
	{
		return GetDayOfWeek(CurrentDay + 1);
	}

	public static int GetNextDayOfWeekNumber(DayOfWeekOrdered day)
	{
		DayOfWeekOrdered dayOfWeek = GetDayOfWeek(CurrentDay);
		int num = (day - dayOfWeek + 7) % 7;
		return CurrentDay + num;
	}

	public static int GetPreviousHour()
	{
		if (CurrentHour != 0)
		{
			return CurrentHour - 1;
		}
		return 23;
	}

	public static DayOfWeekOrdered GetDayOfWeek(int dayNumber)
	{
		return ((dayNumber - 1) % 7) switch
		{
			0 => DayOfWeekOrdered.Monday, 
			1 => DayOfWeekOrdered.Tuesday, 
			2 => DayOfWeekOrdered.Wednesday, 
			3 => DayOfWeekOrdered.Thursday, 
			4 => DayOfWeekOrdered.Friday, 
			5 => DayOfWeekOrdered.Saturday, 
			6 => DayOfWeekOrdered.Sunday, 
			_ => throw new InvalidOperationException(), 
		};
	}

	public static int GetDayOfWeekIndex(DayOfWeekOrdered day)
	{
		return (int)day;
	}

	public static int GetDaysByYears(float years)
	{
		return Mathf.RoundToInt(years * (float)SaveGameManager.Current.gameVariables.daysPerYear);
	}

	public static int GetYearsByDays(int days)
	{
		return days / SaveGameManager.Current.gameVariables.daysPerYear;
	}

	public static string GetFormattedTime(this int hour, float minute = 0f, bool removeMinutes = false)
	{
		if (use12h)
		{
			hour = To12Hour(hour, out var isPm);
			string text = (isPm ? "PM" : "AM");
			if (!removeMinutes)
			{
				return $"{hour:0#}:{Mathf.Floor(minute):0#} {text}";
			}
			return $"{hour:0#} {text}";
		}
		if (!removeMinutes)
		{
			return $"{hour:0#}:{Mathf.Floor(minute):0#}";
		}
		return $"{hour:0#}";
	}

	public static string GetFormattedTime(this Timestamp timestamp, bool removeMinutes = false)
	{
		return timestamp.Hour.GetFormattedTime(timestamp.Minute, removeMinutes);
	}

	public static string GetCurrentFormattedTime(bool removeMinutes = false)
	{
		return CurrentHour.GetFormattedTime(CurrentMinute, removeMinutes);
	}

	public static float GetMinutesFromNow(this Timestamp timestamp)
	{
		return (float)(((timestamp.Day - CurrentDay) * 24 + timestamp.Hour - CurrentHour) * 60) + timestamp.Minute - CurrentMinute;
	}

	public static void SetCurrentFormattedTime(this TMP_Text label)
	{
		label.SetFormattedTime(CurrentHour, CurrentMinute);
	}

	public static void SetFormattedTime(this TMP_Text label, int hour, float minute)
	{
		minute = Mathf.Floor(minute);
		if (!use12h)
		{
			label.SetText("{0:00}:{1:00}", hour, minute);
			return;
		}
		hour = To12Hour(hour, out var isPm);
		label.SetText(isPm ? "{0:00}:{1:00} PM" : "{0:00}:{1:00} AM", hour, minute);
	}

	private static int To12Hour(int hour, out bool isPm)
	{
		int num = hour % 24;
		isPm = num >= 12;
		int num2 = num % 12;
		if (num2 != 0)
		{
			return num2;
		}
		return 12;
	}

	public static string GetCurrentFormattedTimeForced(bool use12Hrs = false)
	{
		bool flag = use12h;
		use12h = use12Hrs;
		string currentFormattedTime = GetCurrentFormattedTime();
		use12h = flag;
		return currentFormattedTime;
	}

	public static Timestamp GetTimeInHours(int hours, int minutes = 0)
	{
		Timestamp timestamp = Now();
		timestamp.Minute = Mathf.Floor(timestamp.Minute);
		timestamp.AddHours(hours);
		if (minutes != 0)
		{
			timestamp.AddMinutes(minutes);
		}
		return timestamp;
	}

	public static int GetPlayerRealAgeInDays(int age)
	{
		return age + GetDaysByYears(10 * SaveGameManager.Current.numberOfDoctorOperations);
	}

	public static bool IsInHourRange(int hourStart, int hourEndExclusive)
	{
		if (hourStart < hourEndExclusive)
		{
			if (CurrentHour >= hourStart)
			{
				return CurrentHour < hourEndExclusive;
			}
			return false;
		}
		if (CurrentHour < hourStart)
		{
			return CurrentHour < hourEndExclusive;
		}
		return true;
	}

	public static bool TryParseTimeParameter(string timeParameter, out Timestamp timestamp)
	{
		Timestamp timestamp2 = ParseTimeParameter(timeParameter);
		if (timestamp2 == null)
		{
			timestamp = null;
			return false;
		}
		timestamp = timestamp2;
		return true;
	}

	public static Timestamp ParseTimeParameter(string timeParameter)
	{
		Timestamp timestamp = Now();
		int num = 0;
		for (int i = 0; i < timeParameter.Length; i++)
		{
			if (char.IsDigit(timeParameter[i]))
			{
				num = num * 10 + (timeParameter[i] - 48);
				continue;
			}
			switch (timeParameter[i])
			{
			case 'w':
				timestamp.AddDays(num * 7);
				break;
			case 'd':
				timestamp.AddDays(num);
				break;
			case 'h':
				timestamp.AddHours(num);
				break;
			case 'm':
				timestamp.AddMinutes(num);
				break;
			default:
				return null;
			}
			num = 0;
		}
		return timestamp;
	}

	public static List<(int day, int hour)> GetUpcomingHourSlots(int minutesFromNow, int slotCount)
	{
		List<(int, int)> list = new List<(int, int)>(slotCount);
		Timestamp timestamp = Now().AddMinutes(minutesFromNow);
		if (timestamp.Minute > 0f)
		{
			timestamp.Minute = 0f;
			timestamp.AddHours(1);
		}
		for (int i = 0; i < slotCount; i++)
		{
			list.Add((timestamp.Day, timestamp.Hour));
			timestamp.AddHours(1);
		}
		return list;
	}

	[ConsoleMethod("SetTimeSpeed", "Set the game speed in percentage. 100% is normal speed.", new string[] { })]
	public static void Command_SetTimeSpeed(int gameSpeedPercentage)
	{
		GameManager.SetMinutesMultiplier((float)gameSpeedPercentage / 100f);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		use12h = false;
		NowCached = new Timestamp();
	}
}
