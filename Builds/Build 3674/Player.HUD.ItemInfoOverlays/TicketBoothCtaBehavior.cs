using System;
using System.Linq;
using Controllers;

namespace Player.HUD.ItemInfoOverlays;

public class TicketBoothCtaBehavior : ICtaBehavior
{
	public override bool ShouldShow(EntityController entityController)
	{
		return entityController is TicketBoothController;
	}

	public override (string, Action) GetCta(EntityController entityController)
	{
		TicketBoothController ticketBoothController = entityController as TicketBoothController;
		if ((object)ticketBoothController == null)
		{
			return ("", null);
		}
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			if (SaveGameManager.Current.JobInstances.FirstOrDefault((JobInstance x) => x.address == InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address && x.hired) != null)
			{
				return ("click_to_work", delegate
				{
					ticketBoothController.Work();
				});
			}
			if (ticketBoothController.CanOrder())
			{
				return ("click_to_buy_ticket", delegate
				{
					InteractWithEntity(entityController);
				});
			}
		}
		return ("", null);
	}

	private static void InteractWithEntity(EntityController entityController)
	{
		InstanceBehavior<GameManager>.Instance.playerController.SetGoal(entityController, delegate
		{
			entityController.Interact();
		});
	}
}
