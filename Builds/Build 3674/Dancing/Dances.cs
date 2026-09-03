using System;

namespace Dancing;

public static class Dances
{
	private static DanceType[] _dances;

	public static DanceType[] GetAllDances()
	{
		if (_dances == null)
		{
			GenerateDancesArray();
		}
		return _dances;
	}

	public static float GetValue(this DanceType danceType)
	{
		return danceType switch
		{
			DanceType.Dance1 => 0f, 
			DanceType.Dance2 => 0.5f, 
			DanceType.Dance3 => 1f, 
			DanceType.Dance4 => 2f, 
			DanceType.Dance5 => 3f, 
			DanceType.Dance6 => 4f, 
			DanceType.Dance7 => 5f, 
			DanceType.Dance8 => 6f, 
			DanceType.Dance9 => 7f, 
			_ => 0f, 
		};
	}

	private static void GenerateDancesArray()
	{
		_dances = (DanceType[])Enum.GetValues(typeof(DanceType));
	}
}
