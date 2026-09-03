using GleyTrafficSystem;
using Helpers;
using UnityEngine;

namespace Vehicles.Components;

public class VehicleLightsToggle : MonoBehaviour
{
	[SerializeField]
	private VehicleLightsComponent vehicleLightsComponent;

	private void OnEnable()
	{
		VehicleHelper.allLightsToggles.Add(this);
	}

	private void OnDisable()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			VehicleHelper.allLightsToggles.Remove(this);
		}
	}

	public void ToggleLights(bool toggle)
	{
		vehicleLightsComponent.SetMainLights(toggle);
		vehicleLightsComponent.UpdateLights(0f);
	}
}
