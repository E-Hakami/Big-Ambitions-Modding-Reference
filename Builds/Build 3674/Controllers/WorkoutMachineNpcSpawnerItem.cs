using PlayerActivity;
using UnityEngine;

namespace Controllers;

public class WorkoutMachineNpcSpawnerItem : NpcSpawnerItem, IWorkoutMachine
{
	[SerializeField]
	protected Animator animator;

	[SerializeField]
	private WorkoutExercise workoutExercise;

	private WorkoutAnimatorController _workoutAnimatorController;

	private int _lastMinutesInMachine;

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
		return spawnPoint;
	}

	public Transform GetEndPoint()
	{
		return null;
	}

	public override void OnNpcSpawn(BaseHuman baseHuman)
	{
		base.OnNpcSpawn(baseHuman);
		if (_workoutAnimatorController == null)
		{
			_workoutAnimatorController = new WorkoutAnimatorController();
		}
		_workoutAnimatorController.InitAnimations(baseHuman, this);
		_workoutAnimatorController.StartWorkoutAnimation();
	}

	public override void OnNpcDespawn()
	{
		base.OnNpcDespawn();
		_workoutAnimatorController?.StopWorkoutAnimation();
	}

	public override void UpdateItem()
	{
		if (occupied && HasPassedAMinute())
		{
			_lastMinutesInMachine = (int)TimeHelper.NowInMinutes();
			_workoutAnimatorController?.UpdateAnimations();
		}
	}

	private bool HasPassedAMinute()
	{
		return TimeHelper.NowInMinutes() - (float)_lastMinutesInMachine >= 1f;
	}

	public void ShowOccupiedNotification()
	{
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
