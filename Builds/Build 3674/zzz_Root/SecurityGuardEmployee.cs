using UnityEngine;
using UnityEngine.AI;

public class SecurityGuardEmployee : Employee
{
	private const float maxWaitTime = 4f;

	private const float minWaitTime = 8f;

	private const float minimumCompletionDistance = 0.1f;

	private Vector3 _targetPosition;

	private bool _isMoving;

	private NavMeshAgent _agent;

	private float _timeWaiting;

	private float _waitTime;

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
			if (!_agent.pathPending && _agent.remainingDistance < 0.1f)
			{
				_isMoving = false;
				_timeWaiting = 0f;
			}
		}
		else if (_timeWaiting >= _waitTime)
		{
			TryStartToiletCoroutine();
			if (!base.IsAway)
			{
				SetRandomPosition();
			}
		}
		else
		{
			_timeWaiting += Time.deltaTime;
		}
	}

	private void SetRandomPosition()
	{
		_targetPosition = employeeTpc.GetRandomPosition();
		_isMoving = _agent.SetDestination(_targetPosition);
		if (_isMoving)
		{
			_timeWaiting = 0f;
			_waitTime = Random.Range(8f, 4f);
		}
	}

	public override void SetEmployeeStation(EmployeeStationController stationController)
	{
		base.SetEmployeeStation(stationController);
		employeeTpc.navmeshAgent.Warp(stationController.GetEmployeePosition());
		employeeTpc.LookTarget = stationController.transform.position;
		employeeTpc.LookTarget.y = base.transform.position.y;
	}
}
