using System;
using BigAmbitions.Tags;
using Helpers;

namespace Player.HUD.ItemInfoOverlays;

public class TaxiCtaBehavior : ICtaBehavior
{
	public override bool ShouldShow(EntityController entityController)
	{
		if (!(entityController is TaxiController) && !(entityController is PermanentTaxiController))
		{
			return entityController is PrivateDriverVehicle;
		}
		return true;
	}

	public override (string, Action) GetCta(EntityController entityController)
	{
		VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
		if (currentVehicle != null && !currentVehicle.VehicleType.HasTag(TagRef.Vehicletag.ishandvehicle))
		{
			return ("", null);
		}
		if (entityController is TaxiController taxiController)
		{
			return ("click_to_use_taxi", taxiController.OnClickToUseTaxi);
		}
		if (entityController is PermanentTaxiController permanentTaxiController)
		{
			return ("click_to_use_taxi", permanentTaxiController.OnClickToUseTaxi);
		}
		if (entityController is PrivateDriverVehicle privateDriverVehicle)
		{
			return ("ba:click_to_use_private_vehicle", privateDriverVehicle.OnClickToSetDestination);
		}
		return ("", null);
	}
}
