using System;
using System.Collections;
using BigAmbitions.Characters;
using DG.Tweening;
using Extensions;
using Helpers;
using UnityEngine;

namespace Controllers;

[Serializable]
public class BigStrikersUnit
{
	private const int MinIdleSeconds = 3;

	private const float BellRingerMovementDuration = 0.6f;

	private const float BellRingerStartDurationOffset = 0.1f;

	private const float BellRingerMaxYOffset = 0.3f;

	private const float BellRingerMinYOffset = 1f;

	private const float BellRingerChanceMaxHeight = 0.05f;

	private const float BellRingerStayDuration = 0.2f;

	private const float WaitAfterBellRingerDuration = 2f;

	public Transform characterPosition;

	public Transform bellRinger;

	public bool isPlayerSpot;

	public float maxYPositionForBellRinger;

	[SerializeField]
	private HandObjectData hammerHandData;

	[SerializeField]
	private AudioSource audioSource;

	[HideInInspector]
	public bool isOccupied;

	private ThirdPersonCharacter _tpc;

	private CarnivalPedestrian _carnivalPedestrian;

	private BigStrikers _bigStrikers;

	private Vector3 _initialHammerPosition;

	private Quaternion _initialHammerRotation;

	private Vector3 _initialBellRingerPosition;

	private WaitForSeconds _waitForCharacterAnimationToEnd;

	private WaitForSeconds _waitForHammerIdleSeconds;

	private WaitForSeconds _waitForBellRingerStaySeconds;

	private WaitForSeconds _waitBeforeBellRingerSeconds;

	private WaitForSeconds _waitAfterBellRingerSeconds;

	private IEnumerator _animationCoroutine;

	public void Init(BigStrikers bigStrikers)
	{
		_bigStrikers = bigStrikers;
		SetInitialPositions();
		float animationLength = PlayerHelper.GetAnimator().GetAnimationLength(AnimationType.HammerHitting);
		_waitForBellRingerStaySeconds = new WaitForSeconds(0.2f);
		float seconds = animationLength - 0.1f;
		_waitBeforeBellRingerSeconds = new WaitForSeconds(seconds);
		_waitForCharacterAnimationToEnd = new WaitForSeconds(0.1f);
		_waitAfterBellRingerSeconds = new WaitForSeconds(2f);
		SetIdleSeconds(animationLength);
	}

	private void SetInitialPositions()
	{
		_initialHammerPosition = hammerHandData.handObject.position;
		_initialHammerRotation = hammerHandData.handObject.rotation;
		_initialBellRingerPosition = bellRinger.position;
	}

	private void SetIdleSeconds(float animationLength)
	{
		float num = Mathf.Ceil(animationLength) - animationLength;
		float seconds = 3f + num;
		_waitForHammerIdleSeconds = new WaitForSeconds(seconds);
	}

	public void ResetHammer()
	{
		hammerHandData.handObject.SetParent(hammerHandData.handObjectParent);
		hammerHandData.handObject.position = _initialHammerPosition;
		hammerHandData.handObject.rotation = _initialHammerRotation;
	}

	public void Use(ThirdPersonCharacter tpc, CarnivalPedestrian carnivalPedestrian = null)
	{
		_tpc = tpc;
		_carnivalPedestrian = carnivalPedestrian;
		tpc.navmeshAgent.Warp(characterPosition.position);
		tpc.ForceToRotation(characterPosition.rotation);
		tpc.animator.ResetTrigger(AnimationType.HammerHitting);
		isOccupied = true;
		AddHammerToHand(tpc);
		_animationCoroutine = PlayAnimationSequence();
		_bigStrikers.StartCoroutine(_animationCoroutine);
	}

	private void AddHammerToHand(ThirdPersonCharacter tpc)
	{
		Transform handObject = hammerHandData.handObject;
		handObject.SetParent(tpc.leftHand);
		handObject.localPosition = hammerHandData.handObjectPosition;
		handObject.localEulerAngles = hammerHandData.handObjectRotation;
	}

	private void ResetAfterAnimation()
	{
		if (!(_tpc == null))
		{
			_tpc.animator.SetBool(PermanentAnimationType.HammerIdle, state: false);
			_tpc = null;
			ResetHammer();
		}
	}

	private IEnumerator PlayAnimationSequence()
	{
		_tpc.animator.SetBool(PermanentAnimationType.HammerIdle);
		yield return _waitForHammerIdleSeconds;
		_tpc.animator.SetTrigger(AnimationType.HammerHitting);
		yield return _waitBeforeBellRingerSeconds;
		audioSource.PlayOneShot(_bigStrikers.hammerImpactSound);
		AnimateBellRinger();
		yield return _waitForCharacterAnimationToEnd;
		ResetAfterAnimation();
		if (_carnivalPedestrian != null)
		{
			yield return _waitAfterBellRingerSeconds;
			isOccupied = false;
			_bigStrikers.npcPositionGiver.FreePositionAtIndex(_carnivalPedestrian.GetCurrentWaitingIndex());
			_carnivalPedestrian.OnCarnivalItemEnd();
			_carnivalPedestrian = null;
		}
	}

	private void AnimateBellRinger()
	{
		float minInclusive = _initialBellRingerPosition.y + 1f;
		float maxInclusive = maxYPositionForBellRinger - 0.3f;
		float targetY = (0.05f.Probability() ? maxYPositionForBellRinger : UnityEngine.Random.Range(minInclusive, maxInclusive));
		ShortcutExtensions.DOMove(endValue: new Vector3(_initialBellRingerPosition.x, targetY, _initialBellRingerPosition.z), target: bellRinger, duration: 0.6f).SetEase(Ease.OutQuad).OnComplete(delegate
		{
			audioSource.PlayOneShot(Mathf.Approximately(targetY, maxYPositionForBellRinger) ? _bigStrikers.topScoreBellSound : _bigStrikers.normalBellSound);
			_bigStrikers.StartCoroutine(WaitAndReturnBellRinger());
		});
	}

	private IEnumerator WaitAndReturnBellRinger()
	{
		yield return _waitForBellRingerStaySeconds;
		bellRinger.DOMove(_initialBellRingerPosition, 0.6f).SetEase(Ease.InQuad);
	}

	public void Cancel()
	{
		if (_animationCoroutine != null)
		{
			_bigStrikers.StopCoroutine(_animationCoroutine);
		}
		ResetAfterAnimation();
		isOccupied = false;
	}
}
