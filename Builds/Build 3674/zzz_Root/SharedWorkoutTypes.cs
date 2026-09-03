using System;
using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using PlayerActivity;

[Serializable]
public class SharedWorkoutTypes : SharedVariable<List<WorkoutType>>
{
	public override string ToString()
	{
		return mValue.ToString();
	}

	public static implicit operator SharedWorkoutTypes(List<WorkoutType> value)
	{
		return new SharedWorkoutTypes
		{
			mValue = value
		};
	}
}
