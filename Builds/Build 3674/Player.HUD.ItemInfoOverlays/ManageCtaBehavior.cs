using System;
using System.Linq;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Buildings;
using Controllers;
using Helpers;
using Items.SpecialItems;
using PlayerActivity;
using PlayerActivity.Tennis;
using Seasons;

namespace Player.HUD.ItemInfoOverlays;

public class ManageCtaBehavior : ICtaBehavior
{
	private readonly (string, Action) _emptyAction = ("", null);

	public override bool ShouldShow(EntityController entityController)
	{
		if (entityController as LoudspeakerController != null || entityController as CleaningStationController != null || entityController as JobBoardController != null || entityController as EducationDoorController != null || entityController as PreviewTerminalController != null || entityController as BusinessEmployeeController != null || entityController as IRSStationController != null || entityController as SignController != null || entityController as FitnessPlanningBoardController != null || entityController as StateItemController != null || entityController as ItemWithTextController != null || entityController as TrashBinController != null || entityController as ComputerController != null || entityController as GolfPlatformController != null || entityController as TennisInteractionNpc != null || entityController as ItemWithSeasonalVisualsController != null || entityController as FactoryAssemblyMachineController != null)
		{
			return true;
		}
		HairdresserChairController hairdresserChairController = entityController as HairdresserChairController;
		if (hairdresserChairController != null && !hairdresserChairController.isHeadWasher)
		{
			return true;
		}
		VehicleSpawnerController vehicleSpawnerController = entityController as VehicleSpawnerController;
		if (vehicleSpawnerController != null && vehicleSpawnerController.Cost == 0f)
		{
			return true;
		}
		TennisCourt tennisCourt = entityController as TennisCourt;
		if (tennisCourt != null && tennisCourt.LinkedInteractionNpc != null)
		{
			return true;
		}
		ItemController itemController = entityController as ItemController;
		if (itemController != null)
		{
			return itemController.Item.HasTag(TagRef.Itemtag.isshoppingcontainerprovider);
		}
		return false;
	}

	public override (string, Action) GetCta(EntityController entityController)
	{
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			if (entityController as LoudspeakerController != null)
			{
				return ("click_to_manage", null);
			}
			CleaningStationController cleaningStationController = entityController as CleaningStationController;
			if (cleaningStationController != null)
			{
				if (!PlayerHelper.IsHoldingAMop)
				{
					return ("click_to_get_mop", cleaningStationController.OnCleaningStationClick);
				}
				return _emptyAction;
			}
			ItemWithSeasonalVisualsController seasonalController = entityController as ItemWithSeasonalVisualsController;
			if (seasonalController != null)
			{
				if (!seasonalController.CanGrabAnyGift(playNotifications: false))
				{
					return _emptyAction;
				}
				return ("click_to_open_present", delegate
				{
					seasonalController.MoveTowardsEntity(delegate
					{
						seasonalController.Interact();
					});
				});
			}
			if (entityController as FactoryAssemblyMachineController != null)
			{
				return ("click_to_manage", null);
			}
		}
		ItemController itemController = entityController as ItemController;
		if (itemController != null && itemController.Item.HasTag(TagRef.Itemtag.isshoppingcontainerprovider) && !PlayerHelper.IsHoldingShoppingBasket)
		{
			return ("click_to_grab", null);
		}
		VehicleSpawnerController vehicleSpawnerController = entityController as VehicleSpawnerController;
		if (vehicleSpawnerController != null && vehicleSpawnerController.Cost == 0f)
		{
			VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
			if (currentVehicle != null)
			{
				if (currentVehicle.VehicleType.HasTag(TagRef.Vehicletag.ishandvehicle) && currentVehicle.cargoInstances.Count == 0)
				{
					return ("click_to_return", null);
				}
				return _emptyAction;
			}
			if (!PlayerHelper.IsHoldingShoppingBasket)
			{
				return ("click_to_grab", null);
			}
			return _emptyAction;
		}
		JobBoardController jobBoardController = entityController as JobBoardController;
		if (jobBoardController != null)
		{
			if (InstanceBehavior<BuildingManager>.Instance.buildingRegistration.RentedByPlayer)
			{
				return ("click_to_manage", null);
			}
			if (!jobBoardController.HasJob())
			{
				return _emptyAction;
			}
			return ("click_to_view_job_offer", jobBoardController.ShowJob);
		}
		EducationDoorController educationDoorController = entityController as EducationDoorController;
		if (educationDoorController != null)
		{
			return ("click_to_learn", educationDoorController.StartLearning);
		}
		PreviewTerminalController previewTerminalController = entityController as PreviewTerminalController;
		if (previewTerminalController != null)
		{
			return ("click_to_preview_designs", previewTerminalController.StartPreview);
		}
		IRSStationController iRSStationController = entityController as IRSStationController;
		if (iRSStationController != null)
		{
			if (TaxHelper.HasCurrentTaxesToPay())
			{
				return ("click_to_pay_taxes", iRSStationController.PayCurrentTaxes);
			}
			if (!TaxHelper.HasBackTaxesToPay())
			{
				return _emptyAction;
			}
			return ("click_to_pay_back_taxes", iRSStationController.PayBackTaxes);
		}
		if (entityController as SignController != null && InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			return ("click_to_manage", null);
		}
		HairdresserChairController hairdresserChairController = entityController as HairdresserChairController;
		if (hairdresserChairController != null && InstanceBehavior<BuildingManager>.Instance.isOpen)
		{
			return ("click_to_get_haircut", delegate
			{
				hairdresserChairController.MoveTowardsEntity(delegate
				{
					hairdresserChairController.Interact();
				});
			});
		}
		BusinessEmployeeController businessEmployeeController = entityController as BusinessEmployeeController;
		if (businessEmployeeController != null)
		{
			return ("click_to_talk", delegate
			{
				InstanceBehavior<GameManager>.Instance.playerController.SetGoal(businessEmployeeController, delegate
				{
					businessEmployeeController.Interact();
					InstanceBehavior<OverlayManager>.Instance.HideSimpleOverlayAndClearCta();
				});
			});
		}
		FitnessPlanningBoardController fitnessPlanningBoardController = entityController as FitnessPlanningBoardController;
		if (fitnessPlanningBoardController != null && !fitnessPlanningBoardController.playerItemPurchaserSettings.enabled)
		{
			return ("click_to_get_a_personalized_workout_plan", fitnessPlanningBoardController.OnFitnessPlanningBoardClick);
		}
		StateItemController stateItemController = entityController as StateItemController;
		if (stateItemController != null)
		{
			return ("click_to_toggle", delegate
			{
				stateItemController.MoveTowardsEntity(stateItemController.SetNextState);
			});
		}
		ItemWithTextController itemWithTextController = entityController as ItemWithTextController;
		if (itemWithTextController != null && BuildingManager.CanBuildOnCurrentBuilding)
		{
			return ("click_to_change_text", itemWithTextController.OpenChangeTextOverlayInInteriorDesigner);
		}
		TrashBinController trashBinController = entityController as TrashBinController;
		if (trashBinController != null)
		{
			ItemInstance itemInstanceInHands = PlayerHelper.ItemInstanceInHands;
			if (itemInstanceInHands != null && itemInstanceInHands.ItemCached.canBeGrabbed && !itemInstanceInHands.cargoInstances.Any((CargoInstance x) => x.IsSealed))
			{
				return ("click_to_discard", trashBinController.DiscardItemInHand);
			}
		}
		if (!PlayerActivityUI.CanStartActivity(showNotification: false))
		{
			TennisCourt tennisCourt = entityController as TennisCourt;
			if (entityController as ComputerController != null || entityController as GolfPlatformController != null || entityController as TennisInteractionNpc != null || (tennisCourt != null && tennisCourt.LinkedInteractionNpc != null))
			{
				return _emptyAction;
			}
		}
		if (entityController is GolfPlatformController)
		{
			if (!entityController.ShouldShowDetailedOverlay())
			{
				return _emptyAction;
			}
			return ("click_to_play_golf", null);
		}
		if ((entityController is TennisInteractionNpc || entityController is TennisCourt { LinkedInteractionNpc: not null }) ? true : false)
		{
			if (!entityController.ShouldShowDetailedOverlay())
			{
				return _emptyAction;
			}
			return ("ba:click_to_play_tennis", null);
		}
		ComputerController computerController = entityController as ComputerController;
		Building building = InstanceBehavior<BuildingManager>.Instance.buildingRegistration?.BuildingCached;
		if (computerController != null && InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && building != null && building.BuildingType == "ba:buildingtype_residential" && !ItemHelper.HasAnyMissingRequirements(computerController.ItemInstance))
		{
			return ("click_to_play", null);
		}
		return _emptyAction;
	}
}
