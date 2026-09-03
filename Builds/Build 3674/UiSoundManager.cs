using System;
using System.Collections.Generic;
using System.Linq;
using Extensions;
using UnityEngine;

public class UiSoundManager : MonoBehaviour
{
	[SerializeField]
	private AudioSource[] audioSources;

	[SerializeField]
	private UiSoundData[] uiSounds;

	private readonly Dictionary<UiSound, UiSoundData> _uiSoundsDictionary = new Dictionary<UiSound, UiSoundData>();

	private void Start()
	{
		UiSoundHelper.playSound = (Action<UiSound, bool>)Delegate.Combine(UiSoundHelper.playSound, new Action<UiSound, bool>(PlayUiSound));
		UiSoundData[] array = uiSounds;
		foreach (UiSoundData uiSoundData in array)
		{
			_uiSoundsDictionary.Add(uiSoundData.type, uiSoundData);
		}
	}

	private void PlayUiSound(UiSound type, bool randomPitch)
	{
		AudioSource audioSource = audioSources.FirstOrDefault((AudioSource x) => !x.isPlaying);
		if (!(audioSource == null))
		{
			UiSoundData uiSoundData = _uiSoundsDictionary[type];
			audioSource.clip = uiSoundData.clips.GetRandom();
			audioSource.volume = uiSoundData.volume;
			if (randomPitch)
			{
				audioSource.pitch = UnityEngine.Random.Range(uiSoundData.pitchRange.x, uiSoundData.pitchRange.y);
			}
			else
			{
				audioSource.pitch = 1f;
			}
			audioSource.Play();
		}
	}

	private void OnDestroy()
	{
		UiSoundHelper.playSound = (Action<UiSound, bool>)Delegate.Remove(UiSoundHelper.playSound, new Action<UiSound, bool>(PlayUiSound));
	}
}
