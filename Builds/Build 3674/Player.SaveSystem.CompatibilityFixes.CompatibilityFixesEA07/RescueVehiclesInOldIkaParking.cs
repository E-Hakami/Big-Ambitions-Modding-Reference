using Extensions;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class RescueVehiclesInOldIkaParking : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		int num = 138;
		int num2 = 183;
		int num3 = -305;
		int num4 = -280;
		Vector3 vehicleRespawnSafetySpot = InstanceBehavior<GlobalReferences>.Instance.vehicleRespawnSafetySpot;
		foreach (VehicleInstance vehicleInstance in gameInstance.VehicleInstances)
		{
			if (vehicleInstance.position.z.InRange(num3, num4) && vehicleInstance.position.x.InRange(num, num2))
			{
				vehicleInstance.position = vehicleRespawnSafetySpot;
				vehicleInstance.rotation = Quaternion.identity;
				vehicleRespawnSafetySpot.x -= 3f;
			}
		}
	}
}
