using System.Collections;
using System.Collections.Generic;
using Extensions;
using GleyTrafficSystem;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AiCarHorn : MonoBehaviour
{
	private const float MinTimeBetweenHorns = 2f;

	private static readonly WaitForSeconds WaitForSecondsInstruction = new WaitForSeconds(2f);

	private static readonly List<SpecialDriveActionTypes> StopType = new List<SpecialDriveActionTypes>
	{
		SpecialDriveActionTypes.StopNow,
		SpecialDriveActionTypes.StopInPoint,
		SpecialDriveActionTypes.TempStop
	};

	private static readonly WaitForSecondsRealtime WaitLong = new WaitForSecondsRealtime(0.2f);

	private static readonly WaitForSecondsRealtime WaitShort = new WaitForSecondsRealtime(0.15f);

	[SerializeField]
	private AudioClip[] randomHornClips;

	[SerializeField]
	private VehicleComponent vehicleComponent;

	[SerializeField]
	private AudioSource audioSource;

	private bool _canTrigger = true;

	private Coroutine _hailCoroutine;

	private void Start()
	{
		audioSource.outputAudioMixerGroup = TrafficComponent.Instance.hornMixerGroup;
	}

	private void OnDisable()
	{
		StopAllCoroutines();
		_canTrigger = true;
		_hailCoroutine = null;
		audioSource.spatialBlend = 1f;
	}

	private void OnTriggerEnter(Collider other)
	{
		if (_canTrigger && base.enabled && vehicleComponent.GetCurrentSpeed() >= 2f && other.gameObject.CompareTag("Player") && !StopType.Contains(vehicleComponent.GetCurrentAction()) && InstanceBehavior<GameManager>.Instance.selectedVehicle == null)
		{
			audioSource.clip = randomHornClips.GetRandom();
			audioSource.Play();
			_canTrigger = false;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (!_canTrigger && base.enabled && other.gameObject.CompareTag("Player"))
		{
			StartCoroutine(WaitForNextHorn());
		}
	}

	private IEnumerator WaitForNextHorn()
	{
		yield return WaitForSecondsInstruction;
		_canTrigger = true;
	}

	public void Hail(bool disableAfterwards = true)
	{
		if (_hailCoroutine == null)
		{
			base.enabled = true;
			_hailCoroutine = StartCoroutine(HailCoroutine(disableAfterwards));
		}
	}

	private IEnumerator HailCoroutine(bool disableAfterwards)
	{
		base.enabled = true;
		_canTrigger = false;
		audioSource.spatialBlend = 0.5f;
		audioSource.clip = randomHornClips.GetRandom();
		audioSource.Play();
		yield return WaitLong;
		audioSource.Stop();
		yield return WaitShort;
		audioSource.Play();
		yield return WaitLong;
		audioSource.Stop();
		audioSource.spatialBlend = 1f;
		_canTrigger = true;
		_hailCoroutine = null;
		if (disableAfterwards)
		{
			base.enabled = false;
		}
	}
}
