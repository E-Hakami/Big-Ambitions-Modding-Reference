using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA02;

public class SetUpVehicleDeformationRandomness : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (VehicleInstance vehicleInstance in gameInstance.VehicleInstances)
		{
			foreach (VehicleDeformationController.VehicleDeformation deformation in vehicleInstance.deformations)
			{
				VehicleDeformationController.VehicleDeformation.VehicleDeformationPoint[] points = deformation.points;
				foreach (VehicleDeformationController.VehicleDeformation.VehicleDeformationPoint vehicleDeformationPoint in points)
				{
					if (vehicleDeformationPoint.deformationRandomness == 0f)
					{
						vehicleDeformationPoint.deformationRandomness = Random.Range(0.99f, 1.01f);
					}
				}
			}
		}
	}
}
