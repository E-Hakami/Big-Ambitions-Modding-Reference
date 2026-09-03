using System;
using UnityEngine;

public class RadioStationSource : MonoBehaviour
{
	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private RadioStation radioStation;

	[SerializeField]
	private bool skipJingles;

	[SerializeField]
	private SphereCollider audioTriggerZone;

	private void Awake()
	{
		if (audioTriggerZone.transform != base.transform)
		{
			Debug.LogWarning("OnTrigger won't work. Audio trigger zone is not attached to this game object");
			return;
		}
		audioTriggerZone.radius = audioSource.maxDistance;
		audioTriggerZone.isTrigger = true;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!(InstanceBehavior<GameManager>.Instance == null) && !(InstanceBehavior<GameManager>.Instance.radioPlayer == null) && skipJingles && other.CompareTag("Player"))
		{
			InstanceBehavior<GameManager>.Instance.radioPlayer.SkipJingles = true;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!(InstanceBehavior<GameManager>.Instance == null) && !(InstanceBehavior<GameManager>.Instance.radioPlayer == null) && skipJingles && other.CompareTag("Player"))
		{
			InstanceBehavior<GameManager>.Instance.radioPlayer.SkipJingles = false;
		}
	}

	private void OnEnable()
	{
		Subscribe(subscribe: true);
		TryPlayCurrentStationAudio();
	}

	private void OnDisable()
	{
		Subscribe(subscribe: false);
	}

	private void Subscribe(bool subscribe)
	{
		if (subscribe)
		{
			GlobalEvents.onPause = (Action<bool>)Delegate.Combine(GlobalEvents.onPause, new Action<bool>(OnPause));
		}
		else
		{
			GlobalEvents.onPause = (Action<bool>)Delegate.Remove(GlobalEvents.onPause, new Action<bool>(OnPause));
		}
		if (!(InstanceBehavior<GameManager>.Instance == null) && !(InstanceBehavior<GameManager>.Instance.radioPlayer == null))
		{
			RadioPlayer radioPlayer = InstanceBehavior<GameManager>.Instance.radioPlayer;
			if (subscribe)
			{
				radioPlayer.onNextSong.AddListener(OnNextSong);
				radioPlayer.onSongsRefreshing.AddListener(OnSongsRefreshing);
				radioPlayer.onSongsLoaded.AddListener(OnSongsLoaded);
			}
			else
			{
				radioPlayer.onNextSong.RemoveListener(OnNextSong);
				radioPlayer.onSongsRefreshing.RemoveListener(OnSongsRefreshing);
				radioPlayer.onSongsLoaded.RemoveListener(OnSongsLoaded);
			}
		}
	}

	private void OnNextSong(RadioStation station)
	{
		if (station == radioStation)
		{
			TryPlayCurrentStationAudio();
		}
	}

	private void OnSongsRefreshing()
	{
		if (radioStation == RadioStation.LocalFiles)
		{
			StopOutput();
		}
	}

	private void OnSongsLoaded()
	{
		if (radioStation == RadioStation.LocalFiles)
		{
			TryPlayCurrentStationAudio();
		}
	}

	private void OnPause(bool paused)
	{
		if (paused)
		{
			StopOutput();
		}
		else
		{
			TryPlayCurrentStationAudio();
		}
	}

	private void TryPlayCurrentStationAudio()
	{
		if (audioSource == null || InstanceBehavior<GameManager>.Instance == null || InstanceBehavior<GameManager>.Instance.radioPlayer == null)
		{
			return;
		}
		RadioPlayer radioPlayer = InstanceBehavior<GameManager>.Instance.radioPlayer;
		RadioStationData radioStationData = radioPlayer.GetRadioStationData(radioStation);
		if (!radioStationData.HasPlayableClips || radioStationData.IsLoading)
		{
			return;
		}
		CorrespondingSongData correspondingSong = radioPlayer.GetCorrespondingSong(radioStationData);
		if (!(correspondingSong.audioClip == null))
		{
			audioSource.loop = false;
			audioSource.clip = correspondingSong.audioClip;
			audioSource.time = correspondingSong.songTime;
			if (audioSource.isActiveAndEnabled)
			{
				audioSource.Play();
			}
		}
	}

	private void StopOutput()
	{
		if (!(audioSource == null))
		{
			audioSource.Stop();
			audioSource.clip = null;
		}
	}
}
