using BigAmbitions.Items;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class RemoveCoatCheckDemand : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.productMarketEntries.RemoveAll((ProductMarketEntry x) => !ItemsGetter.GetByName(x.itemName).isADemandedProduct);
	}
}
