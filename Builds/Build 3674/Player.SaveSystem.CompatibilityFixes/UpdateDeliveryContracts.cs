using Helpers;

namespace Player.SaveSystem.CompatibilityFixes;

public class UpdateDeliveryContracts : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		for (int num = gameInstance.DeliveryContracts.Count - 1; num >= 0; num--)
		{
			if (!BuildingHelper.GetBuildingRegistration(gameInstance.DeliveryContracts[num].businessAddress).RentedByPlayer)
			{
				gameInstance.DeliveryContracts.RemoveAt(num);
			}
		}
	}
}
