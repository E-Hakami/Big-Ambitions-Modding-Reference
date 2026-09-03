using Extensions;
using Helpers;
using JimmysUnityUtilities;
using UI;
using UI.Elements;
using UI.ItemPanel;
using UnityEngine;
using UnityEngine.Events;

namespace PlayerActivity.Activities.Paid;

public class PaidActivity : IPlayerActivity
{
	public readonly PaidActivityEnvironment paidActivityEnvironment;

	private readonly int _happinessBoostPercentage;

	private readonly IPaidActivityItem _paidActivityItem;

	private int _hoursOfHappinessBoost;

	private int _minutesInActivity;

	private int _minutesToStayInActivity;

	private bool _reopenItemPanelAfterActivity;

	private PlayerActivityState _state;

	private PlayerActivityState _stateBeforeFinishing;

	private Coroutine _movingToWaitingPositionCoroutine;

	private ButtonInfo CancelButton => new ButtonInfo("CancelPaidActivity", "common_cancel", "gray", CancelActivity, PlayerAction.Cancel);

	private ButtonInfo StartButton => new ButtonInfo("StartPaidActivity", paidActivityEnvironment.UiStartButtonKey, "blue", StartActivity, PlayerAction.Confirm);

	private ButtonInfo StopButton => new ButtonInfo("StopPaidActivity", "common_cancel", "gray", Finish, PlayerAction.Cancel);

	public PaidActivity(PaidActivityEnvironment paidActivityEnvironment, IPaidActivityItem paidActivityItem)
	{
		_state = PlayerActivityState.NotStarted;
		this.paidActivityEnvironment = paidActivityEnvironment;
		_happinessBoostPercentage = paidActivityEnvironment.BalanceConfig.BoostPercent;
		_paidActivityItem = paidActivityItem;
		SetTimeToStayInActivity(paidActivityEnvironment.BalanceConfig.DefaultDurationMinutes);
	}

	public bool RequiresEnergy()
	{
		return true;
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

	private void StartActivity()
	{
		_state = PlayerActivityState.MovingTowardsActivity;
		_movingToWaitingPositionCoroutine = null;
		Vector3 target = _paidActivityItem.GetClosestNavMeshTargetPosition(PlayerHelper.GetPosition());
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			TryMoveToTarget(target, OnTargetReached, PlayerActivityState.MovingTowardsActivity);
		});
	}

	private void TryMoveToTarget(Vector3 target, UnityAction onTargetReached, PlayerActivityState expectedState, Vector3 lookTarget = default(Vector3))
	{
		if (_state == expectedState)
		{
			PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
			if (_paidActivityItem != null && !playerController.ExistsRoute(target, showErrorNotification: true))
			{
				CancelActivity();
			}
			else if (target == Vector3.zero)
			{
				playerController.ResetWalkingAnimation();
				onTargetReached();
			}
			else if (expectedState == PlayerActivityState.MovingTowardsActivity)
			{
				playerController.SetGoal(target, onTargetReached);
			}
			else
			{
				_movingToWaitingPositionCoroutine = playerController.Character.StartCoroutine(playerController.Character.MoveToPosition(lookTarget, target, 0.5f, rotateToLookTarget: true, null, 0f, onTargetReached));
			}
		}
	}

	private void OnTargetReached()
	{
		if (_state != PlayerActivityState.MovingTowardsActivity)
		{
			return;
		}
		TransactionInfo transactionInfo = new TransactionInfo(paidActivityEnvironment.TransactionType, paidActivityEnvironment.transactionData);
		if (!GameManager.ChangeMoneySafe(0f - paidActivityEnvironment.Price, transactionInfo, null, null, force: false, showNotification: true))
		{
			return;
		}
		PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
		if (ItemPanelUI.IsVisible)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVisibility(visible: false);
			_reopenItemPanelAfterActivity = true;
		}
		_paidActivityItem.OnPlayerReached(playerController, this);
		playerController.SetNavigationBlocker(NavigationBlocker.PaidActivity);
		if (_paidActivityItem.GetPlayerWaitingTransform() != null)
		{
			Vector3 target = _paidActivityItem.GetPlayerWaitingTransform().position;
			Vector3 lookTarget = target + _paidActivityItem.GetPlayerWaitingTransform().forward;
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				TryMoveToTarget(target, OnWaitingPositionReached, PlayerActivityState.Waiting, lookTarget);
			});
		}
	}

	private void OnWaitingPositionReached()
	{
		_paidActivityItem.OnPlayerReachedWaitingPosition();
	}

	public void OnPaidActivityStarted()
	{
		_state = PlayerActivityState.Started;
		if (_movingToWaitingPositionCoroutine != null)
		{
			InstanceBehavior<GameManager>.Instance.playerController.Character.StopCoroutine(_movingToWaitingPositionCoroutine);
		}
		EnableHappinessBoost();
		if (paidActivityEnvironment.IsFixedDuration)
		{
			GameManager.SetMinutesMultiplier(1f);
		}
	}

	private void EnableHappinessBoost()
	{
		ThirdPersonCharacter character = InstanceBehavior<GameManager>.Instance.playerController.Character;
		HappinessHelper.EnableTemporalHappinessBoost(paidActivityEnvironment.BalanceConfig.TemporalType, paidActivityEnvironment.BalanceConfig.FinalType, character);
		SaveGameManager.Current.timeEnteredTemporalBoost = TimeHelper.Now();
		SaveGameManager.Current.currentActivityHappinessPerHour = paidActivityEnvironment.BalanceConfig.BoostHoursPerHour;
	}

	public void Perform(int minutes)
	{
		_minutesInActivity += minutes;
		if (_minutesInActivity >= _minutesToStayInActivity)
		{
			Finish();
		}
	}

	public void Finish()
	{
		ThirdPersonCharacter character = InstanceBehavior<GameManager>.Instance.playerController.Character;
		HappinessHelper.DisableTemporalHappinessBoost(paidActivityEnvironment.BalanceConfig.TemporalType, paidActivityEnvironment.BalanceConfig.FinalType, character);
		if (_state == PlayerActivityState.MovingTowardsActivity)
		{
			InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
		}
		_paidActivityItem.OnFinish();
		character.Reset();
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.PaidActivity);
		if (paidActivityEnvironment.IsFixedDuration)
		{
			GameManager.SetMinutesMultiplier(PlayerPrefSettings.GameSpeed);
		}
		CancelActivity();
	}

	public void CancelActivity()
	{
		_stateBeforeFinishing = _state;
		_state = PlayerActivityState.Finished;
		_minutesInActivity = 0;
		SaveGameManager.Current.timeEnteredTemporalBoost = TimeHelper.Now();
		if (_reopenItemPanelAfterActivity)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVisibility(visible: true);
			_reopenItemPanelAfterActivity = false;
		}
		if (_movingToWaitingPositionCoroutine != null)
		{
			InstanceBehavior<GameManager>.Instance.playerController.Character.StopCoroutine(_movingToWaitingPositionCoroutine);
		}
	}

	private void SetTimeToStayInActivity(int timeToStayInActivity)
	{
		_minutesToStayInActivity = Mathf.FloorToInt((float)timeToStayInActivity * 60f) / 60;
		_hoursOfHappinessBoost = GetHoursOfHappinessBoost(_minutesToStayInActivity);
		paidActivityEnvironment.SetDefaultMinutes(_minutesToStayInActivity);
	}

	private int GetHoursOfHappinessBoost(int minutes)
	{
		return paidActivityEnvironment.BalanceConfig.GetBoostHours(minutes);
	}

	public LabelInfo GetHeadlineLabel()
	{
		return new LabelInfo(paidActivityEnvironment.UiHeadlineKey, InstanceBehavior<GlobalReferences>.Instance.colors.white);
	}

	public LabelInfo[] GetLabels()
	{
		if (_state != PlayerActivityState.Running && paidActivityEnvironment.IsFixedDuration)
		{
			return new LabelInfo[1] { InfoLabel() };
		}
		return null;
	}

	private LabelInfo InfoLabel()
	{
		(string, object) sliderInfo = GetSliderInfo();
		return new LabelInfo(sliderInfo.Item1, sliderInfo.Item2, InstanceBehavior<GlobalReferences>.Instance.colors.white);
	}

	public ButtonInfo[] GetButtons()
	{
		return _state switch
		{
			PlayerActivityState.NotStarted => new ButtonInfo[2] { CancelButton, StartButton }, 
			PlayerActivityState.Running => paidActivityEnvironment.IsFixedDuration ? null : new ButtonInfo[1] { StopButton }, 
			_ => null, 
		};
	}

	public bool HasTimeMachine()
	{
		return _state == PlayerActivityState.Running;
	}

	public int GetRemainingMinutesForTimeMachine()
	{
		return _minutesToStayInActivity - _minutesInActivity;
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
		return 100f * (float)_minutesInActivity / (float)_minutesToStayInActivity;
	}

	public (string, object) GetProgressBarLabel()
	{
		int num = _minutesToStayInActivity - _minutesInActivity;
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
		if (_state == PlayerActivityState.NotStarted)
		{
			return !paidActivityEnvironment.IsFixedDuration;
		}
		return false;
	}

	public int GetMinSliderValue()
	{
		return paidActivityEnvironment.BalanceConfig.MinDurationMinutes;
	}

	public int GetMaxSliderValue()
	{
		if (!paidActivityEnvironment.IsFixedDuration)
		{
			return paidActivityEnvironment.BalanceConfig.MaxDurationMinutes;
		}
		return paidActivityEnvironment.BalanceConfig.DefaultDurationMinutes;
	}

	public float GetCurrentSliderValue()
	{
		return _minutesToStayInActivity;
	}

	public void OnSliderValueChanged(int value)
	{
		SetTimeToStayInActivity(value);
	}

	public (string, object) GetSliderInfo()
	{
		int minutesToStayInActivity = _minutesToStayInActivity;
		object item = new
		{
			price = paidActivityEnvironment.Price.ToCurrencyFormat(),
			minutes = minutesToStayInActivity,
			boostPercentage = _happinessBoostPercentage,
			boostHours = _hoursOfHappinessBoost
		};
		return (paidActivityEnvironment.UiSliderLabelKey, item);
	}
}
