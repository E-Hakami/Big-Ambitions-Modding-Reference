using Extensions;
using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class FixVehiclesFalling : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (VehicleInstance vehicleInstance in gameInstance.VehicleInstances)
		{
			float num = ((BuildingHelper.GetBuilding(vehicleInstance.Address)?.BuildingType == "ba:buildingtype_warehouse") ? (-2.5f) : 0f);
			if (!vehicleInstance.position.y.InRange(num, 5f))
			{
				vehicleInstance.position = new SerializableVector3(vehicleInstance.position.x, 2f, vehicleInstance.position.z);
			}
		}
	}
}
