using Helpers;
using Streets;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class RemoveActiveVehicleIfHasDifferentAddress : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (gameInstance.ActiveVehicleId != null)
		{
			Address address = VehicleHelper.GetCurrentVehicle().Address;
			Address address2 = new Address(gameInstance.CurrentStreetName, gameInstance.CurrentStreetNumber);
			if (!address.IsUndefined() && address != address2)
			{
				gameInstance.ActiveVehicleId = null;
			}
		}
	}
}
