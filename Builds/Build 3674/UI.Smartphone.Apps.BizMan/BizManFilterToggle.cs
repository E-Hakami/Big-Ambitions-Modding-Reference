using Helpers;
using UI.Smartphone.Apps.Shared;

namespace UI.Smartphone.Apps.BizMan;

public sealed class BizManFilterToggle : BaseFilterToggle<BusinessCellView.BusinessModel>
{
	private string _businessType;

	private string _buildingType;

	private string _neighborhood;

	private string _status;

	public void ConfigureBusinessType(string businessType)
	{
		_businessType = businessType;
		_buildingType = null;
		_neighborhood = null;
		label.Key = businessType;
	}

	public void ConfigureBuildingType(string buildingType)
	{
		_buildingType = buildingType;
		_businessType = null;
		_neighborhood = null;
		label.Key = buildingType;
	}

	public void ConfigureNeighborhood(string neighbourhood)
	{
		_neighborhood = neighbourhood;
		_businessType = null;
		_buildingType = null;
		label.Key = neighbourhood;
	}

	public void ConfigureStatus(string status)
	{
		_status = status;
		_businessType = null;
		_buildingType = null;
		_neighborhood = null;
		label.Key = status;
	}

	public override bool PassesFilter(BusinessCellView.BusinessModel item)
	{
		return ShouldKeep(item);
	}

	private bool ShouldKeep(BusinessCellView.BusinessModel business)
	{
		if (!string.IsNullOrEmpty(_businessType))
		{
			return business.BusinessType == _businessType;
		}
		if (!string.IsNullOrEmpty(_buildingType))
		{
			return business.BuildingType == _buildingType;
		}
		if (!string.IsNullOrEmpty(_neighborhood))
		{
			return BuildingHelper.GetBuilding(business.Address).Neighbourhood == _neighborhood;
		}
		if (!string.IsNullOrEmpty(_status))
		{
			return business.statuses.Contains(_status);
		}
		return true;
	}
}
