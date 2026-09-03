using System;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Player.HUD.ItemInfoOverlays;
using Player.PlayerMissions;
using Streets;
using UI;
using UI.Guiders;
using UnityEngine;

namespace Vehicles.DeliveryDriverJob;

public class DeliveryJobVehicle : MonoBehaviour
{
	private const float MaxParkDistance = 5f;

	private const float ResetExitVehicleDelay = 3f;

	public Sprite poiIcon;

	public Color poiColor;

	public float forgiveDamage = 0.05f;

	private VehicleController _vehicleController;

	private Address _poiAddress;

	private bool _isDespawningFromAddressTransition;

	private void Start()
	{
		_vehicleController = GetComponent<VehicleController>();
		GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(CheckIfShouldDisappear));
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, new Action<VehicleController>(OnExitVehicle));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		DeliveryDriverMission.onPinnedAddressChanged = (Action)Delegate.Combine(DeliveryDriverMission.onPinnedAddressChanged, new Action(UpdatePOIs));
		UpdatePOIs();
	}

	private void OnDestroy()
	{
		DeliveryDriverMission.onPinnedAddressChanged = (Action)Delegate.Remove(DeliveryDriverMission.onPinnedAddressChanged, new Action(UpdatePOIs));
		GlobalEvents.onNewHour = (Action)Delegate.Remove(GlobalEvents.onNewHour, new Action(CheckIfShouldDisappear));
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Remove(GlobalEvents.onExitVehicle, new Action<VehicleController>(OnExitVehicle));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		if (_isDespawningFromAddressTransition && IsActiveOngoingMissionVehicle())
		{
			return;
		}
		if ((bool)_vehicleController)
		{
			SaveGameManager.Current.VehicleInstances.Remove(_vehicleController.vehicleInstance);
		}
		if (GameManager.isCitySceneBeingUnloaded)
		{
			return;
		}
		if (!GameManager.isCitySceneBeingUnloaded && InstanceBehavior<OverlayManager>.Instance.IsShowingOverlayOverItem(_vehicleController))
		{
			InstanceBehavior<OverlayManager>.Instance.HideOverlays();
		}
		if (!(SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission))
		{
			DeliveryJobStartController.ActivateAll();
		}
		else if (!(deliveryDriverMission.vehicleId != _vehicleController.vehicleInstance.id))
		{
			DeliveryJobStartController byAddress = DeliveryJobStartController.GetByAddress(deliveryDriverMission.startAddress);
			SaveGameManager.Current.currentPlayerMission = null;
			if ((bool)byAddress)
			{
				byAddress.gameObject.SetActive(value: true);
			}
			if (!GameManager.isCitySceneBeingUnloaded)
			{
				InstanceBehavior<UIs>.Instance.tasksUI.deliveryJobUI.UpdateUI();
			}
		}
	}

	public void UpdatePOIs()
	{
		if (!(SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission))
		{
			GuidersManager.ResetGuider(DirectionGuiderType.JobDestination);
		}
		else if (deliveryDriverMission.IsOngoing())
		{
			Address address = deliveryDriverMission.pinnedAddress ?? deliveryDriverMission.destinations[0].address;
			if (address != _poiAddress)
			{
				_poiAddress = address;
				GuidersManager.SetGuiderTarget(BuildingHelper.GetAddressEntranceTransform(address).position, address.ToFormattedString(), poiIcon, poiColor, DirectionGuiderType.JobDestination);
			}
		}
		else
		{
			GuidersManager.SetGuiderTarget(DeliveryJobStartController.GetByAddress(deliveryDriverMission.startAddress).transform.position, "delivery_job_return_van".GetLocalization(), poiIcon, poiColor, DirectionGuiderType.JobDestination);
		}
	}

	private void OnExitBuilding(Address address)
	{
		if (_vehicleController.vehicleInstance.Address == address)
		{
			_isDespawningFromAddressTransition = true;
			CancelInvoke("ResetDespawnFlag");
			Invoke("ResetDespawnFlag", 3f);
		}
	}

	private void ResetDespawnFlag()
	{
		_isDespawningFromAddressTransition = false;
	}

	private void OnExitVehicle(VehicleController vehicle)
	{
		if (vehicle != _vehicleController)
		{
			return;
		}
		if (Mathf.Approximately(_vehicleController.vehicleInstance.damage, 1f))
		{
			CoroutineUtility.RunAfterFrameDelay(delegate
			{
				if (VehicleHelper.GetCurrentVehicle() != _vehicleController.vehicleInstance)
				{
					UnityEngine.Object.Destroy(base.gameObject);
				}
			}, 2);
		}
		else if (SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission && !deliveryDriverMission.IsOngoing() && !(deliveryDriverMission.vehicleId != vehicle.vehicleInstance.id))
		{
			DeliveryJobStartController nearest = DeliveryJobStartController.GetNearest(base.transform.position, 5f);
			if ((bool)nearest && nearest.building.Address == deliveryDriverMission.startAddress)
			{
				GiveEarningsAndReset(nearest);
			}
		}
	}

	private void GiveEarningsAndReset(DeliveryJobStartController startController)
	{
		if (SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission && deliveryDriverMission.vehicleId == _vehicleController.vehicleInstance.id)
		{
			if (forgiveDamage > 0f)
			{
				_vehicleController.vehicleInstance.damage = Mathf.Clamp01(_vehicleController.vehicleInstance.damage - forgiveDamage);
			}
			deliveryDriverMission.damageFees = _vehicleController.vehicleInstance.CalculateRepairCost();
			deliveryDriverMission.CalculateTips();
			float amount = Mathf.Max(0f, deliveryDriverMission.earnings - deliveryDriverMission.damageFees) + deliveryDriverMission.tips;
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_deliveryjobwage", "ba:transactioncategory_salaryincome");
			GameManager.ChangeMoneySafe(amount, transactionInfo);
			InstanceBehavior<UIs>.Instance.tasksUI.deliveryJobUI.Hide();
			InstanceBehavior<UIs>.Instance.dailySummary.RunDeliveryJobSummary();
		}
		DeliveryJobHelper.DiscardSealedBoxes();
		base.transform.SetPositionAndRotation(startController.transform.position, startController.transform.rotation);
		SaveGameManager.Current.currentPlayerMission = null;
		CoroutineUtility.RunAfterFrameDelay(delegate
		{
			if (VehicleHelper.GetCurrentVehicle() != _vehicleController.vehicleInstance)
			{
				UnityEngine.Object.Destroy(base.gameObject);
				startController.gameObject.SetActive(value: true);
				if ((bool)startController && startController.TryGetRandomAvailableNavMeshTargetPosition(out var navMeshPosition))
				{
					InstanceBehavior<GameManager>.Instance.playerController.Character.navmeshAgent.Warp(navMeshPosition);
					InstanceBehavior<GameManager>.Instance.playerController.Character.ForceToRotation(startController.transform.rotation);
				}
			}
		}, 2);
	}

	private bool IsReturnOverdue()
	{
		if (!(SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission) || deliveryDriverMission.vehicleId != _vehicleController.vehicleInstance.id)
		{
			return true;
		}
		if (!deliveryDriverMission.IsOngoing())
		{
			return deliveryDriverMission.endTime.GetDifferenceInMinutes(TimeHelper.Now()) > 1440f;
		}
		return false;
	}

	private void CheckIfShouldDisappear()
	{
		if (VehicleHelper.GetCurrentVehicle() != _vehicleController.vehicleInstance && !_vehicleController.IsVisible && IsReturnOverdue())
		{
			UnityEngine.Object.Destroy(base.gameObject);
		}
	}

	private bool IsActiveOngoingMissionVehicle()
	{
		if (SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission && deliveryDriverMission.IsOngoing())
		{
			return deliveryDriverMission.vehicleId == _vehicleController.vehicleInstance.id;
		}
		return false;
	}
}
