using System;
using System.Linq;
using Controllers;
using Helpers;

namespace Player.HUD.ItemInfoOverlays;

public class CashRegisterCtaBehavior : ICtaBehavior
{
	public override bool ShouldShow(EntityController entityController)
	{
		return entityController is CashRegisterController;
	}

	public override (string, Action) GetCta(EntityController entityController)
	{
		CashRegisterController cashRegisterController = entityController as CashRegisterController;
		if ((object)cashRegisterController == null)
		{
			return ("", null);
		}
		if (!PlayerHelper.HasPaidForAllItems())
		{
			return ("click_to_purchase", delegate
			{
				cashRegisterController.Order();
			});
		}
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			if (cashRegisterController.CanAcceptFoodDelivery())
			{
				return ("click_to_accept_food_delivery", null);
			}
			if (SaveGameManager.Current.JobInstances.FirstOrDefault((JobInstance x) => x.address == InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address && x.hired) != null)
			{
				return ("click_to_work", delegate
				{
					cashRegisterController.Work();
				});
			}
			if (cashRegisterController.CanOrder())
			{
				return ("click_to_order", delegate
				{
					cashRegisterController.Order();
				});
			}
			return ("", null);
		}
		if (PlayerHelper.IsUsingVehicle)
		{
			if (cashRegisterController.CanAddAnyToInventory(VehicleHelper.GetCurrentVehicle().cargoInstances))
			{
				return ("click_to_add_inventory", delegate
				{
					InteractWithEntity(entityController);
				});
			}
		}
		else if (PlayerHelper.IsHoldingItem && cashRegisterController.CanAddAnyToInventory(PlayerHelper.ItemInstanceInHands.cargoInstances))
		{
			return ("click_to_add_inventory", delegate
			{
				InteractWithEntity(entityController);
			});
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
