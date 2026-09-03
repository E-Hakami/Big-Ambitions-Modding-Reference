using System;
using BehaviorDesigner.Runtime;
using PlayerActivity;

[Serializable]
public class SharedWorkoutType : SharedVariable<WorkoutType>
{
	public override string ToString()
	{
		return mValue.ToString();
	}

	public static implicit operator SharedWorkoutType(WorkoutType value)
	{
		return new SharedWorkoutType
		{
			mValue = value
		};
	}
}
