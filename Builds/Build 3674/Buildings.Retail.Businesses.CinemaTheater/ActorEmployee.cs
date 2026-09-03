using System.Collections;
using BigAmbitions.Characters;
using Buildings.BuildingTypes.Retail.Businesses.CinemaTheater;
using DG.Tweening;
using UnityEngine;

namespace Buildings.Retail.Businesses.CinemaTheater;

public class ActorEmployee : Employee
{
	private RuntimeAnimatorController _originalAnimatorController;

	private bool _isActing;

	private bool _inTransition;

	private float _timer;

	private TheaterStage _currentStage;

	private ActorEmployeeAnimationEvents _animationEvents;

	private static ActorEmployeeAnimationSet AnimationSet => InstanceBehavior<GlobalReferences>.Instance.actorEmployeeAnimationSet;

	public bool InTransition => _inTransition;

	protected override void Awake()
	{
		base.Awake();
		_animationEvents = employeeTpc.animator.gameObject.AddComponent<ActorEmployeeAnimationEvents>();
	}

	private void OnEnable()
	{
		TheaterStage.DeferRedistributeActors();
	}

	private void OnDisable()
	{
		if ((bool)_animationEvents)
		{
			_animationEvents.StopSound();
		}
	}

	protected override void Update()
	{
		base.Update();
		if (_isActing && !_inTransition)
		{
			_timer -= Time.deltaTime;
			if (_timer < 0f)
			{
				StartNewAnimation();
			}
		}
	}

	protected override void TryStartToiletCoroutine()
	{
	}

	public void ClearActingStage(bool walkInOut)
	{
		if ((bool)_currentStage)
		{
			Transform stageTransform = _currentStage.transform;
			_currentStage = null;
			_isActing = false;
			StartCoroutine(WalkOutOfStage(stageTransform, walkInOut));
		}
	}

	public void SetActingStage(TheaterStage stage, Vector3 targetPosition, Quaternion targetRotation, bool walkInOut)
	{
		if ((bool)stage)
		{
			_currentStage = stage;
			if (!_isActing)
			{
				_isActing = true;
				StartCoroutine(WalkOntoStage(targetPosition, targetRotation, walkInOut));
			}
		}
	}

	private Transform GetDressingRoomChair()
	{
		if (!employeeStationController)
		{
			return null;
		}
		return employeeStationController.GetEmployeeSpot();
	}

	private void StartNewAnimation()
	{
		bool isFemale = employeeInstance.characterData.gender == Gender.Female;
		float num = 0f;
		ActorEmployeeAnimationSet.AnimationInfo[] animations = AnimationSet.animations;
		foreach (ActorEmployeeAnimationSet.AnimationInfo animationInfo in animations)
		{
			num += animationInfo.GetChance(isFemale);
		}
		float num2 = Random.Range(0f, num);
		animations = AnimationSet.animations;
		foreach (ActorEmployeeAnimationSet.AnimationInfo animationInfo2 in animations)
		{
			float chance = animationInfo2.GetChance(isFemale);
			if (num2 <= chance)
			{
				employeeTpc.animator.SetTrigger(animationInfo2.trigger);
				RandomizeTimer();
				_timer += animationInfo2.minDurationBeforeChange;
				break;
			}
			num2 -= chance;
		}
	}

	private void RandomizeTimer()
	{
		_timer = Random.Range(AnimationSet.intervalMin, AnimationSet.intervalMax) + AnimationSet.intervalAddPerExtraActor * (float)(_currentStage.CurrentActorCount - 1);
	}

	private IEnumerator WalkOutOfStage(Transform stageTransform, bool walkInOut)
	{
		if (_inTransition)
		{
			yield break;
		}
		_inTransition = true;
		if ((bool)_originalAnimatorController)
		{
			employeeTpc.animator.runtimeAnimatorController = _originalAnimatorController;
		}
		if ((bool)_animationEvents)
		{
			_animationEvents.StopSound();
		}
		if (walkInOut)
		{
			employeeTpc.Reset();
			employeeTpc.navmeshAgent.Warp(base.transform.position);
			Vector3 velocity = -stageTransform.forward * employeeTpc.navmeshAgent.speed;
			float timeout = 1f;
			while (timeout > 0f)
			{
				employeeTpc.Move(velocity, forceMovement: true);
				timeout -= Time.deltaTime;
				yield return null;
			}
		}
		Transform dressingRoomChair = GetDressingRoomChair();
		employeeTpc.SitOnChair(dressingRoomChair);
		_inTransition = false;
	}

	private IEnumerator WalkOntoStage(Vector3 targetPosition, Quaternion targetRotation, bool walkInOut)
	{
		if (_inTransition || !_currentStage)
		{
			yield break;
		}
		_inTransition = true;
		if (walkInOut)
		{
			employeeTpc.Reset();
			Vector3 newPosition = targetPosition - _currentStage.transform.forward;
			employeeTpc.navmeshAgent.Warp(newPosition);
			base.transform.rotation = _currentStage.transform.rotation;
			Vector3 velocity = _currentStage.transform.forward * employeeTpc.navmeshAgent.speed;
			float timeout = 1f / Mathf.Max(0.5f, employeeTpc.navmeshAgent.speed);
			while (timeout > 0f)
			{
				employeeTpc.Move(velocity, forceMovement: true);
				timeout -= Time.deltaTime;
				yield return null;
			}
			float duration = Mathf.Clamp(Quaternion.Angle(base.transform.rotation, targetRotation) / 180f, 0.1f, 0.5f);
			yield return base.transform.DORotateQuaternion(targetRotation, duration).SetLink(base.gameObject).WaitForCompletion();
		}
		employeeTpc.Reset();
		employeeTpc.ForceToPosition(targetPosition);
		employeeTpc.ForceToRotation(targetRotation);
		if (!_originalAnimatorController)
		{
			_originalAnimatorController = employeeTpc.animator.runtimeAnimatorController;
		}
		employeeTpc.animator.runtimeAnimatorController = AnimationSet.actorAnimatorController;
		RandomizeTimer();
		_inTransition = false;
	}
}
