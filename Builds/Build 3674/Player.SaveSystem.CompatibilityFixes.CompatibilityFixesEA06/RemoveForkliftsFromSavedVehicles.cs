using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class RemoveForkliftsFromSavedVehicles : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		for (int num = gameInstance.VehicleInstances.Count - 1; num >= 0; num--)
		{
			VehicleInstance vehicleInstance = gameInstance.VehicleInstances[num];
			if (string.IsNullOrEmpty(vehicleInstance.vehicleTypeName) && !(gameInstance.ActiveVehicleId == vehicleInstance.id))
			{
				vehicleInstance.Delete();
			}
		}
	}
}
