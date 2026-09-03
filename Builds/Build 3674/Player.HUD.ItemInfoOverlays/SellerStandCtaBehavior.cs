using System;
using BigAmbitions.Tags;
using Helpers;

namespace Player.HUD.ItemInfoOverlays;

public class SellerStandCtaBehavior : ICtaBehavior
{
	public override bool ShouldShow(EntityController entityController)
	{
		return entityController is SellerStandController;
	}

	public override (string, Action) GetCta(EntityController entityController)
	{
		VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
		if (currentVehicle != null && !currentVehicle.VehicleType.HasTag(TagRef.Vehicletag.ishandvehicle))
		{
			return ("", null);
		}
		if (entityController is SellerStandController)
		{
			return ("click_to_talk", null);
		}
		return ("", null);
	}
}
