using System;
using System.Collections.Generic;
using Controllers;
using PlayerActivity;

namespace Player.HUD.ItemInfoOverlays;

public static class CtaManager
{
	public static string ctaKey;

	public static Action ctaAction;

	public static object ctaArgs;

	private static readonly List<ICtaBehavior> CtaBehaviors = new List<ICtaBehavior>
	{
		new DecorativeItemHolderCtaBehavior(),
		new PlayerActivityCtaBehavior(),
		new ManageCtaBehavior(),
		new TaxiCtaBehavior(),
		new DeliveryJobCtaBehavior(),
		new VehicleCtaBehavior(),
		new ShelfCtaBehavior(),
		new TicketBoothCtaBehavior(),
		new CashRegisterCtaBehavior(),
		new TicketKioskCtaBehavior(),
		new SellerStandCtaBehavior()
	};

	private static readonly List<Type> IgnoresPurchaseSettings = new List<Type> { typeof(ShowcaseVehicleController) };

	public static bool HasCta(EntityController entityController)
	{
		return CtaBehaviors.Exists((ICtaBehavior ctaBehavior) => ctaBehavior.ShouldShow(entityController));
	}

	public static void UpdateCta(EntityController entityController)
	{
		if (InstanceBehavior<GameManager>.Instance?.playerController == null)
		{
			return;
		}
		Clear();
		if (PlayerActivityUI.IsPanelOpen || (entityController is ItemController itemController && itemController.playerItemPurchaserSettings.enabled && !IgnoresPurchaseSettings.Contains(entityController.GetType())))
		{
			return;
		}
		foreach (ICtaBehavior ctaBehavior in CtaBehaviors)
		{
			if (ctaBehavior.ShouldShow(entityController))
			{
				ctaArgs = null;
				(ctaKey, ctaAction) = ctaBehavior.GetCta(entityController);
				break;
			}
		}
	}

	public static void Clear()
	{
		ctaKey = null;
		ctaAction = null;
	}
}
