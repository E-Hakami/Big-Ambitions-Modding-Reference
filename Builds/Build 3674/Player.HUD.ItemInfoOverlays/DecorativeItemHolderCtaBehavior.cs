using System;
using Helpers;

namespace Player.HUD.ItemInfoOverlays;

public class DecorativeItemHolderCtaBehavior : ICtaBehavior
{
	public override bool ShouldShow(EntityController entityController)
	{
		if (!(entityController is FridgeController) && !(entityController is DecorativeItemHolderController))
		{
			return false;
		}
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			return false;
		}
		return ItemHelper.GetMissingRequirements(((ItemController)entityController).ItemInstance).Count == 0;
	}

	public override (string, Action) GetCta(EntityController entityController)
	{
		VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
		if (entityController is FridgeController fridgeController)
		{
			bool flag = fridgeController.CanStoreAny(PlayerHelper.ItemInstanceInHands) || (currentVehicle != null && !currentVehicle.VehicleType.IsMotorVehicle && fridgeController.CanStoreAny(currentVehicle.cargoInstances));
			if ((fridgeController.ItemInstance.cargoInstances.Count == 0) & flag)
			{
				return ("click_to_add_to_storage", fridgeController.TryAddToStorage);
			}
			return ("click_to_use", null);
		}
		if (entityController is DecorativeItemHolderController decorativeItemHolderController)
		{
			bool flag2 = decorativeItemHolderController.CanStoreAny(PlayerHelper.ItemInstanceInHands) || (currentVehicle != null && !currentVehicle.VehicleType.IsMotorVehicle && decorativeItemHolderController.CanStoreAny(currentVehicle.cargoInstances));
			if ((decorativeItemHolderController.ItemInstance.cargoInstances.Count == 0) & flag2)
			{
				return ("click_to_add_to_storage", decorativeItemHolderController.TryAddToStorage);
			}
			if (decorativeItemHolderController.ItemInstance.cargoInstances.Count > 0)
			{
				return ("click_to_use", null);
			}
			if (decorativeItemHolderController.isWardrobe)
			{
				return ("click_to_change_clothes", decorativeItemHolderController.ChangeClothes);
			}
			return ("", null);
		}
		return ("", null);
	}
}
