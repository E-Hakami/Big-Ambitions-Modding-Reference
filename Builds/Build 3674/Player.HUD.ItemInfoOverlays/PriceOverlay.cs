using Controllers;
using Extensions;
using Localizor;
using TMPro;
using UnityEngine;

namespace Player.HUD.ItemInfoOverlays;

public class PriceOverlay : IOverlay
{
	[SerializeField]
	private TMP_Text priceField;

	private void Start()
	{
		priceField.color = InstanceBehavior<GlobalReferences>.Instance.colors.green;
	}

	public override bool IsValid(EntityController entityController)
	{
		if (entityController is VehicleSpawnerController vehicleSpawnerController)
		{
			return vehicleSpawnerController.Cost > 0f;
		}
		if (entityController is ItemController)
		{
			return true;
		}
		if (!(entityController is SlotMachineController) && !(entityController is SubwayStation) && !(entityController is TicketHouse))
		{
			return entityController is ShowcaseVehicleController;
		}
		return true;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			return false;
		}
		if (entityController is ItemController itemController && itemController.playerItemPurchaserSettings.enabled)
		{
			if (!(itemController.PlayerItemPurchaser.TotalPrice > 0f))
			{
				return itemController is ShowcaseVehicleController;
			}
			return true;
		}
		if (entityController is VehicleSpawnerController { Cost: >0f })
		{
			return true;
		}
		if (!(entityController is SlotMachineController) && !(entityController is SubwayStation) && !(entityController is TicketHouse))
		{
			return entityController is ShowcaseVehicleController;
		}
		return true;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		if (entityController is ShowcaseVehicleController showcaseVehicleController)
		{
			priceField.text = showcaseVehicleController.GetPurchasePrice().ToShortCurrencyFormat();
		}
		else if (entityController is VehicleSpawnerController vehicleSpawnerController)
		{
			if (vehicleSpawnerController.Cost > 0f)
			{
				priceField.text = "vehiclespawner_rent_price".Localize(new
				{
					price = vehicleSpawnerController.Cost.ToShortCurrencyFormat()
				}).ToString();
			}
		}
		else if (entityController is SubwayStation)
		{
			priceField.text = "subwaystation_price_per_ride".Localize(new
			{
				price = 3f.ToShortCurrencyFormat()
			}).ToString();
		}
		else if (entityController is TicketHouse ticketHouse)
		{
			priceField.text = ticketHouse.ticketPrice.ToShortCurrencyFormat();
		}
		else if (entityController is SlotMachineController slotMachineController)
		{
			priceField.text = slotMachineController.startingBet.ToShortCurrencyFormat();
		}
		else if (entityController is ItemController itemController)
		{
			priceField.text = itemController.PlayerItemPurchaser.TotalPrice.ToCurrencyFormat();
		}
	}
}
