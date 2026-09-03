using System.Collections.Generic;
using BigAmbitions.Characters.Appearance;
using Helpers;
using Localizor;
using PlayerActivity;
using UI.Notification;
using UnityEngine;

namespace Controllers;

public class OutsideInteractableItem : EntityController, IPlayerActivityType
{
	public AppearanceTag[] requiredClothingTags;

	public Transform interactPoint;

	public virtual string GetCtaKey()
	{
		return "";
	}

	public virtual string GetItemOccupiedKey()
	{
		return "";
	}

	public override void OnIoEnter()
	{
		if (!CityMap.IsOpen)
		{
			base.OnIoEnter();
		}
	}

	public void ShowOccupiedNotification()
	{
		Dictionary<string, string> notificationData = new Dictionary<string, string> { 
		{
			"itemname",
			GetItemOccupiedKey().GetLocalization()
		} };
		Notifications.Show(NotificationType.Error, "notification_this_item_is_occupied", notificationData);
	}

	public virtual bool ShouldShowOverlay()
	{
		if (!PlayerHelper.IsHoldingItem)
		{
			return !PlayerHelper.IsUsingVehicle;
		}
		return false;
	}

	public virtual void PerformActivity()
	{
	}

	public virtual IPlayerActivity CreateActivity(EntityController attachedEntity)
	{
		return null;
	}
}
