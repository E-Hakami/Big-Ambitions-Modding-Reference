using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Extensions;
using Helpers;
using IngameDebugConsole;
using Player.Sound.Radio;
using UI;
using UI.Load;
using UnityEngine;
using UnityEngine.Events;

public class RadioPlayer : MusicPlayer
{
	private const float SeekEpsilon = 0.01f;

	private const float ResyncThresholdInSeconds = 1f;

	private static readonly int StationCount = Enum.GetValues(typeof(RadioStation)).Length;

	public UnityEvent<string> onNewSong = new UnityEvent<string>();

	public UnityEvent<bool> onRadioToggle = new UnityEvent<bool>();

	public UnityEvent onSongsRefreshing = new UnityEvent();

	public UnityEvent onSongsLoaded = new UnityEvent();

	public UnityEvent<RadioStation> onNextSong = new UnityEvent<RadioStation>();

	public AudioClip currentClip;

	private readonly Dictionary<RadioStation, RadioStationData> _radioStationsData = new Dictionary<RadioStation, RadioStationData>();

	private bool _isDataLoaded;

	private float _lastFrameRealtime;

	private Coroutine _loadSongsCoroutine;

	public bool IsMuted { get; private set; }

	public bool SkipJingles { get; set; }

	public float BroadcastTime { get; private set; }

	public static string GetRadioPath()
	{
		return Path.Combine(Application.persistentDataPath, "Radio");
	}

	[ConsoleMethod("ForceJingleOnNextSong", "Forces the next audio in all radios to be a jingle", new string[] { })]
	public static void Command_ForceJingleOnNextSong()
	{
		foreach (KeyValuePair<RadioStation, RadioStationData> radioStationsDatum in InstanceBehavior<GameManager>.Instance.radioPlayer._radioStationsData)
		{
			radioStationsDatum.Value.ForceNextAudioToBeAJingle();
		}
	}

	[ConsoleMethod("SkipCurrentRadioClip", "Instantly skips the current song or jingle in all radios", new string[] { })]
	public static void Command_SkipCurrentRadioClip()
	{
		foreach (KeyValuePair<RadioStation, RadioStationData> radioStationsDatum in InstanceBehavior<GameManager>.Instance.radioPlayer._radioStationsData)
		{
			radioStationsDatum.Value.SkipCurrentRadioClip();
		}
	}

	[ConsoleMethod("SkipToEndOfCurrentRadioClip", "Jumps all radios to just before the end of the current clip", new string[] { })]
	public static void Command_SkipToEndOfCurrentRadioClip()
	{
		foreach (KeyValuePair<RadioStation, RadioStationData> radioStationsDatum in InstanceBehavior<GameManager>.Instance.radioPlayer._radioStationsData)
		{
			radioStationsDatum.Value.SkipToNearEndOfCurrentRadioClip();
		}
	}

	private static bool VehicleHasRadio(VehicleController vb)
	{
		if (vb == null)
		{
			return false;
		}
		if (!vb.vehicleType.spawnInPlayerObject)
		{
			return vb.vehicleType.hasRadio;
		}
		return false;
	}

	protected override void Start()
	{
		_lastFrameRealtime = Time.realtimeSinceStartup;
		FillRadioStationsData();
		TryLoadLocalSongs();
		ToggleMute(isMute: true);
		base.Start();
	}

	protected override void Update()
	{
		float num = Time.realtimeSinceStartup - _lastFrameRealtime;
		_lastFrameRealtime = Time.realtimeSinceStartup;
		if (InstanceBehavior<UIs>.Instance == null || InstanceBehavior<GameManager>.Instance.IsUIDevScene || CasinoBoatManager.IsOnCasinoBoat || isPaused)
		{
			return;
		}
		BroadcastTime += num;
		if (LoadScene.isLoading)
		{
			return;
		}
		foreach (KeyValuePair<RadioStation, RadioStationData> radioStationsDatum in _radioStationsData)
		{
			radioStationsDatum.Value.UpdateCurrentClip();
		}
		base.Update();
		ResyncIfDrifted();
	}

	private void OnNextSong(RadioStation radioStation)
	{
		if (radioStation == currentStation)
		{
			if (GetRadioStationData(currentStation).HasPlayableClips)
			{
				PlaySong();
			}
			else
			{
				PlayStation(currentStation);
			}
		}
	}

	private void FillRadioStationsData()
	{
		if (_isDataLoaded)
		{
			return;
		}
		_isDataLoaded = true;
		foreach (RadioStation radioStation in Enum.GetValues(typeof(RadioStation)))
		{
			RadioStationClips radioStationClips = InstanceBehavior<GlobalReferences>.Instance.radioStationClips.FirstOrDefault((RadioStationClips x) => x.radioStation == radioStation);
			RadioClip[] radioClips = ((radioStationClips != null) ? radioStationClips.clips.Shuffle().ToArray() : Array.Empty<RadioClip>());
			_radioStationsData[radioStation] = new RadioStationData(radioStation, radioClips);
		}
	}

	public RadioStation GetCurrentStation()
	{
		return currentStation;
	}

	public RadioStationData GetRadioStationData(RadioStation radioStation)
	{
		FillRadioStationsData();
		return _radioStationsData[radioStation];
	}

	protected override void SubscribeToGlobalEvents()
	{
		base.SubscribeToGlobalEvents();
		GlobalEvents.RegisterOnGameLoadedCallback(PlaySavedStation);
		InstanceBehavior<GameManager>.Instance.radioPlayer.onNextSong.AddListener(OnNextSong);
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onEnterVehicle, (Action<VehicleController>)delegate(VehicleController vb)
		{
			if (VehicleHasRadio(vb) && !PlayerHelper.IsWearingHeadset())
			{
				ToggleMute(!audioSource.mute);
				InstanceBehavior<UIs>.Instance.smartphoneUI?.radioControls.EnableUI();
				InstanceBehavior<GameManager>.Instance.radioPlayer.onRadioToggle.Invoke(arg0: true);
			}
		});
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, (Action<VehicleController>)delegate
		{
			if (!PlayerHelper.IsWearingHeadset())
			{
				ToggleMute(isMute: true);
				InstanceBehavior<UIs>.Instance.smartphoneUI?.radioControls.DisableUI();
				InstanceBehavior<GameManager>.Instance.radioPlayer.onRadioToggle.Invoke(arg0: false);
			}
		});
		GlobalEvents.onAccessoryEquipped = (Action<CargoInstance>)Delegate.Combine(GlobalEvents.onAccessoryEquipped, new Action<CargoInstance>(OnAccessoryEquipped));
		GlobalEvents.onAccessoryUnEquipped = (Action<CargoInstance>)Delegate.Combine(GlobalEvents.onAccessoryUnEquipped, new Action<CargoInstance>(OnAccessoryUnEquipped));
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Combine(GlobalEvents.onTimeMachineEnded, (Action)delegate
		{
			foreach (KeyValuePair<RadioStation, RadioStationData> radioStationsDatum in _radioStationsData)
			{
				if (radioStationsDatum.Value.HasPlayableClips)
				{
					radioStationsDatum.Value.AdvanceToRandomPositionInNextClip();
				}
			}
			PlayStation(currentStation);
		});
		onSongsRefreshing.AddListener(delegate
		{
			if (!LoadScene.isLoading && currentStation == RadioStation.LocalFiles)
			{
				Stop();
			}
		});
		onSongsLoaded.AddListener(delegate
		{
			if (!LoadScene.isLoading && currentStation == RadioStation.LocalFiles)
			{
				PlayStation(RadioStation.LocalFiles);
				SetPause(isPaused);
			}
		});
	}

	private void OnAccessoryUnEquipped(CargoInstance accessory)
	{
		Item itemCached = accessory.ItemCached;
		if (((object)itemCached == null || itemCached.HasTag(TagRef.Itemtag.isaudioheadaccessory)) && (!PlayerHelper.IsUsingVehicle || !VehicleHasRadio(VehicleHelper.GetCurrentVehicleBase())))
		{
			InstanceBehavior<UIs>.Instance.smartphoneUI?.radioControls.DisableUI();
			ToggleMute(isMute: true);
			InstanceBehavior<GameManager>.Instance.radioPlayer.onRadioToggle.Invoke(arg0: false);
		}
	}

	private void OnAccessoryEquipped(CargoInstance accessory)
	{
		Item itemCached = accessory.ItemCached;
		if (((object)itemCached == null || itemCached.HasTag(TagRef.Itemtag.isaudioheadaccessory)) && (!PlayerHelper.IsUsingVehicle || !VehicleHasRadio(VehicleHelper.GetCurrentVehicleBase())))
		{
			InstanceBehavior<UIs>.Instance.smartphoneUI?.radioControls.EnableUI();
			ToggleMute(!audioSource.mute);
			InstanceBehavior<GameManager>.Instance.radioPlayer.onRadioToggle.Invoke(arg0: true);
		}
	}

	public void ToggleMute(bool isMute)
	{
		audioSource.mute = isMute;
		audioSource.priority = (isMute ? 255 : 128);
		IsMuted = isMute;
	}

	public void TryLoadLocalSongs()
	{
		if (currentStation == RadioStation.LocalFiles)
		{
			Stop();
		}
		LoadLocalSongs();
	}

	private void LoadLocalSongs()
	{
		List<RadioClip> list = new List<RadioClip>();
		string[] files = Directory.GetFiles(GetRadioPath());
		foreach (string text in files)
		{
			AudioType audioTypeFromExtension = AudioFileFormatHelper.GetAudioTypeFromExtension(text);
			if (audioTypeFromExtension != AudioType.UNKNOWN && !AudioFileFormatHelper.IsKnownUnsupported(text))
			{
				list.Add(new RadioClip
				{
					Name = Path.GetFileNameWithoutExtension(text),
					path = text,
					type = audioTypeFromExtension
				});
			}
		}
		if (list.Count > 0)
		{
			_radioStationsData[RadioStation.LocalFiles] = new RadioStationData(RadioStation.LocalFiles, list.Shuffle().ToArray());
			onSongsRefreshing.Invoke();
		}
		if (currentStation == RadioStation.LocalFiles)
		{
			Play();
		}
	}

	protected override void Stop()
	{
		base.Stop();
		audioSource.clip = null;
		currentClip = null;
	}

	protected override bool ShouldPlayNextSong()
	{
		if (!LoadScene.isLoading && base.ShouldPlayNextSong())
		{
			return !GetRadioStationData(currentStation).IsLoading;
		}
		return false;
	}

	protected override void PlaySong()
	{
		if (!InstanceBehavior<GameManager>.Instance.IsUIDevScene)
		{
			if (CasinoBoatManager.IsOnCasinoBoat)
			{
				InstanceBehavior<CasinoBoatManager>.Instance.boatMusic.SkipSong();
			}
			else
			{
				base.PlaySong();
			}
		}
	}

	protected override void UpdateCurrentSong(AudioClip audioClip, float time)
	{
		currentClip = audioClip;
		onNewSong.Invoke(currentClip.name);
		base.UpdateCurrentSong(audioClip, time);
	}

	private void PlaySavedStation()
	{
		RadioStation radioStation = (RadioStation)PlayerPrefSettings.RadioStation;
		if (!Enum.IsDefined(typeof(RadioStation), radioStation))
		{
			radioStation = RadioStation.Pop;
		}
		PlayStation(radioStation);
	}

	public void PlayNextStation()
	{
		PlayStation(currentStation.Next());
		PlayerPrefSettings.RadioStation = (int)currentStation;
	}

	private void PlayStation(RadioStation radioStation)
	{
		currentStation = radioStation;
		for (int i = 0; i < StationCount; i++)
		{
			if (GetRadioStationData(currentStation).HasPlayableClips)
			{
				break;
			}
			currentStation = currentStation.Next();
		}
		PlayCorrespondingSong(GetRadioStationData(currentStation));
	}

	private void PlayCorrespondingSong(RadioStationData radioStationData)
	{
		CorrespondingSongData correspondingSong = GetCorrespondingSong(radioStationData);
		if (!(correspondingSong.audioClip == null))
		{
			UpdateCurrentSong(correspondingSong.audioClip, correspondingSong.songTime);
		}
	}

	public CorrespondingSongData GetCorrespondingSong(RadioStationData radioStationData)
	{
		AudioClip currentSong = radioStationData.GetCurrentSong();
		if (currentSong == null)
		{
			return new CorrespondingSongData(null, 0f);
		}
		float max = Mathf.Max(0f, currentSong.length - 0.01f);
		float songTime = Mathf.Clamp(radioStationData.CurrentClipProgressedTime, 0f, max);
		return new CorrespondingSongData(currentSong, songTime);
	}

	private void ResyncIfDrifted()
	{
		if (audioSource.isPlaying)
		{
			CorrespondingSongData correspondingSong = GetCorrespondingSong(GetRadioStationData(currentStation));
			if (!(correspondingSong.audioClip != audioSource.clip) && !(Mathf.Abs(audioSource.time - correspondingSong.songTime) < 1f))
			{
				audioSource.time = correspondingSong.songTime;
			}
		}
	}

	[ConsoleMethod("ToggleRadioDebugMode", "Toggles the radio debug mode", new string[] { })]
	public static void ToggleRadioDebugMode()
	{
		RadioPlayerDebug component = InstanceBehavior<GameManager>.Instance.radioPlayer.GetComponent<RadioPlayerDebug>();
		if (component != null)
		{
			UnityEngine.Object.Destroy(component);
		}
		else
		{
			InstanceBehavior<GameManager>.Instance.radioPlayer.gameObject.AddComponent<RadioPlayerDebug>();
		}
	}
}
