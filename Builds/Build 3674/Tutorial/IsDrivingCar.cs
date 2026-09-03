using NaughtyAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Vehicles/IsDrivingCar")]
public class IsDrivingCar : QuestRequirement
{
	public bool specificVehicle;

	[ShowIf("specificVehicle")]
	public string vehicleId;

	[HideIf("specificVehicle")]
	public string vehicleTypeName;

	public override bool CheckIfCompleted()
	{
		if (specificVehicle)
		{
			bool flag = false;
			foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances)
			{
				if (vehicleInstance.id == vehicleId)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return true;
			}
		}
		VehicleController selectedVehicle = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		if (selectedVehicle == null)
		{
			return false;
		}
		if (specificVehicle)
		{
			return selectedVehicle.vehicleInstance?.id == vehicleId;
		}
		return selectedVehicle.vehicleInstance?.vehicleTypeName == vehicleTypeName;
	}
}
