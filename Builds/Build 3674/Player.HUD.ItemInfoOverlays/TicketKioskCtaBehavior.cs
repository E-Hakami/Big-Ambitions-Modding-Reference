using System;
using Controllers;

namespace Player.HUD.ItemInfoOverlays;

public class TicketKioskCtaBehavior : ICtaBehavior
{
	public override bool ShouldShow(EntityController entityController)
	{
		return entityController is TicketKioskController;
	}

	public override (string, Action) GetCta(EntityController entityController)
	{
		if (!(entityController is TicketKioskController))
		{
			return ("", null);
		}
		return ("click_to_purchase", delegate
		{
			InteractWithEntity(entityController);
		});
	}

	private static void InteractWithEntity(EntityController entityController)
	{
		InstanceBehavior<GameManager>.Instance.playerController.SetGoal(entityController, delegate
		{
			entityController.Interact();
		});
	}
}
