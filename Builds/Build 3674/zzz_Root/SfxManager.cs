using System;
using System.Collections.Generic;
using BigAmbitions.SoundSystem;
using Extensions;
using Helpers;
using NaughtyAttributes;
using Parking.UndergroundParking;
using RoboRyanTron.SearchableEnum;
using Scenes.MainMenu;
using UI;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Pool;

public class SfxManager : InstanceBehavior<SfxManager>
{
	[Serializable]
	public class SoundPreset
	{
		[Serializable]
		public class ClipSetting
		{
			[HideInInspector]
			[AllowNesting]
			public string name;

			public AudioClip audioClip;

			[Range(0f, 256f)]
			[Tooltip("0: High; 256: Low")]
			public int priority;

			[Range(0f, 1f)]
			public float volume;

			[MinMaxSlider(-3f, 3f)]
			public Vector2 pitch;

			private ClipSetting()
			{
				priority = 128;
				volume = 1f;
				pitch = new Vector2(1f, 1f);
			}
		}

		[HideInInspector]
		[AllowNesting]
		public string name;

		[SearchableEnum]
		public SoundType type;

		[Range(0f, 1f)]
		public float volumeMultiplier;

		[AllowNesting]
		public ClipSetting[] ClipSettings;

		public AudioMixerGroup audioMixerGroup;

		[Foldout("Bypasses")]
		public bool bypassEffects;

		[Foldout("Bypasses")]
		public bool bypassListenerEffects;

		[Foldout("Bypasses")]
		public bool bypassReverbZones;

		[Range(-1f, 1f)]
		[Tooltip("-1: Left; 0: Middle; 1: Right")]
		public float stereoPan;

		[Range(0f, 1f)]
		[Tooltip("0: 2D, 1: 3D")]
		public float spatialBlend;

		[Range(0f, 1f)]
		public float reverbZoneMix;

		[Range(0f, 5f)]
		[Foldout("3D Sound Settings")]
		public float dopplerLevel;

		[Range(0f, 360f)]
		[Foldout("3D Sound Settings")]
		public float spread;

		[Foldout("3D Sound Settings")]
		public AudioRolloffMode volumeRolloff;

		[Foldout("3D Sound Settings")]
		public float minDistance;

		[Foldout("3D Sound Settings")]
		public float maxDistance;

		public bool useProbability;

		[Range(0f, 1f)]
		[ShowIf("useProbability")]
		[AllowNesting]
		public float probability = 1f;

		public SoundPreset()
		{
			bypassEffects = false;
			bypassListenerEffects = false;
			bypassReverbZones = false;
			spatialBlend = 0f;
			stereoPan = 0f;
			reverbZoneMix = 1f;
			dopplerLevel = 1f;
			spread = 0f;
			volumeRolloff = AudioRolloffMode.Logarithmic;
			minDistance = 0f;
			maxDistance = 500f;
			volumeMultiplier = 1f;
		}
	}

	public AudioMixer audioMixer;

	public AudioMixer vehicleAudioMixer;

	public AudioMixerGroup playerFootStepAudioMixerGroup;

	public AudioMixerSnapshot outdoorSnapshot;

	public AudioMixerSnapshot onlyUISnapshot;

	public AudioMixerSnapshot indoorSnapshot;

	public AudioMixerSnapshot cityMapSnapshot;

	public AudioMixerSnapshot vehicleMutedSnapshot;

	public AudioMixerSnapshot vehicleNotMutedSnapshot;

	public SoundPreset[] soundPresets;

	public AudioSource audioSourcePrefab;

	private readonly Dictionary<SoundType, SoundPreset> soundPresetDictionary = new Dictionary<SoundType, SoundPreset>();

	private ObjectPool<AudioSource> _audioSourcePool;

	private readonly Queue<Tuple<float, AudioSource>> _audioSourcesReturnQueue = new Queue<Tuple<float, AudioSource>>();

	private readonly List<Tuple<float, AudioSource>> _audioSourcesTempList = new List<Tuple<float, AudioSource>>();

	private bool _queueNeedsReordering;

	[Button(null, EButtonEnableMode.Always)]
	private void GenerateSoundDictionary()
	{
		soundPresetDictionary.Clear();
		SoundPreset[] array = soundPresets;
		foreach (SoundPreset soundPreset in array)
		{
			soundPresetDictionary.Add(soundPreset.type, soundPreset);
		}
	}

	protected override void Awake()
	{
		base.Awake();
		if (base.IsMainInstance)
		{
			GenerateSoundDictionary();
			GlobalEvents.onPause = (Action<bool>)Delegate.Combine(GlobalEvents.onPause, new Action<bool>(OnPauseSnapshotChanged));
		}
	}

	private void OnPauseSnapshotChanged(bool pause)
	{
		SetSoundSnapshot(pause, 0.2f);
	}

	private void Start()
	{
		_audioSourcePool = new ObjectPool<AudioSource>(() => UnityEngine.Object.Instantiate(audioSourcePrefab, base.transform), delegate(AudioSource src)
		{
			src.gameObject.SetActive(value: true);
		}, delegate(AudioSource src)
		{
			src.gameObject.SetActive(value: false);
			src.Stop();
		}, delegate(AudioSource src)
		{
			UnityEngine.Object.Destroy(src.gameObject);
		}, collectionCheck: false, 10, 512);
		if (GlobalEvents.onEnterBuilding == null)
		{
			return;
		}
		float volume = Options.GetVolume(PlayerPrefSettings.GlobalVolume);
		audioMixer.SetFloat("MasterVolume", volume);
		vehicleAudioMixer.SetFloat("attenuation", volume);
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, (Action<Address>)delegate
		{
			outdoorSnapshot.TransitionTo(1f);
		});
		GlobalEvents.RegisterOnGameLoadedCallback(delegate
		{
			GlobalEvents.onFullMenuToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onFullMenuToggle, (Action<bool>)delegate(bool open)
			{
				if (open)
				{
					audioMixer.SetFloat("engine", -80f);
				}
				else
				{
					audioMixer.ClearFloat("engine");
				}
			});
		});
	}

	private void OnEnterBuilding(Address address)
	{
		if (!BuildingHelper.GetBuilding(address).IsHamptonsHouse())
		{
			indoorSnapshot.TransitionTo((!InstanceBehavior<GameManager>.Instance.playerController.awaitingRepositioning) ? 1 : 0);
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		GlobalEvents.onPause = (Action<bool>)Delegate.Remove(GlobalEvents.onPause, new Action<bool>(OnPauseSnapshotChanged));
		_audioSourcePool?.Dispose();
	}

	private void Update()
	{
		if (_audioSourcesReturnQueue.Count == 0)
		{
			return;
		}
		if (_queueNeedsReordering)
		{
			_audioSourcesTempList.Clear();
			_audioSourcesTempList.AddRange(_audioSourcesReturnQueue);
			_audioSourcesTempList.Sort(CompareAudioSourceReturnTimes);
			_audioSourcesReturnQueue.Clear();
			for (int i = 0; i < _audioSourcesTempList.Count; i++)
			{
				_audioSourcesReturnQueue.Enqueue(_audioSourcesTempList[i]);
			}
			_queueNeedsReordering = false;
		}
		while (_audioSourcesReturnQueue.Count != 0 && _audioSourcesReturnQueue.Peek().Item1 < Time.unscaledTime)
		{
			_audioSourcePool.Release(_audioSourcesReturnQueue.Dequeue().Item2);
		}
	}

	private static int CompareAudioSourceReturnTimes(Tuple<float, AudioSource> left, Tuple<float, AudioSource> right)
	{
		return left.Item1.CompareTo(right.Item1);
	}

	public void PlayAudio(SoundType type, Vector3 position, float volumeModifier = 1f, bool isPlayerCreatedSound = false, AudioMixerGroup overwriteAudioMixerGroup = null, float delay = -1f, float addPitch = 0f)
	{
		if (_audioSourcePool != null)
		{
			if (!soundPresetDictionary.TryGetValue(type, out var value))
			{
				Debug.LogWarning($"SoundType {type} not found in SoundPresetDictionary", this);
			}
			else if (!value.useProbability || value.probability.Probability())
			{
				AudioSource audioSource = _audioSourcePool.Get();
				audioSource.transform.position = position;
				SoundPreset.ClipSetting random = value.ClipSettings.GetRandom();
				audioSource.clip = random.audioClip;
				audioSource.pitch = random.pitch.RandomValue() + addPitch;
				audioSource.volume = random.volume * value.volumeMultiplier * volumeModifier;
				audioSource.priority = (isPlayerCreatedSound ? (random.priority / 2) : random.priority);
				audioSource.spatialBlend = (isPlayerCreatedSound ? (value.spatialBlend / 2f) : value.spatialBlend);
				audioSource.reverbZoneMix = value.reverbZoneMix;
				audioSource.dopplerLevel = value.dopplerLevel;
				audioSource.panStereo = value.stereoPan;
				audioSource.spread = value.spread;
				audioSource.rolloffMode = value.volumeRolloff;
				audioSource.minDistance = value.minDistance;
				audioSource.maxDistance = value.maxDistance;
				audioSource.bypassEffects = value.bypassEffects;
				audioSource.bypassListenerEffects = value.bypassListenerEffects;
				audioSource.bypassReverbZones = value.bypassReverbZones;
				audioSource.outputAudioMixerGroup = (overwriteAudioMixerGroup ? overwriteAudioMixerGroup : value.audioMixerGroup);
				audioSource.PlayDelayed(delay);
				_audioSourcesReturnQueue.Enqueue(new Tuple<float, AudioSource>(Time.unscaledTime + random.audioClip.length * 1.5f, audioSource));
				_queueNeedsReordering = true;
			}
		}
	}

	public AudioSource PlayAudio(AudioClip clip, Vector3 position, float volumeMultiplier = 1f, float pitch = 1f, float spatialBlend = 1f, bool isPlayerCreatedSound = false, AudioMixerGroup audioMixerGroup = null)
	{
		if (_audioSourcePool == null)
		{
			return null;
		}
		AudioSource audioSource = _audioSourcePool.Get();
		audioSource.transform.position = position;
		audioSource.Stop();
		audioSource.clip = clip;
		audioSource.pitch = pitch;
		audioSource.volume = audioSourcePrefab.volume * volumeMultiplier;
		audioSource.priority = (isPlayerCreatedSound ? Mathf.Min(256, audioSourcePrefab.priority * 2) : audioSourcePrefab.priority);
		audioSource.spatialBlend = (isPlayerCreatedSound ? (spatialBlend / 2f) : spatialBlend);
		audioSource.reverbZoneMix = audioSourcePrefab.reverbZoneMix;
		audioSource.dopplerLevel = audioSourcePrefab.dopplerLevel;
		audioSource.panStereo = audioSourcePrefab.panStereo;
		audioSource.spread = audioSourcePrefab.spread;
		audioSource.rolloffMode = audioSourcePrefab.rolloffMode;
		audioSource.minDistance = audioSourcePrefab.minDistance;
		audioSource.maxDistance = audioSourcePrefab.maxDistance;
		audioSource.bypassEffects = audioSourcePrefab.bypassEffects;
		audioSource.bypassListenerEffects = audioSourcePrefab.bypassListenerEffects;
		audioSource.bypassReverbZones = audioSourcePrefab.bypassReverbZones;
		audioSource.outputAudioMixerGroup = audioMixerGroup;
		audioSource.Play();
		_audioSourcesReturnQueue.Enqueue(new Tuple<float, AudioSource>(Time.unscaledTime + clip.length * 1.5f, audioSource));
		_queueNeedsReordering = true;
		return audioSource;
	}

	public void SetSoundSnapshot(bool onlyUISound, float transitionTime = 1f)
	{
		SetVehicleMixerSnapshot(onlyUISound, transitionTime);
		if ((BuildingManager.IsInsideBuilding && !InstanceBehavior<BuildingManager>.Instance.building.IsHamptonsHouse()) || UndergroundParkingManager.IsInsideParking)
		{
			(onlyUISound ? onlyUISnapshot : indoorSnapshot).TransitionTo(transitionTime);
		}
		else
		{
			(onlyUISound ? onlyUISnapshot : outdoorSnapshot).TransitionTo(transitionTime);
		}
	}

	private void SetVehicleMixerSnapshot(bool mute, float transitionTime = 1f)
	{
		(mute ? vehicleMutedSnapshot : vehicleNotMutedSnapshot).TransitionTo(transitionTime);
	}

	public void SetSoundSnapshotCityMap(bool onlyUISound, float transitionTime)
	{
		if (onlyUISound)
		{
			onlyUISnapshot.TransitionTo(transitionTime);
		}
		else if (CityMap.IsOpen && !BuildingPreview.isPreviewing)
		{
			cityMapSnapshot.TransitionTo(transitionTime);
		}
	}

	private void OnValidate()
	{
		SoundPreset[] array = soundPresets;
		foreach (SoundPreset soundPreset in array)
		{
			int num = 0;
			soundPreset.name = soundPreset.type.ToString();
			SoundPreset.ClipSetting[] clipSettings = soundPreset.ClipSettings;
			foreach (SoundPreset.ClipSetting clipSetting in clipSettings)
			{
				if ((bool)clipSetting.audioClip)
				{
					clipSetting.name = clipSetting.audioClip.name;
				}
				else
				{
					num++;
				}
			}
			if (num != 0)
			{
				soundPreset.name += $" Errors {num}";
			}
			else
			{
				soundPreset.name += $" (clips {soundPreset.ClipSettings.Length})";
			}
		}
	}
}
