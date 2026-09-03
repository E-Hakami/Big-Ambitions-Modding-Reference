using JimmysUnityUtilities;
using UnityEngine;

namespace Parking.UndergroundParking;

public class ParkingSpotLight : MonoBehaviour
{
	[SerializeField]
	private Transform detectionPoint;

	[SerializeField]
	private LayerMask vehiclesLayer;

	[SerializeField]
	private Renderer modelRenderer;

	[SerializeField]
	private Material availableMaterial;

	[SerializeField]
	private Material occupiedMaterial;

	private void OnEnable()
	{
		CoroutineUtility.RunAfterOneFrame(CheckForSpotAvailability);
	}

	private void OnTriggerEnter(Collider other)
	{
		if (IsAVehicle(other.gameObject))
		{
			SetAvailable(isAvailable: false);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (IsAVehicle(other.gameObject))
		{
			SetAvailable(isAvailable: true);
		}
	}

	private void CheckForSpotAvailability()
	{
		Collider[] array = new Collider[1];
		Physics.OverlapSphereNonAlloc(detectionPoint.position, 0.5f, array, vehiclesLayer);
		bool available = array[0] == null;
		SetAvailable(available);
	}

	private void SetAvailable(bool isAvailable)
	{
		modelRenderer.material = (isAvailable ? availableMaterial : occupiedMaterial);
	}

	private bool IsAVehicle(GameObject possibleVehicle)
	{
		return (int)vehiclesLayer == ((int)vehiclesLayer | (1 << possibleVehicle.gameObject.layer));
	}
}
