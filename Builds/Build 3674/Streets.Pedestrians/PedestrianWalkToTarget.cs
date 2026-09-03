using UnityEngine;
using UnityEngine.AI;

namespace Streets.Pedestrians;

public class PedestrianWalkToTarget : MonoBehaviour
{
	private ThirdPersonCharacterPool _pool;

	private Transform _target;

	private ThirdPersonCharacter _tpc;

	private void Awake()
	{
		_tpc = GetComponent<ThirdPersonCharacter>();
	}

	private void OnDisable()
	{
		if ((bool)this)
		{
			Object.Destroy(this);
		}
	}

	private void FixedUpdate()
	{
		NavMeshAgent navmeshAgent = _tpc.navmeshAgent;
		if (!navmeshAgent.pathPending && (!navmeshAgent.hasPath || navmeshAgent.remainingDistance <= navmeshAgent.stoppingDistance))
		{
			Release();
		}
	}

	public void Setup(Transform target, ThirdPersonCharacterPool pool)
	{
		_target = target;
		_pool = pool;
		if (!_tpc.navmeshAgent.SetDestination(_target.position))
		{
			Release();
		}
	}

	private void Release()
	{
		_pool.GetPoolHandler().Release(_tpc);
	}
}
