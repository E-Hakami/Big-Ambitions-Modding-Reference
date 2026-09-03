using BigAmbitions.Characters;
using Controllers;
using Extensions;
using JimmysUnityUtilities;
using UnityEngine;

namespace PlayerActivity;

public class WorkoutAnimatorController
{
	public bool hasSeriesAnimations;

	public bool hasRandomAnimations;

	private int _nextSeriesAnimation;

	private int _remainingActionsInCurrentSeries;

	private int _seriesBreakTime;

	private int _nextRandomAnimation;

	private BaseHuman _baseHuman;

	private IWorkoutMachine _workoutMachine;

	private Transform _workoutMachineModel;

	private Vector3 _machineInitialPosition;

	private Quaternion _machineInitialRotation;

	private bool _isUsingMachine;

	public void InitAnimations(BaseHuman baseHuman, IWorkoutMachine workoutMachine)
	{
		_isUsingMachine = false;
		_workoutMachineModel = null;
		_baseHuman = baseHuman;
		_workoutMachine = workoutMachine;
		WorkoutExercise workoutExercise = workoutMachine.GetWorkoutExercise();
		hasSeriesAnimations = workoutExercise.seriesAnimations.Length != 0;
		if (hasSeriesAnimations)
		{
			_nextSeriesAnimation = Random.Range(workoutExercise.maxMinutesBetweenSeriesAnimations, workoutExercise.minMinutesBetweenSeriesAnimations);
			_remainingActionsInCurrentSeries = workoutExercise.amountPerSeries;
		}
		hasRandomAnimations = workoutExercise.randomAnimations.Length != 0;
		if (hasRandomAnimations)
		{
			_nextRandomAnimation = Random.Range(workoutExercise.minMinutesBetweenRandomAnimations, workoutExercise.maxMinutesBetweenRandomAnimations);
		}
	}

	public void StartWorkoutAnimation()
	{
		_isUsingMachine = true;
		_baseHuman.animator.SetBool(_workoutMachine.GetWorkoutExercise().animation);
		_baseHuman.animator.SetFloat(AppearanceSetter.FatId, 0f);
		if (_workoutMachine.GetAnimator() != null)
		{
			_workoutMachine.GetAnimator().SetBool(_workoutMachine.GetWorkoutExercise().animationOnTheWorkoutMachineBoolName, value: true);
		}
		if (_workoutMachine.GetHandObjectData()?.handObject != null)
		{
			if (_workoutMachine.GetHandObjectData().secondsUntilGrabbingObject > 0f)
			{
				CoroutineUtility.RunAfterSecondsDelay(AddMachineToHand, _workoutMachine.GetHandObjectData().secondsUntilGrabbingObject);
			}
			else
			{
				AddMachineToHand();
			}
		}
		if (_workoutMachine.GetWorkoutExercise().permanentAudioClip != null)
		{
			_workoutMachine.PlayAudioClip(_workoutMachine.GetWorkoutExercise().permanentAudioClip, loop: true);
		}
	}

	private void AddMachineToHand()
	{
		if (_isUsingMachine)
		{
			_workoutMachineModel = _workoutMachine.GetHandObjectData().handObject;
			_machineInitialPosition = _workoutMachineModel.position;
			_machineInitialRotation = _workoutMachineModel.rotation;
			if (_workoutMachine.GetHandObjectData().disablePhysicsOnHandObject)
			{
				_workoutMachine.TogglePhysics(enable: false);
			}
			_workoutMachineModel.SetParent(_baseHuman.rightHand);
			_workoutMachineModel.localPosition = _workoutMachine.GetHandObjectData().handObjectPosition;
			_workoutMachineModel.localEulerAngles = _workoutMachine.GetHandObjectData().handObjectRotation;
		}
	}

	private void RemoveMachineFromHand()
	{
		if (!(_workoutMachineModel == null))
		{
			_workoutMachineModel.SetParent(_workoutMachine.GetHandObjectData().handObjectParent);
			_workoutMachineModel.position = _machineInitialPosition;
			_workoutMachineModel.rotation = _machineInitialRotation;
			if (_workoutMachine.GetHandObjectData().disablePhysicsOnHandObject)
			{
				_workoutMachine.TogglePhysics();
			}
		}
	}

	public void StopWorkoutAnimation()
	{
		_isUsingMachine = false;
		_baseHuman.animator.SetBool(_workoutMachine.GetWorkoutExercise().animation, state: false);
		_baseHuman.appearanceSetter.SetBody();
		if (_workoutMachine.GetAnimator() != null)
		{
			_workoutMachine.GetAnimator().SetBool(_workoutMachine.GetWorkoutExercise().animationOnTheWorkoutMachineBoolName, value: false);
		}
		if (_workoutMachine.GetHandObjectData()?.handObject != null)
		{
			RemoveMachineFromHand();
		}
		_workoutMachine.StopAudio();
		if (_workoutMachine.GetWorkoutExercise().finishAudioClip != null)
		{
			_workoutMachine.PlayAudioClip(_workoutMachine.GetWorkoutExercise().finishAudioClip);
		}
	}

	public void UpdateAnimations()
	{
		if (hasSeriesAnimations)
		{
			UpdateWorkoutAnimations();
		}
		if (hasRandomAnimations)
		{
			UpdateRandomAnimations();
		}
	}

	private void UpdateWorkoutAnimations()
	{
		_nextSeriesAnimation--;
		if (_nextSeriesAnimation <= 0)
		{
			WorkoutExercise workoutExercise = _workoutMachine.GetWorkoutExercise();
			_remainingActionsInCurrentSeries--;
			if (_remainingActionsInCurrentSeries == 0)
			{
				_remainingActionsInCurrentSeries = workoutExercise.amountPerSeries;
				_nextSeriesAnimation = Random.Range(workoutExercise.minMinutesBreakBetweenSeries, workoutExercise.maxMinutesBreakBetweenSeries);
			}
			else
			{
				_nextSeriesAnimation = Random.Range(workoutExercise.minMinutesBetweenSeriesAnimations, workoutExercise.maxMinutesBetweenSeriesAnimations);
			}
			AnimationType random = workoutExercise.seriesAnimations.GetRandom();
			_baseHuman.animator.SetTrigger(random);
			if (workoutExercise.oneTimeAudioClips.Length != 0)
			{
				_workoutMachine.PlayAudioClip(workoutExercise.oneTimeAudioClips.GetRandom());
			}
		}
	}

	private void UpdateRandomAnimations()
	{
		_nextRandomAnimation--;
		if (_nextRandomAnimation <= 0)
		{
			WorkoutExercise workoutExercise = _workoutMachine.GetWorkoutExercise();
			_nextRandomAnimation = Random.Range(workoutExercise.minMinutesBetweenRandomAnimations, workoutExercise.maxMinutesBetweenRandomAnimations);
			AnimationType random = workoutExercise.randomAnimations.GetRandom();
			_baseHuman.animator.SetTrigger(random);
			if (workoutExercise.oneTimeAudioClips.Length != 0)
			{
				_workoutMachine.PlayAudioClip(workoutExercise.oneTimeAudioClips.GetRandom());
			}
		}
	}
}
