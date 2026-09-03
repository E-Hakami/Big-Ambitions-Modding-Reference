using System;
using JimmysUnityUtilities;
using Player.Sound.Radio;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Player.HUD.ItemInfoOverlays;

public class RadioOverlay : IOverlay
{
	[Header("Radio")]
	[SerializeField]
	private Slider slider;

	[SerializeField]
	private TMP_Text stationField;

	[SerializeField]
	private TMP_Text songField;

	[SerializeField]
	private GameObject songControlsObj;

	public override bool IsValid(EntityController entityController)
	{
		return entityController is LoudspeakerController;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		return InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		songControlsObj.SetActive(!LoudSpeakersManager.IsMuted);
		SetSlider();
		SetSong();
	}

	private void SetSlider()
	{
		SetSliderValueOnCurrentBuilding();
		LoudspeakerController.onBuildingRadioVolumeChanged = (Action)Delegate.Combine(LoudspeakerController.onBuildingRadioVolumeChanged, new Action(SetSliderValueOnCurrentBuilding));
		slider.onValueChanged.RemoveAllListeners();
		slider.onValueChanged.AddListener(OnVolumeChanged);
	}

	private void OnVolumeChanged(float volume)
	{
		BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
		if (buildingRegistration != null)
		{
			buildingRegistration.radioVolume = (float)((!(buildingRegistration.radioVolume < 0f)) ? 1 : (-1)) * volume;
			if (buildingRegistration.radioVolume >= 0f)
			{
				LoudspeakerController.onBuildingRadioVolumeChanged?.Invoke();
			}
		}
	}

	private void SetSong()
	{
		stationField.text = LoudSpeakersManager.StationName;
		songField.text = LoudSpeakersManager.SongName;
	}

	private void SetSliderValueOnCurrentBuilding()
	{
		SetSliderValue(Mathf.Abs(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.GetBusinessRadioVolume()));
	}

	private void SetSliderValue(float value)
	{
		slider.SetValueWithoutNotify(value);
	}

	public void NextStation()
	{
		LoudSpeakersManager.PlayNextStation();
		CoroutineUtility.RunAfterOneFrame(UpdateUI);
	}

	private void UpdateUI()
	{
		SetSong();
		LayoutRebuilder.ForceRebuildLayoutImmediate(stationField.transform.parent as RectTransform);
	}

	public void ToggleRadio()
	{
		InstanceBehavior<BuildingManager>.Instance.buildingRegistration.radioVolume *= -1f;
		LoudspeakerController.onBuildingRadioVolumeChanged?.Invoke();
		songControlsObj.SetActive(!LoudSpeakersManager.IsMuted);
	}

	private void OnDestroy()
	{
		LoudspeakerController.onBuildingRadioVolumeChanged = (Action)Delegate.Remove(LoudspeakerController.onBuildingRadioVolumeChanged, new Action(SetSliderValueOnCurrentBuilding));
	}
}
