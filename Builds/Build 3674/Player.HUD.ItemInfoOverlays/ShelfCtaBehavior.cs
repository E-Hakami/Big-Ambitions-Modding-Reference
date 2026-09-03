using System;
using BigAmbitions.Tags;
using Helpers;

namespace Player.HUD.ItemInfoOverlays;

public class ShelfCtaBehavior : ICtaBehavior
{
	public override bool ShouldShow(EntityController entityController)
	{
		if (!(entityController is ShelfController shelfController))
		{
			return false;
		}
		if ((bool)InstanceBehavior<BuildingManager>.Instance && !InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			return false;
		}
		if (ItemHelper.GetMissingRequirements(shelfController.ItemInstance).Count > 0)
		{
			return false;
		}
		return true;
	}

	public override (string, Action) GetCta(EntityController entityController)
	{
		if (entityController is StorageShelfController storageShelfController)
		{
			bool flag = false;
			bool flag2 = false;
			if (PlayerHelper.ItemInstanceInHands != null)
			{
				flag2 = !storageShelfController.IsFull();
			}
			else if (SaveGameManager.Current.ActiveVehicleId != null)
			{
				VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
				if (currentVehicle.VehicleType.HasTag(TagRef.Vehicletag.ishandvehicle))
				{
					flag = true;
					if (currentVehicle.cargoInstances.Count > 0 && !storageShelfController.IsFull())
					{
						flag2 = true;
					}
				}
			}
			else
			{
				flag = true;
			}
			if (flag & flag2)
			{
				return ("click_to_use", null);
			}
			if (flag)
			{
				return ("click_to_manage", storageShelfController.ManageStorage);
			}
			if (flag2)
			{
				return ("click_to_add_to_storage", storageShelfController.AddItemsToStorage);
			}
		}
		else
		{
			if (entityController is DeliverySpot deliverySpot)
			{
				return ("click_to_manage", deliverySpot.ManageStorage);
			}
			if (entityController is PalletController palletController)
			{
				return ("click_to_manage", palletController.ManageStorage);
			}
			if (entityController is ShelfController shelfController)
			{
				if (PlayerHelper.IsHoldingShoppingBasket)
				{
					return ("", null);
				}
				VehicleInstance currentVehicle2 = VehicleHelper.GetCurrentVehicle();
				bool num = currentVehicle2 != null && shelfController.CanAddAnyToInventory(currentVehicle2.cargoInstances);
				bool flag3 = PlayerHelper.IsHoldingItem && shelfController.CanAddAnyToInventory(PlayerHelper.ItemInstanceInHands.cargoInstances);
				if (num | flag3)
				{
					return ("click_to_add_inventory", delegate
					{
						InteractWithEntity(entityController);
					});
				}
				return ("click_to_manage", null);
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
