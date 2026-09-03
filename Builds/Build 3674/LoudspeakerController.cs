using System;
using System.Collections;
using BigAmbitions.Items;
using Buildings.Indoors.InteriorDesign;
using Helpers;
using Player.Sound.Radio;
using UnityEngine;

public class LoudspeakerController : ItemController
{
	public static Action onBuildingRadioVolumeChanged;

	[Header("Audio Settings")]
	public float audioVolume = 1f;

	public float audioMinDistance = 2f;

	public float audioMaxDistance = 100f;

	[Tooltip("Amount of audio volume that is directional. 0 means the audio is full-volume at all angles, 1 means the audio is full-volume only in the direction the speaker is facing.")]
	[Range(0f, 1f)]
	public float directional = 1f;

	private bool _isHamptonsHouse;

	public float AudioMaxDistanceSqr => audioMaxDistance * audioMaxDistance;

	private void OnEnable()
	{
		PlayerItemPurchaserSettings obj = playerItemPurchaserSettings;
		if ((obj == null || !obj.enabled) && !InteriorDesignerHelper.BlueprintCreatorMode)
		{
			StartCoroutine(DeferEnable());
		}
	}

	private void OnEnterBuilding(Address address)
	{
		if (!(address != base.ItemInstance.AddressCached))
		{
			EnableLoudspeaker();
		}
	}

	private void OnExitBuilding(Address address)
	{
		if (!(address != base.ItemInstance.AddressCached))
		{
			DisableLoudspeaker();
		}
	}

	private IEnumerator DeferEnable()
	{
		yield return null;
		_isHamptonsHouse = BuildingHelper.GetBuilding(base.ItemInstance.AddressCached)?.IsHamptonsHouse() ?? false;
		if (_isHamptonsHouse)
		{
			GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
			GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
			if (BuildingManager.IsInsideBuilding)
			{
				OnEnterBuilding(base.ItemInstance.AddressCached);
			}
		}
		else
		{
			EnableLoudspeaker();
		}
	}

	private void EnableLoudspeaker()
	{
		PlayerItemPurchaserSettings obj = playerItemPurchaserSettings;
		if ((obj == null || !obj.enabled) && !InteriorDesignerHelper.BlueprintCreatorMode)
		{
			LoudSpeakersManager.AddSpeaker(this);
		}
	}

	private void OnDisable()
	{
		DisableLoudspeaker();
	}

	private void DisableLoudspeaker()
	{
		StopCoroutine(DeferEnable());
		if (!(InstanceBehavior<GameManager>.Instance == null) && !InteriorDesignerHelper.BlueprintCreatorMode)
		{
			LoudSpeakersManager.RemoveSpeaker(this);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if (_isHamptonsHouse)
		{
			GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
			GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		}
	}
}
