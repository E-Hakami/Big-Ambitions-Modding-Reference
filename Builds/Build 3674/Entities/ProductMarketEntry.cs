using System;
using System.Collections.Generic;

namespace Entities;

[Serializable]
public class ProductMarketEntry
{
	public string itemName;

	public float importPriceIndex;

	public List<NeighborhoodDemand> demandValues;
}
