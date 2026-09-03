namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class RemoveFurnitureContractsFromOldSupplyFactoryDepot : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		Address address = new Address("ba:street_fifthavenue", 57);
		for (int num = gameInstance.FurnitureDeliveryContracts.Count - 1; num >= 0; num--)
		{
			if (gameInstance.FurnitureDeliveryContracts[num].fromAddress == address)
			{
				gameInstance.FurnitureDeliveryContracts.RemoveAt(num);
			}
		}
	}
}
