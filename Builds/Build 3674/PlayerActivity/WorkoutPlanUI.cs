using System.Collections.Generic;
using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerActivity;

public class WorkoutPlanUI
{
	private Transform _workoutPlanGroup;

	public void Init()
	{
		if (SaveGameManager.Current.currentWorkoutPlan != null)
		{
			LoadCurrentWorkoutPlan();
		}
	}

	public void LoadCurrentWorkoutPlan()
	{
		if (_workoutPlanGroup == null)
		{
			_workoutPlanGroup = InstanceBehavior<UIs>.Instance.tasksUI.SetUpTasksGroup("workout_plan");
		}
		else
		{
			_workoutPlanGroup.gameObject.SetActive(value: true);
		}
		_workoutPlanGroup.ClearChildren(keepHiddenChildren: false, "UiTemplate");
		_workoutPlanGroup.SetAsFirstSibling();
		foreach (KeyValuePair<WorkoutType, int> item in SaveGameManager.Current.currentWorkoutPlan.plan)
		{
			Transform transform = Object.Instantiate(InstanceBehavior<UIs>.Instance.tasksUI.taskEntryTemplate, _workoutPlanGroup);
			LanguageChangeEventDataHolder data = new LanguageChangeEventDataHolder
			{
				Key = "workout_plan_objective",
				Arguments = new
				{
					workoutType = item.Key.GetLocalizeKey(),
					duration = 20
				}
			};
			transform.GetLanguageChangeEventByName("Label").SetData(data);
			transform.Find("Checkmark").GetComponent<Toggle>().isOn = item.Value <= 0;
			transform.Find("DestinationButton").gameObject.SetActive(value: false);
			transform.gameObject.SetActive(value: true);
		}
		InstanceBehavior<UIs>.Instance.tasksUI.ScheduleUpdateObjectivesHeight();
	}

	public void Hide()
	{
		if (_workoutPlanGroup != null)
		{
			_workoutPlanGroup.gameObject.SetActive(value: false);
		}
		InstanceBehavior<UIs>.Instance.tasksUI.ScheduleUpdateObjectivesHeight();
	}
}
