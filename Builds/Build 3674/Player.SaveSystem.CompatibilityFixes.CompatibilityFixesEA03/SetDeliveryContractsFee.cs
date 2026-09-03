using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class SetDeliveryContractsFee : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (DeliveryContract deliveryContract in gameInstance.DeliveryContracts)
		{
			deliveryContract.deliveryFee = 400f;
		}
	}
}
