using Controllers;
using Helpers;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace Player.HUD.ItemInfoOverlays;

public class InfoOverlay : IOverlay
{
	[SerializeField]
	private TextLocalizationComponent infoTextField;

	public override bool IsValid(EntityController entityController)
	{
		if (!(entityController is TicketHouse) && !(entityController is FridgeController) && !(entityController is IRSStationController))
		{
			return entityController is DecorativeItemHolderController;
		}
		return true;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		if (entityController is TicketHouse ticketHouse)
		{
			return !ticketHouse.IsOpen;
		}
		if (entityController is FridgeController fridgeController)
		{
			if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
			{
				return fridgeController.ItemInstance.cargoInstances.Count == 0;
			}
			return false;
		}
		if (entityController is IRSStationController)
		{
			return !TaxHelper.HasAnyTaxesToPay();
		}
		if (entityController is DecorativeItemHolderController decorativeItemHolderController)
		{
			if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
			{
				return decorativeItemHolderController.ItemInstance.cargoInstances.Count == 0;
			}
			return false;
		}
		return false;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		if (entityController is TicketHouse ticketHouse)
		{
			TicketHouse.CasinoOpeningHours nextOpeningHours = ticketHouse.GetNextOpeningHours();
			infoTextField.Key = "tickethouse_open_info";
			infoTextField.Arguments = new
			{
				dayOfWeek = nextOpeningHours.dayOfWeek,
				fromTime = nextOpeningHours.startHour.GetFormattedTime(),
				toTime = nextOpeningHours.endHour.GetFormattedTime()
			};
		}
		if (entityController is FridgeController)
		{
			infoTextField.Key = "itemoverlay_fridge_is_empty";
		}
		if (entityController is DecorativeItemHolderController decorativeItemHolderController)
		{
			infoTextField.Key = "itemoverlay_item_is_empty";
			infoTextField.Arguments = new
			{
				itemname = decorativeItemHolderController.itemName
			};
		}
		if (entityController is IRSStationController)
		{
			infoTextField.Key = "irs_no_taxes_due";
		}
	}
}
