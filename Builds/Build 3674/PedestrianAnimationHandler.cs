using System.Collections;
using AI;
using BigAmbitions.Characters;
using BigAmbitions.DayNightCycle;
using Extensions;
using UnityEngine;

public class PedestrianAnimationHandler
{
	private static readonly PermanentAnimationType[] RandomPermanentAnimationTypes = new PermanentAnimationType[3]
	{
		PermanentAnimationType.TalkingPhone,
		PermanentAnimationType.TextingPhone,
		PermanentAnimationType.HoldingIceCream
	};

	private readonly UmbrellaHandler _umbrellaHandler;

	private readonly BaseHuman _baseHuman;

	private Coroutine _currentAnimationCoroutine;

	private PermanentAnimationType _currentAnimation;

	private Timestamp _nextEatIceCreamAnimationTimestamp;

	private bool _enabled;

	public PedestrianAnimationHandler(BaseHuman baseHuman)
	{
		_umbrellaHandler = new UmbrellaHandler(baseHuman, baseHuman.HasUmbrella(), ResetAnimation, OnUmbrellaRemoved);
		_baseHuman = baseHuman;
	}

	private void OnUmbrellaRemoved()
	{
		if (RngHelper.Chance(50))
		{
			PlayRandomAnimation();
		}
	}

	public void Enable()
	{
		_enabled = true;
		ResetBaseHuman();
		_umbrellaHandler?.OnEnable();
		if (!RainHelper.AreRainDropsFalling && RngHelper.Chance(50))
		{
			PlayRandomAnimation();
		}
	}

	public void Disable()
	{
		_enabled = false;
		_umbrellaHandler?.ForceRemoveUmbrella();
		if (_baseHuman != null)
		{
			_baseHuman.RemoveHandObject();
			ResetAnimation();
			_baseHuman.ResetAnimator();
		}
	}

	public void Update()
	{
		if (_enabled)
		{
			_umbrellaHandler?.Update();
			if (_currentAnimation == PermanentAnimationType.HoldingIceCream && _nextEatIceCreamAnimationTimestamp.IsInThePast())
			{
				_baseHuman.animator.RunAnimationLength(AnimationType.EatIceCream);
				SetIceCreamAnimationTimeStamp();
			}
		}
	}

	private void ResetBaseHuman()
	{
		_baseHuman.RemoveHandObject();
		_baseHuman.StopHoldingAnItem();
		ResetAnimation();
	}

	private void ResetAnimation()
	{
		_baseHuman.animator.SetBool(_currentAnimation, state: false);
		_currentAnimation = PermanentAnimationType.Drunk;
	}

	private void PlayRandomAnimation()
	{
		if (_umbrellaHandler == null || !_umbrellaHandler.IsHoldingUmbrella())
		{
			if (_currentAnimationCoroutine != null)
			{
				_baseHuman.StopCoroutine(_currentAnimationCoroutine);
				_currentAnimationCoroutine = null;
			}
			ResetBaseHuman();
			StartRandomAnimation();
			if (_currentAnimation == PermanentAnimationType.HoldingIceCream || RngHelper.Chance(50))
			{
				_currentAnimationCoroutine = _baseHuman.StartCoroutine(StopAnimationCoroutine());
			}
		}
	}

	private void StartRandomAnimation()
	{
		_currentAnimation = RandomPermanentAnimationTypes.GetRandom();
		_baseHuman.animator.SetBool(_currentAnimation);
		string handObjectNameFromPermanentAnimationType = BaseHuman.GetHandObjectNameFromPermanentAnimationType(_currentAnimation);
		_baseHuman.AddHandObject(handObjectNameFromPermanentAnimationType);
		if (_currentAnimation == PermanentAnimationType.HoldingIceCream)
		{
			SetIceCreamAnimationTimeStamp();
		}
	}

	private void SetIceCreamAnimationTimeStamp()
	{
		if (_nextEatIceCreamAnimationTimestamp == null)
		{
			_nextEatIceCreamAnimationTimestamp = TimeHelper.Now();
		}
		else
		{
			_nextEatIceCreamAnimationTimestamp.SetCurrentTime();
		}
		_nextEatIceCreamAnimationTimestamp.AddMinutes(Random.Range(3f, 6f));
	}

	private IEnumerator StopAnimationCoroutine()
	{
		yield return new WaitForSeconds(Random.Range(5f, 50f));
		ResetBaseHuman();
		yield return new WaitForSeconds(Random.Range(5f, 50f));
		if (RngHelper.Chance(50))
		{
			PlayRandomAnimation();
		}
	}
}
