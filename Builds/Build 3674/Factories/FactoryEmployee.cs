using System;
using System.Collections;
using System.Collections.Generic;
using BigAmbitions.Characters;
using BigAmbitions.DayNightCycle;
using Extensions;
using Items.SpecialItems;
using UnityEngine;
using UnityEngine.AI;

namespace Factories;

public class FactoryEmployee : Employee
{
	private const float ChanceOfStartingMoving = 0.5f;

	private const float MinimumCompletionDistance = 0.1f;

	private const int MinSecondsWatchingTheMachine = 10;

	private const int MaxSecondsWatchingTheMachine = 15;

	private const float MinimumDistanceToPlayAnimation = 0.2f;

	private readonly List<Transform> _watchingSpots = new List<Transform>();

	private readonly List<FactoryProductionMachineController> _factoryProductionMachineControllers = new List<FactoryProductionMachineController>();

	private Timestamp _changeAnimationTimestamp;

	private bool _pendingResumeAnimation;

	private FactoryAssemblyMachineController _factoryAssemblyMachineController;

	private Transform _lastSpotWatched;

	private bool _isWatching = true;

	private bool _isWorking = true;

	private float _timeToStopWatching;

	private NavMeshPath _path;

	private WaitUntil _waitUntilDestinationReached;

	public override void Start()
	{
		base.Start();
		SetAgentProperties();
		GlobalEvents.onItemDropped = (Action<ItemController>)Delegate.Combine(GlobalEvents.onItemDropped, new Action<ItemController>(OnItemDropped));
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		RevertAgentProperties();
		GlobalEvents.onItemDropped = (Action<ItemController>)Delegate.Remove(GlobalEvents.onItemDropped, new Action<ItemController>(OnItemDropped));
	}

	protected override void Update()
	{
		if (_watchingSpots.Count > 1 && _isWorking && _isWatching && !(Time.time < _timeToStopWatching))
		{
			TryMoveToNextSpot();
		}
	}

	private void TryMoveToNextSpot()
	{
		_isWatching = false;
		Transform nextSpot = GetNextSpot();
		if (!IsPositionReachable(nextSpot.position))
		{
			OnPositionReached();
			return;
		}
		_lastSpotWatched = nextSpot;
		activeCoroutine = StartCoroutine(MoveToNextSpot());
	}

	private IEnumerator MoveToNextSpot()
	{
		StopWorkingAnimation();
		Vector3 lookPosition = _lastSpotWatched.position + _lastSpotWatched.forward;
		employeeTpc.navmeshAgent.SetDestination(_lastSpotWatched.position);
		yield return WaitForDestinationReached();
		yield return employeeTpc.RotateTowards(lookPosition, 1f);
		OnPositionReached();
	}

	private void StopWorkingAnimation()
	{
		employeeTpc.SetHandIKTargets(null, null, smooth: true);
		employeeTpc.SetHeadIKTarget(null, smooth: true);
		employeeTpc.animator.SetBool(PermanentAnimationType.FactoryWorking, state: false);
	}

	private IEnumerator WaitForDestinationReached()
	{
		if (_waitUntilDestinationReached == null)
		{
			_waitUntilDestinationReached = new WaitUntil(HasReachedDestination);
		}
		yield return _waitUntilDestinationReached;
	}

	private bool HasReachedDestination()
	{
		if (!employeeTpc.navmeshAgent.pathPending)
		{
			return employeeTpc.navmeshAgent.remainingDistance <= 0.1f;
		}
		return false;
	}

	private bool IsPositionReachable(Vector3 position)
	{
		if (_path == null)
		{
			_path = new NavMeshPath();
		}
		NavMesh.CalculatePath(base.transform.position, position, -1, _path);
		return _path.status == NavMeshPathStatus.PathComplete;
	}

	private Transform GetNextSpot()
	{
		int indexToExclude = _watchingSpots.IndexOf(_lastSpotWatched);
		return _watchingSpots.GetRandomExcludingIndex(indexToExclude);
	}

	private void OnPositionReached()
	{
		_isWatching = true;
		_timeToStopWatching = Time.time + (float)UnityEngine.Random.Range(10, 15);
		if (_isWorking && !(MathHelper.DistanceSqr(base.transform.position, _lastSpotWatched.position) > 0.2f))
		{
			StartWorkingAnimation();
		}
	}

	private void StartWorkingAnimation()
	{
		employeeTpc.animator.SetBool(PermanentAnimationType.FactoryWorking);
		SetIkTargets();
	}

	private void SetIkTargets()
	{
		ItemController itemController = null;
		if (_lastSpotWatched == _factoryAssemblyMachineController.GetEmployeeSpot())
		{
			itemController = _factoryAssemblyMachineController;
		}
		else
		{
			foreach (FactoryProductionMachineController factoryProductionMachineController in _factoryProductionMachineControllers)
			{
				if (factoryProductionMachineController.employeeSpot == _lastSpotWatched)
				{
					itemController = factoryProductionMachineController;
					break;
				}
			}
		}
		if (itemController != null)
		{
			employeeTpc.SetHandIKTargets(itemController.LHandIKAttachmentPoint, itemController.RHandIKAttachmentPoint, smooth: true);
			employeeTpc.SetHeadIKTarget(itemController.HeadIKAttachmentPoint, smooth: true);
		}
	}

	public override void SetEmployeeStation(EmployeeStationController stationController)
	{
		base.SetEmployeeStation(stationController);
		employeeTpc.navmeshAgent.Warp(stationController.GetEmployeePosition());
		employeeTpc.ForceToRotation(stationController.GetEmployeeSpot().rotation);
		if (stationController is FactoryAssemblyMachineController assemblyMachine)
		{
			SetAssemblyMachine(assemblyMachine);
		}
	}

	private void SetAssemblyMachine(FactoryAssemblyMachineController assemblyMachineController)
	{
		_factoryAssemblyMachineController = assemblyMachineController;
		_lastSpotWatched = _factoryAssemblyMachineController.GetEmployeeSpot();
		SetWatchingPositions(_lastSpotWatched);
		BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
		_isWorking = _factoryAssemblyMachineController.WorkstationInstance.IsWorkstationActive(buildingRegistration);
		if (_isWorking)
		{
			if (0.5f.Probability())
			{
				TryMoveToNextSpot();
			}
			else
			{
				OnPositionReached();
			}
		}
	}

	private void SetWatchingPositions(Transform assemblyMachineSpot)
	{
		_factoryProductionMachineControllers.Clear();
		_watchingSpots.Clear();
		_watchingSpots.Add(assemblyMachineSpot);
		FactoryProductionMachineController[] attachedMachines = _factoryAssemblyMachineController.GetAttachedMachines();
		foreach (FactoryProductionMachineController factoryProductionMachineController in attachedMachines)
		{
			if (!(factoryProductionMachineController == null))
			{
				_factoryProductionMachineControllers.Add(factoryProductionMachineController);
				_watchingSpots.Add(factoryProductionMachineController.employeeSpot);
			}
		}
	}

	public void StopWorking()
	{
		_isWorking = false;
		if (!(_lastSpotWatched == _watchingSpots[0]) || !_isWatching)
		{
			InterruptToMoveToAssemblyMachine();
		}
	}

	public void ResumeWorking()
	{
		_isWorking = true;
	}

	private void InterruptToMoveToAssemblyMachine()
	{
		if (activeCoroutine != null)
		{
			StopCoroutine(activeCoroutine);
		}
		_lastSpotWatched = _watchingSpots[0];
		activeCoroutine = StartCoroutine(MoveToNextSpot());
	}

	private void OnItemDropped(ItemController itemController)
	{
		if (itemController is FactoryProductionMachineController factoryProductionMachineController && _watchingSpots.Contains(factoryProductionMachineController.employeeSpot))
		{
			SetWatchingPositions(_factoryAssemblyMachineController.GetEmployeeSpot());
			if (!_watchingSpots.Contains(_lastSpotWatched))
			{
				InterruptToMoveToAssemblyMachine();
			}
		}
	}
}
