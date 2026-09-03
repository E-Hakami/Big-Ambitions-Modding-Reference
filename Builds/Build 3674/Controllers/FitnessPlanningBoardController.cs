using System.Collections.Generic;
using System.Linq;
using Entities;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using PlayerActivity;
using UI;
using UI.Notification;
using UnityEngine;

namespace Controllers;

public class FitnessPlanningBoardController : EmployeeStationController
{
	public override Vector3 GetEmployeePosition()
	{
		if (!IndoorCustomerSpawner.GetRandomPositionInside(out var randomPosition))
		{
			return base.GetEmployeePosition();
		}
		return randomPosition;
	}

	public override void Start()
	{
		employeeType = typeof(GymTrainerEmployee);
		base.Start();
	}

	public void OnFitnessPlanningBoardClick()
	{
		InstanceBehavior<GameManager>.Instance.playerController.SetGoal(this, ShowPersonalizedWorkoutPlanPrompt);
	}

	private void ShowPersonalizedWorkoutPlanPrompt()
	{
		LanguageChangeEventDataHolder bodyData = ((SaveGameManager.Current.currentWorkoutPlan == null) ? "gym_confirm_personalized_workout" : "gym_confirm_change_personalized_workout").Localize();
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			OnConfirmGymWorkoutPlan(this);
		});
	}

	private static void OnConfirmGymWorkoutPlan(FitnessPlanningBoardController fitnessPlanningBoardController)
	{
		if (fitnessPlanningBoardController.employee == null)
		{
			Notifications.ShowError("notification_no_gym_trainer_assigned");
			return;
		}
		List<WorkoutType> list = (from x in InstanceBehavior<BuildingManager>.Instance.allItemControllers.OfType<WorkoutMachineController>()
			select x.GetWorkoutExercise().workoutType).Distinct().ToList();
		if (list.Count < 3)
		{
			Notifications.ShowError("notification_gym_not_enough_machines");
			return;
		}
		SaveGameManager.Current.currentWorkoutPlan = new WorkoutPlan(list, SaveGameManager.Current.currentWorkoutPlan?.plan.Keys.ToList());
		InstanceBehavior<UIs>.Instance.tasksUI.workoutPlanUI.LoadCurrentWorkoutPlan();
	}

	public override void AssignEmployee(ThirdPersonCharacter tpc, EmployeeInstance employeeInstance)
	{
		base.AssignEmployee(tpc, employeeInstance);
		tpc.GetComponent<GymTrainerEmployee>().SetEmployeeStation(this);
	}

	public override EmployeeInstance GetAIEmployeeInstance()
	{
		return EmployeeHelper.CreateAIEmployeeInstance("ba:skill_gymtrainer");
	}
}
