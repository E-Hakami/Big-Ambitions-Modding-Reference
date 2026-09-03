using UnityEngine;
using UnityEngine.AI;

public class CharacterMoveToPosition
{
	private NavMeshAgent _navMeshAgent;

	private float _completionRadius;

	private bool _checkingDestination;

	public void Init(NavMeshAgent navMeshAgent)
	{
		_navMeshAgent = navMeshAgent;
	}

	public bool TryStartMovingToPosition(Vector3 target, float completionRadius = 0.1f)
	{
		_completionRadius = completionRadius;
		_checkingDestination = true;
		bool num = _navMeshAgent.SetDestination(target);
		if (!num)
		{
			_checkingDestination = false;
		}
		return num;
	}

	public bool HasReachedDestination()
	{
		if (_checkingDestination)
		{
			if (!_navMeshAgent.pathPending)
			{
				return _navMeshAgent.remainingDistance <= _completionRadius;
			}
			return false;
		}
		return true;
	}

	public bool HasPartialPath()
	{
		if (_checkingDestination)
		{
			if (!_navMeshAgent.pathPending && _navMeshAgent.hasPath)
			{
				return _navMeshAgent.pathStatus != NavMeshPathStatus.PathComplete;
			}
			return false;
		}
		return true;
	}

	public void StopCheckingDestination()
	{
		_checkingDestination = false;
	}
}
