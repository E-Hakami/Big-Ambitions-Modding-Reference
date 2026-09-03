using System;
using BigAmbitions.PlacementSystem;
using UI;
using UI.Smartphone;
using UnityEngine;

public class MusicPlayer : MonoBehaviour
{
	[SerializeField]
	protected AudioSource audioSource;

	protected bool isPaused;

	protected RadioStation currentStation;

	private bool isActive;

	protected virtual void Start()
	{
		SubscribeToGlobalEvents();
	}

	protected virtual void SubscribeToGlobalEvents()
	{
		GlobalEvents.onPause = (Action<bool>)Delegate.Combine(GlobalEvents.onPause, new Action<bool>(SetPause));
	}

	protected virtual void UnSubscribeToGlobalEvents()
	{
		GlobalEvents.onPause = (Action<bool>)Delegate.Remove(GlobalEvents.onPause, new Action<bool>(SetPause));
	}

	protected virtual void Update()
	{
		if (ShouldPlayNextSong())
		{
			PlaySong();
		}
	}

	protected virtual bool ShouldPlayNextSong()
	{
		if (currentStation == RadioStation.LocalFiles && !audioSource.isPlaying && isActive)
		{
			return !isPaused;
		}
		return false;
	}

	protected void Play()
	{
		audioSource.Play();
		isActive = true;
	}

	protected virtual void Stop()
	{
		audioSource.Stop();
		isActive = false;
	}

	private void Pause()
	{
		audioSource.Pause();
		isPaused = true;
	}

	private void UnPause()
	{
		audioSource.UnPause();
		isPaused = false;
	}

	protected void SetPause(bool pause)
	{
		if (audioSource == null)
		{
			Debug.LogError("AudioSource is null in MusicPlayer");
		}
		else if (ShouldNotPause())
		{
			if (!pause && isPaused)
			{
				UnPause();
			}
		}
		else if (pause)
		{
			Pause();
		}
		else
		{
			UnPause();
		}
	}

	private static bool ShouldNotPause()
	{
		if (!PlacementSystem.IsInPlacementMode && (!(InstanceBehavior<UIs>.Instance != null) || !FullMenu.IsOpen))
		{
			return CityMap.IsOpen;
		}
		return true;
	}

	protected virtual void PlaySong()
	{
		RadioPlayer radioPlayer = InstanceBehavior<GameManager>.Instance.radioPlayer;
		CorrespondingSongData correspondingSong = radioPlayer.GetCorrespondingSong(radioPlayer.GetRadioStationData(currentStation));
		if (!(correspondingSong.audioClip == null))
		{
			UpdateCurrentSong(correspondingSong.audioClip, correspondingSong.songTime);
		}
	}

	protected virtual void UpdateCurrentSong(AudioClip audioClip, float time)
	{
		audioSource.clip = audioClip;
		audioSource.time = time;
		Play();
	}

	public float GetSongTime()
	{
		return audioSource.time;
	}
}
