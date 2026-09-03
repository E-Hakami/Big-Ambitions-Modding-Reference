using System;
using System.Collections.Generic;
using BigAmbitions.Characters.Appearance;
using Helpers;
using JimmysUnityUtilities;
using UI.Elements;
using UnityEngine;

namespace PlayerActivity;

public class SwimmingActivity : IPlayerActivity
{
	private const float EnergyConsumptionPerMinute = 0.21f;

	private static readonly int Swimming = Animator.StringToHash("Swimming");

	public Action onActivityStarted;

	public Action onActivityFinished;

	private readonly SwimmingPoolController _swimmingPoolController;

	private readonly int _happinessBoostPercentage;

	private PlayerActivityState _state;

	private PlayerActivityState _stateBeforeFinishing;

	private int _minutesToSwim;

	private int _minutesSwum;

	private int _hoursOfHappinessBoost;

	private ButtonInfo CancelButton => new ButtonInfo("CancelSwimming", "common_cancel", "gray", CancelSwimming, PlayerAction.Cancel);

	private ButtonInfo StartButton => new ButtonInfo("StartSwimming", "swimmingui_start", "blue", StartSwimming, PlayerAction.Confirm);

	private ButtonInfo StopButton => new ButtonInfo("StopSwimming", "swimmingui_stop", "gray", Finish, PlayerAction.Cancel);

	public bool RequiresEnergy()
	{
		return true;
	}

	public SwimmingActivity(EntityController attachedEntity)
	{
		_state = PlayerActivityState.NotStarted;
		_swimmingPoolController = attachedEntity as SwimmingPoolController;
		_happinessBoostPercentage = _swimmingPoolController.BalanceConfig.BoostPercent;
		SetTimeToSwim(GetDefaultMinutes());
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

	private void StartSwimming()
	{
		_state = PlayerActivityState.MovingTowardsActivity;
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if (_state == PlayerActivityState.MovingTowardsActivity)
			{
				InstanceBehavior<GameManager>.Instance.playerController.SetGoal(_swimmingPoolController, delegate
				{
					if (_state == PlayerActivityState.MovingTowardsActivity)
					{
						_state = PlayerActivityState.Started;
						TurnPlayerIntoASwimmingPedestrian();
						EnableHappinessBoost();
						EnergyHelper.RemoveEnergySpender("move");
						EnergyHelper.AddEnergySpender("swimming", 0.21f);
						onActivityStarted?.Invoke();
					}
				});
			}
		});
	}

	private void EnableHappinessBoost()
	{
		HappinessHelper.EnableTemporalHappinessBoost(_swimmingPoolController.BalanceConfig.TemporalType, _swimmingPoolController.BalanceConfig.FinalType);
		SaveGameManager.Current.timeEnteredTemporalBoost = TimeHelper.Now();
		SaveGameManager.Current.currentActivityHappinessPerHour = _swimmingPoolController.BalanceConfig.BoostHoursPerHour;
	}

	private void TurnPlayerIntoASwimmingPedestrian()
	{
		PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
		Vector3 positionInWaterFromGroundPosition = _swimmingPoolController.GetPositionInWaterFromGroundPosition(playerController.transform.position);
		WaterPedestrian waterPedestrian = playerController.AddComponent<WaterPedestrian>();
		List<AppearanceElementData> swimwearElements = playerController.Character.appearanceSetter.GetSwimwearElements();
		playerController.Character.appearanceSetter.UpdateVisuals(swimwearElements);
		waterPedestrian.tpc = playerController.Character;
		waterPedestrian.tpc.navmeshAgent.Warp(positionInWaterFromGroundPosition);
		waterPedestrian.tpc.animator.SetBool(Swimming, value: true);
		waterPedestrian.Init();
		playerController.SetNavigationBlocker(NavigationBlocker.SwimmingActivity);
	}

	public void Perform(int minutes)
	{
		_minutesSwum += minutes;
		if (_minutesSwum >= _minutesToSwim)
		{
			Finish();
		}
	}

	public void Finish()
	{
		ThirdPersonCharacter character = InstanceBehavior<GameManager>.Instance.playerController.Character;
		ResetPlayer(character);
		EnergyHelper.RemoveEnergySpender("swimming");
		DisableHappinessBoost(character);
		CancelSwimming();
	}

	private void DisableHappinessBoost(ThirdPersonCharacter tpc)
	{
		HappinessHelper.DisableTemporalHappinessBoost(_swimmingPoolController.BalanceConfig.TemporalType, _swimmingPoolController.BalanceConfig.FinalType, tpc);
	}

	private void ResetPlayer(ThirdPersonCharacter tpc)
	{
		if (_state == PlayerActivityState.MovingTowardsActivity)
		{
			InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
		}
		else
		{
			tpc.navmeshAgent.Warp(_swimmingPoolController.GetClosestNavMeshTargetPositionStraightLine(tpc.transform.position));
		}
		tpc.Reset();
		tpc.ToggleRunning(running: true, force: true);
		tpc.appearanceSetter.SetAppearance(PlayerHelper.CharacterData);
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.SwimmingActivity);
		InstanceBehavior<GameManager>.Instance.playerController.RemoveComponent<WaterPedestrian>();
		tpc.animator.SetBool(Swimming, value: false);
	}

	private void CancelSwimming()
	{
		_stateBeforeFinishing = _state;
		_state = PlayerActivityState.Finished;
		SaveGameManager.Current.timeEnteredTemporalBoost = TimeHelper.Now();
		_minutesSwum = 0;
		onActivityFinished?.Invoke();
	}

	private void SetTimeToSwim(int timeToSwim)
	{
		_minutesToSwim = Mathf.FloorToInt((float)timeToSwim * 60f) / 60;
		_hoursOfHappinessBoost = GetHoursOfHappinessBoost(_minutesToSwim);
		SaveGameManager.Current.PlayerDefaults.swimmingMinutes = _minutesToSwim;
	}

	private int GetDefaultMinutes()
	{
		int swimmingMinutes = SaveGameManager.Current.PlayerDefaults.swimmingMinutes;
		return _swimmingPoolController.BalanceConfig.GetDefaultMinutes(swimmingMinutes);
	}

	private int GetHoursOfHappinessBoost(int minutes)
	{
		return _swimmingPoolController.BalanceConfig.GetBoostHours(minutes);
	}

	public LabelInfo GetHeadlineLabel()
	{
		return new LabelInfo("swimmingui_headline", InstanceBehavior<GlobalReferences>.Instance.colors.white);
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
		return _minutesToSwim - _minutesSwum;
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
		return 100f * (float)_minutesSwum / (float)_minutesToSwim;
	}

	public (string, object) GetProgressBarLabel()
	{
		int num = _minutesToSwim - _minutesSwum;
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
		return _swimmingPoolController.BalanceConfig.MaxDurationMinutes;
	}

	public float GetCurrentSliderValue()
	{
		return _minutesToSwim;
	}

	public void OnSliderValueChanged(int value)
	{
		SetTimeToSwim(value);
	}

	public (string, object) GetSliderInfo()
	{
		int num = Mathf.FloorToInt((float)_minutesToSwim / 60f);
		int swimMinutes = _minutesToSwim - num * 60;
		object item = new
		{
			swimHours = num,
			swimMinutes = swimMinutes,
			boostPercentage = _happinessBoostPercentage,
			boostHours = _hoursOfHappinessBoost
		};
		return ("swimmingui_slider_label", item);
	}
}
