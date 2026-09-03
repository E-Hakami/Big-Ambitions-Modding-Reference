using System.Collections.Generic;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/World/WorldItemToInteractWithPlayerVehicle")]
public class TutorialPointerDataWorldItemToInteractWithPlayerVehicle : TutorialPointerDataWorldItem
{
	[SerializeField]
	private bool chooseClosest;

	private readonly List<VehicleController> _vehiclesWithCargo = new List<VehicleController>();

	private readonly List<VehicleController> _vehiclesWithoutCargo = new List<VehicleController>();

	public override bool ShouldBeEnabled()
	{
		if (base.ShouldBeEnabled() && !PlayerHelper.IsUsingVehicle)
		{
			return IsThereAnyPlayerVehicle();
		}
		return false;
	}

	private bool IsThereAnyPlayerVehicle()
	{
		foreach (VehicleController allPlayerVehicle in VehicleHelper.AllPlayerVehicles)
		{
			if (allPlayerVehicle.vehicleInstance.Address == addressTarget.GetAddress())
			{
				return true;
			}
		}
		return false;
	}

	public override void FindEntityController()
	{
		FindAllVehiclesInTargetAddress();
		if (_vehiclesWithCargo.Count > 0 || _vehiclesWithoutCargo.Count > 0)
		{
			if (chooseClosest)
			{
				FindClosestVehicle((_vehiclesWithCargo.Count > 0) ? _vehiclesWithCargo : _vehiclesWithoutCargo);
			}
			else
			{
				entityControllerTarget = ((_vehiclesWithCargo.Count > 0) ? _vehiclesWithCargo[0] : _vehiclesWithoutCargo[0]);
			}
		}
	}

	private void FindAllVehiclesInTargetAddress()
	{
		_vehiclesWithCargo.Clear();
		_vehiclesWithoutCargo.Clear();
		foreach (VehicleController allPlayerVehicle in VehicleHelper.AllPlayerVehicles)
		{
			if (!(allPlayerVehicle.vehicleInstance.Address != addressTarget.GetAddress()))
			{
				if (allPlayerVehicle.vehicleInstance.cargoInstances.Count > 0)
				{
					_vehiclesWithCargo.Add(allPlayerVehicle);
				}
				else
				{
					_vehiclesWithoutCargo.Add(allPlayerVehicle);
				}
			}
		}
	}

	private void FindClosestVehicle(List<VehicleController> vehicleControllers)
	{
		float num = float.MaxValue;
		Vector3 position = PlayerHelper.GetPosition();
		foreach (VehicleController vehicleController in vehicleControllers)
		{
			float num2 = Vector3.SqrMagnitude(vehicleController.transform.position - position);
			if (!(num2 > num))
			{
				num = num2;
				entityControllerTarget = vehicleController;
			}
		}
	}
}
