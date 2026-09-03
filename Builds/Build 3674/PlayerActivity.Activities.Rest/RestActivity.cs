using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using Buildings.Retail.Businesses.CinemaTheater;
using Controllers;
using Helpers;
using JimmysUnityUtilities;
using UI.Elements;
using UnityEngine;

namespace PlayerActivity.Activities.Rest;

public class RestActivity : IPlayerActivity
{
	private readonly EntityController _entityController;

	private readonly string _environmentType;

	private readonly RestEnvironment _restEnvironment;

	private int _minutesRested;

	private int _minutesToRest;

	private PlayerActivityState _state;

	private PlayerActivityState _stateBeforeFinishing;

	private Vector3 _lastPosition = Vector3.zero;

	private readonly int _happinessBoostPercentage;

	private int _hoursOfHappinessBoost;

	private Timestamp _finishTime;

	private ButtonInfo CancelButton => new ButtonInfo("CancelRest", "common_cancel", "gray", CancelResting, PlayerAction.Cancel);

	private ButtonInfo StartButton => new ButtonInfo("StartRest", IsWatchingShow() ? "watchshow_start_button" : "restui_start_button", "blue", StartResting, PlayerAction.Confirm);

	private ButtonInfo StopButton => new ButtonInfo("StopRest", IsWatchingShow() ? "watchshow_stop_button" : "restui_stop_button", "gray", Finish, PlayerAction.Cancel);

	public RestActivity(RestEnvironment restEnvironment, EntityController attachedEntity)
	{
		_state = PlayerActivityState.NotStarted;
		_restEnvironment = restEnvironment;
		_entityController = attachedEntity;
		_happinessBoostPercentage = restEnvironment.WatchShowBalanceConfig.BoostPercent;
		SetTimeToRest(restEnvironment.GetDefaultMinutes());
		_environmentType = _restEnvironment.EnvironmentType switch
		{
			RestEnvironmentType.OutsideBench => RestEnvironmentType.Bench.ToStringFast(), 
			RestEnvironmentType.OutsideChair => RestEnvironmentType.Chair.ToStringFast(), 
			_ => _restEnvironment.EnvironmentType.ToStringFast(), 
		};
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

	private void StartResting()
	{
		_state = PlayerActivityState.MovingTowardsActivity;
		_finishTime = (IsWatchingShow() ? null : TimeHelper.GetTimeInHours(0, _minutesToRest));
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if (_state == PlayerActivityState.MovingTowardsActivity)
			{
				if (!InstanceBehavior<GameManager>.Instance.playerController.ExistsRoute(_entityController, showErrorNotification: true))
				{
					CancelResting();
				}
				else
				{
					InstanceBehavior<GameManager>.Instance.playerController.SetGoal(_entityController, delegate
					{
						if (_state == PlayerActivityState.MovingTowardsActivity)
						{
							_state = PlayerActivityState.Started;
							if (!(_entityController == null))
							{
								_lastPosition = PlayerHelper.GetPosition();
								ThirdPersonCharacter character = InstanceBehavior<GameManager>.Instance.playerController.Character;
								Transform transform = null;
								switch (_restEnvironment.EnvironmentType)
								{
								case RestEnvironmentType.Chair:
								case RestEnvironmentType.Bench:
									if (_entityController is SeatController seatController)
									{
										if (!seatController.CanSit)
										{
											CancelResting();
											return;
										}
										transform = seatController.SittingPosition;
										if (!transform)
										{
											CancelResting();
											return;
										}
										character.SitOnChair(transform);
										OccupySeat(seatController, transform);
									}
									break;
								case RestEnvironmentType.OutsideBench:
									if (_entityController is OutsideBenchController outsideBenchController)
									{
										transform = outsideBenchController.sittingPosition;
										character.SitOnChair(transform);
									}
									break;
								case RestEnvironmentType.OutsideChair:
									if (_entityController is OutsideChairController outsideChairController)
									{
										transform = outsideChairController.sittingPosition;
										character.SitOnChair(transform);
									}
									break;
								case RestEnvironmentType.DeckChair:
									ApplySwimwear(character);
									TryPositionForLaying(character);
									character.animator.SetBool(PermanentAnimationType.LayingInDeckChair);
									break;
								case RestEnvironmentType.SunLounger:
									ApplySwimwear(character);
									TryPositionForLaying(character);
									character.animator.SetBool(PermanentAnimationType.LayingInLoungerChair);
									break;
								case RestEnvironmentType.BeachTowel:
									ApplySwimwear(character);
									TryPositionForLaying(character);
									character.animator.SetBool(PermanentAnimationType.LayingInTowel01);
									break;
								}
								if ((bool)transform && SeatController.IsInCinemaOrTheater())
								{
									CinemaTheaterHelper.ForceUnseatSittingPosition(transform);
								}
								EnergyHelper.RemoveEnergySpender("move");
								EnergyHelper.SetCurrentEnergyRegen(_restEnvironment.EnergyRegen);
								float num = EnergyHelper.DefaultEnergyRegenMultiplier;
								if (_entityController is ItemController itemController && !string.IsNullOrEmpty(itemController.itemName))
								{
									num += (float)ItemsGetter.GetByName(itemController.itemName).quality / 100f;
								}
								EnergyHelper.energyRegenMultiplier = num;
								InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.RestActivity);
								if (IsWatchingShow())
								{
									HappinessHelper.EnableTemporalHappinessBoost(_restEnvironment.WatchShowBalanceConfig.TemporalType, _restEnvironment.WatchShowBalanceConfig.FinalType, character);
									SaveGameManager.Current.timeEnteredTemporalBoost = TimeHelper.Now();
									SaveGameManager.Current.currentActivityHappinessPerHour = _restEnvironment.WatchShowBalanceConfig.BoostHoursPerHour;
								}
							}
						}
					});
				}
			}
		});
	}

	private void CancelResting()
	{
		EnergyHelper.SetCurrentEnergyRegen(EnergyRegen.None);
		EnergyHelper.energyRegenMultiplier = EnergyHelper.DefaultEnergyRegenMultiplier;
		_stateBeforeFinishing = _state;
		_state = PlayerActivityState.Finished;
		_minutesRested = 0;
	}

	private void SetTimeToRest(int timeToRest)
	{
		_minutesToRest = Mathf.FloorToInt((float)timeToRest * 60f) / 60;
		_restEnvironment.SetDefaultMinutes(_minutesToRest);
		_hoursOfHappinessBoost = GetHoursOfHappinessBoost(_minutesToRest);
	}

	private int GetHoursOfHappinessBoost(int minutes)
	{
		return _restEnvironment.WatchShowBalanceConfig.GetBoostHours(minutes);
	}

	public void Perform(int minutes)
	{
		_minutesRested += minutes;
		if (_minutesRested >= _minutesToRest || (_finishTime != null && _finishTime.IsInThePast()))
		{
			Finish();
		}
	}

	public void Finish()
	{
		PlayerActivityState state = _state;
		if (state == PlayerActivityState.Started || state == PlayerActivityState.Running)
		{
			if (_restEnvironment.BalanceConfig != null && !string.IsNullOrEmpty(_restEnvironment.BalanceConfig.FinalType))
			{
				int minutes = Mathf.Min(_minutesRested, _minutesToRest);
				HappinessHelper.AddModifier(_restEnvironment.BalanceConfig.FinalType, _restEnvironment.BalanceConfig.GetBoostHours(minutes), additiveHours: true);
			}
			GameEvent.Invoke("ba:gameevent_playerenergyincreased");
		}
		else if (_state == PlayerActivityState.MovingTowardsActivity)
		{
			InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
		}
		ThirdPersonCharacter character = InstanceBehavior<GameManager>.Instance.playerController.Character;
		Transform isSittingOn = character.isSittingOn;
		character.Reset();
		RestEnvironmentType environmentType = _restEnvironment.EnvironmentType;
		if (environmentType == RestEnvironmentType.DeckChair || environmentType == RestEnvironmentType.SunLounger || environmentType == RestEnvironmentType.BeachTowel)
		{
			character.appearanceSetter.SetAppearance(PlayerHelper.CharacterData);
			character.animator.SetBool(PermanentAnimationType.LayingInDeckChair, state: false);
			character.animator.SetBool(PermanentAnimationType.LayingInLoungerChair, state: false);
			character.animator.SetBool(PermanentAnimationType.LayingInTowel01, state: false);
		}
		if (_lastPosition != Vector3.zero)
		{
			character.WarpSafely(_lastPosition);
			_lastPosition = Vector3.zero;
		}
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.RestActivity);
		if (_entityController is SeatController seatController)
		{
			OccupySeat(seatController, occupied: false, isSittingOn);
		}
		if (IsWatchingShow())
		{
			HappinessHelper.DisableTemporalHappinessBoost(_restEnvironment.WatchShowBalanceConfig.TemporalType, _restEnvironment.WatchShowBalanceConfig.FinalType, character);
		}
		CancelResting();
	}

	private bool IsWatchingShow()
	{
		if (SeatController.IsInCinemaOrTheater() && _entityController is SeatController seatController)
		{
			return CinemaTheaterHelper.IsSeatUsable(seatController);
		}
		return false;
	}

	private static void OccupySeat(SeatController seatController, Transform sittingPosition)
	{
		OccupySeat(seatController, occupied: true, sittingPosition);
	}

	private static void OccupySeat(SeatController seatController, bool occupied, Transform sittingPosition)
	{
		if ((bool)sittingPosition)
		{
			seatController.OnSittingChanged(sittingPosition, occupied);
		}
		else
		{
			seatController.Occupied = occupied;
		}
		SeatSpot seatSpot = seatController.SeatSpots.FirstOrDefault((SeatSpot x) => x.GetAttachedChair == seatController);
		if (seatSpot == null)
		{
			seatSpot = seatController.parentItemController?.SeatSpots.FirstOrDefault((SeatSpot x) => x.GetAttachedChair == seatController);
		}
		if (seatSpot != null)
		{
			seatSpot.occupied = occupied;
		}
	}

	private static void ApplySwimwear(ThirdPersonCharacter tpc)
	{
		List<AppearanceElementData> swimwearElements = tpc.appearanceSetter.GetSwimwearElements();
		tpc.appearanceSetter.UpdateVisuals(swimwearElements);
	}

	private void TryPositionForLaying(ThirdPersonCharacter tpc)
	{
		if (_entityController is OutsideInteractableItemToRest outsideInteractableItemToRest)
		{
			tpc.ForceToTransform(outsideInteractableItemToRest.interactPoint);
			tpc.navmeshAgent.enabled = false;
		}
		else if (_entityController is SeatController seatController)
		{
			tpc.ForceToTransform(seatController.SittingPosition);
		}
	}

	public LabelInfo GetHeadlineLabel()
	{
		return new LabelInfo(IsWatchingShow() ? "watchshow_headline" : ("restui_" + _environmentType + "_headline"), InstanceBehavior<GlobalReferences>.Instance.colors.white);
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

	public bool HasFastForward()
	{
		return _state == PlayerActivityState.Running;
	}

	public bool HasTimeMachine()
	{
		return _state == PlayerActivityState.Running;
	}

	public int GetRemainingMinutesForTimeMachine()
	{
		if (_finishTime != null)
		{
			return Mathf.CeilToInt(_finishTime.GetMinutesFromNow());
		}
		return _minutesToRest - _minutesRested;
	}

	public Timestamp GetTimeMachineGoal()
	{
		return _finishTime ?? TimeHelper.Now().AddMinutes(GetRemainingMinutesForTimeMachine());
	}

	public bool HasProgressBar()
	{
		return _state == PlayerActivityState.Running;
	}

	public float GetProgressBarPercentageValue()
	{
		return 100f * (float)(_minutesToRest - GetRemainingMinutesForTimeMachine()) / (float)_minutesToRest;
	}

	public (string key, object arguments) GetProgressBarLabel()
	{
		int remainingMinutesForTimeMachine = GetRemainingMinutesForTimeMachine();
		int num = Mathf.FloorToInt((float)remainingMinutesForTimeMachine / 60f);
		int minutes = remainingMinutesForTimeMachine - num * 60;
		object item = new
		{
			hours = num,
			minutes = minutes
		};
		return (key: "playeractivityui_time_remaining", arguments: item);
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
		if (!IsWatchingShow())
		{
			return _restEnvironment.BalanceConfig.MaxDurationMinutes;
		}
		return _restEnvironment.WatchShowBalanceConfig.MaxDurationMinutes;
	}

	public float GetCurrentSliderValue()
	{
		return _minutesToRest;
	}

	public void OnSliderValueChanged(int value)
	{
		SetTimeToRest(value);
	}

	public (string key, object arguments) GetSliderInfo()
	{
		int num = Mathf.FloorToInt((float)_minutesToRest / 60f);
		int num2 = _minutesToRest - num * 60;
		Timestamp timeInHours = TimeHelper.GetTimeInHours(num, num2);
		bool num3 = IsWatchingShow();
		return new ValueTuple<string, object>(item2: num3 ? ((object)new
		{
			entertainingHours = num,
			entertainingMinutes = num2,
			boostPercentage = _happinessBoostPercentage,
			boostHours = _hoursOfHappinessBoost
		}) : ((object)new
		{
			restHours = num,
			restMinutes = num2,
			time = timeInHours.GetFormattedTime()
		}), item1: num3 ? "entertainui_slider_label_watchshow" : "restui_slider_label");
	}
}
