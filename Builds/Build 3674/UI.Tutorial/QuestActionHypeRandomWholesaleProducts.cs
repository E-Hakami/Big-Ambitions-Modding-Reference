using System.Collections.Generic;
using System.Linq;
using Entities;
using Extensions;
using Helpers;
using Tutorial;
using UnityEngine;

namespace UI.Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Quest Actions/HypeRandomWholesaleProducts")]
public class QuestActionHypeRandomWholesaleProducts : QuestAction
{
	[SerializeField]
	private int minDemandBoostTarget = 80;

	[SerializeField]
	private int maxDemandBoostTarget = 86;

	public override void Do()
	{
		List<string> itemsToBoost = GetItemsToBoost();
		foreach (string neighborhood in NeighborhoodHelper.Neighborhoods)
		{
			if (string.IsNullOrEmpty(neighborhood) || neighborhood == "ba:neighborhood_global")
			{
				continue;
			}
			foreach (string item in itemsToBoost)
			{
				NeighborhoodDemand neighborhoodDemand = ProductMarketHelper.GetProductMarketEntry(item)?.demandValues.FirstOrDefault((NeighborhoodDemand x) => x.neighborhood == neighborhood);
				if (neighborhoodDemand != null)
				{
					int demand = neighborhoodDemand.demand;
					int num = Random.Range(minDemandBoostTarget, maxDemandBoostTarget) - demand;
					if (num > 0)
					{
						ProductMarketHelper.CreateItemRelatedMarketEvent(MarketEventType.Hype, item, neighborhood, null, -1, num);
					}
				}
			}
		}
		ProductMarketHelper.UpdateMarketDemands();
	}

	private static List<string> GetItemsToBoost()
	{
		List<string> list = new List<string>();
		foreach (BusinessType allPlayerAvailableBusiness in BusinessTypeHelper.GetAllPlayerAvailableBusinesses())
		{
			if (allPlayerAvailableBusiness.productSources.Contains("ba:businessproductsource_wholesaler"))
			{
				List<string> primaryRetailProducts = allPlayerAvailableBusiness.GetPrimaryRetailProducts();
				if (primaryRetailProducts.Count != 0)
				{
					list.Add(primaryRetailProducts.GetRandom());
				}
			}
		}
		return list;
	}
}
