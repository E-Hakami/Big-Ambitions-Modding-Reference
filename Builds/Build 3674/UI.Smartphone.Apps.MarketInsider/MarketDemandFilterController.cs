using System.Collections.Generic;
using BigAmbitions.Tags;
using Helpers;
using JimmysUnityUtilities;
using UI.Smartphone.Apps.Shared;

namespace UI.Smartphone.Apps.MarketInsider;

public class MarketDemandFilterController : BaseFilterController<MarketDemandCellView.DemandModel>
{
	protected override void CreateToggles()
	{
		foreach (BusinessType businessType in BusinessTypeHelper.GetAllPlayerAvailableBusinesses())
		{
			if (!businessType.HasTag(TagRef.Businesstag.generatesrevenue))
			{
				continue;
			}
			HashSet<string> products = BusinessTypeHelper.GetAllProducts(businessType.businessTypeName);
			if (!products.IsEmpty())
			{
				CreateToggle(delegate(MarketDemandFilterToggle toggle)
				{
					toggle.ConfigureBusinessType(businessType.businessTypeName, products);
				}, FilterToggleGroup.BusinessType);
			}
		}
	}

	protected override IEnumerable<string> GetSearchableText(MarketDemandCellView.DemandModel item)
	{
		yield return item.ProductName;
	}
}
