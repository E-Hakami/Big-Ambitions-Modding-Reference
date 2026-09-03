using System;
using System.Collections.Generic;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using BigAmbitions.Tags;
using Extensions;
using JimmysUnityUtilities;
using UI.Load;
using UI.Smartphone;
using UnityEngine;

namespace Player.Sound.Radio;

public class LoudSpeakersManager : InstanceBehavior<LoudSpeakersManager>
{
	[SerializeField]
	private AudioSource audioSource;

	[SerializeField]
	private AnimationCurve falloffCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

	[SerializeField]
	private float spatialBlendRadius = 10f;

	private readonly List<LoudspeakerController> _speakers = new List<LoudspeakerController>();

	private RadioStation _currentStation;

	private bool _isActive;

	private bool _isPaused;

	private bool _pendingPlay;

	private bool _speakersInitialized;

	private bool _isAudioMixerVolumeMuted;

	private float _volumeMultiplier = 1f;

	public static string StationName => InstanceBehavior<LoudSpeakersManager>.Instance._currentStation.ToString();

	public static string SongName => InstanceBehavior<LoudSpeakersManager>.Instance.audioSource?.clip?.name;

	public static bool IsMuted => InstanceBehavior<LoudSpeakersManager>.Instance.audioSource.mute;

	protected override void Awake()
	{
		base.Awake();
		if (base.IsMainInstance)
		{
			GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
			GlobalEvents.onPause = (Action<bool>)Delegate.Combine(GlobalEvents.onPause, new Action<bool>(SetPause));
			PlacementSystem.onPlacementModeStart = (Action)Delegate.Combine(PlacementSystem.onPlacementModeStart, new Action(OnPlacementModeToggled));
			PlacementSystem.onPlacementModeEnd = (Action)Delegate.Combine(PlacementSystem.onPlacementModeEnd, new Action(OnPlacementModeToggled));
			GlobalEvents.onAccessoryEquipped = (Action<CargoInstance>)Delegate.Combine(GlobalEvents.onAccessoryEquipped, new Action<CargoInstance>(OnAccessoryEquipped));
			GlobalEvents.onAccessoryUnEquipped = (Action<CargoInstance>)Delegate.Combine(GlobalEvents.onAccessoryUnEquipped, new Action<CargoInstance>(OnAccessoryUnEquipped));
			LoudspeakerController.onBuildingRadioVolumeChanged = (Action)Delegate.Combine(LoudspeakerController.onBuildingRadioVolumeChanged, new Action(OnVolumeUpdate));
			GlobalEvents.onTimeMachineEnded = (Action)Delegate.Combine(GlobalEvents.onTimeMachineEnded, new Action(OnTimeMachineEnd));
			InstanceBehavior<GameManager>.Instance.radioPlayer.onSongsRefreshing.AddListener(OnSongsRefreshing);
			InstanceBehavior<GameManager>.Instance.radioPlayer.onSongsLoaded.AddListener(OnSongsLoaded);
			InstanceBehavior<GameManager>.Instance.radioPlayer.onNextSong.AddListener(OnNextSong);
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		GlobalEvents.onPause = (Action<bool>)Delegate.Remove(GlobalEvents.onPause, new Action<bool>(SetPause));
		PlacementSystem.onPlacementModeStart = (Action)Delegate.Remove(PlacementSystem.onPlacementModeStart, new Action(OnPlacementModeToggled));
		PlacementSystem.onPlacementModeEnd = (Action)Delegate.Remove(PlacementSystem.onPlacementModeEnd, new Action(OnPlacementModeToggled));
		LoudspeakerController.onBuildingRadioVolumeChanged = (Action)Delegate.Remove(LoudspeakerController.onBuildingRadioVolumeChanged, new Action(OnVolumeUpdate));
		GlobalEvents.onAccessoryEquipped = (Action<CargoInstance>)Delegate.Remove(GlobalEvents.onAccessoryEquipped, new Action<CargoInstance>(OnAccessoryEquipped));
		GlobalEvents.onAccessoryUnEquipped = (Action<CargoInstance>)Delegate.Remove(GlobalEvents.onAccessoryUnEquipped, new Action<CargoInstance>(OnAccessoryUnEquipped));
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Remove(GlobalEvents.onTimeMachineEnded, new Action(OnTimeMachineEnd));
		if (InstanceBehavior<GameManager>.Instance != null)
		{
			InstanceBehavior<GameManager>.Instance.radioPlayer.onSongsRefreshing.RemoveListener(OnSongsRefreshing);
			InstanceBehavior<GameManager>.Instance.radioPlayer.onSongsLoaded.RemoveListener(OnSongsLoaded);
			InstanceBehavior<GameManager>.Instance.radioPlayer.onNextSong.RemoveListener(OnNextSong);
		}
	}

	private void LateUpdate()
	{
		if (!_speakersInitialized && _speakers.Count > 0)
		{
			InitSpeakers();
		}
		if (_speakersInitialized)
		{
			if (!_isPaused && (_pendingPlay || ShouldPlayNextSong()))
			{
				PlaySong();
			}
			if (audioSource.isPlaying)
			{
				UpdateSpatialBlend();
			}
		}
	}

	private void UpdateSpatialBlend()
	{
		AudioListenerPositioner instance = AudioListenerPositioner.Instance;
		if (!instance)
		{
			return;
		}
		float num = 0f;
		float num2 = 0f;
		float num3 = 0f;
		foreach (LoudspeakerController speaker in _speakers)
		{
			Vector3 vector = instance.transform.InverseTransformPoint(speaker.transform.position);
			if (!(vector.sqrMagnitude > speaker.AudioMaxDistanceSqr))
			{
				float value = (vector.magnitude - speaker.audioMinDistance) / (speaker.audioMaxDistance - speaker.audioMinDistance);
				value = Mathf.Clamp01(value);
				if (speaker.directional > 0f)
				{
					Vector3 normalized = (instance.transform.position - speaker.transform.position).normalized;
					Vector3 forward = speaker.transform.forward;
					float value2 = Vector3.Dot(normalized, forward);
					float num4 = Mathf.InverseLerp(-1f, 1f, value2);
					value = Mathf.Lerp(value, 1f, speaker.directional * (1f - num4));
				}
				float num5 = falloffCurve.Evaluate(value) * speaker.audioVolume;
				num += num5;
				num2 += num5 * Envelope(0f - vector.x);
				num3 += num5 * Envelope(vector.x);
			}
		}
		audioSource.volume = Mathf.Clamp01(num) * _volumeMultiplier;
		float num6 = num2 + num3;
		audioSource.panStereo = (Mathf.Approximately(num6, 0f) ? 0f : Mathf.Clamp((num3 - num2) / num6, -1f, 1f));
		float Envelope(float x)
		{
			if (x >= 0f)
			{
				return 1f;
			}
			return 1f - Mathf.Min(1f, (0f - x) / spatialBlendRadius);
		}
	}

	private void OnNextSong(RadioStation radioStation)
	{
		if (radioStation == _currentStation)
		{
			_pendingPlay = true;
		}
	}

	private void OnTimeMachineEnd()
	{
		if (_speakersInitialized)
		{
			CoroutineUtility.RunAfterOneFrame(DoPlay);
		}
	}

	private void SetPause(bool pause)
	{
		if (audioSource == null)
		{
			Debug.LogError("Audio source is null in LoudSpeakersManager");
			return;
		}
		_isPaused = pause || PlacementSystem.IsInPlacementMode || FullMenu.IsOpen || CityMap.IsOpen;
		if (_isPaused)
		{
			if (audioSource.isPlaying)
			{
				audioSource.Pause();
			}
		}
		else if (audioSource.clip != null)
		{
			_pendingPlay = true;
		}
	}

	private void OnPlacementModeToggled()
	{
		if (_speakersInitialized)
		{
			SetPause(_isPaused);
		}
	}

	public void OnExitBuilding(Address _)
	{
		Stop();
		_speakers.Clear();
		_speakersInitialized = false;
	}

	private void OnAccessoryEquipped(CargoInstance accessoryCargoInstance)
	{
		Item itemCached = accessoryCargoInstance.ItemCached;
		if ((object)itemCached == null || itemCached.HasTag(TagRef.Itemtag.isaudioheadaccessory))
		{
			InstanceBehavior<SfxManager>.Instance.audioMixer.SetFloat("LoudspeakerVolume", -80f);
			_isAudioMixerVolumeMuted = true;
		}
	}

	private void OnAccessoryUnEquipped(CargoInstance accessoryCargoInstance)
	{
		Item itemCached = accessoryCargoInstance.ItemCached;
		if ((object)itemCached == null || itemCached.HasTag(TagRef.Itemtag.isaudioheadaccessory))
		{
			InstanceBehavior<SfxManager>.Instance.audioMixer.SetFloat("LoudspeakerVolume", 0f);
			_isAudioMixerVolumeMuted = false;
		}
	}

	private void InitSpeakers()
	{
		RadioStation businessRadioStation = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.GetBusinessRadioStation();
		RadioStationData radioStationData = InstanceBehavior<GameManager>.Instance.radioPlayer.GetRadioStationData(businessRadioStation);
		if (!radioStationData.HasPlayableClips || (businessRadioStation == RadioStation.LocalFiles && radioStationData.IsLoading))
		{
			businessRadioStation = businessRadioStation.Next();
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.radioStation = businessRadioStation;
		}
		_speakersInitialized = true;
		InitMusic();
		DoPlay();
	}

	private void InitMusic()
	{
		if (_speakersInitialized)
		{
			OnVolumeUpdate();
			_currentStation = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.GetBusinessRadioStation();
		}
	}

	private void OnSongsRefreshing()
	{
		if (_currentStation == RadioStation.LocalFiles)
		{
			Stop();
		}
	}

	private void OnSongsLoaded()
	{
		if (_speakersInitialized && !LoadScene.isLoading && _currentStation == RadioStation.LocalFiles)
		{
			PlayStation(_currentStation);
			SetPause(_isPaused);
		}
	}

	private bool ShouldPlayNextSong()
	{
		if (_isActive && !_isPaused && !audioSource.isPlaying)
		{
			return !InstanceBehavior<GameManager>.Instance.radioPlayer.GetRadioStationData(_currentStation).IsLoading;
		}
		return false;
	}

	private void OnVolumeUpdate()
	{
		if (_speakersInitialized)
		{
			float businessRadioVolume = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.GetBusinessRadioVolume();
			if (businessRadioVolume < 0f)
			{
				audioSource.mute = true;
				return;
			}
			audioSource.mute = false;
			_volumeMultiplier = businessRadioVolume;
		}
	}

	private void DoPlay()
	{
		if (!_speakersInitialized || CasinoBoatManager.IsOnCasinoBoat || InstanceBehavior<GameManager>.Instance.radioPlayer.GetRadioStationData(_currentStation).IsLoading)
		{
			return;
		}
		if (LoadScene.isLoading)
		{
			GlobalEvents.RegisterOnGameLoadedCallback(delegate
			{
				CoroutineUtility.RunAfterOneFrame(DoPlay);
			});
		}
		else
		{
			PlayStation(_currentStation);
		}
	}

	private void Stop()
	{
		audioSource.Stop();
		audioSource.clip = null;
		_isActive = false;
		_pendingPlay = false;
	}

	private void PlaySong()
	{
		if (_speakersInitialized)
		{
			RadioPlayer radioPlayer = InstanceBehavior<GameManager>.Instance.radioPlayer;
			CorrespondingSongData correspondingSong = radioPlayer.GetCorrespondingSong(radioPlayer.GetRadioStationData(_currentStation));
			if (!(correspondingSong.audioClip == null))
			{
				audioSource.clip = correspondingSong.audioClip;
				audioSource.Play();
				_isActive = true;
				_pendingPlay = false;
				audioSource.time = correspondingSong.songTime;
			}
		}
	}

	private void PlayNextStation(RadioStation currentStation)
	{
		if (!_isPaused && _speakersInitialized)
		{
			RadioStation radioStation = currentStation.Next();
			PlayStation(radioStation);
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.radioStation = radioStation;
		}
	}

	private void PlayStation(RadioStation radioStation)
	{
		if (_speakersInitialized)
		{
			_currentStation = radioStation;
			RadioStationData radioStationData = InstanceBehavior<GameManager>.Instance.radioPlayer.GetRadioStationData(radioStation);
			if (!radioStationData.HasPlayableClips)
			{
				PlayNextStation(_currentStation);
			}
			else if (radioStation == RadioStation.LocalFiles && radioStationData.IsLoading)
			{
				Stop();
			}
			else
			{
				_pendingPlay = true;
			}
		}
	}

	public static bool IsAudioMixerMuted()
	{
		return InstanceBehavior<LoudSpeakersManager>.Instance._isAudioMixerVolumeMuted;
	}

	public static void PlayNextStation()
	{
		if (InstanceBehavior<LoudSpeakersManager>.Instance._speakersInitialized)
		{
			RadioStation businessRadioStation = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.GetBusinessRadioStation();
			RadioStation radioStation = businessRadioStation.Next();
			RadioStationData radioStationData = InstanceBehavior<GameManager>.Instance.radioPlayer.GetRadioStationData(radioStation);
			if (!radioStationData.HasPlayableClips)
			{
				InstanceBehavior<BuildingManager>.Instance.buildingRegistration.radioStation = radioStation;
				PlayNextStation();
			}
			else if (radioStation == RadioStation.LocalFiles)
			{
				_ = radioStationData.IsLoading;
				InstanceBehavior<LoudSpeakersManager>.Instance.PlayNextStation(businessRadioStation);
			}
			else
			{
				InstanceBehavior<LoudSpeakersManager>.Instance.PlayNextStation(businessRadioStation);
			}
		}
	}

	private static void PlayStation()
	{
		if (!CasinoBoatManager.IsOnCasinoBoat)
		{
			RadioStation businessRadioStation = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.GetBusinessRadioStation();
			InstanceBehavior<LoudSpeakersManager>.Instance.PlayStation(businessRadioStation);
		}
	}

	public static void AddSpeaker(LoudspeakerController loudspeakerController)
	{
		InstanceBehavior<LoudSpeakersManager>.Instance._speakers.Add(loudspeakerController);
		if (InstanceBehavior<LoudSpeakersManager>.Instance._speakersInitialized)
		{
			InstanceBehavior<LoudSpeakersManager>.Instance.InitMusic();
			PlayStation();
		}
	}

	public static void RemoveSpeaker(LoudspeakerController loudspeakerController)
	{
		if (InstanceBehavior<LoudSpeakersManager>.Instance._speakers.Count != 0)
		{
			InstanceBehavior<LoudSpeakersManager>.Instance._speakers.Remove(loudspeakerController);
			if (InstanceBehavior<LoudSpeakersManager>.Instance._speakers.Count == 0)
			{
				InstanceBehavior<LoudSpeakersManager>.Instance.Stop();
				InstanceBehavior<LoudSpeakersManager>.Instance._speakersInitialized = false;
			}
		}
	}
}
