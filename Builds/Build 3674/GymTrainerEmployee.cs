using System.Collections.Generic;
using System.Linq;
using Extensions;
using UnityEngine;
using UnityEngine.AI;

public class GymTrainerEmployee : Employee
{
	private const float MaxWaitTime = 10f;

	private const float MinWaitTime = 5f;

	private const float MinimumCompletionDistance = 1.5f;

	private const int NumberOfCheeringAnimations = 2;

	public static readonly List<ItemController> workoutMachinesBeingCheered = new List<ItemController>();

	private Vector3 _targetPosition;

	private bool _isMoving;

	private NavMeshAgent _agent;

	private float _timeWaiting;

	private float _waitTime;

	private WorkoutMachineController _currentWorkoutMachine;

	private static readonly int GymTrainerCheeringNumber = Animator.StringToHash("GymTrainerCheeringNumber");

	private static readonly int GymTrainerCheering = Animator.StringToHash("GymTrainerCheering");

	private static readonly int CancelGymTrainerCheering = Animator.StringToHash("GymTrainerStopCheering");

	public override void Start()
	{
		base.Start();
		employeeTpc.navmeshAgent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;
		_agent = employeeTpc.navmeshAgent;
		SetRandomPosition();
	}

	protected override void Update()
	{
		if (base.IsAway)
		{
			return;
		}
		if (_isMoving)
		{
			if (!_agent.pathPending && ((_currentWorkoutMachine != null && Vector3.SqrMagnitude(_currentWorkoutMachine.characterPosition.position - base.transform.position) < 2.25f) || _agent.remainingDistance < 1.5f))
			{
				OnTargetPositionReached();
			}
		}
		else if (_timeWaiting >= _waitTime)
		{
			employeeTpc.animator.SetTrigger(CancelGymTrainerCheering);
			workoutMachinesBeingCheered.Remove(_currentWorkoutMachine);
			TryStartToiletCoroutine();
			if (!base.IsAway)
			{
				SetRandomPosition();
			}
		}
		else
		{
			_timeWaiting += Time.deltaTime;
			if (_currentWorkoutMachine != null && !_currentWorkoutMachine.Occupied)
			{
				SetRandomPosition();
			}
		}
	}

	private void OnTargetPositionReached()
	{
		_isMoving = false;
		_timeWaiting = 0f;
		_agent.ResetPath();
		if (_currentWorkoutMachine != null)
		{
			if (_currentWorkoutMachine.Occupied)
			{
				employeeTpc.StartCoroutine(employeeTpc.RotateTowards(_currentWorkoutMachine.characterPosition.position, 0.5f));
				employeeTpc.animator.ResetTrigger(CancelGymTrainerCheering);
				employeeTpc.animator.SetFloat(GymTrainerCheeringNumber, Random.Range(0, 2));
				employeeTpc.animator.SetTrigger(GymTrainerCheering);
			}
			else
			{
				_waitTime = 1f;
			}
		}
	}

	private void SetRandomPosition()
	{
		employeeTpc.animator.SetTrigger(CancelGymTrainerCheering);
		workoutMachinesBeingCheered.Remove(_currentWorkoutMachine);
		_currentWorkoutMachine = GetRandomOccupiedWorkoutMachine();
		_targetPosition = ((_currentWorkoutMachine != null) ? _currentWorkoutMachine.characterPosition.position : employeeTpc.GetRandomPosition());
		_isMoving = _agent.SetDestination(_targetPosition);
		if (_isMoving)
		{
			_timeWaiting = 0f;
			_waitTime = Random.Range(5f, 10f);
		}
	}

	private WorkoutMachineController GetRandomOccupiedWorkoutMachine()
	{
		ItemController random = InstanceBehavior<BuildingManager>.Instance.allItemControllers.Where((ItemController x) => x is WorkoutMachineController && x != _currentWorkoutMachine && x.Occupied && !workoutMachinesBeingCheered.Contains(x)).GetRandom();
		workoutMachinesBeingCheered.Add(random);
		return random as WorkoutMachineController;
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
		workoutMachinesBeingCheered.Remove(_currentWorkoutMachine);
	}
}
