using Helpers;
using UnityEngine;

namespace Controllers;

public class TurnstileBarrier : MonoBehaviour
{
	private static readonly int UseId = Animator.StringToHash("Use");

	private static readonly int UseReverseId = Animator.StringToHash("UseReverse");

	[SerializeField]
	private Animator animator;

	private int _peopleUsing;

	public void OnTriggerEnter(Collider other)
	{
		if ((other.transform.CompareTag("Player") || other.transform.CompareTag("NPC") || other.transform.gameObject.layer == LayerHelper.VehiclesLayerIndex) && (other.transform.gameObject.layer != LayerHelper.VehiclesLayerIndex || !(other.transform.GetComponent<VehicleController>() != InstanceBehavior<GameManager>.Instance.selectedVehicle)))
		{
			if (_peopleUsing == 0)
			{
				RunAnimation(other);
			}
			_peopleUsing++;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if ((other.transform.CompareTag("Player") || other.transform.CompareTag("NPC") || other.transform.gameObject.layer == LayerHelper.VehiclesLayerIndex) && (other.transform.gameObject.layer != LayerHelper.VehiclesLayerIndex || !(other.transform.GetComponent<VehicleController>() != InstanceBehavior<GameManager>.Instance.selectedVehicle)))
		{
			_peopleUsing--;
		}
	}

	public void RunAnimation(Collider other)
	{
		Vector3 vector = other.transform.position - base.transform.position;
		float num = Vector3.Dot(base.transform.forward, vector.normalized);
		animator.SetTrigger((num > 0f) ? UseReverseId : UseId);
	}
}
