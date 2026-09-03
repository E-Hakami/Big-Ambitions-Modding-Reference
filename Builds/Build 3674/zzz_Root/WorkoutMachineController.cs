using System.Collections.Generic;
using BigAmbitions.Characters.Appearance;
using Controllers;
using Helpers;
using PlayerActivity;
using UI.Notification;
using UnityEngine;

public class WorkoutMachineController : ItemController, IWorkoutMachine, IPlayerActivityType
{
	private static readonly AppearanceTag[] SportClothingTags = new AppearanceTag[1] { AppearanceTag.Sport };

	[SerializeField]
	private WorkoutExercise workoutExercise;

	[SerializeField]
	private Transform finishWorkoutPosition;

	[SerializeField]
	private AudioSource audioSource;

	public Transform characterPosition;

	public HandObjectData handObjectData;

	public Animator animator;

	public Transform EndOfWorkoutPoint
	{
		get
		{
			if (!(finishWorkoutPosition != null))
			{
				return navMeshTargets[0];
			}
			return finishWorkoutPosition;
		}
	}

	public WorkoutExercise GetWorkoutExercise()
	{
		return workoutExercise;
	}

	public HandObjectData GetHandObjectData()
	{
		return handObjectData;
	}

	public Animator GetAnimator()
	{
		return animator;
	}

	public override void Awake()
	{
		base.Awake();
		if (handObjectData.handObject != null && handObjectData.handObjectParent == null)
		{
			handObjectData.handObjectParent = handObjectData.handObject.parent;
		}
	}

	public void PerformActivity()
	{
		if (CanUseMachine())
		{
			PlayerActivityUI.Show(this, this);
		}
	}

	public Transform GetCharacterPoint()
	{
		if (!string.IsNullOrEmpty(base.ItemInstance.parentId))
		{
			return navMeshTargets[0];
		}
		return characterPosition;
	}

	public Transform GetEndPoint()
	{
		return EndOfWorkoutPoint;
	}

	public void PlayAudioClip(AudioClip audioClip, bool loop = false)
	{
		if (audioSource == null)
		{
			Debug.LogError(itemName + " doesn't have an AudioSource component");
			return;
		}
		audioSource.outputAudioMixerGroup = ((BuildingManager.IsInsideBuilding && !InstanceBehavior<BuildingManager>.Instance.building.IsHamptonsHouse()) ? InstanceBehavior<GlobalReferences>.Instance.indoorMixerGroup : InstanceBehavior<GlobalReferences>.Instance.foleyMixerGroup);
		audioSource.clip = audioClip;
		audioSource.loop = loop;
		audioSource.Play();
	}

	public void StopAudio()
	{
		if (audioSource != null)
		{
			audioSource.Stop();
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
		if (base.ItemInstance != null && ItemHelper.HasAnyMissingRequirements(base.ItemInstance))
		{
			Notifications.ShowError("workout_machine_requirements_not_met");
			return false;
		}
		if (!base.BuildingContext.IsPlayerOwnedBusiness && !InstanceBehavior<GameManager>.Instance.playerController.Character.appearanceSetter.IsWearingClothesWithTags(SportClothingTags))
		{
			Notifications.ShowError("workout_machine_gym_need_sport_clothes");
			return false;
		}
		return true;
	}

	public void ShowOccupiedNotification()
	{
		Dictionary<string, string> notificationData = new Dictionary<string, string> { { "itemname", itemName } };
		Notifications.Show(NotificationType.Error, "notification_this_item_is_occupied", notificationData);
	}

	public IPlayerActivity CreateActivity(EntityController attachedEntity)
	{
		return WorkoutActivity.CreateWithMachine(GetWorkoutExercise(), attachedEntity);
	}
}
