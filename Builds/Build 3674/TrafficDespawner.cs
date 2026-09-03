using GleyTrafficSystem;
using Helpers;
using UnityEngine;

public class TrafficDespawner : MonoBehaviour
{
	private void OnTriggerEnter(Collider other)
	{
		if (other.gameObject.layer == LayerHelper.AiVehiclesLayerIndex)
		{
			Manager.RemoveVehicle(other.GetComponentInParent<VehicleComponent>().gameObject);
		}
	}
}
