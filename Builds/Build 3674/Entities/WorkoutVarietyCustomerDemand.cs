using System.Collections.Generic;
using BigAmbitions.Items;
using Helpers;
using PlayerActivity;
using UnityEngine;

namespace Entities;

[CreateAssetMenu(fileName = "WorkoutVarietyCustomerDemand", menuName = "BigAmbitions/CustomerDemands/WorkoutVariety")]
public class WorkoutVarietyCustomerDemand : CustomerDemand
{
	private static readonly HashSet<WorkoutType> WorkoutTypes = new HashSet<WorkoutType>();

	private static readonly Dictionary<string, WorkoutExercise> ItemWorkoutExercises = new Dictionary<string, WorkoutExercise>();

	public override bool Fulfilled(BuildingRegistration registration, HashSet<Item> items)
	{
		WorkoutTypes.Clear();
		foreach (Item item in items)
		{
			if ((item.type & ItemType.WorkoutMachine) == 0)
			{
				continue;
			}
			if (!ItemWorkoutExercises.TryGetValue(item.itemName, out var value))
			{
				if (!(PrefabHelper.LoadItemControllerFromPrefab(item.itemName) is WorkoutMachineController workoutMachineController))
				{
					ItemWorkoutExercises.Add(item.itemName, null);
					continue;
				}
				value = workoutMachineController.GetWorkoutExercise();
				ItemWorkoutExercises.Add(item.itemName, value);
			}
			if (!(value == null))
			{
				WorkoutTypes.Add(value.workoutType);
				if (WorkoutTypes.Count >= 5)
				{
					return true;
				}
			}
		}
		return false;
	}
}
