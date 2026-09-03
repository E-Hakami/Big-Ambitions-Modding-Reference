using System.Collections.Generic;
using BigAmbitions.Tags;
using Helpers;
using UI.Smartphone.Apps.Shared;

namespace UI.Smartphone.Apps.BizMan.PricingManagers;

public class PricingManagerFilterController : BaseFilterController<PricingManagerProductModel>
{
	protected override void CreateToggles()
	{
		foreach (BusinessType businessType in BusinessTypeHelper.GetAllPlayerAvailableBusinesses())
		{
			if (businessType.HasTag(TagRef.Businesstag.generatesrevenue))
			{
				CreateToggle(delegate(PricingManagerFilterToggle toggle)
				{
					toggle.ConfigureBusinessType(businessType.businessTypeName);
				}, FilterToggleGroup.BusinessType);
			}
		}
	}

	protected override IEnumerable<string> GetSearchableText(PricingManagerProductModel item)
	{
		yield return item.ProductName;
	}
}
