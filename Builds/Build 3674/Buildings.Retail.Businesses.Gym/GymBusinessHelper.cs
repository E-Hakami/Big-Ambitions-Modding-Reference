using System;
using System.Linq;
using Helpers;
using PlayerActivity;
using UI;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Buildings.Retail.Businesses.Gym;

public static class GymBusinessHelper
{
	private const string AddressableLabel = "WorkoutGroups";

	private static WorkoutGroup[] WorkoutGroups;

	private static bool IsInitialized;

	public static void Init()
	{
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		if (!IsInitialized)
		{
			WorkoutGroups = Addressables.LoadAssetsAsync<WorkoutGroup>("WorkoutGroups", null).WaitForCompletion().ToArray();
			IsInitialized = true;
		}
	}

	public static WorkoutGroupType GetWorkoutGroupType(WorkoutType workoutType)
	{
		WorkoutGroup[] workoutGroups = WorkoutGroups;
		foreach (WorkoutGroup workoutGroup in workoutGroups)
		{
			if (workoutGroup.workoutTypes.Contains(workoutType))
			{
				return workoutGroup.workoutGroupType;
			}
		}
		return WorkoutGroupType.Cardio;
	}

	private static void OnExitBuilding(Address address)
	{
		BusinessType data = BusinessTypeHelper.GetData(BuildingHelper.GetBuildingRegistration(address));
		if (!(data == null) && data.businessTypeName == "ba:businesstype_gym")
		{
			GymTrainerEmployee.workoutMachinesBeingCheered.Clear();
			if (SaveGameManager.Current.currentWorkoutPlan != null)
			{
				SaveGameManager.Current.currentWorkoutPlan = null;
				InstanceBehavior<UIs>.Instance.tasksUI.workoutPlanUI.Hide();
			}
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		WorkoutGroups = null;
		IsInitialized = false;
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
	}
}
