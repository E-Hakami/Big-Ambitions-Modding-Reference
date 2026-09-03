using BigAmbitions.Items;
using Buildings.Retail.Businesses.CinemaTheater;
using Helpers;
using Streets;
using UI.Smartphone.Apps.BizMan.Schedule;
using UnityEngine;

namespace Furniture.Requirements;

[CreateAssetMenu(menuName = "BigAmbitions/Furniture/Requirements/HasActiveLicensingFee")]
public class HasActiveLicensingFee : FurnitureRequirement
{
	public override bool IsRequirementMet(ItemInstance itemInstance)
	{
		if (itemInstance.AddressCached.IsUndefined())
		{
			return true;
		}
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(itemInstance.AddressCached);
		if (buildingRegistration != null && !buildingRegistration.RentedByPlayer)
		{
			return true;
		}
		if (!BusinessHelper.IsBusinessOpen(buildingRegistration) && !LicensingFeesHelper.ShownLicensingFeeWarnings.Contains(buildingRegistration))
		{
			return true;
		}
		string itemId = ((itemInstance.itemName == "ba:itemname_screencinema") ? itemInstance.id : string.Empty);
		return ScheduleHelper.IsLicensingFeePaidToday(itemInstance.AddressCached, itemId);
	}
}
