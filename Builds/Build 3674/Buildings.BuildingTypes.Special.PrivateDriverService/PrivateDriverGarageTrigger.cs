using System;
using System.Collections;
using GleyTrafficSystem;
using Helpers;
using NWH.VehiclePhysics2;
using UI;
using UI.Overlays;
using UnityEngine;

namespace Buildings.BuildingTypes.Special.PrivateDriverService;

public class PrivateDriverGarageTrigger : MonoBehaviour
{
	private const float MinTimeBetweenDropOffs = 1.5f;

	private const float FadeTime = 0.5f;

	public CityBuildingController cbc;

	[SerializeField]
	private Collider stationCollider;

	[SerializeField]
	private PrivateDriverGarageDoor garageDoor;

	[SerializeField]
	private SpriteRenderer visuals;

	[SerializeField]
	private Transform playerResetPosition;

	private bool _isActive;

	private float _lastDropOffTime;

	public void Start()
	{
		GlobalEvents.RegisterOnGameLoadedCallback(UpdateVisuals);
		GlobalEvents.onScreenshotModeToggled = (Action<bool>)Delegate.Combine(GlobalEvents.onScreenshotModeToggled, (Action<bool>)delegate
		{
			UpdateVisuals();
		});
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onEnterVehicle, new Action<VehicleController>(OnEnterVehicle));
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, new Action<VehicleController>(OnExitVehicle));
	}

	private void UpdateVisuals()
	{
		visuals.enabled = ShouldShowDecal(VehicleHelper.GetCurrentVehicleBase());
	}

	private void OnEnterVehicle(VehicleController vehicle)
	{
		visuals.enabled = ShouldShowDecal(vehicle);
		if (vehicle.vehicleType.IsMotorVehicle && stationCollider.bounds.Intersects(vehicle.vehicleCollider.bounds))
		{
			OnStationTriggerEntered();
		}
	}

	private void OnExitVehicle(VehicleController vehicle)
	{
		visuals.enabled = false;
		OnStationTriggerExited();
	}

	private bool ShouldShowDecal(VehicleController vehicle)
	{
		if (!ScreenshotController.uiIsVisible || ScreenshotController.isInFreeLookMode)
		{
			return false;
		}
		if (vehicle == null)
		{
			return false;
		}
		return !vehicle.vehicleType.spawnInPlayerObject;
	}

	public void OnTriggerEnter(Collider other)
	{
		if (IsColliderRelevant(other))
		{
			OnStationTriggerEntered();
		}
	}

	public void OnTriggerExit(Collider other)
	{
		if (IsColliderRelevant(other))
		{
			OnStationTriggerExited();
		}
	}

	private void OnStationTriggerEntered()
	{
		if (!_isActive)
		{
			_isActive = true;
			GasStationOverlay.Hide();
			GasStationOverlay.Show(this);
		}
	}

	private void OnStationTriggerExited()
	{
		if (_isActive)
		{
			_isActive = false;
			GasStationOverlay.Hide(this);
		}
	}

	private static bool IsColliderRelevant(Collider other)
	{
		if (VehicleHelper.IsInsideMotorVehicle())
		{
			return VehicleHelper.IsColliderFromCurrentVehicle(other);
		}
		return false;
	}

	public void StartDropOff(VehicleInstance vehicleInstance)
	{
		if (!(Time.time - _lastDropOffTime < 1.5f))
		{
			_lastDropOffTime = Time.time;
			StartCoroutine(DropOffCoroutine(vehicleInstance));
		}
	}

	private IEnumerator DropOffCoroutine(VehicleInstance vehicleInstance)
	{
		PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
		playerController.ResetNavigation();
		playerController.SetNavigationBlocker(NavigationBlocker.VehicleDropOff);
		VehicleController vehicleController = InstanceBehavior<GameManager>.Instance.selectedVehicle;
		NWH.VehiclePhysics2.VehicleController nwhController = vehicleController.GetComponent<NWH.VehiclePhysics2.VehicleController>();
		if ((bool)nwhController)
		{
			nwhController.enabled = false;
		}
		yield return UiFader.Fade(0.5f);
		if (vehicleController == InstanceBehavior<GameManager>.Instance.selectedVehicle)
		{
			vehicleController.ExitVehicle();
			yield return null;
			if (!vehicleController.controlledByPlayer && !InstanceBehavior<GameManager>.Instance.selectedVehicle)
			{
				Manager.TriggerColliderRemovedEvent(vehicleController.vehicleCollider);
				if ((bool)vehicleController.additionalCollider)
				{
					Manager.TriggerColliderRemovedEvent(vehicleController.additionalCollider);
				}
				vehicleController.vehicleInstance.Delete(vehicleController);
				vehicleInstance.parkingState = ParkingState.Legal;
				SaveGameManager.Current.privateDriverVehicleInstances.Add(vehicleInstance);
				InstanceBehavior<UIs>.Instance.smartphoneUI.RebuildPrivateDriverUI();
				garageDoor.InstantCloseDoor();
				playerController.Character.navmeshAgent.enabled = false;
				playerController.transform.position = playerResetPosition.position;
				playerController.Character.navmeshAgent.enabled = true;
				_isActive = false;
			}
		}
		playerController.UnsetNavigationBlocker(NavigationBlocker.VehicleDropOff);
		playerController.Character.Reset();
		if ((bool)nwhController)
		{
			nwhController.enabled = true;
		}
		yield return UiFader.UnFade();
	}
}
