using Extensions;
using UnityEngine;
using UnityEngine.AI;

public class WaterPedestrian : MonoBehaviour
{
	private const float MinimumCompletionDistance = 0.1f;

	private const float MinWaitTime = 2f;

	private const float MaxWaitItem = 5f;

	private const int NumberOfTriesToPlaceAgent = 30;

	private const float MaxRangeForRandomPosition = 10f;

	public ThirdPersonCharacter tpc;

	private bool _isMoving;

	private float _timeWaiting;

	private float _waitTime;

	public void Init()
	{
		tpc.navmeshAgent.speed = InstanceBehavior<GlobalReferences>.Instance.walkingSpeedWalkFast;
		OnReachSpot();
	}

	public void Update()
	{
		if (_isMoving)
		{
			if (tpc.navmeshAgent.isOnNavMesh && !tpc.navmeshAgent.pathPending && !(tpc.navmeshAgent.remainingDistance > 0.1f))
			{
				OnReachSpot();
			}
		}
		else if (_timeWaiting >= _waitTime)
		{
			SetRandomDestination();
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
		_waitTime = Random.Range(2f, 5f);
	}

	private void SetRandomDestination()
	{
		for (int i = 0; i < 30; i++)
		{
			if (NavMesh.SamplePosition(Random.insideUnitSphere * 10f + base.transform.position, out var hit, 10f, NavMeshHelper.SwimmingAreaMask))
			{
				tpc.navmeshAgent.SetDestination(hit.position);
				_isMoving = true;
				break;
			}
		}
	}
}
