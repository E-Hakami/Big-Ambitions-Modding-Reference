using BigAmbitions.Items;
using Buildings.Office.Headquarters;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class RemoveLegacyFactoryItemsFromLogisticPlans : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (LogisticsManagerPlan logisticsManagerPlan in gameInstance.logisticsManagerPlans)
		{
			foreach (LogisticsManagerPlanDestination destination in logisticsManagerPlan.destinations)
			{
				for (int num = destination.stockTargets.Count - 1; num >= 0; num--)
				{
					if (ItemsGetter.GetByName(destination.stockTargets[num].itemName) == null)
					{
						destination.stockTargets.RemoveAt(num);
					}
				}
			}
		}
	}
}
