using System.Collections.Generic;
using UI.Smartphone.Apps.Shared;

namespace UI.Smartphone.Apps.MarketInsider;

public class MarketDemandFilterToggle : BaseFilterToggle<MarketDemandCellView.DemandModel>
{
	private HashSet<string> _products;

	public void ConfigureBusinessType(string businessType, HashSet<string> products)
	{
		_products = products;
		label.Key = businessType;
	}

	public override bool PassesFilter(MarketDemandCellView.DemandModel item)
	{
		return _products.Contains(item.ItemName);
	}
}
