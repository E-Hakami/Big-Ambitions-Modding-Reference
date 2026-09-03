using System;
using System.Collections;
using Scenes.MainMenu;
using UnityEngine;
using UnityEngine.Audio;

public class BoatMusic : MonoBehaviour
{
	public AudioSource audioSource;

	public AudioClip[] audioClips;

	public bool shouldPlay;

	public float fadeTime = 5f;

	public float volume = 0.5f;

	[SerializeField]
	private AudioMixerGroup onIndoorsMixer;

	[SerializeField]
	private AudioMixerGroup onOutdoorsMixer;

	private bool _isInside;

	public IEnumerator Start()
	{
		UpdateVolume();
		Options.onAiStoreMusicVolumeUpdated = (Action)Delegate.Combine(Options.onAiStoreMusicVolumeUpdated, new Action(UpdateVolume));
		yield return new WaitForSeconds(2f);
		if (CasinoBoatManager.IsOnCasinoBoat)
		{
			shouldPlay = true;
			SetIndoors();
		}
		else
		{
			SetIndoors(indoors: false);
		}
	}

	private void UpdateVolume()
	{
		audioSource.volume = volume * PlayerPrefSettings.AiStoreMusicVolume;
	}

	public void PlayMusic()
	{
		audioSource.clip = audioClips[0];
		InstanceBehavior<GameManager>.Instance.radioPlayer.onNewSong.Invoke(audioSource.clip.name);
		audioSource.Play();
		shouldPlay = true;
	}

	private void Update()
	{
		if (shouldPlay)
		{
			if (!audioSource.isPlaying)
			{
				SkipSong();
			}
			audioSource.spatialBlend = Mathf.Lerp(audioSource.spatialBlend, (!_isInside) ? 1 : 0, Time.deltaTime * fadeTime);
		}
	}

	public void SkipSong()
	{
		int num = 0;
		if (audioClips.Length > 1)
		{
			int num2 = Array.IndexOf(audioClips, audioSource.clip);
			num = UnityEngine.Random.Range(0, audioClips.Length - 1);
			if (num2 >= 0 && num >= num2)
			{
				num++;
			}
		}
		AudioClip audioClip = audioClips[num];
		audioSource.clip = audioClip;
		InstanceBehavior<GameManager>.Instance.radioPlayer.onNewSong.Invoke(audioClip.name);
		audioSource.Play();
	}

	public void SetIndoors(bool indoors = true)
	{
		_isInside = indoors;
		audioSource.outputAudioMixerGroup = (indoors ? onIndoorsMixer : onOutdoorsMixer);
	}

	public void StopMusic()
	{
		audioSource.Stop();
	}

	private void OnDestroy()
	{
		Options.onAiStoreMusicVolumeUpdated = (Action)Delegate.Remove(Options.onAiStoreMusicVolumeUpdated, new Action(UpdateVolume));
	}
}
