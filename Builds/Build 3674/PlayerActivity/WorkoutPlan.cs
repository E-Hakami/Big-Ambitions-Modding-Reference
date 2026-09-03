using System.Collections.Generic;
using Buildings.Retail.Businesses.Gym;
using Extensions;
using Helpers;
using UI;
using UI.Notification;

namespace PlayerActivity;

public class WorkoutPlan
{
	public const int NumberOfWorkoutExercises = 3;

	public const int MinutesPerExercise = 20;

	private static readonly List<WorkoutType> SelectedWorkoutTypes = new List<WorkoutType>();

	public readonly Dictionary<WorkoutType, int> plan = new Dictionary<WorkoutType, int>();

	public WorkoutPlan(IList<WorkoutType> workoutTypes, List<WorkoutType> previousWorkoutTypes)
	{
		SelectedWorkoutTypes.Clear();
		workoutTypes.Shuffle();
		Dictionary<WorkoutGroupType, List<WorkoutType>> workoutTypesByWorkoutGroup = GetWorkoutTypesByWorkoutGroup(workoutTypes);
		PreventIdenticalPlan(workoutTypes, previousWorkoutTypes, workoutTypesByWorkoutGroup);
		foreach (KeyValuePair<WorkoutGroupType, List<WorkoutType>> item in workoutTypesByWorkoutGroup)
		{
			WorkoutType random = item.Value.GetRandom();
			SelectedWorkoutTypes.Add(random);
			workoutTypes.Remove(random);
			if (SelectedWorkoutTypes.Count == 3)
			{
				break;
			}
		}
		if (workoutTypes.Count < 3)
		{
			SelectedWorkoutTypes.AddRange(workoutTypes.GetRandom(3 - SelectedWorkoutTypes.Count));
		}
		foreach (WorkoutType selectedWorkoutType in SelectedWorkoutTypes)
		{
			plan.Add(selectedWorkoutType, 20);
		}
	}

	private static Dictionary<WorkoutGroupType, List<WorkoutType>> GetWorkoutTypesByWorkoutGroup(IList<WorkoutType> workoutTypes)
	{
		Dictionary<WorkoutGroupType, List<WorkoutType>> dictionary = new Dictionary<WorkoutGroupType, List<WorkoutType>>();
		foreach (WorkoutType workoutType in workoutTypes)
		{
			WorkoutGroupType workoutGroupType = GymBusinessHelper.GetWorkoutGroupType(workoutType);
			if (!dictionary.TryGetValue(workoutGroupType, out var value))
			{
				value = new List<WorkoutType>();
				dictionary.Add(workoutGroupType, value);
			}
			value.Add(workoutType);
		}
		return dictionary;
	}

	private static void PreventIdenticalPlan(IList<WorkoutType> workoutTypes, List<WorkoutType> previousWorkoutTypes, Dictionary<WorkoutGroupType, List<WorkoutType>> workoutExercisesGroupedByType)
	{
		if (previousWorkoutTypes == null || workoutTypes.Count <= 3)
		{
			return;
		}
		if (workoutExercisesGroupedByType.Keys.Count > 3)
		{
			WorkoutGroupType workoutGroupType = GymBusinessHelper.GetWorkoutGroupType(previousWorkoutTypes.GetRandom());
			workoutExercisesGroupedByType.Remove(workoutGroupType);
			return;
		}
		foreach (KeyValuePair<WorkoutGroupType, List<WorkoutType>> item in workoutExercisesGroupedByType)
		{
			if (item.Value.Count == 1)
			{
				continue;
			}
			bool flag = false;
			foreach (WorkoutType previousWorkoutType in previousWorkoutTypes)
			{
				if (item.Value.Remove(previousWorkoutType))
				{
					workoutTypes.Remove(previousWorkoutType);
					flag = true;
					break;
				}
			}
			if (flag)
			{
				break;
			}
		}
	}

	public void UpdateWorkoutPlan(WorkoutType workoutType, int minutesExercised, PlayerActivityBalanceConfig completionBalanceConfig)
	{
		if (!plan.ContainsKey(workoutType) || plan[workoutType] <= 0)
		{
			return;
		}
		plan[workoutType] -= minutesExercised;
		if (IsComplete())
		{
			Notifications.Show(NotificationType.Success, "notification_workout_plan_completed");
			if (completionBalanceConfig != null && !string.IsNullOrEmpty(completionBalanceConfig.FinalType))
			{
				HappinessHelper.AddModifier(completionBalanceConfig.FinalType, completionBalanceConfig.GetBoostHours(0));
			}
			SaveGameManager.Current.currentWorkoutPlan = null;
			InstanceBehavior<UIs>.Instance.tasksUI.workoutPlanUI.Hide();
		}
		else if (plan[workoutType] <= 0)
		{
			InstanceBehavior<UIs>.Instance.tasksUI.workoutPlanUI.LoadCurrentWorkoutPlan();
		}
	}

	private bool IsComplete()
	{
		foreach (KeyValuePair<WorkoutType, int> item in plan)
		{
			if (item.Value > 0)
			{
				return false;
			}
		}
		return true;
	}
}
