using Buildings.BuildingTypes.Special.FoodDelivery;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;

public class AddFoodDeliveryContact : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (BuildingRegistration buildingRegistration in gameInstance.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer)
			{
				FoodDeliveryHelper.TryAddWelcomeContact();
				break;
			}
		}
	}
}
