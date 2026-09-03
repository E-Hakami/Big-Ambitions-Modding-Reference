using System;
using Vehicles.DeliveryDriverJob;

namespace Player.HUD.ItemInfoOverlays;

public class DeliveryJobCtaBehavior : ICtaBehavior
{
	public override bool ShouldShow(EntityController entityController)
	{
		return entityController is DeliveryJobStartController;
	}

	public override (string, Action) GetCta(EntityController entityController)
	{
		if (entityController is DeliveryJobStartController deliveryJobStartController)
		{
			return ("click_to_start_delivery_driver_job", deliveryJobStartController.OnClickToStartJob);
		}
		return ("", null);
	}
}
