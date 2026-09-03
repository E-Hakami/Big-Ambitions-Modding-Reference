using System.Collections.Generic;
using Helpers;
using JimmysUnityUtilities;
using UnityEngine;

public class PlayerVolumeFormedByMultipleColliders : MonoBehaviour
{
	private const string PlayerTag = "Player";

	private const string VehicleTag = "Vehicle";

	private static readonly Collider[] ColliderHits = new Collider[16];

	[SerializeField]
	private BoxCollider[] volumeBoxColliders;

	private readonly HashSet<Collider> _insideColliders = new HashSet<Collider>();

	public bool IsInside
	{
		get
		{
			foreach (Collider insideCollider in _insideColliders)
			{
				if ((bool)insideCollider)
				{
					return true;
				}
			}
			return false;
		}
	}

	private void OnValidate()
	{
		volumeBoxColliders = GetComponents<BoxCollider>();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Vehicle") && other.TryGetComponentInParent<CarController>(out var component) && PlayerHelper.IsUsingVehicle && VehicleHelper.GetCurrentVehicle().id == component.vehicleInstance.id)
		{
			_insideColliders.Add(other);
		}
		else if (other.CompareTag("Player"))
		{
			_insideColliders.Add(other);
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			_insideColliders.Remove(other);
		}
		else if (IsPlayerVehicle(other))
		{
			_insideColliders.Remove(other);
		}
	}

	private static bool IsPlayerVehicle(Collider other)
	{
		if (!other.CompareTag("Vehicle"))
		{
			return false;
		}
		if (!other.TryGetComponentInParent<VehicleController>(out var component))
		{
			return false;
		}
		if (PlayerHelper.IsUsingVehicle)
		{
			return VehicleHelper.GetCurrentVehicle().id == component.vehicleInstance.id;
		}
		return false;
	}

	public void ResetInsideCount()
	{
		_insideColliders.Clear();
	}

	public void ForceColliderDetectionForVehicle()
	{
		BoxCollider[] array = volumeBoxColliders;
		foreach (BoxCollider boxCollider in array)
		{
			Transform transform = boxCollider.transform;
			Vector3 center = transform.TransformPoint(boxCollider.center);
			Vector3 halfExtents = boxCollider.size * 0.5f;
			int num = Physics.OverlapBoxNonAlloc(center, halfExtents, ColliderHits, transform.rotation, LayerHelper.vehiclesMask);
			for (int j = 0; j < num; j++)
			{
				Collider collider = ColliderHits[j];
				if (IsPlayerVehicle(collider))
				{
					_insideColliders.Add(collider);
					return;
				}
			}
		}
	}

	public void ForceColliderDetectionForPlayer()
	{
		BoxCollider[] array = volumeBoxColliders;
		foreach (BoxCollider boxCollider in array)
		{
			Transform transform = boxCollider.transform;
			Vector3 center = transform.TransformPoint(boxCollider.center);
			Vector3 halfExtents = boxCollider.size * 0.5f;
			int num = Physics.OverlapBoxNonAlloc(center, halfExtents, ColliderHits, transform.rotation, 1 << LayerHelper.PlayerLayerIndex);
			for (int j = 0; j < num; j++)
			{
				Collider collider = ColliderHits[j];
				if (collider.CompareTag("Player"))
				{
					_insideColliders.Add(collider);
					return;
				}
			}
		}
	}
}
