using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Extensions;
using GleyTrafficSystem;
using UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class GameSpeedController : MonoBehaviour
{
	public bool isTimeControlDisabled;

	[SerializeField]
	private Button pauseButton;

	[SerializeField]
	private Image pauseOverlays;

	[SerializeField]
	private float pauseFadeSpeed = 4f;

	[SerializeField]
	private float pauseFadeMaxAlpha = 0.1f;

	[SerializeField]
	private Color pauseButtonActiveColor;

	[SerializeField]
	private Color pauseButtonColor;

	[HideInInspector]
	public bool isFastForwarding;

	[HideInInspector]
	public GameSpeed playerGameSpeed;

	private Image _pauseButtonIcon;

	private GameSpeed currentGameSpeed;

	private TweenerCore<float, float, FloatOptions> timeScaleTween;

	private float _fixedTimeStep;

	public bool Paused => currentGameSpeed.paused;

	private void Awake()
	{
		_fixedTimeStep = Time.fixedDeltaTime;
		_pauseButtonIcon = pauseButton.transform.Find("Icon").GetComponent<Image>();
	}

	private void Start()
	{
		playerGameSpeed = new GameSpeed(paused: true, TimeSpeed.Normal);
		GlobalEvents.RegisterOnGameLoadedCallback(Init);
	}

	public void Init()
	{
		playerGameSpeed = new GameSpeed(SaveGameManager.Current?.LastPlayerPause ?? false, TimeSpeed.Normal);
		Reset();
	}

	public void SetPause(bool newPause, bool showOverlay = true)
	{
		Set(new GameSpeed(newPause, currentGameSpeed.speed, showOverlay));
	}

	public void SetPlayerPause(bool newPause, bool showOverlay = true)
	{
		playerGameSpeed = new GameSpeed(newPause, playerGameSpeed.speed, showOverlay);
		if (SaveGameManager.Current != null)
		{
			SaveGameManager.Current.LastPlayerPause = newPause;
		}
		Set(new GameSpeed(newPause, currentGameSpeed.speed, showOverlay));
	}

	public void TogglePause()
	{
		if (!isTimeControlDisabled)
		{
			playerGameSpeed = new GameSpeed(!playerGameSpeed.paused, playerGameSpeed.speed);
			SaveGameManager.Current.LastPlayerPause = playerGameSpeed.paused;
			Set(new GameSpeed(playerGameSpeed.paused, currentGameSpeed.speed));
			UiSoundHelper.Play(Paused ? UiSound.PauseOn : UiSound.PauseOff);
			InstanceBehavior<SfxManager>.Instance.SetSoundSnapshot(InstanceBehavior<UIs>.Instance.gameSpeed.Paused, 0.2f);
		}
	}

	public void DisableTimeControl(bool disabled)
	{
		isTimeControlDisabled = disabled;
	}

	public void Set(GameSpeed newGameSpeed)
	{
		SetPauseOverlay(newGameSpeed);
		if (currentGameSpeed?.paused != newGameSpeed.paused)
		{
			SetPauseState(newGameSpeed);
		}
		if (!newGameSpeed.paused && currentGameSpeed?.speed != newGameSpeed.speed)
		{
			ChangeTimeScale(newGameSpeed.speed);
		}
		Time.fixedDeltaTime = _fixedTimeStep * ((newGameSpeed.speed == TimeSpeed.FastForward) ? ((float)newGameSpeed.speed) : 1f);
		currentGameSpeed = newGameSpeed;
	}

	public void Reset()
	{
		if (playerGameSpeed == currentGameSpeed)
		{
			return;
		}
		if (isFastForwarding)
		{
			Set(new GameSpeed(playerGameSpeed.paused, TimeSpeed.FastForward));
			return;
		}
		if (currentGameSpeed?.paused != playerGameSpeed.paused)
		{
			SetPauseState(playerGameSpeed);
		}
		if (!playerGameSpeed.paused && currentGameSpeed?.speed != playerGameSpeed.speed)
		{
			ChangeTimeScale(playerGameSpeed.speed);
		}
		SetPauseOverlay(playerGameSpeed);
		currentGameSpeed = playerGameSpeed;
		Time.fixedDeltaTime = _fixedTimeStep;
	}

	private void SetPauseState(GameSpeed newGameSpeed)
	{
		try
		{
			ChangeTimeScale((!newGameSpeed.paused) ? newGameSpeed.speed : TimeSpeed.Pause);
			GlobalEvents.onPause?.InvokeSafely(newGameSpeed.paused);
			if ((bool)_pauseButtonIcon)
			{
				_pauseButtonIcon.color = (newGameSpeed.paused ? pauseButtonActiveColor : pauseButtonColor);
				_pauseButtonIcon.rectTransform.localScale = Vector3.one;
			}
		}
		catch (Exception exception)
		{
			Debug.LogException(exception);
		}
	}

	private void ChangeTimeScale(TimeSpeed scale, float tweenDuration = 0.1f)
	{
		NavMesh.pathfindingIterationsPerFrame = Mathf.Max(100 * (int)scale, 100);
		timeScaleTween?.Kill();
		timeScaleTween = DOTween.To(() => Time.timeScale, delegate(float x)
		{
			Time.timeScale = x;
		}, (float)scale, tweenDuration).SetEase(Ease.OutQuint).SetUpdate(isIndependentUpdate: true)
			.SetLink(base.gameObject);
		TrafficManager.Instance.SetPause(scale == TimeSpeed.FastForward || BuildingManager.IsInsideBuilding);
	}

	private void SetPauseOverlay(GameSpeed newGameSpeed)
	{
		if (newGameSpeed.showPauseOverlay)
		{
			pauseOverlays.gameObject.SetActive(newGameSpeed.paused);
			UpdatePauseOverlayColor();
		}
		else
		{
			pauseOverlays.gameObject.SetActive(value: false);
		}
		_pauseButtonIcon.rectTransform.localScale = Vector3.one;
	}

	public void UpdateVisuals()
	{
		GameSpeed gameSpeed = currentGameSpeed;
		if (gameSpeed != null && gameSpeed.paused)
		{
			UpdatePauseOverlayColor();
		}
	}

	private void UpdatePauseOverlayColor()
	{
		Color color = pauseOverlays.color;
		color.a = (Mathf.Sin(Time.unscaledTime * (pauseFadeSpeed / 2f)) + 1f) * 0.5f * pauseFadeMaxAlpha;
		pauseOverlays.color = color;
		float num = Mathf.Sin(Time.unscaledTime * 4f) / 4f;
		_pauseButtonIcon.rectTransform.localScale = new Vector3(1f + num, 1f + num, 1f + num);
	}
}
