using System.Collections.Generic;
using BigAmbitions.Characters;
using Entities;
using UnityEngine;
using UnityEngine.AI;

public class CleanerEmployee : Employee
{
	private const float MinimumCompletionDistance = 0.1f;

	private const int NumberOfLastDirtSpotsCached = 3;

	private const float MinimumDistanceBetweenSpots = 2.5f;

	private static readonly List<DirtSpot> DirtSpotsBeingCleaned = new List<DirtSpot>();

	private bool _isMoving;

	private NavMeshAgent _agent;

	private float _timeWaiting;

	private float _waitTime;

	private List<DirtSpot> _dirtSpots;

	private DirtSpot _currentDirtSpot;

	private readonly Queue<Vector3> _lastPositionsCleaned = new Queue<Vector3>();

	private float _cleaningAnimationLength;

	private bool _pendingRetakeMop;

	public override void Start()
	{
		base.Start();
		employeeTpc.navmeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
		_agent = employeeTpc.navmeshAgent;
		_dirtSpots = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.dirtSpots;
		_cleaningAnimationLength = employeeTpc.animator.GetAnimationLength(PermanentAnimationType.Cleaning);
		MoveToNextPosition();
		TakeMop();
	}

	private void TakeMop()
	{
		employeeTpc.AddHandObject(BaseHuman.GetHandObjectNameFromPermanentAnimationType(PermanentAnimationType.CleaningIdle));
		employeeTpc.animator.SetBool(PermanentAnimationType.CleaningIdle);
	}

	protected override void Update()
	{
		if (base.IsAway)
		{
			return;
		}
		if (_pendingRetakeMop)
		{
			_pendingRetakeMop = false;
			TakeMop();
		}
		if (_isMoving)
		{
			if (!_agent.pathPending && _agent.remainingDistance < 0.1f)
			{
				OnReachSpot();
			}
		}
		else if (_timeWaiting >= _waitTime)
		{
			employeeTpc.animator.SetBool(PermanentAnimationType.Cleaning, state: false);
			TryStartToiletCoroutine();
			if (base.IsAway)
			{
				employeeTpc.animator.SetBool(PermanentAnimationType.CleaningIdle, state: false);
				employeeTpc.RemoveHandObject();
				_pendingRetakeMop = true;
			}
			else
			{
				MoveToNextPosition();
			}
		}
		else
		{
			_timeWaiting += Time.deltaTime;
		}
	}

	private void OnReachSpot()
	{
		_isMoving = false;
		_timeWaiting = 0f;
		employeeTpc.animator.SetBool(PermanentAnimationType.Cleaning);
		if (_lastPositionsCleaned.Count > 3)
		{
			_lastPositionsCleaned.Dequeue();
		}
		DirtSpotsBeingCleaned.Remove(_currentDirtSpot);
	}

	private Vector3 GetClosestDirtPosition()
	{
		DirtSpot dirtSpot = null;
		float value = float.MaxValue;
		bool flag = false;
		Vector3 position = base.transform.position;
		foreach (DirtSpot dirtSpot2 in _dirtSpots)
		{
			if (IsDirtSpotValid(dirtSpot2))
			{
				float num = Vector3.SqrMagnitude(new Vector3(dirtSpot2.x, 0f, dirtSpot2.z) - position);
				if (!flag || num.CompareTo(value) < 0)
				{
					flag = true;
					value = num;
					dirtSpot = dirtSpot2;
				}
			}
		}
		if (dirtSpot == null)
		{
			return Vector3.zero;
		}
		_currentDirtSpot = dirtSpot;
		return new Vector3(dirtSpot.x, 0f, dirtSpot.z);
	}

	private bool IsDirtSpotValid(DirtSpot dirtSpot)
	{
		if (dirtSpot.dirtiness < 5f || DirtSpotsBeingCleaned.Contains(dirtSpot))
		{
			return false;
		}
		foreach (Vector3 item in _lastPositionsCleaned)
		{
			if (Vector3.SqrMagnitude(new Vector3(dirtSpot.x, 0f, dirtSpot.z) - new Vector3(item.x, 0f, item.z)) < 2.5f)
			{
				return false;
			}
		}
		return true;
	}

	private Vector3 GetNextPosition()
	{
		Vector3 closestDirtPosition = GetClosestDirtPosition();
		if (!(closestDirtPosition != Vector3.zero))
		{
			return employeeTpc.GetRandomPosition();
		}
		return closestDirtPosition;
	}

	private void MoveToNextPosition()
	{
		employeeTpc.animator.SetBool(PermanentAnimationType.Cleaning, state: false);
		Vector3 nextPosition = GetNextPosition();
		_isMoving = _agent.SetDestination(nextPosition);
		if (_isMoving)
		{
			_timeWaiting = 0f;
			_waitTime = _cleaningAnimationLength;
			_lastPositionsCleaned.Enqueue(nextPosition);
			if (_currentDirtSpot != null)
			{
				DirtSpotsBeingCleaned.Add(_currentDirtSpot);
			}
		}
	}

	public override void SetEmployeeStation(EmployeeStationController stationController)
	{
		base.SetEmployeeStation(stationController);
		employeeTpc.navmeshAgent.Warp(stationController.GetEmployeePosition());
		employeeTpc.LookTarget = stationController.transform.position;
		employeeTpc.LookTarget.y = base.transform.position.y;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		DirtSpotsBeingCleaned.Remove(_currentDirtSpot);
	}
}
