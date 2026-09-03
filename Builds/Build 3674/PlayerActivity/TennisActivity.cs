using Helpers;
using JimmysUnityUtilities;
using PlayerActivity.Tennis;
using UI.Elements;
using UnityEngine;

namespace PlayerActivity;

public class TennisActivity : IPlayerActivity
{
	private const float EnergyConsumptionPerMinute = 0.2f;

	private readonly TennisInteractionNpc _tennisInteractionNpc;

	private readonly PlayerActivityBalanceConfig _balanceConfig;

	private readonly int _happinessBoostPercentage;

	private PlayerActivityState _state;

	private PlayerActivityState _stateBeforeFinishing;

	private int _minutesToPlay;

	private int _minutesDone;

	private int _hoursOfHappinessBoost;

	private Vector3 _initialPlayerPosition;

	private ButtonInfo CancelButton => new ButtonInfo("Cancel", "common_cancel", "gray", CancelActivity, PlayerAction.Cancel);

	private ButtonInfo StartButton => new ButtonInfo("Start", "ba:tennisui_start", "blue", StartPlaying, PlayerAction.Confirm);

	private ButtonInfo StopButton => new ButtonInfo("Stop", "ba:tennisui_stop", "gray", Finish, PlayerAction.Cancel);

	public bool RequiresEnergy()
	{
		return true;
	}

	public TennisActivity(TennisInteractionNpc attachedEntity)
	{
		_state = PlayerActivityState.NotStarted;
		_tennisInteractionNpc = attachedEntity;
		_balanceConfig = _tennisInteractionNpc.court.BalanceConfig;
		_happinessBoostPercentage = _balanceConfig.BoostPercent;
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
				_state = PlayerActivityState.Started;
				PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
				_initialPlayerPosition = playerController.transform.position;
				playerController.Character.navmeshAgent.enabled = false;
				playerController.transform.position = _tennisInteractionNpc.court.GetSide(0).GetAimableAreaCenter();
				_tennisInteractionNpc.court.StartGame(_tennisInteractionNpc, automated: true);
				EnableHappinessBoost();
				EnergyHelper.RemoveEnergySpender("move");
				EnergyHelper.AddEnergySpender("tennis", 0.2f);
			}
		});
	}

	private void EnableHappinessBoost()
	{
		_balanceConfig.EnableTemporalBoost();
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
		PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
		playerController.transform.position = _initialPlayerPosition;
		playerController.Character.navmeshAgent.enabled = true;
		_tennisInteractionNpc.court.Finish();
		EnergyHelper.RemoveEnergySpender("tennis");
		DisableHappinessBoost(playerController.Character);
		CancelActivity();
	}

	private void DisableHappinessBoost(ThirdPersonCharacter tpc)
	{
		_balanceConfig.DisableTemporalBoost(tpc);
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
		_minutesToPlay = minutes;
		_hoursOfHappinessBoost = GetHoursOfHappinessBoost(_minutesToPlay);
		SaveGameManager.Current.PlayerDefaults.tennisMinutes = _minutesToPlay;
	}

	private int GetDefaultMinutes()
	{
		int tennisMinutes = SaveGameManager.Current.PlayerDefaults.tennisMinutes;
		return _balanceConfig.GetDefaultMinutes(tennisMinutes);
	}

	private int GetHoursOfHappinessBoost(int minutes)
	{
		return _balanceConfig.GetBoostHours(minutes);
	}

	public LabelInfo GetHeadlineLabel()
	{
		return new LabelInfo("ba:tennisui_headline", InstanceBehavior<GlobalReferences>.Instance.colors.white);
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
		return _balanceConfig.MaxDurationMinutes;
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
		return ("ba:tennisui_slider_label", item);
	}
}
