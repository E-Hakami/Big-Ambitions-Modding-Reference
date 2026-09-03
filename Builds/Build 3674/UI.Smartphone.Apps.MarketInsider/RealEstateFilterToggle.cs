using UI.Smartphone.Apps.Shared;

namespace UI.Smartphone.Apps.MarketInsider;

public class RealEstateFilterToggle : BaseFilterToggle<RealEstateCellView.RealEstateModel>
{
	private string _buildingType;

	private bool _isForSaleToggle;

	public void ConfigureBuildingType(string buildingType)
	{
		_buildingType = buildingType;
		label.Key = buildingType;
	}

	public void ConfigureForSale()
	{
		_isForSaleToggle = true;
		label.Key = "market_insider_show_only_for_sale";
	}

	public override bool PassesFilter(RealEstateCellView.RealEstateModel item)
	{
		if (_isForSaleToggle)
		{
			return item.IsForSale;
		}
		return item.BuildingType == _buildingType;
	}
}
