namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class FixDrivingRemovedForklift : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		VehicleInstance vehicleInstance = gameInstance.VehicleInstances.Find((VehicleInstance x) => x.id == gameInstance.ActiveVehicleId);
		if (vehicleInstance != null && string.IsNullOrEmpty(vehicleInstance.vehicleTypeName))
		{
			gameInstance.ActiveVehicleId = null;
		}
		gameInstance.VehicleInstances.RemoveAll((VehicleInstance x) => string.IsNullOrEmpty(x.vehicleTypeName));
	}
}
