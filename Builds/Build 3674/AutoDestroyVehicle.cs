using BigAmbitions.Items;
using Helpers;
using UI.Smartphone;
using UnityEngine;

public class AutoDestroyVehicle : MonoBehaviour
{
	public VehicleController vehicleController;

	public int minimumMinutesBeforeDestroy;

	public void Update()
	{
		if (!FullMenu.IsOpen && !CityMap.IsOpen && !GameManager.isCitySceneBeingUnloaded && !(VehicleHelper.GetCurrentVehicleBase() == vehicleController) && vehicleController.vehicleInstance.lastSeen != null && vehicleController.vehicleInstance.lastSeen.GetTotalMinutes() != 0f)
		{
			TryToDestroy();
		}
	}

	private void TryToDestroy()
	{
		if (ShouldDestroyVehicle(vehicleController.vehicleInstance, minimumMinutesBeforeDestroy))
		{
			SaveGameManager.Current.VehicleInstances.RemoveAll((VehicleInstance x) => x.id == vehicleController.vehicleInstance.id);
			Object.Destroy(base.gameObject);
		}
	}

	public static bool ShouldDestroyVehicle(VehicleInstance vehicleInstance, float minimumMinutesBeforeDestroy)
	{
		if (minimumMinutesBeforeDestroy >= 0f && TimeHelper.NowInMinutes() - vehicleInstance.lastSeen.GetTotalMinutes() < minimumMinutesBeforeDestroy)
		{
			return false;
		}
		if (SaveGameManager.Current.ActiveVehicleId == vehicleInstance.id)
		{
			return false;
		}
		return !vehicleInstance.cargoInstances.Exists((CargoInstance cargoInstance) => cargoInstance.paid);
	}
}
