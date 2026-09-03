using System.Collections.Generic;
using System.Linq;
using Helpers;
using PlayerActivity;
using UI.Notification;
using UnityEngine;

public class BedController : ItemController
{
	[Header("BedController")]
	[SerializeField]
	private SleepEnvironment sleepEnvironment;

	[SerializeField]
	private Transform sleepPositionTransform;

	public Transform SleepPositionTransform => sleepPositionTransform;

	public void PerformActivity()
	{
		if (!base.BuildingContext.IsPlayerOwnedBusiness)
		{
			return;
		}
		if (PlayerHelper.IsHoldingItem)
		{
			Notifications.ShowError("notification_need_empty_hands_to_interact");
		}
		else if (SaveGameManager.Current.ActiveVehicleId != null)
		{
			string vehicleTypeName = SaveGameManager.Current.VehicleInstances.Single((VehicleInstance x) => x.id == SaveGameManager.Current.ActiveVehicleId).vehicleTypeName;
			Dictionary<string, string> notificationData = new Dictionary<string, string> { { "vehicle", vehicleTypeName } };
			Notifications.Show(NotificationType.Error, "bed_notification_cant_use_with_vehicle", notificationData);
		}
		else
		{
			PlayerActivityUI.Show(sleepEnvironment, this);
		}
	}
}
