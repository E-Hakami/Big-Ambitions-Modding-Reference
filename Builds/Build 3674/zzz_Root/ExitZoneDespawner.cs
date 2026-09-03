using BigAmbitions.Tags;
using Helpers;
using NaughtyAttributes;
using Parking.UndergroundParking;
using UI.Notification;
using UI.Purchase;
using UnityEngine;

public class ExitZoneDespawner : MonoBehaviour
{
	public bool isCasinoExit;

	public bool isParkingExit;

	public int exitToZoneId;

	public bool hasSpecialCondition;

	[ShowIf("hasSpecialCondition")]
	public ExitCondition[] exitConditions;

	private void OnTriggerEnter(Collider other)
	{
		if ((!other.transform.CompareTag("Player") && other.transform.gameObject.layer != LayerHelper.VehiclesLayerIndex && other.transform.gameObject.layer != LayerHelper.PlayerVehiclesLayerIndex) || ((other.transform.gameObject.layer == LayerHelper.VehiclesLayerIndex || other.transform.gameObject.layer == LayerHelper.PlayerVehiclesLayerIndex) && other != InstanceBehavior<GameManager>.Instance.selectedVehicle?.vehicleCollider) || InstanceBehavior<BuildingManager>.Instance.enteringBuilding || (InstanceBehavior<GameManager>.Instance.playerController.NavigationDisabled && !IsUsingExitVehicle()) || (!BuildingManager.IsInsideBuilding && !UndergroundParkingManager.IsInsideParking) || InstanceBehavior<GameManager>.Instance.playerController.hasOnGoalReachedAction || PurchaseUI.IsPanelOpen)
		{
			return;
		}
		if (isParkingExit)
		{
			UndergroundParkingManager.ExitParking();
			return;
		}
		if (PlayerHelper.IsHoldingItem && PlayerHelper.ItemInHands.HasTag(TagRef.Itemtag.cannottakeoutside) && PlayerHelper.ItemInstanceInHands.cargoInstances.Count == 0)
		{
			PlayerHelper.ItemInstanceInHands = null;
			MouseController.Reset();
		}
		if (!PlayerHelper.HasPaidForAllItems())
		{
			Notifications.ShowError(InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness ? "exitzonedespawner_notification_cant_leave_without_paying_playerowned" : "exitzonedespawner_notification_cant_leave_without_paying", "unpaid_items_notification");
			InstanceBehavior<GameManager>.Instance.playerController.SetNewDestination(InstanceBehavior<GameManager>.Instance.playerController.transform.position, showParticleEffect: false);
			return;
		}
		if (hasSpecialCondition && exitConditions != null)
		{
			ExitCondition[] array = exitConditions;
			foreach (ExitCondition exitCondition in array)
			{
				if (!(exitCondition == null) && !exitCondition.CanExit())
				{
					string blockedNotificationKey = exitCondition.BlockedNotificationKey;
					if (blockedNotificationKey != null)
					{
						Notifications.ShowError(blockedNotificationKey, blockedNotificationKey);
					}
					InstanceBehavior<GameManager>.Instance.playerController.SetNewDestination(InstanceBehavior<GameManager>.Instance.playerController.transform.position, showParticleEffect: false);
					return;
				}
			}
		}
		if (isCasinoExit)
		{
			InstanceBehavior<CasinoBoatManager>.Instance.KickOutPlayer();
		}
		else
		{
			InstanceBehavior<BuildingManager>.Instance.ExitFromBuilding(exitToZoneId);
		}
	}

	private static bool IsUsingExitVehicle()
	{
		if (!VehicleHelper.IsInsideMotorVehicle())
		{
			return VehicleHelper.GetCurrentVehicle()?.VehicleType.HasTag(TagRef.Vehicletag.isscooter) ?? false;
		}
		return true;
	}
}
