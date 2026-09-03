using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Boats;
using Helpers;
using Localizor;

namespace Player.HUD.ItemInfoOverlays;

public static class OverlayHelper
{
	public static Type GetOverlayType(DetailedOverlayType overlayType)
	{
		return overlayType switch
		{
			DetailedOverlayType.Employee => typeof(EmployeeOverlay), 
			DetailedOverlayType.Dropdown => typeof(DropdownOverlay), 
			DetailedOverlayType.Fridge => typeof(DecorativeItemHolderOverlay), 
			DetailedOverlayType.Radio => typeof(RadioOverlay), 
			DetailedOverlayType.Vehicle => typeof(VehicleOverlay), 
			DetailedOverlayType.StorageShelf => typeof(StorageShelfOverlay), 
			DetailedOverlayType.Machine => typeof(MachineOverlay), 
			DetailedOverlayType.Buttons => typeof(ButtonOverlay), 
			DetailedOverlayType.TextInput => typeof(TextInputOverlay), 
			DetailedOverlayType.JobBoard => typeof(JobBoardOverlay), 
			DetailedOverlayType.SellerStand => typeof(SellerStandOverlay), 
			DetailedOverlayType.MachineInfo => typeof(MachineInfoOverlay), 
			DetailedOverlayType.CustomizableButtons => typeof(CustomizableButtonsOverlay), 
			_ => null, 
		};
	}

	public static Type GetOverlayType(SimpleOverlayType overlayType)
	{
		return overlayType switch
		{
			SimpleOverlayType.Stock => typeof(StockOverlay), 
			SimpleOverlayType.Employee => typeof(EmployeeNameOverlay), 
			SimpleOverlayType.ClickToAction => typeof(CtaOverlay), 
			SimpleOverlayType.Price => typeof(PriceOverlay), 
			SimpleOverlayType.Requirements => typeof(RequirementsOverlay), 
			SimpleOverlayType.Security => typeof(SecurityOverlay), 
			SimpleOverlayType.Info => typeof(InfoOverlay), 
			SimpleOverlayType.CustomerCapacity => typeof(CustomerCapacitySimpleOverlay), 
			_ => null, 
		};
	}

	internal static string GetOverlayHeaderText(EntityController entityController)
	{
		if (!string.IsNullOrEmpty(entityController.customOverlayHeaderKey))
		{
			return entityController.customOverlayHeaderKey.GetLocalization();
		}
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && entityController is ItemController { playerItemPurchaserSettings: { enabled: not false }, PlayerItemPurchaser: { TotalPrice: >0f } } itemController)
		{
			return LocalizationHelper.GetItemLabel(itemController.playerItemPurchaserSettings.itemName).Key.GetLocalization();
		}
		if (entityController is ItemController itemController2)
		{
			if (itemController2.ItemInstance == null)
			{
				return itemController2.itemName.GetLocalization();
			}
			if (itemController2.Item.HasTag(TagRef.Itemtag.isbox))
			{
				return GetPackedAlias(itemController2);
			}
		}
		if (!(entityController is EducationDoorController educationDoorController))
		{
			if (!(entityController is VehicleSpawnerController vehicleSpawnerController))
			{
				if (!(entityController is VehicleController vehicleController))
				{
					if (!(entityController is SubwayStation subwayStation))
					{
						if (!(entityController is BoatController boatController))
						{
							if (entityController is ItemController itemController3)
							{
								return GetAliasHeader(itemController3.ItemInstance);
							}
							return "";
						}
						return boatController.BoatTypeKey.GetLocalization();
					}
					return ("subwaystation_" + subwayStation.stationName.ToStringFast()).GetLocalization();
				}
				return vehicleController.vehicleType.vehicleTypeName.GetLocalization();
			}
			return vehicleSpawnerController.vehicleType.GetLocalization();
		}
		return educationDoorController.StudyDiploma.diplomaName.GetLocalization();
	}

	private static string GetPackedAlias(ItemController itemController)
	{
		CargoInstance cargoInstance = itemController.ItemInstance.cargoInstances.First();
		if (!cargoInstance.ItemCached.HasTag(TagRef.Itemtag.issealedcontainer))
		{
			return string.Format("{0} {1}", cargoInstance.GetLabel(), "itemoverlay_packed".GetLocalization());
		}
		return cargoInstance.GetLabel().ToString();
	}

	internal static string GetAliasHeader(ItemInstance itemInstance)
	{
		string localization = itemInstance.itemName.GetLocalization();
		string alias = itemInstance.alias;
		if (alias == null || alias.Length <= 0)
		{
			return localization;
		}
		if (!IsSimilar(localization, alias))
		{
			return alias + " - " + localization;
		}
		return alias;
	}

	private static bool IsSimilar(string itemLocalization, string alias)
	{
		string text = Regex.Replace(itemLocalization, "[()]", "").Trim().ToLower();
		string text2 = alias.ToLower();
		string[] array = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
		foreach (string text3 in array)
		{
			if (text3.Length < 4)
			{
				if (text2.Contains(text3))
				{
					return true;
				}
				continue;
			}
			string value = text3.Substring(0, 4);
			if (text2.Contains(value))
			{
				return true;
			}
		}
		return false;
	}

	public static (EntityController, bool) GetRelevantEntity(EntityController entityController)
	{
		if (!(entityController is ItemController itemController) || itemController.Item.HasTag(TagRef.Itemtag.isbag))
		{
			return (entityController, false);
		}
		if (itemController.parentItemController == null)
		{
			List<ItemController> childItemControllers = itemController.childItemControllers;
			if (childItemControllers != null && childItemControllers.Count == 0)
			{
				return (entityController, false);
			}
		}
		if (itemController.playerItemPurchaserSettings.enabled)
		{
			return (entityController, false);
		}
		if (itemController.AttachmentPoints.Count((AttachmentPoint x) => (x.AttachmentPointType & AttachmentPointType.WorkSurface) != 0) > 1)
		{
			return (entityController, false);
		}
		List<ItemController> itemControllersInGroup = ItemHelper.GetItemControllersInGroup(itemController);
		if (InstanceBehavior<BuildingManager>.Instance == null)
		{
			return (entityController, false);
		}
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			foreach (ItemController item in itemControllersInGroup)
			{
				if (item.playerItemPurchaserSettings.enabled)
				{
					return (item, false);
				}
				if (item is BusinessEmployeeController && (((itemController.Item.type & ItemType.Seat) == 0 && (itemController.Item.type & ItemType.ShowcaseShelf) == 0) || (item.Item.type & ItemType.PointOfSale) == 0))
				{
					return (item, true);
				}
			}
		}
		string text = InstanceBehavior<BuildingManager>.Instance.building?.BuildingType;
		if (text == "ba:buildingtype_office" || text == "ba:buildingtype_retail")
		{
			foreach (ItemController item2 in itemControllersInGroup)
			{
				if ((((itemController.Item.type & ItemType.Seat) == 0 && (itemController.Item.type & ItemType.ShowcaseShelf) == 0) || (item2.Item.type & ItemType.PointOfSale) == 0) && item2 is EmployeeStationController)
				{
					return (item2, true);
				}
			}
		}
		return (entityController, false);
	}
}
