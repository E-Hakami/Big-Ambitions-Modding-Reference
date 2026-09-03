using System;
using BehaviorDesigner.Runtime;

[Serializable]
public class SharedWorkoutMachineController : SharedVariable<WorkoutMachineController>
{
	public override string ToString()
	{
		return mValue.ToString();
	}

	public static implicit operator SharedWorkoutMachineController(WorkoutMachineController value)
	{
		return new SharedWorkoutMachineController
		{
			mValue = value
		};
	}
}
