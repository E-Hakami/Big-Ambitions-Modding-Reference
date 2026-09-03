using BigAmbitions.DayNightCycle;

namespace Player.PlayerMissions;

public abstract class PlayerMission
{
	public Timestamp startTime;

	public Timestamp endTime;

	public int timeLimitMinutes;

	public bool IsOngoing()
	{
		return endTime.IsInTheFuture();
	}

	public virtual bool TryDeliverToAddress(Address address)
	{
		return false;
	}

	public int GetMinutesLeft()
	{
		if (!IsOngoing())
		{
			return 0;
		}
		return TimeHelper.Now().GetDifferenceInWholeMinutes(endTime);
	}

	public string GetTimeLeftFormatted()
	{
		if (!IsOngoing())
		{
			return string.Empty;
		}
		int minutesLeft = GetMinutesLeft();
		int num = minutesLeft / 60;
		int num2 = minutesLeft % 60;
		return $"{num:0#}:{num2:0#}";
	}
}
