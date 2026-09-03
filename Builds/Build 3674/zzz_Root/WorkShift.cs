using System;

[Serializable]
public class WorkShift
{
	public int startingHour;

	public int endingHour;

	public string employeeId;

	public string itemInstanceId;

	public WorkShiftType type;

	public int DurationInHours => endingHour - startingHour;

	public WorkShift Clone()
	{
		return (WorkShift)MemberwiseClone();
	}

	public bool IsHourInShift(int hour)
	{
		if (hour >= startingHour)
		{
			return hour < endingHour;
		}
		return false;
	}

	public int GetFirstOverlapHour(int startHour, int endHour)
	{
		for (int i = startHour; i < endHour; i++)
		{
			if (IsHourInShift(i))
			{
				return i;
			}
		}
		return -1;
	}

	public int GetLastOverlapHour(int startHour, int endHour)
	{
		for (int num = endHour - 1; num >= startHour; num--)
		{
			if (IsHourInShift(num))
			{
				return num;
			}
		}
		return -1;
	}
}
