using System.Collections.Generic;
using Buildings;
using UI.Smartphone.Apps.Shared;

namespace UI.Smartphone.Apps.MarketInsider;

public class RealEstateFilterController : BaseFilterController<RealEstateCellView.RealEstateModel>
{
	protected override void CreateToggles()
	{
		foreach (KeyValuePair<string, BuildingTypeData> buildingType in BuildingTypeHelper.BuildingTypes)
		{
			if (buildingType.Value.hasCityMapFilter)
			{
				CreateToggle(delegate(RealEstateFilterToggle toggle)
				{
					toggle.ConfigureBuildingType(buildingType.Key);
				}, FilterToggleGroup.BuildingType);
			}
		}
		CreateToggle(delegate(RealEstateFilterToggle toggle)
		{
			toggle.ConfigureForSale();
		}, FilterToggleGroup.Other);
	}

	protected override IEnumerable<string> GetSearchableText(RealEstateCellView.RealEstateModel item)
	{
		yield return item.FormattedAddress;
	}
}
