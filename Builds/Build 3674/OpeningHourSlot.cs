using System;

[Serializable]
public class OpeningHourSlot
{
	public int startingHour;

	public int endingHour;

	public int GetDurationInHours => endingHour - startingHour;

	public OpeningHourSlot()
	{
	}

	public OpeningHourSlot(int startingHour, int endingHour)
	{
		this.startingHour = startingHour;
		this.endingHour = endingHour;
	}

	public OpeningHourSlot Clone()
	{
		return (OpeningHourSlot)MemberwiseClone();
	}
}
