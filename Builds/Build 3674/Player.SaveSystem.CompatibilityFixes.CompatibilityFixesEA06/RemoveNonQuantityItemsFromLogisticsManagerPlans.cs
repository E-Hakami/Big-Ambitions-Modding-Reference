using BigAmbitions.Items;
using Buildings.Office.Headquarters;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class RemoveNonQuantityItemsFromLogisticsManagerPlans : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (LogisticsManagerPlan logisticsManagerPlan in gameInstance.logisticsManagerPlans)
		{
			for (int num = logisticsManagerPlan.destinations.Count - 1; num >= 0; num--)
			{
				LogisticsManagerPlanDestination logisticsManagerPlanDestination = logisticsManagerPlan.destinations[num];
				for (int num2 = logisticsManagerPlanDestination.stockTargets.Count - 1; num2 >= 0; num2--)
				{
					ItemAmountTarget itemAmountTarget = logisticsManagerPlanDestination.stockTargets[num2];
					Item byName = ItemsGetter.GetByName(itemAmountTarget.itemName);
					if ((!(byName != null) || (byName.type & ItemType.RetailProduct) == 0) && !(itemAmountTarget.itemName == "ba:itemname_paperbag"))
					{
						logisticsManagerPlanDestination.stockTargets.RemoveAt(num2);
					}
				}
			}
		}
	}
}
