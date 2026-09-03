using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/ThereIsAPlayerVehicle")]
public class TutorialPointerHideConditionIfThereIsAPlayerVehicle : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		foreach (VehicleController allPlayerVehicle in VehicleHelper.AllPlayerVehicles)
		{
			if (!(allPlayerVehicle.vehicleInstance.Address != InstanceBehavior<BuildingManager>.Instance.buildingRegistration?.Address))
			{
				return true;
			}
		}
		return false;
	}
}
