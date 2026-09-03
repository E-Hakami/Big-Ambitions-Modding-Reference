using System;
using UnityEngine;

public class AutoParkSpot : MonoBehaviour
{
	public SpriteRenderer visuals;

	public float maxVehicleLength;

	public BoxCollider boxCollider;

	private void Start()
	{
		visuals.enabled = false;
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, new Action<VehicleController>(OnExitVehicle));
	}

	private void OnExitVehicle(VehicleController _)
	{
		visuals.enabled = false;
	}

	public void Destroy()
	{
		if (Application.isPlaying)
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
		else
		{
			UnityEngine.Object.DestroyImmediate(base.gameObject);
		}
	}

	private void OnDestroy()
	{
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Remove(GlobalEvents.onExitVehicle, new Action<VehicleController>(OnExitVehicle));
	}
}
