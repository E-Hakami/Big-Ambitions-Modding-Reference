using System.Collections;
using BigAmbitions.DayNightCycle;
using BigAmbitions.InputSystem;
using Extensions;
using Helpers;
using IngameDebugConsole;
using Localizor;
using Localizor.LanguageChangeEvent;
using Player;
using Player.HUD.ItemInfoOverlays;
using Player.HUD.ItemWarningIcons;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Timemachine;

public class TimeMachine : MonoBehaviour
{
	private const float MaxDeltaTime = 1f / 15f;

	private const float ConsoleSkipDuration = 1f;

	private const float UiDevSceneSpeed = 800f;

	private const float FullPitchSpeed = 100f;

	private const float MinPitch = 0.2f;

	private const float MaxPitch = 1.8f;

	private const float LastTickOvershootMinutes = 0.001f;

	public Canvas canvas;

	public TextMeshProUGUI timeLabel;

	public TextLocalizationComponent dayLabel;

	public TextLocalizationComponent infoLabel;

	public UnityEvent onTimeMachineStopped = new UnityEvent();

	public AnimationCurve timeSpeedCurve;

	public AudioSource audioSource;

	public Button cancelButton;

	private bool _disableCancel;

	private Timestamp _goal;

	private int _lastDayUpdated = -1;

	private int _lastMinuteUpdated = -1;

	private float _timeDistance;

	private bool _useConstantSpeed;

	public bool isRunning => base.enabled;

	public bool isBlockingUi { get; private set; }

	private void Awake()
	{
		canvas.gameObject.SetActive(value: false);
		base.enabled = false;
	}

	private void OnEnable()
	{
		SetUpKeysLabels();
	}

	private void Update()
	{
		float minutesFromNow = _goal.GetMinutesFromNow();
		float speed = GetSpeed(minutesFromNow);
		audioSource.pitch = Mathf.Clamp(speed / 100f * 1.8f, 0.2f, 1.8f);
		float max = speed * Mathf.Min(1f / 15f, Time.unscaledDeltaTime);
		float deltaTimeWithMultiplier = Mathf.Clamp(minutesFromNow + 0.001f, 0f, max);
		InstanceBehavior<GameManager>.Instance.RunMainGameTick(deltaTimeWithMultiplier);
		UpdateTimeLabel();
		UpdateDayLabel();
		if (minutesFromNow <= 0f)
		{
			StartCoroutine(StopTimeMachineCoroutine());
		}
	}

	private float GetSpeed(float remainingMinutes)
	{
		if (_useConstantSpeed)
		{
			return _timeDistance / 1f;
		}
		if (InstanceBehavior<GameManager>.Instance.IsUIDevScene)
		{
			return 800f;
		}
		float time = 1f - Mathf.Min(1f, Mathf.Max(remainingMinutes / _timeDistance, 0f));
		return timeSpeedCurve.Evaluate(time);
	}

	private void UpdateTimeLabel()
	{
		int num = SaveGameManager.Current.Hour * 60 + (int)SaveGameManager.Current.Minute;
		if (_lastMinuteUpdated != num)
		{
			timeLabel.SetCurrentFormattedTime();
			_lastMinuteUpdated = num;
		}
	}

	private void UpdateDayLabel()
	{
		int day = SaveGameManager.Current.Day;
		if (_lastDayUpdated != day)
		{
			dayLabel.Key = "topbar_date_format";
			dayLabel.Arguments = new
			{
				DayOfWeek = TimeHelper.GetDayOfWeek().GetLocalizeKey(),
				CurrentNumberDay = day
			};
			_lastDayUpdated = day;
		}
	}

	private void SetUpKeysLabels()
	{
		cancelButton.transform.Find("Container").GetLanguageChangeEventByName("Text (TMP)").Suffix = PlayerAction.Cancel.AsSuffix();
	}

	[ConsoleMethod("skiptime", "Skips time ex. [skiptime 1d2h50m]", new string[] { })]
	public static void SkipTime(string timeToSkip)
	{
		SkipTime(timeToSkip, useConstantSpeed: false);
	}

	[ConsoleMethod("skiptime", "Skips time, optionally at constant speed ex. [skiptime 7d true]", new string[] { })]
	public static void SkipTime(string timeToSkip, bool useConstantSpeed)
	{
		if (TimeHelper.TryParseTimeParameter(timeToSkip, out var timestamp))
		{
			InstanceBehavior<UIs>.Instance.timeMachine.StartTimeMachine(timestamp, disableCancel: false, null, showBlur: true, useConstantSpeed);
		}
		else
		{
			Debug.LogWarning("Cannot parse time parameter: " + timeToSkip + ". Use format: 2w1d2h50m or 2d5h or 5d or 10m");
		}
	}

	[ContextMenu("StartTimeMachine")]
	public void StartTimeMachine(Timestamp goal)
	{
		StartTimeMachine(goal, disableCancel: false);
	}

	public void StartTimeMachine(Timestamp goal, bool disableCancel, string infoKey = null, bool showBlur = true, bool useConstantSpeed = false)
	{
		_disableCancel = disableCancel;
		_useConstantSpeed = useConstantSpeed;
		audioSource.enabled = true;
		audioSource.Play();
		audioSource.pitch = 0.1f;
		_goal = goal;
		_lastMinuteUpdated = -1;
		if (infoKey == null)
		{
			infoLabel.gameObject.SetActive(value: false);
		}
		else
		{
			infoLabel.SetData(infoKey.Localize());
			infoLabel.gameObject.SetActive(value: true);
		}
		_timeDistance = goal.GetMinutesFromNow();
		InstanceBehavior<UIs>.Instance.gameSpeed.Set(new GameSpeed(paused: true, TimeSpeed.Pause, showPauseOverlay: false));
		InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: true);
		base.enabled = true;
		isBlockingUi = true;
		if (showBlur)
		{
			BlurEffect.Enable();
		}
		ToggleUiVisibility(show: false);
		canvas.gameObject.SetActive(value: true);
		GlobalEvents.onTimeMachineStarted?.Invoke();
		cancelButton.gameObject.SetActive(!disableCancel);
	}

	public void StopTimeMachine(float endDelay = 1f)
	{
		if (isRunning)
		{
			StartCoroutine(StopTimeMachineCoroutine(endDelay));
		}
	}

	public IEnumerator StopTimeMachineCoroutine(float endDelay = 1f)
	{
		base.enabled = false;
		if (endDelay > 0f)
		{
			yield return new WaitForSecondsRealtime(endDelay);
		}
		BlurEffect.Disable();
		ToggleUiVisibility(show: true);
		canvas.gameObject.SetActive(value: false);
		InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: false);
		InstanceBehavior<UIs>.Instance.gameSpeed.Reset();
		audioSource.enabled = false;
		GlobalEvents.onTimeMachineEnded?.Invoke();
		GameEvent.Invoke("ba:gameevent_timemachineended");
		onTimeMachineStopped.Invoke();
		isBlockingUi = false;
	}

	private void ToggleUiVisibility(bool show)
	{
		InstanceBehavior<UIs>.Instance.playerHUD.gameObject.SetActive(show);
		InstanceBehavior<UIs>.Instance.smartphoneUI.gameObject.SetActive(show);
		InstanceBehavior<UIs>.Instance.tasksUI.ChangeVisibility(show);
		InstanceBehavior<UIs>.Instance.topBar.gameObject.SetActive(show);
		InstanceBehavior<UIs>.Instance.buildingResume.Close();
		InstanceBehavior<CityManager>.Instance.cityMap.gameObject.SetActive(show);
		InstanceBehavior<OverlayManager>.Instance.gameObject.SetActive(show);
		InstanceBehavior<ItemWarningIconManager>.Instance.gameObject.SetActive(show);
	}

	public void CancelTimeMachine()
	{
		if (!_disableCancel)
		{
			StartCoroutine(StopTimeMachineCoroutine(0f));
		}
	}
}
