using Helpers;
using PlayerActivity;
using PlayerActivity.Activities.Paid;
using UI.ItemPanel;
using UI.Notification;
using UnityEngine;

namespace Controllers;

public class TicketSeller : OutsideInteractableItem, IPaidActivityItem
{
	public PaidActivityEnvironment paidActivityEnvironment;

	public override string GetCtaKey()
	{
		return "click_to_buy_ticket";
	}

	public override string GetItemOccupiedKey()
	{
		return "";
	}

	public override void PerformActivity()
	{
		if (PlayerHelper.IsHoldingItem)
		{
			Notifications.Show(NotificationType.Error, "notification_need_empty_hands_to_interact");
		}
		else if (!ItemPanelUI.IsVisible)
		{
			PlayerActivityUI.Show(this, this);
		}
	}

	public override IPlayerActivity CreateActivity(EntityController attachedEntity)
	{
		paidActivityEnvironment.transactionData["element"] = customOverlayHeaderKey;
		return new PaidActivity(paidActivityEnvironment, this);
	}

	public override bool ShouldShowOverlay()
	{
		return !PlayerHelper.IsUsingVehicle;
	}

	public virtual Transform GetPlayerWaitingTransform()
	{
		return null;
	}

	public virtual void OnPlayerReached(PlayerController playerController, PaidActivity paidActivity)
	{
	}

	public virtual void OnPlayerReachedWaitingPosition()
	{
	}

	public virtual void OnFinish()
	{
	}
}
