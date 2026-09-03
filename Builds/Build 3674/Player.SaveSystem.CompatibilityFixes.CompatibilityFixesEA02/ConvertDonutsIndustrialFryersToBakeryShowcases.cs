using System.Collections.Generic;
using BigAmbitions.Items;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA02;

public class ConvertDonutsIndustrialFryersToBakeryShowcases : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		int num = 0;
		foreach (ItemInstance item in gameInstance.WorldItemsHashSet)
		{
			string itemName = item.itemName;
			if (itemName == "ba:itemname_industrialfryermachine" || itemName == "ba:itemname_donut")
			{
				item.itemName = "ba:itemname_bakeryshowcase";
				num++;
			}
		}
		if (num > 0)
		{
			float defaultMarketPrice = ItemHelper.GetDefaultMarketPrice("ba:itemname_industrialfryermachine");
			float defaultMarketPrice2 = ItemHelper.GetDefaultMarketPrice("ba:itemname_bakeryshowcase");
			float num2 = defaultMarketPrice - defaultMarketPrice2;
			if (num2 > 0f)
			{
				float num3 = (float)num * num2;
				string value = $"{num}x Industrial Fryer Machines with donuts converted to " + "Bakery Showcases (caused by compatibility support)";
				gameInstance.Money += num3;
				Dictionary<string, string> data = new Dictionary<string, string> { { "text", value } };
				TransactionInfo info = new TransactionInfo("ba:transaction_compatibilityfix", data);
				gameInstance.Transactions.Enqueue(new Transaction(info)
				{
					amount = num3,
					balance = gameInstance.Money
				});
			}
		}
	}
}
