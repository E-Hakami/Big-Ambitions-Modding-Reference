using Helpers;
using PlayerActivity;
using UI.ItemPanel;
using UI.Notification;
using UnityEngine;

public class OutsideChairController : EntityController
{
	[Header("Resting")]
	public Transform sittingPosition;

	[SerializeField]
	private RestEnvironment restEnvironment;

	public override void OnIoEnter()
	{
		if (!CityMap.IsOpen)
		{
			base.OnIoEnter();
		}
	}

	public void PerformActivity()
	{
		if (!string.IsNullOrEmpty(SaveGameManager.Current.ActiveVehicleId))
		{
			Notifications.ShowError("sleepingbench_notification_cant_use_with_handtruck");
		}
		else if (PlayerHelper.ItemInstanceInHands != null)
		{
			Notifications.ShowError("sleepingbench_notification_cant_use_with_item_in_hand");
		}
		else if (!ItemPanelUI.IsVisible)
		{
			PlayerActivityUI.Show(restEnvironment, this);
		}
	}
}
