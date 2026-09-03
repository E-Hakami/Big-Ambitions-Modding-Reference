using System.Collections;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.DayNightCycle;
using Buildings;
using Dancing;
using Extensions;
using UnityEngine;
using UnityEngine.AI;

[TaskCategory("Big Ambitions/Nightclub")]
public class NightclubDance : Action
{
	[RequiredField]
	public SharedNightclubCustomer sharedNightclubCustomer;

	private DanceSpot _danceSpot;

	private Timestamp _stopDancingTimestamp;

	private NavMeshAgent _navMeshAgent;

	private bool _isValidMove;

	private bool _hasStartedRotating;

	private bool _isDancing;

	private Vector3 _targetPosition;

	private Coroutine _currentCoroutine;

	private CharacterRotateTowards _characterRotateTowards = new CharacterRotateTowards();

	private CharacterMoveToPosition _characterMoveToPosition = new CharacterMoveToPosition();

	public override void OnAwake()
	{
		_navMeshAgent = sharedNightclubCustomer.Value.tpc.navmeshAgent;
		_characterRotateTowards.Init(sharedNightclubCustomer.Value.tpc, base.Owner);
		_characterMoveToPosition.Init(_navMeshAgent);
	}

	public override void OnStart()
	{
		_targetPosition = GetDancingPosition();
		_isValidMove = _characterMoveToPosition.TryStartMovingToPosition(_targetPosition);
	}

	public override TaskStatus OnUpdate()
	{
		if (!_isValidMove)
		{
			return TaskStatus.Failure;
		}
		if (!_characterMoveToPosition.HasReachedDestination())
		{
			return TaskStatus.Running;
		}
		if (!_hasStartedRotating)
		{
			OnDanceSpotReached();
		}
		else if (_characterRotateTowards.HasFinishedRotating() && !_isDancing)
		{
			OnRotationFinished();
		}
		else if (_isDancing && _stopDancingTimestamp.IsInThePast())
		{
			OnDancingFinished();
			return TaskStatus.Success;
		}
		return TaskStatus.Running;
	}

	public override void OnEnd()
	{
		Reset();
	}

	public override void OnBehaviorComplete()
	{
		Reset();
	}

	private void Reset()
	{
		_isDancing = false;
		_hasStartedRotating = false;
		_characterRotateTowards.StopRotating();
		_characterMoveToPosition.StopCheckingDestination();
		if (_currentCoroutine != null)
		{
			base.Owner.StopCoroutine(_currentCoroutine);
		}
	}

	private Vector3 GetDancingPosition()
	{
		if (InstanceBehavior<BuildingManager>.Instance.AreThereItemsByName(NightclubBusinessHelper.DanceFloorsNames))
		{
			_danceSpot = NightclubBusinessHelper.GetRandomDanceFloorSpot();
			if (_danceSpot != null)
			{
				_danceSpot.Occupy();
				sharedNightclubCustomer.Value.danceSpot = _danceSpot;
				return _danceSpot.position;
			}
			_currentCoroutine = base.Owner.StartCoroutine(RunExpression(CharacterEmojiName.CustomerDanceFloorsFull));
			return sharedNightclubCustomer.Value.tpc.GetRandomPosition();
		}
		_currentCoroutine = base.Owner.StartCoroutine(RunExpression(CharacterEmojiName.CustomerCantFindDanceFloor));
		return sharedNightclubCustomer.Value.tpc.GetRandomPosition();
	}

	private IEnumerator RunExpression(CharacterEmojiName characterEmojiName)
	{
		yield return sharedNightclubCustomer.Value.tpc.ShowExpression(characterEmojiName, 2f);
	}

	private void OnDanceSpotReached()
	{
		StartRotatingTowardsDJIfThereIsOne();
		_hasStartedRotating = true;
		_characterMoveToPosition.StopCheckingDestination();
	}

	private void OnRotationFinished()
	{
		StartDancing();
		SetDancingStopTime();
		_isDancing = true;
	}

	private void OnDancingFinished()
	{
		sharedNightclubCustomer.Value.tpc.StopDancing();
		sharedNightclubCustomer.Value.ReleaseDanceSpot();
	}

	private void StartRotatingTowardsDJIfThereIsOne()
	{
		ItemController itemController = InstanceBehavior<BuildingManager>.Instance.FindClosestItemByName(transform.position, NightclubBusinessHelper.DJBoothItemName);
		if (itemController != null)
		{
			_characterRotateTowards.StartRotatingTowards(itemController.transform.position);
		}
	}

	private void StartDancing()
	{
		sharedNightclubCustomer.Value.tpc.SetDance(Dances.GetAllDances().GetRandom());
	}

	private void SetDancingStopTime()
	{
		_stopDancingTimestamp = TimeHelper.Now();
		_stopDancingTimestamp.AddMinutes(Random.Range(10, 30));
	}
}
