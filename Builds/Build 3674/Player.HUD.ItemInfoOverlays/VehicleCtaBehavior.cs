using System;
using BigAmbitions.Tags;
using Boats;
using Controllers;
using Helpers;
using UI.Notification;
using Vehicles.VehicleTypes;

namespace Player.HUD.ItemInfoOverlays;

public class VehicleCtaBehavior : ICtaBehavior
{
	public override bool ShouldShow(EntityController entityController)
	{
		if (!(entityController is VehicleController) && !(entityController is BoatController))
		{
			return entityController is ShowcaseVehicleController;
		}
		return true;
	}

	public override (string, Action) GetCta(EntityController entityController)
	{
		ShowcaseVehicleController showcaseVehicleController = entityController as ShowcaseVehicleController;
		if ((object)showcaseVehicleController != null && showcaseVehicleController.playerItemPurchaserSettings.enabled)
		{
			return ("click_to_purchase", delegate
			{
				showcaseVehicleController.MoveTowardsEntity(delegate
				{
					showcaseVehicleController.ShowPurchaseUi();
				});
			});
		}
		if (entityController is VehicleController vehicleController)
		{
			return HandleVehicleController(vehicleController);
		}
		if (entityController is BoatController boatController)
		{
			return (boatController.IsPlayerOwned ? "click_to_rest" : "click_to_purchase", null);
		}
		return ("", null);
	}

	private (string, Action) HandleVehicleController(VehicleController vehicleController)
	{
		VehicleType vehicleType = vehicleController.vehicleType;
		if (vehicleType.maxCargoCapacity > 0)
		{
			if (PlayerHelper.IsHoldingItem)
			{
				if (PlayerHelper.IsHoldingShoppingBasket)
				{
					return ("", delegate
					{
						InstanceBehavior<GameManager>.Instance.playerController.SetGoal(vehicleController.GetManageStoragePosition(), delegate
						{
							Notifications.ShowError("notification_storage_is_full");
						});
					});
				}
				CarController carController = vehicleController as CarController;
				if ((object)carController != null && carController.ShouldUseJerryCan())
				{
					return ("click_to_refuel", delegate
					{
						carController.MoveTowardsEntity(delegate
						{
							carController.Interact();
						});
					});
				}
				return ("click_to_add_to_storage", delegate
				{
					AddItemToStorage(vehicleController, fromHandTruck: false);
				});
			}
			if (PlayerHelper.IsUsingVehicle)
			{
				VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
				if (currentVehicle.VehicleType.HasTag(TagRef.Vehicletag.ishandvehicle))
				{
					if (vehicleController.vehicleType.HasTag(TagRef.Vehicletag.ishandvehicle))
					{
						return ("click_to_manage", delegate
						{
							ManageStorage(vehicleController);
						});
					}
					bool flag = vehicleType.fitsHandTruck && currentVehicle.VehicleType.HasTag(TagRef.Vehicletag.canbestoredsmall);
					bool flag2 = vehicleType.fitsFlatbed && currentVehicle.VehicleType.HasTag(TagRef.Vehicletag.canbestoredbig);
					if (currentVehicle.cargoInstances.Count == 0 && (flag | flag2) && vehicleController.vehicleInstance.cargoInstances.Count < vehicleType.maxCargoCapacity)
					{
						return ("click_to_use", null);
					}
					if (currentVehicle.cargoInstances.Count == 0)
					{
						return ("click_to_manage", delegate
						{
							ManageStorage(vehicleController);
						});
					}
					return ("click_to_use", null);
				}
			}
			else if (vehicleController.vehicleInstance.cargoInstances.Count == 0)
			{
				return ("click_to_drive", delegate
				{
					DriveVehicle(vehicleController);
				});
			}
		}
		if (!PlayerHelper.IsUsingVehicle)
		{
			if (vehicleController.vehicleInstance.cargoInstances.Count > 0 && !vehicleController.vehicleType.HasTag(TagRef.Vehicletag.ishandvehicle))
			{
				return ("click_to_use", null);
			}
			return ("click_to_drive", delegate
			{
				DriveVehicle(vehicleController);
			});
		}
		return ("", null);
	}

	private void AddItemToStorage(VehicleController vehicleController, bool fromHandTruck)
	{
		if (fromHandTruck)
		{
			vehicleController.MoveAndAddHandTruckItemsToStorage();
		}
		else
		{
			vehicleController.MoveAndAddHeldItemToStorage();
		}
		InstanceBehavior<OverlayManager>.Instance.UpdateDynamicComponents(vehicleController, DynamicOverlayUpdateType.CtaUpdate);
	}

	private void ManageStorage(VehicleController vehicleController)
	{
		vehicleController.MoveAndManageStorage();
		InstanceBehavior<OverlayManager>.Instance.UpdateDynamicComponents(vehicleController, DynamicOverlayUpdateType.CtaUpdate);
	}

	private void DriveVehicle(VehicleController vehicleController)
	{
		vehicleController.DriveVehicle();
		InstanceBehavior<OverlayManager>.Instance.UpdateDynamicComponents(null, DynamicOverlayUpdateType.CtaUpdate);
	}
}
