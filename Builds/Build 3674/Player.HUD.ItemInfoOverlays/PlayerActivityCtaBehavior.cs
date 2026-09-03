using System;
using Controllers;
using Helpers;

namespace Player.HUD.ItemInfoOverlays;

public class PlayerActivityCtaBehavior : ICtaBehavior
{
	public override bool ShouldShow(EntityController entityController)
	{
		if (!(entityController is DJBoothController) && !(entityController is TVController) && !(entityController is SeatController) && !(entityController is WardrobeController) && !(entityController is WorkoutMachineController) && !(entityController is OutsideChairController) && !(entityController is OutsideBenchController) && !(entityController is PublicShowerController) && !(entityController is HygieneItemController) && !(entityController is BedController) && !(entityController is DanceFloorController) && !(entityController is OutsideInteractableItem) && !(entityController is SwimmingPoolController) && !(entityController is SwimmingPoolBuildingController))
		{
			return false;
		}
		if (PlayerHelper.IsUsingVehicle)
		{
			return false;
		}
		if (!(entityController is ItemController itemController))
		{
			return true;
		}
		if (itemController.playerItemPurchaserSettings.enabled)
		{
			return false;
		}
		return ItemHelper.GetMissingRequirements(itemController.ItemInstance).Count == 0;
	}

	public override (string, Action) GetCta(EntityController entityController)
	{
		if (entityController is TVController tVController)
		{
			if (TVController.CanUseTV)
			{
				return ("click_to_watch", tVController.PerformActivity);
			}
			return ("", null);
		}
		DJBoothController djBoothController = entityController as DJBoothController;
		if ((object)djBoothController != null)
		{
			if (!djBoothController.Occupied)
			{
				return ("click_to_dj", delegate
				{
					djBoothController.Interact();
				});
			}
			return ("", null);
		}
		DanceFloorController danceFloorController = entityController as DanceFloorController;
		if ((object)danceFloorController != null)
		{
			if (DanceFloorController.CanDance())
			{
				return ("click_to_dance", delegate
				{
					danceFloorController.Interact();
				});
			}
			return ("", null);
		}
		if (entityController is SeatController seatController)
		{
			return seatController.GetSeatControllerCta();
		}
		if (entityController is OutsideBenchController outsideBenchController)
		{
			return ("click_to_rest", outsideBenchController.PerformActivity);
		}
		if (entityController is OutsideChairController outsideChairController)
		{
			return ("click_to_rest", outsideChairController.PerformActivity);
		}
		if (entityController is SwimmingPoolController swimmingPoolController)
		{
			return HandleSwimmingPoolController(swimmingPoolController);
		}
		if (entityController is PublicShowerController publicShowerController)
		{
			if (publicShowerController.itemName == "ba:itemname_bathtub")
			{
				return ("click_to_take_bath", publicShowerController.PerformActivity);
			}
			return ("click_to_shower", publicShowerController.PerformActivity);
		}
		if (entityController is HygieneItemController hygieneItemController)
		{
			return ("click_to_use", hygieneItemController.PerformActivity);
		}
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && entityController is BedController bedController)
		{
			return ("click_to_sleep", bedController.PerformActivity);
		}
		if (entityController is WorkoutMachineController workoutMachineController)
		{
			return HandleWorkoutMachineController(workoutMachineController);
		}
		if (entityController is OutsideInteractableItem outsideInteractableItem)
		{
			return HandleOutsideInteractableItem(outsideInteractableItem);
		}
		if (entityController is SwimmingPoolBuildingController swimmingPoolBuildingController)
		{
			return HandleSwimmingPoolBuildingController(swimmingPoolBuildingController);
		}
		if (entityController is UniformLockerController uniformLockerController && uniformLockerController.CanAssignUniforms())
		{
			return ("click_to_change_clothes", null);
		}
		if (entityController is WardrobeController wardrobeController && wardrobeController.CanChangeClothes())
		{
			return ("click_to_change_clothes", wardrobeController.PerformAction);
		}
		return ("", null);
	}

	private static (string, Action) HandleWorkoutMachineController(WorkoutMachineController workoutMachineController)
	{
		if (!workoutMachineController.playerItemPurchaserSettings.enabled && !PlayerHelper.IsHoldingItem && !PlayerHelper.IsUsingVehicle)
		{
			return ("click_to_exercise", workoutMachineController.PerformActivity);
		}
		return ("", null);
	}

	private static (string, Action) HandleOutsideInteractableItem(OutsideInteractableItem outsideInteractableItem)
	{
		if (outsideInteractableItem.ShouldShowOverlay())
		{
			return (outsideInteractableItem.GetCtaKey(), outsideInteractableItem.PerformActivity);
		}
		return ("", null);
	}

	private static (string, Action) HandleSwimmingPoolController(SwimmingPoolController swimmingPoolController)
	{
		if (PlayerHelper.IsUsingVehicle || PlayerHelper.IsHoldingItem || !swimmingPoolController.IsPoolReachable())
		{
			return ("", null);
		}
		return ("click_to_swim", swimmingPoolController.PerformActivity);
	}

	private static (string, Action) HandleSwimmingPoolBuildingController(SwimmingPoolBuildingController swimmingPoolBuildingController)
	{
		if (PlayerHelper.IsUsingVehicle)
		{
			return ("", null);
		}
		return (swimmingPoolBuildingController.GetActionKey(), swimmingPoolBuildingController.PerformAction);
	}
}
