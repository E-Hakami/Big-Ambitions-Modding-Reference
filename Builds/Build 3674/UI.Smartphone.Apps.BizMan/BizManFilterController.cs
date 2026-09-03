using System.Collections.Generic;
using BigAmbitions.Tags;
using Buildings;
using Helpers;
using UI.Smartphone.Apps.Shared;

namespace UI.Smartphone.Apps.BizMan;

public sealed class BizManFilterController : BaseFilterController<BusinessCellView.BusinessModel>
{
	protected override void CreateToggles()
	{
		foreach (string neighborhood in NeighborhoodHelper.Neighborhoods)
		{
			if (!(neighborhood == "ba:neighborhood_global"))
			{
				CreateToggle(delegate(BizManFilterToggle t)
				{
					t.ConfigureNeighborhood(neighborhood);
				}, FilterToggleGroup.Neighborhood);
			}
		}
		foreach (KeyValuePair<string, BuildingTypeData> buildingType in BuildingTypeHelper.BuildingTypes)
		{
			if (buildingType.Value.hasCityMapFilter)
			{
				CreateToggle(delegate(BizManFilterToggle t)
				{
					t.ConfigureBuildingType(buildingType.Key);
				}, FilterToggleGroup.BuildingType);
			}
		}
		foreach (BusinessType businessType in BusinessTypeHelper.GetAllPlayerAvailableBusinesses())
		{
			if (businessType.HasTag(TagRef.Businesstag.generatesrevenue))
			{
				CreateToggle(delegate(BizManFilterToggle t)
				{
					t.ConfigureBusinessType(businessType.businessTypeName);
				}, FilterToggleGroup.BusinessType);
			}
		}
		foreach (string status in BusinessStatusFilter.All)
		{
			CreateToggle(delegate(BizManFilterToggle t)
			{
				t.ConfigureStatus(status);
			}, FilterToggleGroup.Other);
		}
	}

	protected override IEnumerable<string> GetSearchableText(BusinessCellView.BusinessModel item)
	{
		yield return item.BusinessName;
	}
}
