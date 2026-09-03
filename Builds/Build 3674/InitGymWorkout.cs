using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using PlayerActivity;
using UnityEngine;

[TaskCategory("Big Ambitions/Gym")]
public class InitGymWorkout : Action
{
	private const int MaxNumberOfMachines = 4;

	private const int MinNumberOfMachines = 1;

	private const int MaxMinutesSpentOnMachines = 45;

	private const int MinMinutesSpentOnMachines = 15;

	[RequiredField]
	public SharedCustomer sharedCustomer;

	[RequiredField]
	public SharedWorkoutTypes sharedWorkoutTypes;

	[RequiredField]
	public SharedItemController sharedItemController;

	[RequiredField]
	public SharedInt numberOfMachinesToBeUsed;

	[RequiredField]
	public SharedInt minutesSpentOnMachines;

	public override void OnStart()
	{
		if (sharedWorkoutTypes.Value == null)
		{
			sharedWorkoutTypes.Value = new List<WorkoutType>();
		}
		else
		{
			sharedWorkoutTypes.Value.Clear();
		}
		sharedItemController.Value = null;
		float strength = sharedCustomer.Value.tpc.appearanceSetter.data.strength;
		numberOfMachinesToBeUsed.Value = Mathf.FloorToInt(Mathf.Lerp(1f, 4f, strength));
		minutesSpentOnMachines.Value = Mathf.FloorToInt(Mathf.Lerp(15f, 45f, strength));
	}
}
