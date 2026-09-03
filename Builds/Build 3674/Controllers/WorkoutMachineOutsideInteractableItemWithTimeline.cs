using PlayerActivity;
using UI.Notification;
using UnityEngine;
using UnityEngine.Playables;

namespace Controllers;

public class WorkoutMachineOutsideInteractableItemWithTimeline : OutsideInteractableItem
{
	[SerializeField]
	private PlayableDirector playableDirector;

	[SerializeField]
	[Tooltip("Name of the track in the timeline that binds to the human animator.")]
	private string humanTrackAssetName;

	[SerializeField]
	private WorkoutExercise workoutExercise;

	public override string GetCtaKey()
	{
		return "click_to_exercise";
	}

	public override string GetItemOccupiedKey()
	{
		return "machine";
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
		if (requiredClothingTags.Length != 0 && !InstanceBehavior<GameManager>.Instance.playerController.Character.appearanceSetter.IsWearingClothesWithTags(requiredClothingTags))
		{
			Notifications.ShowError("workout_machine_gym_need_sport_clothes");
			return false;
		}
		return true;
	}

	public override IPlayerActivity CreateActivity(EntityController attachedEntity)
	{
		return WorkoutActivity.CreateWithMachineAndTimeline(workoutExercise, attachedEntity, playableDirector, humanTrackAssetName);
	}
}
