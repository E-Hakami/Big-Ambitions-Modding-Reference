using System;
using UnityEngine.AI;

namespace Streets.Pedestrians;

public class PedestrianPlayerRangeHandler
{
	private readonly NavMeshAgent _agent;

	private readonly Action _onOutsidePlayerRange;

	private float _timeBeingStationary;

	public PedestrianPlayerRangeHandler(NavMeshAgent agent, Action onOutsidePlayerRange)
	{
		_agent = agent;
		_onOutsidePlayerRange = onOutsidePlayerRange;
	}

	public void Update()
	{
		if (IsOutsidePlayerRange())
		{
			_onOutsidePlayerRange?.Invoke();
		}
	}

	private bool IsOutsidePlayerRange()
	{
		return InstanceBehavior<CityManager>.Instance.IsOutsidePlayerRange(_agent.transform.position);
	}
}
