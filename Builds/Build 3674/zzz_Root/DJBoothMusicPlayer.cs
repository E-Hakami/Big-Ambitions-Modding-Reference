using Helpers;
using Player.Sound.Radio;
using UI;
using UnityEngine;

public class DJBoothMusicPlayer : MonoBehaviour
{
	private bool _speakersMuted;

	private void Awake()
	{
		GetComponent<AudioSource>().outputAudioMixerGroup = InstanceBehavior<GlobalReferences>.Instance.loudspeakerMixerGroup;
	}

	public void Enable()
	{
		DisableSpeakersIfNeeded();
		InstanceBehavior<GameManager>.Instance.radioPlayer.ToggleMute(isMute: false);
		InstanceBehavior<GameManager>.Instance.radioPlayer.onRadioToggle.Invoke(arg0: true);
		InstanceBehavior<UIs>.Instance.smartphoneUI.radioControls.EnableUI();
	}

	public void Disable()
	{
		EnableSpeakersIfNeeded();
		if (!PlayerHelper.IsWearingHeadset())
		{
			InstanceBehavior<GameManager>.Instance.radioPlayer.ToggleMute(isMute: true);
			InstanceBehavior<GameManager>.Instance.radioPlayer.onRadioToggle.Invoke(arg0: false);
			InstanceBehavior<UIs>.Instance.smartphoneUI.radioControls.DisableUI();
		}
	}

	private void DisableSpeakersIfNeeded()
	{
		if (!LoudSpeakersManager.IsMuted)
		{
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.radioVolume *= -1f;
			LoudspeakerController.onBuildingRadioVolumeChanged?.Invoke();
			_speakersMuted = true;
		}
	}

	private void EnableSpeakersIfNeeded()
	{
		if (_speakersMuted)
		{
			_speakersMuted = false;
			InstanceBehavior<BuildingManager>.Instance.buildingRegistration.radioVolume *= -1f;
			LoudspeakerController.onBuildingRadioVolumeChanged?.Invoke();
		}
	}
}
