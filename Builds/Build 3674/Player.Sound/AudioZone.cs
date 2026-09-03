using System;
using DG.Tweening;
using UnityEngine;

namespace Player.Sound;

[RequireComponent(typeof(Collider))]
public class AudioZone : MonoBehaviour
{
	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip clip;

	[SerializeField]
	private float fadeDuration = 0.5f;

	private void Awake()
	{
		audioSource.loop = true;
		audioSource.clip = clip;
		audioSource.volume = 0f;
	}

	private void OnDestroy()
	{
		if (audioSource.isPlaying)
		{
			audioSource.Stop();
		}
		GlobalEvents.onPause = (Action<bool>)Delegate.Remove(GlobalEvents.onPause, new Action<bool>(OnPause));
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			PlaySound();
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			StopSound();
		}
	}

	private void PlaySound()
	{
		GlobalEvents.onPause = (Action<bool>)Delegate.Combine(GlobalEvents.onPause, new Action<bool>(OnPause));
		audioSource.DOKill();
		audioSource.Play();
		float duration = (1f - audioSource.volume) * fadeDuration;
		audioSource.DOFade(1f, duration).SetEase(Ease.Linear);
	}

	private void StopSound()
	{
		GlobalEvents.onPause = (Action<bool>)Delegate.Remove(GlobalEvents.onPause, new Action<bool>(OnPause));
		audioSource.DOKill();
		float duration = audioSource.volume * fadeDuration;
		audioSource.DOFade(0f, duration).SetEase(Ease.Linear).OnComplete(delegate
		{
			audioSource.Stop();
		});
	}

	private void OnPause(bool paused)
	{
		if (!paused)
		{
			audioSource?.Play();
		}
		else
		{
			audioSource?.Pause();
		}
	}
}
