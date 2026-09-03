using System.Collections;
using System.Collections.Generic;
using Extensions;
using UnityEngine;

namespace Items.SpecialItems;

public class SandCastleController : MonoBehaviour
{
	private const string PlayerTag = "Player";

	[SerializeField]
	private GameObject sandCastleObj;

	[SerializeField]
	private GameObject destroyedSandCastleObj;

	[SerializeField]
	private ParticleSystem destroyParticles;

	[SerializeField]
	private AudioSource destroyAudioSource;

	[SerializeField]
	private List<AudioClip> destroyAudioClips;

	private Coroutine _coroutine;

	private bool _isDestroyed;

	private WaitForSeconds _swapModelsWait;

	private void Awake()
	{
		_swapModelsWait = new WaitForSeconds(destroyParticles.totalTime / 2f);
	}

	private void OnEnable()
	{
		SetDefaultState();
	}

	private void OnDisable()
	{
		if (_coroutine != null)
		{
			StopCoroutine(_coroutine);
		}
		_coroutine = null;
		destroyParticles.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (!_isDestroyed && other.CompareTag("Player"))
		{
			DestroySandCastle();
		}
	}

	private void SetDefaultState()
	{
		sandCastleObj.SetActive(value: true);
		destroyedSandCastleObj.SetActive(value: false);
		_isDestroyed = false;
	}

	private void DestroySandCastle()
	{
		if (!_isDestroyed)
		{
			_isDestroyed = true;
			destroyAudioSource.PlayOneShot(destroyAudioClips.GetRandom());
			_coroutine = StartCoroutine(DestroySandCastleCoroutine());
			if (!SaveGameManager.Current.achievementsData.destroyedSandCastle)
			{
				SaveGameManager.Current.achievementsData.destroyedSandCastle = true;
				GameEvent.Invoke("ba:gameevent_destroyedsandcastle");
			}
		}
	}

	private IEnumerator DestroySandCastleCoroutine()
	{
		destroyParticles.Play();
		yield return _swapModelsWait;
		sandCastleObj.SetActive(value: false);
		destroyedSandCastleObj.SetActive(value: true);
		_coroutine = null;
	}
}
