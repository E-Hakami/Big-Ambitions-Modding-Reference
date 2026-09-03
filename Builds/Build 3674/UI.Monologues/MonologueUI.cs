using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Localizor.LanguageChangeEvent;
using Player.Sound.Radio;
using Scenes.MainMenu;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

namespace UI.Monologues;

[RequireComponent(typeof(AudioSource))]
public class MonologueUI : MonoBehaviour
{
	private struct MonologueEntry
	{
		public string messageKey;

		public AudioClip audioClip;

		public Sprite sprite;

		public Action<string> onMonologueFinished;
	}

	[SerializeField]
	private RectTransform monologuePosition;

	[SerializeField]
	private RectTransform monologuePanel;

	[SerializeField]
	private TextLocalizationComponent monologueText;

	[SerializeField]
	private Image monologueImage;

	[SerializeField]
	private AudioMixer audioMixer;

	private AudioSource _audioSource;

	private bool _isPaused;

	private Transform _tasksGroup;

	private readonly Queue<MonologueEntry> _monologueQueue = new Queue<MonologueEntry>();

	private Coroutine _queueCoroutine;

	private Coroutine _showMonologueCoroutine;

	private Coroutine _hideMonologueCoroutine;

	private bool _isUp;

	private bool _closeMonologue;

	public bool IsUp => monologuePanel.gameObject.activeSelf;

	protected void Awake()
	{
		_audioSource = GetComponent<AudioSource>();
		GlobalEvents.onPause = (Action<bool>)Delegate.Combine(GlobalEvents.onPause, new Action<bool>(OnPauseMonologue));
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, new Action<bool>(OnCityMapToggleMonologue));
		GlobalEvents.onTimeMachineStarted = (Action)Delegate.Combine(GlobalEvents.onTimeMachineStarted, new Action(OnTimeMachineStartedMonologue));
		InstanceBehavior<UIs>.Instance.timeMachine.onTimeMachineStopped.AddListener(OnTimeMachineStoppedMonologue);
		InstantClose();
	}

	private void OnDestroy()
	{
		GlobalEvents.onPause = (Action<bool>)Delegate.Remove(GlobalEvents.onPause, new Action<bool>(OnPauseMonologue));
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Remove(GlobalEvents.onCityMapToggle, new Action<bool>(OnCityMapToggleMonologue));
		GlobalEvents.onTimeMachineStarted = (Action)Delegate.Remove(GlobalEvents.onTimeMachineStarted, new Action(OnTimeMachineStartedMonologue));
		if (InstanceBehavior<UIs>.Instance != null && InstanceBehavior<UIs>.Instance.timeMachine != null)
		{
			InstanceBehavior<UIs>.Instance.timeMachine.onTimeMachineStopped.RemoveListener(OnTimeMachineStoppedMonologue);
		}
	}

	private void OnPauseMonologue(bool paused)
	{
		PauseMonologue(paused, hideDialog: false);
	}

	private void OnCityMapToggleMonologue(bool paused)
	{
		PauseMonologue(paused, hideDialog: true);
	}

	private void OnTimeMachineStartedMonologue()
	{
		PauseMonologue(pause: true, hideDialog: true);
	}

	private void OnTimeMachineStoppedMonologue()
	{
		PauseMonologue(pause: false, hideDialog: true);
	}

	public void PauseMonologue(bool pause, bool hideDialog)
	{
		if (pause)
		{
			_audioSource?.Pause();
		}
		else
		{
			_audioSource?.UnPause();
		}
		if (hideDialog && _isUp)
		{
			monologuePanel.gameObject.SetActive(!pause);
		}
		_isPaused = pause;
	}

	public void InstantClose()
	{
		if (monologuePanel.gameObject.activeSelf)
		{
			_audioSource.DOComplete();
			_audioSource.Stop();
			_audioSource.UnPause();
			_isPaused = false;
			audioMixer.DOKill();
			audioMixer.SetFloat("RadioVolume", Options.GetVolume(PlayerPrefSettings.RadioVolume));
			monologuePanel.gameObject.SetActive(value: false);
			StopAllCoroutines();
			_showMonologueCoroutine = null;
			_hideMonologueCoroutine = null;
			_queueCoroutine = null;
			_isUp = false;
		}
	}

	public void StopMonologue()
	{
		_closeMonologue = true;
	}

	public void EnqueueMonologue(string messageLocalizeKey, AudioClip clip, Sprite sprite, Action<string> onMonologueFinished = null)
	{
		MonologueEntry item = new MonologueEntry
		{
			messageKey = messageLocalizeKey,
			audioClip = clip,
			sprite = sprite,
			onMonologueFinished = onMonologueFinished
		};
		if (!_monologueQueue.Contains(item))
		{
			_monologueQueue.Enqueue(item);
		}
		if (_queueCoroutine == null)
		{
			_queueCoroutine = StartCoroutine(QueueCoroutine());
		}
	}

	private IEnumerator QueueCoroutine()
	{
		if (_monologueQueue.Count < 1)
		{
			yield break;
		}
		while (_monologueQueue.Count > 0)
		{
			MonologueEntry currentEntry = _monologueQueue.Dequeue();
			_closeMonologue = false;
			_showMonologueCoroutine = StartCoroutine(ShowMonologueCoroutine(currentEntry));
			yield return new WaitUntil(() => _showMonologueCoroutine == null || _closeMonologue);
			if (_closeMonologue)
			{
				if (_showMonologueCoroutine != null)
				{
					StopCoroutine(_showMonologueCoroutine);
				}
				_showMonologueCoroutine = null;
				_audioSource.Stop();
				_closeMonologue = false;
			}
			currentEntry.onMonologueFinished?.Invoke(currentEntry.messageKey);
			_hideMonologueCoroutine = StartCoroutine(HideMonologue());
			yield return _hideMonologueCoroutine;
		}
		_queueCoroutine = null;
	}

	private IEnumerator ShowMonologueCoroutine(MonologueEntry monologue)
	{
		if (string.IsNullOrEmpty(monologue.messageKey) || monologue.audioClip == null)
		{
			yield break;
		}
		_isUp = true;
		monologuePanel.position = new Vector3(monologuePanel.position.x, 5000f, 0f);
		monologueText.Key = monologue.messageKey;
		monologueImage.sprite = monologue.sprite;
		if (_isPaused)
		{
			yield return new WaitUntil(() => !_isPaused);
		}
		monologuePanel.gameObject.SetActive(value: true);
		UiSoundHelper.Play(UiSound.NotificationMessage);
		audioMixer.DOKill();
		audioMixer.DOSetFloat("RadioVolume", Options.GetVolume(0f), 2f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
		if (!LoudSpeakersManager.IsAudioMixerMuted())
		{
			audioMixer.DOSetFloat("LoudspeakerVolume", Options.GetVolume(0f), 2f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
		}
		yield return monologuePanel.DOMoveY(monologuePosition.position.y, 1.5f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true)
			.WaitForCompletion();
		if (_isPaused)
		{
			yield return new WaitUntil(() => !_isPaused);
		}
		_audioSource.clip = monologue.audioClip;
		_audioSource.Play();
		yield return new WaitUntil(() => !_audioSource.isPlaying && !_isPaused);
		yield return new WaitForSeconds(1f);
		_showMonologueCoroutine = null;
	}

	private IEnumerator HideMonologue()
	{
		if (_audioSource.isPlaying)
		{
			float initialVolume = _audioSource.volume;
			_audioSource.DOFade(0f, 1f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true)
				.OnComplete(delegate
				{
					_audioSource.Stop();
					_audioSource.volume = initialVolume;
				});
		}
		else if (_isPaused)
		{
			_audioSource.Stop();
			_isPaused = false;
		}
		audioMixer.DOKill();
		audioMixer.DOSetFloat("RadioVolume", Options.GetVolume(PlayerPrefSettings.RadioVolume), 2f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
		if (!LoudSpeakersManager.IsAudioMixerMuted())
		{
			audioMixer.DOSetFloat("LoudspeakerVolume", 0f, 2f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
		}
		yield return monologuePanel.DOLocalMoveY(5000f, 2f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true)
			.WaitForCompletion();
		monologuePanel.gameObject.SetActive(value: false);
		_hideMonologueCoroutine = null;
		_isUp = false;
	}
}
