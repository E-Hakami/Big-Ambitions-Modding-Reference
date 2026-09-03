using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.SoundSystem;
using Boats;
using Helpers;
using JimmysUnityUtilities;
using UI;
using UI.Elements;
using UnityEngine;
using UnityEngine.Events;

namespace PlayerActivity;

public class SleepActivity : IPlayerActivity
{
	private readonly SleepEnvironment _sleepEnvironment;

	private readonly EntityController _entityController;

	private readonly string _environmentType;

	private PlayerActivityState _state;

	private PlayerActivityState _stateBeforeFinishing;

	private int _minutesToSleep;

	private int _minutesSlept;

	private Timestamp _finishTime;

	private ButtonInfo CancelButton => new ButtonInfo("CancelSleep", "common_cancel", "gray", CancelSleeping, PlayerAction.Cancel);

	private ButtonInfo StartButton => new ButtonInfo("StartSleep", "sleepui_start_" + _environmentType + "_button", "blue", StartSleeping, PlayerAction.Confirm);

	private ButtonInfo StopButton => new ButtonInfo("StopSleep", "sleepui_stop_" + _environmentType + "_button", "gray", Finish, PlayerAction.Cancel);

	public bool RequiresEnergy()
	{
		return false;
	}

	public bool BlocksAutoSave()
	{
		return false;
	}

	public SleepActivity(SleepEnvironment sleepEnvironment, EntityController attachedEntity)
	{
		_state = PlayerActivityState.NotStarted;
		_sleepEnvironment = sleepEnvironment;
		_environmentType = _sleepEnvironment.SleepEnvironmentType.ToStringFast();
		_entityController = attachedEntity;
		SetTimeToSleep(sleepEnvironment.GetDefaultMinutes());
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

	private void StartSleeping()
	{
		_state = PlayerActivityState.MovingTowardsActivity;
		_finishTime = TimeHelper.GetTimeInHours(0, _minutesToSleep);
		UnityAction onReached = delegate
		{
			if (_state == PlayerActivityState.MovingTowardsActivity)
			{
				_state = PlayerActivityState.Started;
				InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.SleepActivity);
				if (!(_entityController == null))
				{
					switch (_sleepEnvironment.SleepEnvironmentType)
					{
					case SleepEnvironmentType.Bed:
						if (_entityController is BedController bedController)
						{
							InstanceBehavior<GameManager>.Instance.playerController.Character.LayOnBed(bedController, bedController.SleepPositionTransform);
							InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.BedLayDown, bedController.transform.position, 1f, isPlayerCreatedSound: true);
							_entityController.Occupied = true;
						}
						break;
					case SleepEnvironmentType.Boat:
						InstanceBehavior<GameManager>.Instance.playerController.Character.ToggleVisibility(show: false);
						break;
					}
					EnergyHelper.RemoveEnergySpender("move");
					EnergyHelper.SetCurrentEnergyRegen(_sleepEnvironment.EnergyRegen);
					float num = EnergyHelper.DefaultEnergyRegenMultiplier;
					if (_entityController is ItemController itemController && !string.IsNullOrEmpty(itemController.itemName))
					{
						num += (float)ItemsGetter.GetByName(itemController.itemName).quality / 100f;
					}
					EnergyHelper.energyRegenMultiplier = num;
					InstanceBehavior<UIs>.Instance.playerActivityUI.ForceTimeMachine();
				}
			}
		};
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if (_state == PlayerActivityState.MovingTowardsActivity)
			{
				if (VehicleHelper.IsInsideMotorVehicle())
				{
					onReached();
				}
				else if (!InstanceBehavior<GameManager>.Instance.playerController.ExistsRoute(_entityController, showErrorNotification: true))
				{
					CancelSleeping();
				}
				else
				{
					InstanceBehavior<GameManager>.Instance.playerController.SetGoal(_entityController, onReached);
				}
			}
		});
	}

	public void Perform(int minutes)
	{
		_minutesSlept += minutes;
		if (_finishTime.IsInThePast())
		{
			Finish();
		}
	}

	public void Finish()
	{
		PlayerActivityState state = _state;
		if (state == PlayerActivityState.Started || state == PlayerActivityState.Running)
		{
			PlayerActivityBalanceConfig balanceConfig = GetBalanceConfig();
			if (balanceConfig != null && !string.IsNullOrEmpty(balanceConfig.FinalType))
			{
				HappinessHelper.AddModifier(balanceConfig.FinalType, balanceConfig.GetBoostHours(_minutesSlept));
			}
			GameEvent.Invoke("ba:gameevent_playerenergyincreased");
		}
		else if (_state == PlayerActivityState.MovingTowardsActivity)
		{
			InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
		}
		EnergyHelper.SetCurrentEnergyRegen(EnergyRegen.None);
		EnergyHelper.energyRegenMultiplier = EnergyHelper.DefaultEnergyRegenMultiplier;
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.SleepActivity);
		switch (_sleepEnvironment.SleepEnvironmentType)
		{
		case SleepEnvironmentType.Bed:
			if (_entityController is BedController)
			{
				InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.BedStandUp, InstanceBehavior<GameManager>.Instance.playerController.transform.position, 1f, isPlayerCreatedSound: true);
				InstanceBehavior<GameManager>.Instance.playerController.Character.Reset();
				_entityController.Occupied = false;
			}
			break;
		case SleepEnvironmentType.Boat:
			InstanceBehavior<GameManager>.Instance.playerController.Character.ToggleVisibility(show: true);
			break;
		}
		CancelSleeping();
	}

	private void CancelSleeping()
	{
		_stateBeforeFinishing = _state;
		_state = PlayerActivityState.Finished;
		_minutesSlept = 0;
	}

	private void SetTimeToSleep(int timeToSleep)
	{
		_minutesToSleep = Mathf.FloorToInt((float)timeToSleep * 60f) / 60;
		_sleepEnvironment.SetDefaultMinutes(_minutesToSleep);
	}

	private PlayerActivityBalanceConfig GetBalanceConfig()
	{
		switch (_sleepEnvironment.SleepEnvironmentType)
		{
		case SleepEnvironmentType.Car:
		{
			VehicleController currentVehicleBase = VehicleHelper.GetCurrentVehicleBase();
			if (currentVehicleBase?.vehicleType != null && currentVehicleBase.vehicleType.isLuxuryCar && _sleepEnvironment.LuxuryOverrideBalanceConfig != null)
			{
				return _sleepEnvironment.LuxuryOverrideBalanceConfig;
			}
			break;
		}
		case SleepEnvironmentType.Boat:
			if (_entityController is BoatController { IsLuxuryYacht: not false } && _sleepEnvironment.LuxuryOverrideBalanceConfig != null)
			{
				return _sleepEnvironment.LuxuryOverrideBalanceConfig;
			}
			break;
		}
		return _sleepEnvironment.BalanceConfig;
	}

	public LabelInfo GetHeadlineLabel()
	{
		return new LabelInfo("sleepui_" + _environmentType + "_headline", InstanceBehavior<GlobalReferences>.Instance.colors.white);
	}

	public LabelInfo[] GetLabels()
	{
		return null;
	}

	public ButtonInfo[] GetButtons()
	{
		return _state switch
		{
			PlayerActivityState.NotStarted => (!(_entityController is BoatController boatController)) ? new ButtonInfo[2] { CancelButton, StartButton } : new ButtonInfo[3]
			{
				CancelButton,
				SellBoatButton(boatController),
				StartButton
			}, 
			PlayerActivityState.Running => new ButtonInfo[1] { StopButton }, 
			_ => null, 
		};
	}

	private ButtonInfo SellBoatButton(BoatController boatController)
	{
		return new ButtonInfo("SellBoat", "itempanelui_sell", "red", boatController.Sell, PlayerAction.Sell);
	}

	public bool HasTimeMachine()
	{
		PlayerActivityState state = _state;
		return state == PlayerActivityState.Started || state == PlayerActivityState.Running;
	}

	public int GetRemainingMinutesForTimeMachine()
	{
		return Mathf.CeilToInt(_finishTime.GetMinutesFromNow());
	}

	public Timestamp GetTimeMachineGoal()
	{
		return _finishTime;
	}

	public bool HasFastForward()
	{
		return false;
	}

	public bool HasProgressBar()
	{
		return _state == PlayerActivityState.Running;
	}

	public float GetProgressBarPercentageValue()
	{
		return 100f * (float)(_minutesToSleep - GetRemainingMinutesForTimeMachine()) / (float)_minutesToSleep;
	}

	public (string, object) GetProgressBarLabel()
	{
		int remainingMinutesForTimeMachine = GetRemainingMinutesForTimeMachine();
		int num = Mathf.FloorToInt((float)remainingMinutesForTimeMachine / 60f);
		int minutes = remainingMinutesForTimeMachine - num * 60;
		object item = new
		{
			hours = num,
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
		return _sleepEnvironment.BalanceConfig.MaxDurationMinutes;
	}

	public float GetCurrentSliderValue()
	{
		return _minutesToSleep;
	}

	public void OnSliderValueChanged(int value)
	{
		SetTimeToSleep(value);
	}

	public (string, object) GetSliderInfo()
	{
		int num = Mathf.FloorToInt((float)_minutesToSleep / 60f);
		int num2 = _minutesToSleep - num * 60;
		Timestamp timeInHours = TimeHelper.GetTimeInHours(num, num2);
		object item = new
		{
			sleepHours = num,
			sleepMinutes = num2,
			time = timeInHours.GetFormattedTime()
		};
		return ("sleepui_slider_label_" + _environmentType, item);
	}
}
