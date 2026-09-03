using System;
using Helpers;
using JimmysUnityUtilities;
using UI.Load;

public class BuildingOutsideMusic : MusicPlayer
{
	public CityBuildingController buildingController;

	private bool _hasMusicOutside;

	private Action _onTimeMachineEndAction;

	private bool ShouldPlayMusic()
	{
		if (BusinessHelper.IsBusinessOpen(buildingController.buildingRegistration) && !BuildingManager.IsInsideBuilding)
		{
			return _hasMusicOutside;
		}
		return false;
	}

	private void Awake()
	{
		audioSource.outputAudioMixerGroup = InstanceBehavior<GlobalReferences>.Instance.outsideBuildingMusicMixerGroup;
	}

	protected override void Start()
	{
	}

	public void Init(CityBuildingController cityBuildingController)
	{
		buildingController = cityBuildingController;
		base.transform.position = cityBuildingController.entranceDoors[0].doorTransform.transform.position;
		base.transform.rotation = cityBuildingController.entranceDoors[0].doorTransform.transform.rotation;
		base.gameObject.SetActive(value: true);
		SubscribeToGlobalEvents();
		InitMusic();
	}

	public void ReleaseFromPool()
	{
		UnSubscribeToGlobalEvents();
		Stop();
		base.gameObject.SetActive(value: false);
		InstanceBehavior<CityManager>.Instance.buildingOutsideMusicSpawner.buildingOutsideMusicPool.Release(this);
	}

	protected override void SubscribeToGlobalEvents()
	{
		base.SubscribeToGlobalEvents();
		InstanceBehavior<GameManager>.Instance.radioPlayer.onSongsRefreshing.AddListener(OnSongsRefreshing);
		InstanceBehavior<GameManager>.Instance.radioPlayer.onSongsLoaded.AddListener(OnSongsLoaded);
		InstanceBehavior<GameManager>.Instance.radioPlayer.onNextSong.AddListener(OnNextSong);
		GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(PlayMusicIfPossible));
		_onTimeMachineEndAction = delegate
		{
			CoroutineUtility.RunAfterOneFrame(PlayMusicIfPossible);
		};
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Combine(GlobalEvents.onTimeMachineEnded, _onTimeMachineEndAction);
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
	}

	protected override void UnSubscribeToGlobalEvents()
	{
		base.UnSubscribeToGlobalEvents();
		InstanceBehavior<GameManager>.Instance?.radioPlayer.onSongsRefreshing.RemoveListener(OnSongsRefreshing);
		InstanceBehavior<GameManager>.Instance?.radioPlayer.onSongsLoaded.RemoveListener(OnSongsLoaded);
		InstanceBehavior<GameManager>.Instance?.radioPlayer.onNextSong.RemoveListener(OnNextSong);
		GlobalEvents.onNewHour = (Action)Delegate.Remove(GlobalEvents.onNewHour, new Action(PlayMusicIfPossible));
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Remove(GlobalEvents.onTimeMachineEnded, _onTimeMachineEndAction);
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
	}

	private void OnBuildingRegistrationChange(Address address)
	{
		if (!(address != buildingController.building.Address))
		{
			SetHasMusicOutside();
			PlayMusicIfPossible();
		}
	}

	private void OnSongsRefreshing()
	{
		if (currentStation == RadioStation.LocalFiles)
		{
			Stop();
		}
	}

	private void OnSongsLoaded()
	{
		if (!LoadScene.isLoading && _hasMusicOutside && currentStation == RadioStation.LocalFiles)
		{
			PlayStation(currentStation);
			PlayMusicIfPossible();
			SetPause(isPaused);
		}
	}

	private void OnNextSong(RadioStation radioStation)
	{
		if (radioStation == currentStation && ShouldPlayMusic())
		{
			PlaySong();
		}
	}

	private RadioStation GetBusinessRadioStation()
	{
		return buildingController.buildingRegistration.GetBusinessRadioStation();
	}

	private void PlayMusicIfPossible()
	{
		if (ShouldPlayMusic())
		{
			DoPlay();
		}
		else
		{
			Stop();
		}
	}

	private void DoPlay()
	{
		if (!audioSource.isPlaying && !isPaused && !InstanceBehavior<GameManager>.Instance.radioPlayer.GetRadioStationData(currentStation).IsLoading)
		{
			PlayStation(currentStation);
		}
	}

	private void InitMusic()
	{
		currentStation = GetBusinessRadioStation();
		SetHasMusicOutside();
		PlayMusicIfPossible();
	}

	private void SetHasMusicOutside()
	{
		_hasMusicOutside = BusinessTypeHelper.GetData(buildingController.buildingRegistration).hasMusicOutside;
	}

	private void PlayStation(RadioStation radioStation)
	{
		RadioStationData radioStationData = InstanceBehavior<GameManager>.Instance.radioPlayer.GetRadioStationData(radioStation);
		if (radioStationData.HasPlayableClips)
		{
			CorrespondingSongData correspondingSong = InstanceBehavior<GameManager>.Instance.radioPlayer.GetCorrespondingSong(radioStationData);
			if (!(correspondingSong.audioClip == null))
			{
				UpdateCurrentSong(correspondingSong.audioClip, correspondingSong.songTime);
			}
		}
	}
}
