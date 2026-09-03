using System;
using System.Collections;
using BigAmbitions.DayNightCycle;
using Helpers;
using NWH.VehiclePhysics2.Modules.Fuel;
using UI;
using UI.Notification;
using UnityEngine;

namespace Vehicles;

public static class SkipBridgeHelper
{
	private const float MinutePerMeter = 0.04f;

	private const float MetersPerKilometer = 1000f;

	private const float MoveBackIterationDistance = 5f;

	private const float MoveBackMaxDistance = 50f;

	private const float SkipBridgeFuelConsumptionMultiplier = 5.5f;

	public static bool skipBridgeEnabled;

	public static Action onSkipBridgeEnabled;

	public static Action onSkipBridgeDisabled;

	private static Transform[] CurrentTargets;

	public static void EnableSkipBridge(Transform[] targets)
	{
		CurrentTargets = targets;
		if (!skipBridgeEnabled)
		{
			skipBridgeEnabled = true;
			onSkipBridgeEnabled?.Invoke();
		}
	}

	public static Transform FindTarget(VehicleController vehicle)
	{
		Transform transform = CurrentTargets[0];
		float y = vehicle.transform.eulerAngles.y;
		float num = Mathf.Abs(Mathf.DeltaAngle(y, transform.eulerAngles.y));
		for (int i = 1; i < CurrentTargets.Length; i++)
		{
			float num2 = Mathf.Abs(Mathf.DeltaAngle(y, CurrentTargets[i].eulerAngles.y));
			if (!(num2 >= num))
			{
				num = num2;
				transform = CurrentTargets[i];
			}
		}
		return transform;
	}

	public static void SkipBridge()
	{
		VehicleController currentVehicleBase = VehicleHelper.GetCurrentVehicleBase();
		Transform target = FindTarget(currentVehicleBase);
		currentVehicleBase.StartCoroutine(SkipBridge(currentVehicleBase, target));
	}

	private static IEnumerator SkipBridge(VehicleController vehicle, Transform target)
	{
		if (!TryGetFreePosition(vehicle, target, out var position))
		{
			Notifications.ShowError("notification_cant_skip_bridge_blocked");
			yield break;
		}
		float distance = Vector3.Distance(vehicle.transform.position, position);
		if (!TryGetFuelConsumption(vehicle, distance, out var fuelConsumption))
		{
			Notifications.ShowError("ba:notification_cant_skip_bridge_notenoughfuel");
			yield break;
		}
		yield return UiFader.Fade();
		ConsumeFuel(vehicle, fuelConsumption);
		vehicle.GetComponent<Rigidbody>().Move(position + Vector3.up * 0.5f, target.rotation);
		vehicle.SavePosition();
		if (vehicle is CarController carController)
		{
			carController.Reset();
		}
		yield return null;
		Timestamp timestamp = TimeHelper.Now();
		timestamp.AddMinutes(distance * 0.04f * PlayerPrefSettings.GameSpeed);
		InstanceBehavior<UIs>.Instance.timeMachine.StartTimeMachine(timestamp, disableCancel: true);
		yield return UiFader.UnFade();
	}

	private static bool TryGetFuelConsumption(VehicleController vehicle, float distance, out float fuelConsumption)
	{
		fuelConsumption = 0f;
		GameInstance current = SaveGameManager.Current;
		if ((current != null && current.gameVariables.disableVehicleFuel) || !(vehicle is CarController { fuelModule: { useFuel: not false } } carController))
		{
			return true;
		}
		fuelConsumption = GetFuelConsumption(carController, distance);
		if (fuelConsumption <= 0f)
		{
			return true;
		}
		return carController.GetCurrentFuel() >= fuelConsumption;
	}

	private static void ConsumeFuel(VehicleController vehicle, float fuelConsumption)
	{
		if (!(fuelConsumption <= 0f) && vehicle is CarController carController)
		{
			carController.SetFuel(carController.GetCurrentFuel() - fuelConsumption);
		}
	}

	private static float GetFuelConsumption(CarController carController, float distance)
	{
		if (carController.vehicleType.maxSpeed <= 0)
		{
			return 0f;
		}
		FuelModule fuelModule = carController.fuelModule;
		float num = fuelModule.maxConsumptionPerHour * fuelModule.idleConsumption * 5.5f * fuelModule.consumptionMultiplier;
		return distance / 1000f / (float)carController.vehicleType.maxSpeed * num;
	}

	private static bool TryGetFreePosition(VehicleController vehicle, Transform target, out Vector3 position)
	{
		position = target.position;
		Ray ray = new Ray(target.position + Vector3.up * 50f, Vector3.down);
		if (Physics.Raycast(ray, out var hitInfo, 100f, 1 << LayerHelper.RoadsLayerIndex, QueryTriggerInteraction.Ignore))
		{
			position = hitInfo.point;
		}
		Vector3 vector = -target.forward;
		float num = 0f;
		Bounds bounds = new Bounds(vehicle.transform.position, Vector3.zero);
		Collider[] componentsInChildren = vehicle.GetComponentsInChildren<Collider>();
		foreach (Collider collider in componentsInChildren)
		{
			bounds.Encapsulate(collider.bounds);
		}
		while (Physics.CheckBox(position, bounds.extents, target.rotation, LayerHelper.freeParkingSpotDetectionMask))
		{
			num += 5f;
			position = target.position + vector * num;
			ray.origin = position + Vector3.up * 50f;
			if (Physics.Raycast(ray, out hitInfo, 100f, 1 << LayerHelper.RoadsLayerIndex, QueryTriggerInteraction.Ignore))
			{
				position = hitInfo.point;
			}
			if (num >= 50f)
			{
				break;
			}
		}
		return num < 50f;
	}

	public static void DisableSkipBridge()
	{
		if (skipBridgeEnabled)
		{
			skipBridgeEnabled = false;
			CurrentTargets = null;
			onSkipBridgeDisabled?.Invoke();
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		skipBridgeEnabled = false;
		CurrentTargets = null;
		onSkipBridgeEnabled = null;
		onSkipBridgeDisabled = null;
	}
}
