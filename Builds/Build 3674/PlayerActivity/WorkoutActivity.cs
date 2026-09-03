using Controllers;
using Helpers;
using JimmysUnityUtilities;
using UI.Elements;
using UnityEngine;
using UnityEngine.Playables;

namespace PlayerActivity;

public class WorkoutActivity : IPlayerActivity
{
	private readonly WorkoutExercise _workoutExercise;

	private readonly EntityController _attachedEntity;

	private readonly IWorkoutMachine _workoutMachine;

	private readonly WorkoutMachineOutsideInteractableItemWithTimeline _timelineWorkoutMachine;

	private readonly PlayableDirector _playableDirector;

	private readonly WorkoutAnimatorController _workoutAnimatorController = new WorkoutAnimatorController();

	private readonly int _happinessBoostPercentage;

	private readonly bool _isUsingTimeline;

	private PlayerActivityState _state;

	private PlayerActivityState _stateBeforeFinishing;

	private int _minutesToWorkOut;

	private int _minutesWorkedOut;

	private int _hoursOfHappinessBoost;

	private ButtonInfo CancelButton => new ButtonInfo("CancelWorkout", "common_cancel", "gray", CancelWorkingOut, PlayerAction.Cancel);

	private ButtonInfo StartButton => new ButtonInfo("StartWorkout", "workoutui_start", "blue", StartWorkingOut, PlayerAction.Confirm);

	private ButtonInfo StopButton => new ButtonInfo("StopWorkout", "workoutui_stop", "gray", Finish, PlayerAction.Cancel);

	public bool RequiresEnergy()
	{
		return true;
	}

	private WorkoutActivity(WorkoutExercise workoutExercise, EntityController attachedEntity, IWorkoutMachine workoutMachine, WorkoutMachineOutsideInteractableItemWithTimeline timelineWorkoutMachine = null, PlayableDirector playableDirector = null, string humanTrackAssetName = null, bool isUsingTimeline = false)
	{
		_workoutExercise = workoutExercise;
		_attachedEntity = attachedEntity;
		_state = PlayerActivityState.NotStarted;
		_happinessBoostPercentage = workoutExercise.balanceConfig.BoostPercent;
		SetTimeToWorkOut(workoutExercise.GetDefaultMinutes());
		if (isUsingTimeline)
		{
			_timelineWorkoutMachine = timelineWorkoutMachine;
			_playableDirector = playableDirector;
			_isUsingTimeline = true;
			playableDirector.SetBindingOnTimelineFromTrackAssetName(InstanceBehavior<GameManager>.Instance.playerController.Character.animator, humanTrackAssetName);
		}
		else
		{
			_workoutMachine = workoutMachine;
			_workoutAnimatorController.InitAnimations(InstanceBehavior<GameManager>.Instance.playerController.Character, workoutMachine);
		}
	}

	public static WorkoutActivity CreateWithMachine(WorkoutExercise workoutExercise, EntityController attachedEntity)
	{
		if (attachedEntity is IWorkoutMachine workoutMachine)
		{
			return new WorkoutActivity(workoutExercise, attachedEntity, workoutMachine);
		}
		Debug.LogError("You are trying to do a work out exercise on an item that is not implementing IWorkoutMachine interface " + attachedEntity.gameObject.name);
		return null;
	}

	public static WorkoutActivity CreateWithMachineAndTimeline(WorkoutExercise workoutExercise, EntityController attachedEntity, PlayableDirector playableDirector, string humanTrackAssetName)
	{
		if (attachedEntity is WorkoutMachineOutsideInteractableItemWithTimeline timelineWorkoutMachine)
		{
			return new WorkoutActivity(workoutExercise, attachedEntity, null, timelineWorkoutMachine, playableDirector, humanTrackAssetName, isUsingTimeline: true);
		}
		Debug.LogError("You are trying to do a work out exercise on an item that isn't a OutsideInteractableItemWithTimeline " + attachedEntity.gameObject.name);
		return null;
	}

	public PlayerActivityState GetState()
	{
		return _state;
	}

	public PlayerActivityState GetStateBeforeFinishing()
	{
		return _stateBeforeFinishing;
	}

	public void ChangeState(PlayerActivityState state)
	{
		_state = state;
	}

	private void StartWorkingOut()
	{
		_state = PlayerActivityState.MovingTowardsActivity;
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if (_state == PlayerActivityState.MovingTowardsActivity)
			{
				if (!InstanceBehavior<GameManager>.Instance.playerController.ExistsRoute(_attachedEntity, showErrorNotification: true))
				{
					CancelWorkingOut();
				}
				else
				{
					InstanceBehavior<GameManager>.Instance.playerController.SetGoal(_attachedEntity, delegate
					{
						if (_state == PlayerActivityState.MovingTowardsActivity)
						{
							if (_attachedEntity.Occupied)
							{
								if (_isUsingTimeline)
								{
									_timelineWorkoutMachine.ShowOccupiedNotification();
								}
								else
								{
									_workoutMachine.ShowOccupiedNotification();
								}
								CancelWorkingOut();
							}
							else
							{
								_state = PlayerActivityState.Started;
								ThirdPersonCharacter character = InstanceBehavior<GameManager>.Instance.playerController.Character;
								EnergyHelper.AddEnergySpender("Workout", _workoutExercise.energyConsumptionPerMinute);
								HappinessHelper.EnableTemporalHappinessBoost(_workoutExercise.balanceConfig.TemporalType, _workoutExercise.balanceConfig.FinalType, character);
								SaveGameManager.Current.timeEnteredTemporalBoost = TimeHelper.Now();
								SaveGameManager.Current.currentActivityHappinessPerHour = _workoutExercise.balanceConfig.BoostHoursPerHour;
								InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.WorkoutActivity);
								_attachedEntity.Occupied = true;
								if (_isUsingTimeline)
								{
									character.ForceToTransform(_timelineWorkoutMachine.interactPoint);
									Animator component = _playableDirector.GetComponent<Animator>();
									if ((bool)component)
									{
										component.enabled = true;
									}
									_playableDirector.Play();
								}
								else
								{
									character.ForceToTransform(_workoutMachine.GetCharacterPoint());
									character.SetItemIKTargets(_attachedEntity as ItemController);
									_workoutAnimatorController.StartWorkoutAnimation();
								}
							}
						}
					});
				}
			}
		});
	}

	public void Perform(int minutes)
	{
		_minutesWorkedOut += minutes;
		if (SaveGameManager.Current.currentWorkoutPlan != null)
		{
			SaveGameManager.Current.currentWorkoutPlan.UpdateWorkoutPlan(_workoutExercise.workoutType, minutes, _workoutExercise.workoutPlanCompletionBalanceConfig);
		}
		if (_minutesWorkedOut >= _minutesToWorkOut)
		{
			Finish();
		}
		else if (!_isUsingTimeline)
		{
			_workoutAnimatorController.UpdateAnimations();
		}
	}

	public void Finish()
	{
		ThirdPersonCharacter character = InstanceBehavior<GameManager>.Instance.playerController.Character;
		if (_state == PlayerActivityState.MovingTowardsActivity || _isUsingTimeline)
		{
			InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
		}
		else
		{
			Transform endPoint = _workoutMachine.GetEndPoint();
			character.navmeshAgent.Warp(endPoint.position);
			character.ForceToRotation(endPoint.rotation);
		}
		EnergyHelper.RemoveEnergySpender("Workout");
		HappinessHelper.DisableTemporalHappinessBoost(_workoutExercise.balanceConfig.TemporalType, _workoutExercise.balanceConfig.FinalType, character);
		if (_isUsingTimeline)
		{
			_playableDirector.time = 0.0;
			_playableDirector.Evaluate();
			_playableDirector.Stop();
			Animator component = _playableDirector.GetComponent<Animator>();
			if ((bool)component)
			{
				component.enabled = false;
			}
		}
		else
		{
			_workoutAnimatorController.StopWorkoutAnimation();
			character.SetItemIKTargets(null);
		}
		_attachedEntity.Occupied = false;
		character.Reset();
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.WorkoutActivity);
		CancelWorkingOut();
	}

	private void CancelWorkingOut()
	{
		_stateBeforeFinishing = _state;
		_state = PlayerActivityState.Finished;
		SaveGameManager.Current.timeEnteredTemporalBoost = TimeHelper.Now();
		_minutesWorkedOut = 0;
	}

	private void SetTimeToWorkOut(int timeToWorkOut)
	{
		_minutesToWorkOut = Mathf.FloorToInt((float)timeToWorkOut * 60f) / 60;
		_hoursOfHappinessBoost = GetHoursOfHappinessBoost(_minutesToWorkOut);
		_workoutExercise.SetDefaultMinutes(_minutesToWorkOut);
	}

	private int GetHoursOfHappinessBoost(int minutes)
	{
		return _workoutExercise.balanceConfig.GetBoostHours(minutes);
	}

	public LabelInfo GetHeadlineLabel()
	{
		return new LabelInfo(_workoutExercise.workoutType.GetLocalizeKey(), InstanceBehavior<GlobalReferences>.Instance.colors.white);
	}

	public LabelInfo[] GetLabels()
	{
		return null;
	}

	public ButtonInfo[] GetButtons()
	{
		return _state switch
		{
			PlayerActivityState.NotStarted => new ButtonInfo[2] { CancelButton, StartButton }, 
			PlayerActivityState.Running => new ButtonInfo[1] { StopButton }, 
			_ => null, 
		};
	}

	public bool HasTimeMachine()
	{
		return _state == PlayerActivityState.Running;
	}

	public int GetRemainingMinutesForTimeMachine()
	{
		return _minutesToWorkOut - _minutesWorkedOut;
	}

	public bool HasFastForward()
	{
		return _state == PlayerActivityState.Running;
	}

	public bool HasProgressBar()
	{
		return _state == PlayerActivityState.Running;
	}

	public float GetProgressBarPercentageValue()
	{
		return 100f * (float)_minutesWorkedOut / (float)_minutesToWorkOut;
	}

	public (string, object) GetProgressBarLabel()
	{
		int num = _minutesToWorkOut - _minutesWorkedOut;
		int num2 = Mathf.FloorToInt((float)num / 60f);
		int minutes = num - num2 * 60;
		object item = new
		{
			hours = num2,
			minutes = minutes
		};
		return ("playeractivityui_time_remaining", item);
	}

	public bool HasSlider()
	{
		return _state == PlayerActivityState.NotStarted;
	}

	public int GetMinSliderValue()
	{
		return 5;
	}

	public int GetMaxSliderValue()
	{
		return _workoutExercise.balanceConfig.MaxDurationMinutes;
	}

	public float GetCurrentSliderValue()
	{
		return _minutesToWorkOut;
	}

	public void OnSliderValueChanged(int value)
	{
		SetTimeToWorkOut(value);
	}

	public (string, object) GetSliderInfo()
	{
		int num = Mathf.FloorToInt((float)_minutesToWorkOut / 60f);
		int workoutMinutes = _minutesToWorkOut - num * 60;
		object item = new
		{
			workoutHours = num,
			workoutMinutes = workoutMinutes,
			boostPercentage = _happinessBoostPercentage,
			boostHours = _hoursOfHappinessBoost
		};
		return ("workoutui_slider_label", item);
	}
}
