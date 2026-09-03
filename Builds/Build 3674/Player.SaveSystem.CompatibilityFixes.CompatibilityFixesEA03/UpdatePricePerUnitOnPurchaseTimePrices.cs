using BigAmbitions.Items;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class UpdatePricePerUnitOnPurchaseTimePrices : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (ItemInstance item in gameInstance.WorldItemsHashSet)
		{
			Item byName = ItemsGetter.GetByName(item.itemName);
			if (item.pricePerUnitOnPurchaseTime == 0f && (byName.type & ItemType.RetailProduct) == 0)
			{
				item.pricePerUnitOnPurchaseTime = ItemHelper.GetDefaultMarketPrice(byName.itemName);
			}
		}
	}
}
