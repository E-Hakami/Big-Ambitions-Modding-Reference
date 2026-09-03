namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class ResetVehicleInstanceDeformations : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (VehicleInstance vehicleInstance in gameInstance.VehicleInstances)
		{
			vehicleInstance.deformations.Clear();
		}
	}
}
