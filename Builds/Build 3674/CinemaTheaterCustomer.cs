using System;
using System.Collections;
using BigAmbitions.Characters;
using BigAmbitions.Items;
using Buildings.Retail.Businesses.CinemaTheater;
using Controllers;
using JimmysUnityUtilities;
using UnityEngine;

public class CinemaTheaterCustomer : Customer
{
	public const float ClappingStartMaxDelay = 0.3f;

	private const float SeatAnimatorDisableDelay = 1f;

	private const float SeatAnimatorDisableDelayClapping = 5f;

	private const float ClappingResetDelay = 0.5f;

	private static readonly int Clapping = Animator.StringToHash("Clapping");

	private static readonly int ClappingSpeed = Animator.StringToHash("ClappingSpeed");

	private static readonly WaitForSeconds DelayedResetClappingWait = new WaitForSeconds(0.5f);

	private bool _isSitting;

	private bool _isCulled;

	private Coroutine _seatCoroutine;

	private float _clappingTimer;

	protected override void Awake()
	{
		base.Awake();
		ThirdPersonCharacter thirdPersonCharacter = tpc;
		thirdPersonCharacter.onSittingChanged = (Action<bool>)Delegate.Combine(thirdPersonCharacter.onSittingChanged, new Action<bool>(OnSittingChanged));
	}

	public override void Init()
	{
		base.Init();
		behaviorTree.EnableBehavior();
	}

	private void OnDisable()
	{
		tpc.animator.SetInteger(Clapping, 0);
	}

	private void OnSittingChanged(bool isSitting)
	{
		if (_isSitting == isSitting)
		{
			return;
		}
		_isSitting = isSitting;
		if (isSitting)
		{
			tpc.SetHandContent(null);
		}
		else
		{
			tpc.animator.SetInteger(Clapping, 0);
		}
		if (!_isCulled)
		{
			if (_seatCoroutine != null)
			{
				StopCoroutine(_seatCoroutine);
			}
			if (isSitting)
			{
				_seatCoroutine = StartCoroutine(DelayedSitStill(1f));
				return;
			}
			_seatCoroutine = null;
			UpdateAnimatorEnabled();
		}
	}

	public override void ResetVisibilityValues()
	{
		base.ResetVisibilityValues();
		_isCulled = false;
	}

	private IEnumerator DelayedSitStill(float delay)
	{
		yield return new WaitForSeconds(delay);
		_seatCoroutine = null;
		UpdateAnimatorEnabled();
	}

	private IEnumerator DelayedResetClapping()
	{
		yield return DelayedResetClappingWait;
		tpc.animator.SetInteger(Clapping, 0);
	}

	public override void OnCustomerVisibleChanged(bool visible)
	{
		bool flag = !visible;
		if (_isCulled == flag)
		{
			return;
		}
		_isCulled = flag;
		base.OnCustomerVisibleChanged(visible);
		if (_seatCoroutine != null)
		{
			StopCoroutine(_seatCoroutine);
		}
		_seatCoroutine = null;
		if (!_isCulled)
		{
			if (_isSitting)
			{
				tpc.animator.enabled = true;
				PermanentAnimationType value = ((tpc.isSittingOn.TryGetComponentInParent<SeatController>(out var component) && component.Item.leanBackSitAnimation) ? PermanentAnimationType.SittingOnCinemaChair : PermanentAnimationType.Sitting);
				tpc.animator.Play(value.ToStringFast(), -1, 1f);
				_seatCoroutine = StartCoroutine(DelayedSitStill(1f));
			}
			UpdateAnimatorEnabled();
		}
	}

	private void UpdateAnimatorEnabled()
	{
		tpc.animator.enabled = !_isCulled && (!_isSitting || _seatCoroutine != null);
	}

	public void StartClapping()
	{
		if (tpc.isActiveAndEnabled && (bool)tpc.isSittingOn && CinemaTheaterHelper.IsValidSittingPosition(tpc.isSittingOn))
		{
			tpc.animator.enabled = true;
			tpc.animator.SetInteger(Clapping, UnityEngine.Random.Range(2, 4));
			tpc.animator.SetFloat(ClappingSpeed, UnityEngine.Random.Range(0.8f, 1.2f));
			if (_seatCoroutine != null)
			{
				StopCoroutine(_seatCoroutine);
			}
			_seatCoroutine = StartCoroutine(DelayedSitStill(5f));
			StartCoroutine(DelayedResetClapping());
		}
	}

	public override void SetCurrentTimeState()
	{
		if (customerEntry.spawnTime.GetTotalMinutes() <= TimeHelper.NowInMinutes() - 2f)
		{
			customerTimeState = CustomerTimeState.AlreadyInAction;
		}
		else if (customerEntry.spawnTime.GetTotalMinutes() <= TimeHelper.NowInMinutes() - 1f)
		{
			customerTimeState = CustomerTimeState.RecentlyArrived;
		}
		else
		{
			customerTimeState = CustomerTimeState.JustArrived;
		}
	}

	protected override void ReleaseGameObject()
	{
		InstanceBehavior<BuildingManager>.Instance.customerSpawner.ReleaseCustomer(this, CustomerType.CinemaTheater);
	}

	protected override string GetContainerItemName()
	{
		return ItemsGetter.GetRandomBag();
	}
}
