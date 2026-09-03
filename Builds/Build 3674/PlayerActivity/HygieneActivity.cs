using System.Collections.Generic;
using BigAmbitions.Items;
using Helpers;
using JimmysUnityUtilities;
using UI;
using UI.Elements;
using UI.ItemPanel;
using UI.Notification;
using UnityEngine;

namespace PlayerActivity;

public class HygieneActivity : IPlayerActivity
{
	private readonly HygieneItemController _controller;

	private readonly HygieneEnvironment _environment;

	private int _minutesToUse;

	private int _minutesUsed;

	private bool _reopenItemPanelAfterActivity;

	private PlayerActivityState _state;

	private PlayerActivityState _stateBeforeFinishing;

	private ButtonInfo CancelButton => new ButtonInfo("CancelHygiene", "common_cancel", "gray", CancelUse, PlayerAction.Cancel);

	private ButtonInfo StartButton => new ButtonInfo("StartHygiene", _environment.UIStartKey, "blue", StartUse, PlayerAction.Confirm);

	private ButtonInfo StopButton => new ButtonInfo("StopHygiene", "common_cancel", "gray", Finish, PlayerAction.Cancel);

	public HygieneActivity(HygieneEnvironment environment, HygieneItemController controller)
	{
		_state = PlayerActivityState.NotStarted;
		_environment = environment;
		_controller = controller;
		SetTimeToUse(environment.GetDefaultMinutes());
	}

	public bool RequiresEnergy()
	{
		return false;
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

	private void StartUse()
	{
		_state = PlayerActivityState.MovingTowardsActivity;
		Vector3 target = _controller.GetClosestNavMeshTargetPosition(PlayerHelper.GetPosition());
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if (_controller != null && !InstanceBehavior<GameManager>.Instance.playerController.ExistsRoute(_controller, showErrorNotification: true))
			{
				CancelUse();
			}
			else if (target == Vector3.zero)
			{
				InstanceBehavior<GameManager>.Instance.playerController.ResetWalkingAnimation();
				OnEnvironmentReached();
			}
			else
			{
				InstanceBehavior<GameManager>.Instance.playerController.SetGoal(target, OnEnvironmentReached);
			}
		});
	}

	private void OnEnvironmentReached()
	{
		PlayerItemPurchaserSettings playerItemPurchaserSettings = _controller.playerItemPurchaserSettings;
		if ((playerItemPurchaserSettings != null && playerItemPurchaserSettings.enabled) || !_controller.BeginUse(InstanceBehavior<GameManager>.Instance.playerController.Character))
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { { "itemname", _controller.itemName } };
			Notifications.Show(NotificationType.Error, "notification_this_item_is_occupied", notificationData);
			_stateBeforeFinishing = _state;
			_state = PlayerActivityState.Finished;
			return;
		}
		_state = PlayerActivityState.Started;
		InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.HygieneActivity);
		if (ItemPanelUI.IsVisible)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVisibility(visible: false);
			_reopenItemPanelAfterActivity = true;
		}
	}

	public void Perform(int minutes)
	{
		_minutesUsed += minutes;
		if (_minutesUsed >= _minutesToUse)
		{
			Finish();
		}
	}

	public void Finish()
	{
		if (_state == PlayerActivityState.MovingTowardsActivity)
		{
			InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
		}
		_controller.EndUse(InstanceBehavior<GameManager>.Instance.playerController.Character);
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.HygieneActivity);
		CancelUse();
	}

	private void CancelUse()
	{
		_stateBeforeFinishing = _state;
		_state = PlayerActivityState.Finished;
		int hoursOfHappinessBoost = GetHoursOfHappinessBoost(_minutesUsed);
		if (hoursOfHappinessBoost > 0)
		{
			HappinessHelper.AddModifier(_environment.BalanceConfig.FinalType, hoursOfHappinessBoost, additiveHours: true);
		}
		_minutesUsed = 0;
		InstanceBehavior<GameManager>.Instance.playerController.Character.Reset();
		if (_reopenItemPanelAfterActivity)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVisibility(visible: true);
			_reopenItemPanelAfterActivity = false;
		}
		if ((bool)_environment.exitPoint)
		{
			InstanceBehavior<GameManager>.Instance.playerController.SetGoal(_environment.exitPoint.transform.position, null);
		}
	}

	private void SetTimeToUse(int minutes)
	{
		if (_environment.IsFixedDuration)
		{
			_minutesToUse = _environment.GetDefaultMinutes();
			return;
		}
		_minutesToUse = minutes;
		_environment.SetDefaultMinutes(_minutesToUse);
	}

	private int GetHappinessBoostPercentage()
	{
		return _environment.BalanceConfig.BoostPercent;
	}

	private int GetHoursOfHappinessBoost(int minutes)
	{
		if (_environment.IsFixedDuration && minutes < _environment.GetDefaultMinutes())
		{
			return 0;
		}
		return _environment.BalanceConfig.GetBoostHours(minutes);
	}

	public LabelInfo GetHeadlineLabel()
	{
		string key = string.Empty;
		ItemController controller = _controller;
		if ((object)controller != null)
		{
			key = controller.itemName;
		}
		return new LabelInfo(key, InstanceBehavior<GlobalReferences>.Instance.colors.white);
	}

	public LabelInfo[] GetLabels()
	{
		if (_state != PlayerActivityState.Running && _environment.IsFixedDuration)
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
		return _minutesToUse - _minutesUsed;
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
		return 100f * (float)_minutesUsed / (float)Mathf.Max(1, _minutesToUse);
	}

	public (string, object) GetProgressBarLabel()
	{
		int num = _minutesToUse - _minutesUsed;
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
		if (!_environment.IsFixedDuration)
		{
			return _state == PlayerActivityState.NotStarted;
		}
		return false;
	}

	public int GetMinSliderValue()
	{
		return _environment.BalanceConfig.MinDurationMinutes;
	}

	public int GetMaxSliderValue()
	{
		return _environment.BalanceConfig.MaxDurationMinutes;
	}

	public float GetCurrentSliderValue()
	{
		return _minutesToUse;
	}

	public void OnSliderValueChanged(int value)
	{
		SetTimeToUse(value);
	}

	public (string, object) GetSliderInfo()
	{
		int minutesToUse = _minutesToUse;
		int happinessBoostPercentage = GetHappinessBoostPercentage();
		int hoursOfHappinessBoost = GetHoursOfHappinessBoost(minutesToUse);
		object item = new
		{
			minutes = minutesToUse,
			boostPercentage = happinessBoostPercentage,
			boostHours = hoursOfHappinessBoost
		};
		return (_environment.UILabelKey, item);
	}
}
