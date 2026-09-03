using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.DayNightCycle;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using UI;
using UI.Elements;
using UI.ItemPanel;
using UI.Notification;
using UnityEngine;

namespace PlayerActivity;

public class StudyActivity : IPlayerActivity
{
	private const float MinTimeBetweenAnimations = 5f;

	private const float MaxTimeBetweenAnimations = 10f;

	private readonly StudyDiploma _studyDiploma;

	private readonly EntityController _educationDoor;

	private readonly DiplomaData _diplomaData;

	private readonly Diploma _diploma;

	private PlayerActivityState _state;

	private PlayerActivityState _stateBeforeFinishing;

	private int _minutesToStudy;

	private int _minutesStudied;

	private Timestamp _finishTime;

	private float _nextAnimation;

	private bool _isRunningAnimation;

	private bool _reopenItemPanelAfterActivity;

	private LabelInfo StudyingUntilLabel => new LabelInfo("studyui_studying_until", new
	{
		time = _finishTime.GetFormattedTime()
	}, InstanceBehavior<GlobalReferences>.Instance.colors.white);

	private LabelInfo PricePerHourLabel => new LabelInfo("studyui_diploma_price", new
	{
		pricePerHour = _diplomaData.pricePerHour.ToShortCurrencyFormat()
	}, InstanceBehavior<GlobalReferences>.Instance.colors.white);

	private ButtonInfo CancelButton => new ButtonInfo("CancelStudy", "common_cancel", "gray", CancelStudying, PlayerAction.Cancel);

	private ButtonInfo StartButton => new ButtonInfo("StartStudy", "studypanel_start_studying", "blue", StartStudying, PlayerAction.Confirm);

	private ButtonInfo StopButton => new ButtonInfo("StopStudy", "studyui_stop_button", "gray", Finish, PlayerAction.Cancel);

	public bool RequiresEnergy()
	{
		return true;
	}

	public StudyActivity(StudyDiploma studyDiploma, EntityController attachedEntity)
	{
		_state = PlayerActivityState.NotStarted;
		_studyDiploma = studyDiploma;
		_diplomaData = EducationHelper.GetDiplomaData(_studyDiploma.diplomaName);
		_diploma = EducationHelper.GetDiploma(_studyDiploma.diplomaName);
		_educationDoor = attachedEntity;
		SetTimeToStudy(0);
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
		if (state == PlayerActivityState.Running)
		{
			_minutesStudied = _minutesToStudy - Mathf.Max(GetRemainingMinutesForTimeMachine(), 0);
		}
	}

	private void StartStudying()
	{
		int num = Mathf.CeilToInt((float)_minutesToStudy / 60f * (float)_diplomaData.pricePerHour);
		if (_diplomaData.requiredDiploma != DiplomaName.Undefined && !EducationHelper.HasCompletedDiploma(_diplomaData.requiredDiploma))
		{
			ShowDiplomaRequiredNotification();
			return;
		}
		if (SaveGameManager.Current.Money < (float)num)
		{
			Notifications.ShowInsufficientMoney();
			return;
		}
		_state = PlayerActivityState.MovingTowardsActivity;
		_finishTime = TimeHelper.GetTimeInHours(0, _minutesToStudy);
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if (_state == PlayerActivityState.MovingTowardsActivity)
			{
				if (!InstanceBehavior<GameManager>.Instance.playerController.ExistsRoute(_educationDoor, showErrorNotification: true))
				{
					CancelStudying();
				}
				else
				{
					InstanceBehavior<GameManager>.Instance.playerController.SetGoal(_educationDoor, delegate
					{
						if (_state == PlayerActivityState.MovingTowardsActivity)
						{
							_state = PlayerActivityState.Started;
							if (Random.Range(0f, 1f) < 0.5f)
							{
								DoTypingAnimation();
							}
							else
							{
								_nextAnimation = Time.time + Random.Range(0f, 10f);
							}
							_studyDiploma.computer?.PlayVideoOnScreen(VideoClipData.VideoType.Work);
							InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.StudyActivity);
							InstanceBehavior<GameManager>.Instance.playerController.Character.SitOnChair(_studyDiploma.chair);
							EnergyHelper.AddEnergySpender("Study", EnergyConsumption.Low);
							if (ItemPanelUI.IsVisible && !PlayerHelper.IsHoldingBag)
							{
								InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVisibility(visible: false);
								_reopenItemPanelAfterActivity = true;
							}
						}
					});
				}
			}
		});
	}

	private void ShowDiplomaRequiredNotification()
	{
		Dictionary<string, string> notificationData = new Dictionary<string, string> { 
		{
			"name",
			_diplomaData.requiredDiploma.GetLocalization()
		} };
		Notifications.Show(NotificationType.Error, "study_activity_notification_course_required", notificationData);
	}

	public void Perform(int minutes)
	{
		_minutesStudied = Mathf.Min(_minutesStudied + minutes, _minutesToStudy);
		if (_finishTime.IsInThePast())
		{
			Finish();
		}
		else if (_nextAnimation <= Time.time)
		{
			DoTypingAnimation();
		}
	}

	public void Finish()
	{
		if (_state == PlayerActivityState.MovingTowardsActivity)
		{
			InstanceBehavior<GameManager>.Instance.playerController.ResetNavigation();
		}
		EnergyHelper.RemoveEnergySpender("Study");
		InstanceBehavior<GameManager>.Instance.playerController.Character.Reset();
		InstanceBehavior<GameManager>.Instance.playerController.Character.navmeshAgent.Warp(_studyDiploma.exitPosition.position);
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.StudyActivity);
		PlayerHelper.GetAnimator().ResetTrigger(AnimationType.UsingDesktopComputer.ToStringFast());
		if (_minutesStudied != 0)
		{
			int num = Mathf.CeilToInt((float)_minutesStudied / 60f * (float)_diplomaData.pricePerHour);
			int num2 = Mathf.FloorToInt((float)_minutesStudied / 60f);
			int num3 = _minutesStudied - num2 * 60;
			Dictionary<string, string> data = new Dictionary<string, string>
			{
				{
					"diplomaName",
					_diploma.name.GetLocalization()
				},
				{
					"hours",
					num2.ToString()
				},
				{
					"minutes",
					num3.ToString()
				}
			};
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_tuitionfee", data);
			transactionInfo.SetTaxDeductibleName("ba:businesstype_school");
			GameManager.ChangeMoneySafe(-num, transactionInfo, null, null, force: true);
			_diploma.minutesStudied += _minutesStudied;
			if (_diploma.minutesStudied >= _diplomaData.requiredMinutes)
			{
				_diploma.minutesStudied = _diplomaData.requiredMinutes;
				_diploma.completed = true;
				GameEvent.Invoke("ba:gameevent_diplomagranted");
			}
		}
		CancelStudying();
	}

	private void CancelStudying()
	{
		_stateBeforeFinishing = _state;
		_state = PlayerActivityState.Finished;
		_minutesStudied = 0;
		_studyDiploma.computer?.StopVideoOnScreen();
		if (_reopenItemPanelAfterActivity)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetVisibility(visible: true);
			_reopenItemPanelAfterActivity = false;
		}
	}

	private void SetTimeToStudy(int timeToStudy)
	{
		_minutesToStudy = Mathf.FloorToInt((float)timeToStudy * 60f) / 60;
	}

	private int MinutesUntilSchoolClosing()
	{
		OpeningHourSlot openingHourSlot = BuildingHelper.GetTodaySchedule(InstanceBehavior<BuildingManager>.Instance.buildingRegistration).openingHourSlots.FirstOrDefault();
		if (openingHourSlot == null)
		{
			return 0;
		}
		Timestamp timestamp = TimeHelper.Now();
		timestamp.Hour = openingHourSlot.endingHour;
		timestamp.Minute = 0f;
		if (timestamp.IsInThePast())
		{
			return 0;
		}
		return Mathf.CeilToInt(timestamp.GetMinutesFromNow());
	}

	private void DoTypingAnimation()
	{
		Animator animator = PlayerHelper.GetAnimator();
		float num = animator.RunAnimationLength(AnimationType.UsingDesktopComputer);
		animator.SetTrigger(AnimationType.UsingDesktopComputer);
		_nextAnimation = Time.time + Random.Range(5f, 10f) + num;
	}

	public LabelInfo GetHeadlineLabel()
	{
		return new LabelInfo(_diploma.name.GetLocalizeKey(), InstanceBehavior<GlobalReferences>.Instance.colors.white);
	}

	public LabelInfo[] GetLabels()
	{
		if (_state == PlayerActivityState.Running)
		{
			return new LabelInfo[1] { StudyingUntilLabel };
		}
		return new LabelInfo[1] { PricePerHourLabel };
	}

	public ButtonInfo[] GetButtons()
	{
		return _state switch
		{
			PlayerActivityState.NotStarted => (!_diploma.completed) ? new ButtonInfo[2] { CancelButton, StartButton } : new ButtonInfo[1] { CancelButton }, 
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
		return Mathf.CeilToInt(_finishTime.GetMinutesFromNow());
	}

	public Timestamp GetTimeMachineGoal()
	{
		return _finishTime;
	}

	public bool HasFastForward()
	{
		return _state == PlayerActivityState.Running;
	}

	public bool HasProgressBar()
	{
		return true;
	}

	public float GetProgressBarPercentageValue()
	{
		return 100f * (float)(_diploma.minutesStudied + _minutesStudied) / (float)_diplomaData.requiredMinutes;
	}

	public (string, object) GetProgressBarLabel()
	{
		int num = _diplomaData.requiredMinutes - (_diploma.minutesStudied + _minutesStudied);
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
			return !_diploma.completed;
		}
		return false;
	}

	public int GetMinSliderValue()
	{
		return 1;
	}

	public int GetMaxSliderValue()
	{
		return Mathf.Min(_diplomaData.requiredMinutes - _diploma.minutesStudied, MinutesUntilSchoolClosing());
	}

	public float GetCurrentSliderValue()
	{
		return _minutesToStudy;
	}

	public void OnSliderValueChanged(int value)
	{
		SetTimeToStudy(value);
	}

	public (string, object) GetSliderInfo()
	{
		int num = Mathf.FloorToInt((float)_minutesToStudy / 60f);
		int num2 = _minutesToStudy - num * 60;
		Timestamp timeInHours = TimeHelper.GetTimeInHours(num, num2);
		object item = new
		{
			studyHours = num,
			studyMinutes = num2,
			time = timeInHours.GetFormattedTime()
		};
		return ("studyui_slider_label", item);
	}
}
