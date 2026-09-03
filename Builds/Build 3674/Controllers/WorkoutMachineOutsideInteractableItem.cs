using Helpers;
using PlayerActivity;
using UI.Notification;
using UnityEngine;

namespace Controllers;

public class WorkoutMachineOutsideInteractableItem : OutsideInteractableItem, IWorkoutMachine
{
	[SerializeField]
	protected Animator animator;

	[SerializeField]
	private WorkoutExercise workoutExercise;

	[SerializeField]
	private Transform finishWorkoutPosition;

	private WorkoutAnimatorController _workoutAnimatorController;

	private int _lastMinutesInMachine;

	public override string GetCtaKey()
	{
		return "click_to_exercise";
	}

	public override string GetItemOccupiedKey()
	{
		return "machine";
	}

	public WorkoutExercise GetWorkoutExercise()
	{
		return workoutExercise;
	}

	public Animator GetAnimator()
	{
		return animator;
	}

	public HandObjectData GetHandObjectData()
	{
		return null;
	}

	public Transform GetCharacterPoint()
	{
		return interactPoint;
	}

	public Transform GetEndPoint()
	{
		if (!(finishWorkoutPosition != null))
		{
			return navMeshTargets[0];
		}
		return finishWorkoutPosition;
	}

	public override void PerformActivity()
	{
		if (CanUseMachine())
		{
			PlayerActivityUI.Show(this, this);
		}
	}

	private bool CanUseMachine()
	{
		if (Occupied)
		{
			ShowOccupiedNotification();
			return false;
		}
		if (!string.IsNullOrEmpty(SaveGameManager.Current.ActiveVehicleId) || PlayerHelper.ItemInstanceInHands != null)
		{
			Notifications.ShowError("workout_machine_cant_use_with_vehicle_or_item");
			return false;
		}
		if (requiredClothingTags.Length != 0 && !InstanceBehavior<GameManager>.Instance.playerController.Character.appearanceSetter.IsWearingClothesWithTags(requiredClothingTags))
		{
			Notifications.ShowError("workout_machine_gym_need_sport_clothes");
			return false;
		}
		return true;
	}

	public override IPlayerActivity CreateActivity(EntityController attachedEntity)
	{
		return WorkoutActivity.CreateWithMachine(workoutExercise, attachedEntity);
	}

	public void TogglePhysics(bool enable = true)
	{
	}

	public void PlayAudioClip(AudioClip audioClip, bool loop = false)
	{
	}

	public void StopAudio()
	{
	}
}
