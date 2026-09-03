using BigAmbitions.Tags;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class RescueMissingVehiclesWithWrongAddress : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (VehicleInstance vehicleInstance in gameInstance.VehicleInstances)
		{
			bool flag = vehicleInstance.position.x < 500f;
			bool flag2 = !vehicleInstance.VehicleType.HasTag(TagRef.Vehicletag.ishandvehicle);
			bool flag3 = !string.IsNullOrEmpty(vehicleInstance.streetName);
			if (flag & flag2 & flag3)
			{
				vehicleInstance.SetStreetData(string.Empty, 0);
			}
		}
	}
}
