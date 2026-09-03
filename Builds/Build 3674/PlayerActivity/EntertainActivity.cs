using BigAmbitions.Characters;
using BigAmbitions.Items;
using BigAmbitions.SoundSystem;
using Controllers;
using Helpers;
using JimmysUnityUtilities;
using UI;
using UI.Elements;
using UI.ItemPanel;
using UnityEngine;

namespace PlayerActivity;

public class EntertainActivity : IPlayerActivity
{
	private readonly EntityController _assignedChair;

	private readonly string _deviceType;

	private readonly EntertainDevice _entertainDevice;

	private readonly EntertainSeatingResolver _seatingResolver;

	private readonly int _happinessBoostPercentage;

	private readonly EntityController _targetObject;

	private readonly AnimationType _usingAnimation = AnimationType.None;

	private readonly float _usingAnimationLength;

	private int _hoursOfHappinessBoost;

	private int _minutesEntertained;

	private int _minutesToEntertain;

	private float _nextAnimation;

	private bool _reopenItemPanelAfterActivity;

	private EntertainSeatingResult _seatingResult;

	private PlayerActivityState _state;

	private PlayerActivityState _stateBeforeFinishing;

	private ButtonInfo CancelButton => new ButtonInfo("CancelEntertaining", "common_cancel", "gray", CancelEntertaining, PlayerAction.Cancel);

	private ButtonInfo StartButton => new ButtonInfo("StartEntertaining", "entertain_panel_start_" + _deviceType + "_button", "blue", StartEntertaining, PlayerAction.Confirm);

	private ButtonInfo StopButton => new ButtonInfo("StopEntertaining", "entertainui_stop_" + _deviceType, "gray", Finish, PlayerAction.Cancel);

	public EntertainActivity(EntertainDevice entertainDevice, EntityController attachedEntity)
	{
		_state = PlayerActivityState.NotStarted;
		_entertainDevice = entertainDevice;
		_happinessBoostPercentage = entertainDevice.balanceConfig.BoostPercent;
		_deviceType = _entertainDevice.entertainType.ToStringFast();
		_targetObject = attachedEntity;
		_seatingResolver = new EntertainSeatingResolver(_targetObject, _entertainDevice.maxSeatingDistance);
		if (_targetObject != null && _targetObject is ComputerController computerController)
		{
			_assignedChair = computerController.FindChair();
		}
		if (_targetObject is ItemController itemController)
		{
			Item byName = ItemsGetter.GetByName(itemController.itemName);
			_usingAnimation = byName.usingAnimation;
			if (_usingAnimation != AnimationType.None)
			{
				_usingAnimationLength = PlayerHelper.GetAnimator().GetAnimationLength(_usingAnimation);
			}
		}
		SetTimeToEntertain(entertainDevice.GetDefaultMinutes());
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

	private void StartEntertaining()
	{
		_state = PlayerActivityState.MovingTowardsActivity;
		Vector3 target = Vector3.zero;
		Transform seatingPosition = null;
		EntityController interactedEntity = _targetObject;
		if (_assignedChair != null)
		{
			interactedEntity = _assignedChair;
			target = _assignedChair.GetClosestNavMeshTargetPosition(PlayerHelper.GetPosition());
		}
		else if (_entertainDevice.preferSeating)
		{
			_seatingResult = _seatingResolver.Resolve();
			seatingPosition = _seatingResult.Position;
			if (_seatingResult.HasSeat)
			{
				interactedEntity = _seatingResult.Chair;
				target = _seatingResult.Chair.GetClosestNavMeshTargetPositionStraightLine(seatingPosition.position);
			}
			else if (_targetObject != null)
			{
				target = _targetObject.GetClosestNavMeshTargetPosition(PlayerHelper.GetPosition());
			}
		}
		else if (_targetObject != null)
		{
			target = _targetObject.GetClosestNavMeshTargetPosition(PlayerHelper.GetPosition());
		}
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if (_state == PlayerActivityState.MovingTowardsActivity)
			{
				if (interactedEntity != null && !InstanceBehavior<GameManager>.Instance.playerController.ExistsRoute(interactedEntity, showErrorNotification: true))
				{
					CancelEntertaining();
				}
				else if (target == Vector3.zero)
				{
					InstanceBehavior<GameManager>.Instance.playerController.ResetWalkingAnimation();
					OnEntertainDeviceReached(seatingPosition);
				}
				else
				{
					InstanceBehavior<GameManager>.Instance.playerController.SetGoal(target, delegate
					{
						OnEntertainDeviceReached(seatingPosition);
					});
				}
			}
		});
	}

	private void OnEntertainDeviceReached(Transform seatingPosition)
	{
		if (_state != PlayerActivityState.MovingTowardsActivity)
		{
			return;
		}
		_state = PlayerActivityState.Started;
		ThirdPersonCharacter character = InstanceBehavior<GameManager>.Instance.playerController.Character;
		if (_entertainDevice.preferSeating && seatingPosition != null && (!_seatingResult.HasSeat || !_seatingResult.Chair.IsSittingPositionAvailable(seatingPosition)))
		{
			_seatingResult = _seatingResolver.Resolve();
			seatingPosition = _seatingResult.Position;
			if (!_seatingResult.HasSeat)
			{
				CancelEntertaining();
				return;
			}
		}
		HappinessHelper.EnableTemporalHappinessBoost(_entertainDevice.balanceConfig.TemporalType, _entertainDevice.balanceConfig.FinalType, character);
		SaveGameManager.Current.timeEnteredTemporalBoost = TimeHelper.Now();
		SaveGameManager.Current.currentActivityHappinessPerHour = _entertainDevice.balanceConfig.BoostHoursPerHour;
		InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.EntertainActivity);
		if (ItemPanelUI.IsVisible && (!PlayerHelper.IsHoldingBag || _entertainDevice.entertainType != EntertainType.WatchTV))
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVisibility(visible: false);
			_reopenItemPanelAfterActivity = true;
		}
		if (_assignedChair != null)
		{
			character.SitOnChair(_assignedChair.transform);
			_assignedChair.Occupied = true;
		}
		if ((bool)_targetObject)
		{
			_targetObject.Occupied = true;
			character.SetItemIKTargets(_targetObject as ItemController);
			if (_entertainDevice.entertainType == EntertainType.DJ)
			{
				PlayerHelper.GetAnimator().SetBool(PermanentAnimationType.DJ);
				character.ForceToPosition(_targetObject.GetComponent<DJBoothController>().GetEmployeePosition());
				character.ForceToRotation(Quaternion.LookRotation(_targetObject.transform.forward));
				_targetObject.GetComponent<DJBoothMusicPlayer>().Enable();
				InstanceBehavior<GameManager>.Instance.playerController.Character.LinkToPointAndClickObject(_targetObject);
			}
			else
			{
				((ItemController)_targetObject)?.PlayVideoOnScreen((_entertainDevice.entertainType != EntertainType.Play) ? VideoClipData.VideoType.TV : VideoClipData.VideoType.Game);
			}
		}
		if (_entertainDevice.preferSeating && seatingPosition != null)
		{
			character.SitOnChair(seatingPosition, _seatingResult.Chair?.RestEnvironment?.GetEntertainAnimationOverride() ?? PermanentAnimationType.Sitting);
		}
		if (_targetObject == null && _entertainDevice.entertainType == EntertainType.Read)
		{
			PlayerHelper.GetAnimator().SetBool(PermanentAnimationType.ReadingABook);
			string handObjectNameFromPermanentAnimationType = BaseHuman.GetHandObjectNameFromPermanentAnimationType(PermanentAnimationType.ReadingABook);
			character.AddHandObject(handObjectNameFromPermanentAnimationType, isRightHand: false);
		}
		if (_usingAnimation != AnimationType.None)
		{
			if (Random.Range(0f, 1f) < 0.5f)
			{
				RunUsingAnimation();
			}
			else
			{
				_nextAnimation = Time.time + Random.Range(0f, _entertainDevice.maxTimeBetweenAnims);
			}
		}
	}

	public void Perform(int minutes)
	{
		_minutesEntertained += minutes;
		if (_minutesEntertained >= _minutesToEntertain)
		{
			Finish();
		}
		else if (_usingAnimation != AnimationType.None && _nextAnimation <= Time.time)
		{
			RunUsingAnimation();
		}
	}

	public void Finish()
	{
		ThirdPersonCharacter character = InstanceBehavior<GameManager>.Instance.playerController.Character;
		HappinessHelper.DisableTemporalHappinessBoost(_entertainDevice.balanceConfig.TemporalType, _entertainDevice.balanceConfig.FinalType, character);
		if (_state == PlayerActivityState.MovingTowardsActivity)
		{
			InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
		}
		if (_assignedChair != null)
		{
			character.Reset();
			InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.ChairStandUp, InstanceBehavior<GameManager>.Instance.playerController.transform.position, 1f, isPlayerCreatedSound: true);
			_assignedChair.Occupied = false;
		}
		if ((bool)_targetObject)
		{
			_targetObject.Occupied = false;
			character.SetItemIKTargets(null);
			if (_entertainDevice.entertainType == EntertainType.DJ)
			{
				PlayerHelper.GetAnimator().SetBool(PermanentAnimationType.DJ, state: false);
				character.Reset();
				_targetObject.GetComponent<DJBoothMusicPlayer>().Disable();
			}
		}
		else if (_entertainDevice.entertainType == EntertainType.Read)
		{
			PlayerHelper.GetAnimator().SetBool(PermanentAnimationType.ReadingABook, state: false);
			character.RemoveHandObject();
		}
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.EntertainActivity);
		CancelEntertaining();
	}

	private void CancelEntertaining()
	{
		_stateBeforeFinishing = _state;
		_state = PlayerActivityState.Finished;
		_minutesEntertained = 0;
		SaveGameManager.Current.timeEnteredTemporalBoost = TimeHelper.Now();
		((ItemController)_targetObject)?.StopVideoOnScreen();
		if (_entertainDevice.preferSeating)
		{
			Transform isSittingOn = InstanceBehavior<GameManager>.Instance.playerController.Character.isSittingOn;
			if ((bool)isSittingOn && isSittingOn.TryGetComponentInParent<SeatController>(out var component))
			{
				component.OnSittingChanged(isSittingOn, isSitting: false);
			}
			InstanceBehavior<GameManager>.Instance.playerController.Character.Reset();
		}
		if (_reopenItemPanelAfterActivity)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVisibility(visible: true);
			_reopenItemPanelAfterActivity = false;
		}
	}

	private void SetTimeToEntertain(int timeToEntertain)
	{
		_minutesToEntertain = Mathf.FloorToInt((float)timeToEntertain * 60f) / 60;
		_hoursOfHappinessBoost = GetHoursOfHappinessBoost(_minutesToEntertain);
		_entertainDevice.SetDefaultMinutes(_minutesToEntertain);
	}

	private int GetHoursOfHappinessBoost(int minutes)
	{
		return _entertainDevice.balanceConfig.GetBoostHours(minutes);
	}

	private void RunUsingAnimation()
	{
		PlayerHelper.GetAnimator().SetTrigger(_usingAnimation);
		_nextAnimation = Time.time + Random.Range(_entertainDevice.minTimeBetweenAnims, _entertainDevice.maxTimeBetweenAnims) + _usingAnimationLength;
	}

	public LabelInfo GetHeadlineLabel()
	{
		return new LabelInfo("entertain_panel_" + _deviceType + "_headline", InstanceBehavior<GlobalReferences>.Instance.colors.white);
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
		return _minutesToEntertain - _minutesEntertained;
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
		return 100f * (float)_minutesEntertained / (float)_minutesToEntertain;
	}

	public (string, object) GetProgressBarLabel()
	{
		int num = _minutesToEntertain - _minutesEntertained;
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
		return _entertainDevice.balanceConfig.MaxDurationMinutes;
	}

	public float GetCurrentSliderValue()
	{
		return _minutesToEntertain;
	}

	public void OnSliderValueChanged(int value)
	{
		SetTimeToEntertain(value);
	}

	public (string, object) GetSliderInfo()
	{
		int num = Mathf.FloorToInt((float)_minutesToEntertain / 60f);
		int entertainingMinutes = _minutesToEntertain - num * 60;
		object item = new
		{
			entertainingHours = num,
			entertainingMinutes = entertainingMinutes,
			boostPercentage = _happinessBoostPercentage,
			boostHours = _hoursOfHappinessBoost
		};
		return ("entertainui_slider_label_" + _deviceType, item);
	}
}
