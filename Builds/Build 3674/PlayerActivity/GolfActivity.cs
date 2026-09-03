using AI;
using Helpers;
using Items.SpecialItems;
using JimmysUnityUtilities;
using UI.Elements;
using UnityEngine;

namespace PlayerActivity;

public class GolfActivity : IPlayerActivity
{
	private const float EnergyConsumptionPerMinute = 0.2f;

	private static RuntimeAnimatorController InitialAnimatorController;

	private readonly GolfPlatformController _golfPlatformController;

	private readonly int _happinessBoostPercentage;

	private PlayerActivityState _state;

	private PlayerActivityState _stateBeforeFinishing;

	private int _minutesToPlay;

	private int _minutesDone;

	private int _hoursOfHappinessBoost;

	private ButtonInfo CancelButton => new ButtonInfo("Cancel", "common_cancel", "gray", CancelActivity, PlayerAction.Cancel);

	private ButtonInfo StartButton => new ButtonInfo("Start", "ba:golfui_start", "blue", StartPlaying, PlayerAction.Confirm);

	private ButtonInfo StopButton => new ButtonInfo("Stop", "ba:golfui_stop", "gray", Finish, PlayerAction.Cancel);

	public bool RequiresEnergy()
	{
		return true;
	}

	public GolfActivity(EntityController attachedEntity)
	{
		_state = PlayerActivityState.NotStarted;
		_golfPlatformController = attachedEntity as GolfPlatformController;
		_happinessBoostPercentage = _golfPlatformController.balanceConfig.BoostPercent;
		SetDurationMinutes(GetDefaultMinutes());
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

	private void StartPlaying()
	{
		_state = PlayerActivityState.MovingTowardsActivity;
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if (_state == PlayerActivityState.MovingTowardsActivity)
			{
				InstanceBehavior<GameManager>.Instance.playerController.SetGoal(_golfPlatformController, delegate
				{
					if (_state == PlayerActivityState.MovingTowardsActivity)
					{
						_state = PlayerActivityState.Started;
						TurnPlayerIntoGolfer();
						EnableHappinessBoost();
						EnergyHelper.RemoveEnergySpender("move");
						EnergyHelper.AddEnergySpender("golfing", 0.2f);
					}
				});
			}
		});
	}

	private void EnableHappinessBoost()
	{
		_golfPlatformController.balanceConfig.EnableTemporalBoost();
	}

	private void TurnPlayerIntoGolfer()
	{
		PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
		playerController.Character.navmeshAgent.Warp(_golfPlatformController.standingPoint.position);
		playerController.Character.transform.forward = _golfPlatformController.standingPoint.forward;
		InitialAnimatorController = playerController.Character.animator.runtimeAnimatorController;
		playerController.Character.animator.runtimeAnimatorController = _golfPlatformController.GolferAnimatorController;
		playerController.SetNavigationBlocker(NavigationBlocker.PlayingGolf);
		GolferNpc golferNpc = playerController.AddComponent<GolferNpc>();
		golferNpc.pool = _golfPlatformController.npcPool;
		golferNpc.animator = playerController.Character.animator;
	}

	public void Perform(int minutes)
	{
		_minutesDone += minutes;
		if (_minutesDone >= _minutesToPlay)
		{
			Finish();
		}
	}

	public void Finish()
	{
		ThirdPersonCharacter character = InstanceBehavior<GameManager>.Instance.playerController.Character;
		ResetPlayer(character);
		EnergyHelper.RemoveEnergySpender("golfing");
		DisableHappinessBoost(character);
		CancelActivity();
	}

	private void DisableHappinessBoost(ThirdPersonCharacter tpc)
	{
		_golfPlatformController.balanceConfig.DisableTemporalBoost(tpc);
	}

	private void ResetPlayer(ThirdPersonCharacter tpc)
	{
		if (_state == PlayerActivityState.MovingTowardsActivity)
		{
			InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
		}
		tpc.Reset();
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.PlayingGolf);
		tpc.animator.runtimeAnimatorController = InitialAnimatorController;
		GolferNpc component = tpc.GetComponent<GolferNpc>();
		if ((bool)component)
		{
			Object.Destroy(component);
		}
	}

	private void CancelActivity()
	{
		_stateBeforeFinishing = _state;
		_state = PlayerActivityState.Finished;
		SaveGameManager.Current.timeEnteredTemporalBoost = TimeHelper.Now();
		_minutesDone = 0;
	}

	private void SetDurationMinutes(int minutes)
	{
		_minutesToPlay = Mathf.FloorToInt((float)minutes * 60f) / 60;
		_hoursOfHappinessBoost = GetHoursOfHappinessBoost(_minutesToPlay);
		SaveGameManager.Current.PlayerDefaults.golfMinutes = _minutesToPlay;
	}

	private int GetDefaultMinutes()
	{
		int golfMinutes = SaveGameManager.Current.PlayerDefaults.golfMinutes;
		return _golfPlatformController.balanceConfig.GetDefaultMinutes(golfMinutes);
	}

	private int GetHoursOfHappinessBoost(int minutes)
	{
		return _golfPlatformController.balanceConfig.GetBoostHours(minutes);
	}

	public LabelInfo GetHeadlineLabel()
	{
		return new LabelInfo("ba:golfui_headline", InstanceBehavior<GlobalReferences>.Instance.colors.white);
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
		return _minutesToPlay - _minutesDone;
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
		return 100f * (float)_minutesDone / (float)_minutesToPlay;
	}

	public (string, object) GetProgressBarLabel()
	{
		int num = _minutesToPlay - _minutesDone;
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
		return _golfPlatformController.balanceConfig.MaxDurationMinutes;
	}

	public float GetCurrentSliderValue()
	{
		return _minutesToPlay;
	}

	public void OnSliderValueChanged(int value)
	{
		SetDurationMinutes(value);
	}

	public (string, object) GetSliderInfo()
	{
		int num = Mathf.FloorToInt((float)_minutesToPlay / 60f);
		int minutes = _minutesToPlay - num * 60;
		object item = new
		{
			hours = num,
			minutes = minutes,
			boostPercentage = _happinessBoostPercentage,
			boostHours = _hoursOfHappinessBoost
		};
		return ("ba:golfui_slider_label", item);
	}
}
