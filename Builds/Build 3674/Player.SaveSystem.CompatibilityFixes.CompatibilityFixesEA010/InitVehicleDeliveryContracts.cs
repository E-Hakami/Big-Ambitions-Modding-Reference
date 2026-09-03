using System.Collections.Generic;
using Vehicles;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class InitVehicleDeliveryContracts : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (gameInstance.vehicleDeliveryContracts == null)
		{
			gameInstance.vehicleDeliveryContracts = new List<VehicleDeliveryContract>();
		}
	}
}
