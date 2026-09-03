using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class PedestrianDetector : MonoBehaviour
{
	private NavMeshAgent _agent;

	public Transform lastAwaitedCrosswalk;

	private void Start()
	{
		_agent = GetComponentInParent<NavMeshAgent>();
	}

	private void OnTriggerEnter(Collider other)
	{
		NavMeshAgent agent = _agent;
		if (((object)agent == null || agent.enabled) && other.gameObject.CompareTag("Crosswalk"))
		{
			CrosswalkToTrafficLightLink componentInParent = other.GetComponentInParent<CrosswalkToTrafficLightLink>();
			if ((bool)componentInParent && lastAwaitedCrosswalk != other.transform)
			{
				StartCoroutine(WaitForTrafficLight(other.transform, componentInParent));
			}
		}
	}

	private IEnumerator WaitForTrafficLight(Transform crosswalk, CrosswalkToTrafficLightLink link)
	{
		_agent.isStopped = true;
		yield return new WaitUntil(() => link.redTrafficLight.gameObject.activeSelf);
		lastAwaitedCrosswalk = crosswalk;
	}
}
