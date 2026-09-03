using System;
using Extensions;
using GleyTrafficSystem;
using UI.Load;
using UnityEngine;

public class AiCarMusic : MonoBehaviour
{
	public AudioSource musicPlayer;

	private void Start()
	{
		musicPlayer.outputAudioMixerGroup = TrafficComponent.Instance.musicMixerGroup;
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, (Action<Address>)delegate
		{
			musicPlayer.Stop();
		});
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, (Action<Address>)delegate
		{
			if (base.isActiveAndEnabled)
			{
				DoPlay();
			}
		});
	}

	public void OnDisable()
	{
		musicPlayer.Stop();
	}

	public void OnEnable()
	{
		DoPlay();
	}

	private void DoPlay()
	{
		if (RngHelper.Chance(10))
		{
			if (LoadScene.isLoading)
			{
				GlobalEvents.RegisterOnGameLoadedCallback(PlayRandom);
			}
			else
			{
				PlayRandom();
			}
		}
	}

	private void PlayRandom()
	{
		if (BuildingManager.IsInsideBuilding)
		{
			musicPlayer.Stop();
			return;
		}
		musicPlayer.time = UnityEngine.Random.Range(0f, musicPlayer.clip.length);
		musicPlayer.Play();
	}
}
