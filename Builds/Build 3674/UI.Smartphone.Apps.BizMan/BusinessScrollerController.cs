using System.Collections.Generic;
using BigAmbitions.Tags;
using Buildings;
using Helpers;
using UI.Smartphone.Apps.Shared;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan;

public class BusinessScrollerController : BaseFilteredScrollerController<BusinessCellView, BusinessCellView.BusinessModel>
{
	[SerializeField]
	private BizManFilterController filterController;

	protected override BaseFilterController<BusinessCellView.BusinessModel> FilterController => filterController;

	protected override void PopulateAllModels(List<BusinessCellView.BusinessModel> allModels)
	{
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && IsVisibleBusiness(buildingRegistration))
			{
				allModels.Add(new BusinessCellView.BusinessModel(buildingRegistration));
			}
			else if (buildingRegistration.BuildingOwnedByPlayer && !buildingRegistration.BuildingCached.IsHamptonsHouse())
			{
				allModels.Add(new BusinessCellView.BusinessModel(buildingRegistration, isRealEstate: true));
			}
		}
	}

	protected override string GetDataId(BusinessCellView.BusinessModel model)
	{
		return model.Id;
	}

	private static bool IsVisibleBusiness(BuildingRegistration registration)
	{
		if (BusinessTypeHelper.GetData(registration).HasTag(TagRef.Businesstag.generatesrevenue))
		{
			return true;
		}
		if (registration.businessTypeName == "ba:businesstype_empty")
		{
			return BuildingTypeHelper.GetData(registration).HasTag(TagRef.Buildingtypetag.showinbusinesslist);
		}
		return false;
	}
}
