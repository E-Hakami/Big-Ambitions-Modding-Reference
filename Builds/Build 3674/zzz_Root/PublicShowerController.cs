using System;
using DG.Tweening;
using UnityEngine;

public class PublicShowerController : HygieneItemController
{
	private const float FadeDuration = 0.5f;

	[SerializeField]
	private ParticleSystem[] showerVfxs;

	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AudioClip loopingShowerSound;

	public override void Awake()
	{
		base.Awake();
		if (audioSource != null)
		{
			audioSource.outputAudioMixerGroup = InstanceBehavior<GlobalReferences>.Instance.indoorMixerGroup;
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (audioSource.isPlaying)
		{
			audioSource.Stop();
		}
		GlobalEvents.onPause = (Action<bool>)Delegate.Remove(GlobalEvents.onPause, new Action<bool>(OnPause));
	}

	public void EnableParticles()
	{
		ParticleSystem[] array = showerVfxs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Play();
		}
	}

	public void DisableParticles()
	{
		ParticleSystem[] array = showerVfxs;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Stop();
		}
	}

	protected override void OnBeginUse(ThirdPersonCharacter tpc)
	{
		base.OnBeginUse(tpc);
		tpc.appearanceSetter.SetNakedAppearance();
		EnableParticles();
		PlaySound();
	}

	protected override void OnEndUse(ThirdPersonCharacter tpc)
	{
		base.OnEndUse(tpc);
		DisableParticles();
		StopSound();
	}

	private void PlaySound()
	{
		GlobalEvents.onPause = (Action<bool>)Delegate.Combine(GlobalEvents.onPause, new Action<bool>(OnPause));
		audioSource.DOKill();
		audioSource.volume = 0f;
		audioSource.loop = true;
		audioSource.clip = loopingShowerSound;
		audioSource.Play();
		audioSource.DOFade(1f, 0.5f).SetEase(Ease.Linear);
	}

	private void StopSound()
	{
		audioSource.DOKill();
		audioSource.DOFade(0f, 0.5f).SetEase(Ease.Linear).OnComplete(delegate
		{
			audioSource.Stop();
			GlobalEvents.onPause = (Action<bool>)Delegate.Remove(GlobalEvents.onPause, new Action<bool>(OnPause));
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
