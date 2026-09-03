using System;
using System.Linq;
using BigAmbitions.DayNightCycle;
using BigAmbitions.InputSystem;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Extensions;
using GleyTrafficSystem;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using Player.HUD.ItemInfoOverlays;
using PlayerActivity.Activities.Paid;
using PlayerActivity.Activities.Rest;
using UI;
using UI.Elements;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerActivity;

public class PlayerActivityUI : MonoBehaviour
{
	private const PlayerAction TimeMachineAction = PlayerAction.SpecialInteract;

	private const PlayerAction FastForwardAction = PlayerAction.SecondaryInteract;

	[SerializeField]
	private CanvasGroup canvasGroup;

	[Header("Progression")]
	[SerializeField]
	private TextLocalizationComponent headlineLabel;

	[Header("Progression")]
	[SerializeField]
	private GameObject progressionGameObject;

	[SerializeField]
	private ProgressBar progressBar;

	[SerializeField]
	private TextLocalizationComponent progressBarLabel;

	[Header("Labels")]
	[SerializeField]
	private GameObject labelsGameObject;

	[SerializeField]
	private TextLocalizationComponent labelTemplate;

	[Header("Slider")]
	[SerializeField]
	private GameObject sliderGameObject;

	[SerializeField]
	private Slider slider;

	[SerializeField]
	private TextLocalizationComponent sliderLabel;

	[SerializeField]
	private TextLocalizationComponent sliderDecreaseKeyLabel;

	[SerializeField]
	private TextLocalizationComponent sliderIncreaseKeyLabel;

	[Header("Speed Options")]
	[SerializeField]
	private GameObject speedOptionsGameObject;

	[SerializeField]
	private Button timeMachineButton;

	[SerializeField]
	private Toggle fastForwardToggle;

	[Header("Buttons")]
	[SerializeField]
	private GameObject buttonsGameObject;

	[SerializeField]
	private Button buttonTemplate;

	private bool _isRunningTimeMachine;

	private int _minutesInLastCheck;

	private int _minutesOnTimeMachineStart;

	private bool _allowCancelMoveTowards;

	private bool _hiddenHandContentForActivity;

	private bool _isPanelVisible;

	private PlayerActivityState _lastActivityState;

	public bool ShouldBePaused
	{
		get
		{
			if (IsPanelOpen)
			{
				return GetCurrentActivity.GetState() == PlayerActivityState.NotStarted;
			}
			return false;
		}
	}

	public IPlayerActivity GetCurrentActivity { get; private set; }

	public Type GetCurrentActivityType => GetCurrentActivity?.GetType();

	public PlayerActivityState GetCurrentActivityState => GetCurrentActivity?.GetState() ?? PlayerActivityState.NotStarted;

	public static bool IsPanelOpen
	{
		get
		{
			if (InstanceBehavior<UIs>.Instance?.playerActivityUI != null)
			{
				return InstanceBehavior<UIs>.Instance.playerActivityUI.gameObject.activeSelf;
			}
			return false;
		}
	}

	public static bool IsMovingTowardsActivity
	{
		get
		{
			if (InstanceBehavior<UIs>.Instance?.playerActivityUI != null)
			{
				return InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivityState == PlayerActivityState.MovingTowardsActivity;
			}
			return false;
		}
	}

	public static bool IsWaiting
	{
		get
		{
			if (InstanceBehavior<UIs>.Instance?.playerActivityUI != null)
			{
				return InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivityState == PlayerActivityState.Waiting;
			}
			return false;
		}
	}

	public static bool HasNavigationDisabled()
	{
		if (IsPanelOpen)
		{
			return InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivityState != PlayerActivityState.NotStarted;
		}
		return false;
	}

	public static bool CanStartActivity(bool showNotification = true, bool allowUsingVehicle = false)
	{
		if (!PlayerHelper.IsUsingVehicle | allowUsingVehicle)
		{
			return true;
		}
		if (showNotification)
		{
			Notifications.ShowError("notification_need_empty_hands_to_interact");
		}
		return false;
	}

	public void CancelActivityMovement()
	{
		IPlayerActivity getCurrentActivity = GetCurrentActivity;
		if (_allowCancelMoveTowards && getCurrentActivity.GetState() == PlayerActivityState.MovingTowardsActivity)
		{
			InstanceBehavior<GameManager>.Instance.playerController.SetNewDestination(PlayerHelper.GetPosition(), showParticleEffect: false);
			InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: false);
			canvasGroup.alpha = 1f;
			getCurrentActivity.ChangeState(PlayerActivityState.NotStarted);
			RestoreHandContentHiddenForActivity();
			_allowCancelMoveTowards = false;
			GetCurrentActivity = null;
			base.gameObject.SetActive(value: false);
		}
	}

	public static void Show(IPlayerActivityType activityType, EntityController attachedEntity = null)
	{
		Show(activityType.CreateActivity(attachedEntity), CanStartWhileUsingVehicle(activityType));
	}

	public static void Show(IPlayerActivity playerActivity, EntityController attachedEntity = null)
	{
		Show(playerActivity, allowUsingVehicle: false);
	}

	private static void Show(IPlayerActivity playerActivity, bool allowUsingVehicle)
	{
		if (!IsPanelOpen && CanStartActivity(showNotification: true, allowUsingVehicle))
		{
			if (playerActivity.RequiresEnergy() && SaveGameManager.Current.Energy <= 0f)
			{
				Notifications.ShowInsufficientEnergy();
			}
			else
			{
				InstanceBehavior<UIs>.Instance.playerActivityUI.ShowActivity(playerActivity);
			}
		}
	}

	private static bool CanStartWhileUsingVehicle(IPlayerActivityType activityType)
	{
		if (activityType is SleepEnvironment sleepEnvironment)
		{
			return sleepEnvironment.SleepEnvironmentType == SleepEnvironmentType.Car;
		}
		return false;
	}

	public static void SetActivityWithoutUI(IPlayerActivity activity)
	{
		GameManager.SetPreventAutoSave(activity?.BlocksAutoSave() ?? false);
		InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivity = activity;
	}

	public void ForceTimeMachine()
	{
		OnTimeMachinePress();
	}

	private void ShowActivity(IPlayerActivity activity)
	{
		InstanceBehavior<OverlayManager>.Instance.HideOverlays();
		Item itemInHands = PlayerHelper.ItemInHands;
		if ((object)itemInHands != null && itemInHands.HasTag(TagRef.Itemtag.isbag))
		{
			InstanceBehavior<GameManager>.Instance.playerController.Character.HideHandContent();
			_hiddenHandContentForActivity = true;
		}
		GetCurrentActivity = activity;
		UpdateActivityDisplay();
		InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: true);
		InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: true, showOverlay: false);
		base.gameObject.SetActive(value: true);
		HidePanel(hide: false);
	}

	private void Update()
	{
		PlayerActivityState state = GetCurrentActivity.GetState();
		switch (state)
		{
		case PlayerActivityState.MovingTowardsActivity:
			if (_lastActivityState != PlayerActivityState.MovingTowardsActivity)
			{
				OnMoveTowardsActivity();
			}
			break;
		case PlayerActivityState.Started:
			OnActivityStarted();
			UpdateActivityDisplay();
			break;
		case PlayerActivityState.Running:
			OnActivityRunning();
			break;
		case PlayerActivityState.Finished:
			OnActivityFinished();
			break;
		}
		_lastActivityState = state;
	}

	private void HidePanel(bool hide)
	{
		canvasGroup.alpha = ((!hide) ? 1 : 0);
		canvasGroup.interactable = !hide;
		canvasGroup.blocksRaycasts = !hide;
		_isPanelVisible = !hide;
	}

	private void OnMoveTowardsActivity()
	{
		HidePanel(hide: true);
		InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: false);
		CoroutineUtility.RunAfterFrameDelay(delegate
		{
			if (GetCurrentActivityState == PlayerActivityState.MovingTowardsActivity)
			{
				_allowCancelMoveTowards = true;
				InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: false);
				if (InstanceBehavior<UIs>.Instance.gameSpeed.playerGameSpeed.paused)
				{
					InstanceBehavior<UIs>.Instance.gameSpeed.TogglePause();
				}
			}
		}, 1);
	}

	private void OnActivityStarted()
	{
		HidePanel(hide: false);
		GetCurrentActivity.ChangeState(PlayerActivityState.Running);
		_minutesInLastCheck = (int)TimeHelper.NowInMinutes();
		InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: false);
		_allowCancelMoveTowards = false;
		GameManager.SetPreventAutoSave(GetCurrentActivity.BlocksAutoSave());
	}

	private void OnActivityRunning()
	{
		int num = (int)TimeHelper.NowInMinutes();
		int num2 = num - _minutesInLastCheck;
		_minutesInLastCheck = num;
		if (num2 != 0)
		{
			GetCurrentActivity.Perform(num2);
			SetUpProgressBar();
		}
	}

	private void OnActivityFinished()
	{
		HidePanel(hide: false);
		base.gameObject.SetActive(value: false);
		if (InstanceBehavior<UIs>.Instance.gameSpeed.isFastForwarding)
		{
			InstanceBehavior<UIs>.Instance.gameSpeed.isFastForwarding = false;
			InstanceBehavior<UIs>.Instance.gameSpeed.Reset();
		}
		else
		{
			InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: false);
			if (GetCurrentActivity.GetStateBeforeFinishing() == PlayerActivityState.NotStarted)
			{
				InstanceBehavior<UIs>.Instance.gameSpeed.Reset();
			}
		}
		RestoreHandContentHiddenForActivity();
		_allowCancelMoveTowards = false;
		MouseController.currentTargetEntity = null;
		GameManager.SetPreventAutoSave(newPreventAutosaveValue: false);
		GetCurrentActivity = null;
		GameEvent.Invoke("ba:gameevent_activityfinished");
	}

	private void Start()
	{
		SetUpListeners();
		SetUpKeysLabels();
		GlobalEvents.onBindingsChanged = (Action)Delegate.Combine(GlobalEvents.onBindingsChanged, (Action)delegate
		{
			SetUpKeysLabels();
			UpdateActivityDisplay();
		});
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool toggled)
		{
			if (IsPanelOpen && _isPanelVisible)
			{
				canvasGroup.alpha = ((!toggled) ? 1 : 0);
				canvasGroup.interactable = !toggled;
				canvasGroup.blocksRaycasts = !toggled;
			}
		});
	}

	public void OnPlayerActionPressed(PlayerAction playerAction)
	{
		switch (playerAction)
		{
		case PlayerAction.SpecialInteract:
			if (GetCurrentActivity.HasTimeMachine())
			{
				playerAction.Reset();
				OnTimeMachinePress();
				return;
			}
			break;
		case PlayerAction.SecondaryInteract:
			if (GetCurrentActivity.HasFastForward() && BuildingManager.IsInsideBuilding)
			{
				playerAction.Reset();
				fastForwardToggle.isOn = !fastForwardToggle.isOn;
				return;
			}
			break;
		}
		ButtonInfo buttonInfo = GetCurrentActivity.GetButtons()?.FirstOrDefault((ButtonInfo x) => x.playerAction == playerAction);
		if (buttonInfo != null)
		{
			playerAction.Reset();
			buttonInfo.onClick();
		}
	}

	public void ChangeSliderValue(float amountToAdd, bool exactMinutes = false)
	{
		if (GetCurrentActivity.HasSlider())
		{
			if (!exactMinutes)
			{
				amountToAdd *= (float)GetCurrentActivity.GetMaxSliderValue() / 4f;
			}
			slider.value += amountToAdd;
		}
	}

	private void SetUpListeners()
	{
		slider.onValueChanged.AddListener(OnSliderValueChange);
		timeMachineButton.onClick.AddListener(OnTimeMachinePress);
		fastForwardToggle.onValueChanged.AddListener(OnFastForwardToggle);
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Combine(GlobalEvents.onTimeMachineEnded, new Action(OnTimeMachineEnd));
		InstanceBehavior<GameManager>.Instance.playerController.OnPlayerOutOfEnergy.AddListener(OnPlayerOutOfEnergy);
		InstanceBehavior<GameManager>.Instance.playerController.PlayerChangedNavigation.AddListener(CancelActivityMovement);
	}

	private void SetUpKeysLabels()
	{
		timeMachineButton.transform.GetLanguageChangeEventByName("Label").Suffix = PlayerAction.SpecialInteract.AsSuffix();
		fastForwardToggle.transform.parent.GetLanguageChangeEventByName("Label").Suffix = PlayerAction.SecondaryInteract.AsSuffix();
		sliderDecreaseKeyLabel.Suffix = PlayerAction.SliderLeft.AsSuffix();
		sliderIncreaseKeyLabel.Suffix = PlayerAction.SliderRight.AsSuffix();
	}

	private void OnTimeMachineEnd()
	{
		if (IsPanelOpen)
		{
			_isRunningTimeMachine = false;
			FinishActivityAfterTimeSkip();
		}
	}

	private void FinishActivityAfterTimeSkip()
	{
		int minutes = (int)TimeHelper.NowInMinutes() - _minutesOnTimeMachineStart;
		GetCurrentActivity.Perform(minutes);
		if (GetCurrentActivity.GetState() != PlayerActivityState.Finished)
		{
			GetCurrentActivity.Finish();
		}
		OnActivityFinished();
	}

	private void OnPlayerOutOfEnergy()
	{
		IPlayerActivity getCurrentActivity = GetCurrentActivity;
		if (IsPanelOpen && !(getCurrentActivity is SleepActivity) && !(getCurrentActivity is RestActivity) && (!(getCurrentActivity is PaidActivity paidActivity) || paidActivity.paidActivityEnvironment.CanBeStoppedInTheMiddle))
		{
			CancelActivity();
			Notifications.ShowInsufficientEnergy();
		}
	}

	public void CancelActivity()
	{
		if (InstanceBehavior<UIs>.Instance.timeMachine.isRunning)
		{
			InstanceBehavior<UIs>.Instance.timeMachine.CancelTimeMachine();
		}
		else
		{
			GetCurrentActivity.Finish();
		}
	}

	private void RestoreHandContentHiddenForActivity()
	{
		if (_hiddenHandContentForActivity)
		{
			_hiddenHandContentForActivity = false;
			if (PlayerHelper.ItemInstanceInHands != null)
			{
				InstanceBehavior<GameManager>.Instance.playerController.Character.ShowHandContent();
				InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetItemInstance(PlayerHelper.ItemInstanceInHands, spawnIntoPlayerHands: false, changeOnlyVisibility: true);
			}
		}
	}

	public void UpdateActivityDisplay()
	{
		if (GetCurrentActivity != null)
		{
			SetUpHeadline();
			SetUpProgressBar();
			SetUpLabels();
			SetUpSlider();
			SetUpSpeedOptions();
			SetUpButtons();
		}
	}

	private void SetUpHeadline()
	{
		LabelInfo labelInfo = GetCurrentActivity.GetHeadlineLabel();
		SetLabel(labelInfo, headlineLabel);
	}

	private void SetUpProgressBar()
	{
		bool flag = GetCurrentActivity.HasProgressBar();
		progressionGameObject.SetActive(flag);
		if (flag)
		{
			progressBar.SetValue(GetCurrentActivity.GetProgressBarPercentageValue());
			var (key, arguments) = GetCurrentActivity.GetProgressBarLabel();
			progressBarLabel.SetData(key.Localize(arguments));
		}
	}

	private void SetUpLabels()
	{
		labelTemplate.transform.ResetTemplate();
		LabelInfo[] labels = GetCurrentActivity.GetLabels();
		labelsGameObject.SetActive(labels != null);
		if (labels != null)
		{
			LabelInfo[] array = labels;
			foreach (LabelInfo labelInfo in array)
			{
				TextLocalizationComponent textLocalizationComponent = UnityEngine.Object.Instantiate(labelTemplate, labelTemplate.transform.parent);
				SetLabel(labelInfo, textLocalizationComponent);
				textLocalizationComponent.gameObject.SetActive(value: true);
			}
		}
	}

	private void SetUpSlider()
	{
		int minSliderValue = GetCurrentActivity.GetMinSliderValue();
		int maxSliderValue = GetCurrentActivity.GetMaxSliderValue();
		float currentSliderValue = GetCurrentActivity.GetCurrentSliderValue();
		slider.maxValue = maxSliderValue;
		slider.minValue = minSliderValue;
		slider.value = currentSliderValue;
		OnSliderValueChange(slider.value);
		sliderGameObject.SetActive(GetCurrentActivity.HasSlider());
	}

	private void SetUpSpeedOptions()
	{
		speedOptionsGameObject.SetActive(GetCurrentActivity.HasTimeMachine() || GetCurrentActivity.HasFastForward());
		timeMachineButton.gameObject.SetActive(GetCurrentActivity.HasTimeMachine());
		bool active = GetCurrentActivity.HasFastForward() && BuildingManager.IsInsideBuilding;
		fastForwardToggle.transform.parent.gameObject.SetActive(active);
		fastForwardToggle.isOn = false;
	}

	private void SetUpButtons()
	{
		buttonTemplate.transform.ResetTemplate();
		ButtonInfo[] buttons = GetCurrentActivity.GetButtons();
		buttonsGameObject.SetActive(buttons != null);
		if (buttons != null)
		{
			ButtonInfo[] array = buttons;
			foreach (ButtonInfo button in array)
			{
				SetButton(button);
			}
		}
	}

	private void SetLabel(LabelInfo labelInfo, TextLocalizationComponent label)
	{
		if (labelInfo.localize)
		{
			label.SetData(labelInfo.key.Localize(labelInfo.arguments));
		}
		else
		{
			label.SetValue(labelInfo.key);
		}
		label.TextContainer.color = labelInfo.color;
	}

	private void SetButton(ButtonInfo buttonInfo)
	{
		Button button = UnityEngine.Object.Instantiate(buttonTemplate, buttonTemplate.transform.parent);
		button.name = buttonInfo.name;
		TextLocalizationComponent componentInChildren = button.GetComponentInChildren<TextLocalizationComponent>();
		componentInChildren.Key = buttonInfo.key;
		componentInChildren.Suffix = buttonInfo.playerAction.AsSuffix();
		button.GetComponent<Image>().sprite = GlobalReferences.GetButtonImageByName(buttonInfo.color);
		button.onClick.AddListener(buttonInfo.onClick.Invoke);
		button.gameObject.SetActive(value: true);
	}

	private void OnSliderValueChange(float value)
	{
		int value2 = Mathf.RoundToInt(value);
		GetCurrentActivity.OnSliderValueChanged(value2);
		var (key, arguments) = GetCurrentActivity.GetSliderInfo();
		sliderLabel.SetData(key.Localize(arguments));
	}

	private void OnFastForwardToggle(bool toggled)
	{
		if (GetCurrentActivity != null && GetCurrentActivity.HasFastForward())
		{
			InstanceBehavior<UIs>.Instance.gameSpeed.isFastForwarding = toggled;
			if (toggled)
			{
				InstanceBehavior<UIs>.Instance.gameSpeed.Set(new GameSpeed(paused: false, TimeSpeed.FastForward));
			}
			else
			{
				InstanceBehavior<UIs>.Instance.gameSpeed.Reset();
			}
		}
	}

	private void OnTimeMachinePress()
	{
		if (!_isRunningTimeMachine && GetCurrentActivity != null && GetCurrentActivity.HasTimeMachine())
		{
			_minutesOnTimeMachineStart = (int)TimeHelper.NowInMinutes();
			Timestamp timeMachineGoal = GetCurrentActivity.GetTimeMachineGoal();
			if (timeMachineGoal.IsInThePast())
			{
				FinishActivityAfterTimeSkip();
				return;
			}
			TrafficManager.Instance.ClearTraffic();
			InstanceBehavior<UIs>.Instance.timeMachine.StartTimeMachine(timeMachineGoal);
			_isRunningTimeMachine = true;
		}
	}
}
