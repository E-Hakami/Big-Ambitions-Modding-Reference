using System.Collections.Generic;
using GleyTrafficSystem;
using Helpers;
using UnityEngine;
using Vehicles;

namespace Streets;

public class BridgeTriggerController : MonoBehaviour
{
	private const float StatusUpdateInterval = 0.25f;

	private static readonly List<BridgeTriggerController> Instances = new List<BridgeTriggerController>();

	private static bool IsOnBridge;

	private static bool HasPendingStatusChange;

	private static float NextStatusUpdateTime;

	[SerializeField]
	private BridgeController bridgeController;

	[SerializeField]
	private Transform[] skipTargets;

	private bool _containsPlayer;

	private void Awake()
	{
		Instances.Add(this);
	}

	private void OnDestroy()
	{
		Instances.Remove(this);
		if (Instances.Count == 0)
		{
			IsOnBridge = false;
		}
	}

	private void OnValidate()
	{
		if (GetComponents<Collider>().Length > 1)
		{
			Debug.LogWarning("More than one collider found on " + base.name, this);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			_containsPlayer = true;
			UpdateStatus();
		}
		else
		{
			if (other.gameObject.name != "BodyCollider")
			{
				return;
			}
			CarFeatures componentInParent = other.GetComponentInParent<CarFeatures>();
			if ((bool)componentInParent)
			{
				bridgeController.AddCarToBridge(componentInParent);
				if (IsActiveVehicle(componentInParent))
				{
					UpdateStatus();
				}
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("Player"))
		{
			_containsPlayer = false;
			UpdateStatus();
		}
		else
		{
			if (other.gameObject.name != "BodyCollider")
			{
				return;
			}
			CarFeatures componentInParent = other.GetComponentInParent<CarFeatures>();
			if ((bool)componentInParent)
			{
				bridgeController.RemoveCarFromBridge(componentInParent);
				if (IsActiveVehicle(componentInParent))
				{
					UpdateStatus();
				}
			}
		}
	}

	private static bool IsActiveVehicle(CarFeatures carFeatures)
	{
		VehicleController currentVehicleBase = VehicleHelper.GetCurrentVehicleBase();
		if ((bool)currentVehicleBase)
		{
			return currentVehicleBase.CarFeatures == carFeatures;
		}
		return false;
	}

	public static void UpdateStatusPeriodically()
	{
		if (!(Time.time < NextStatusUpdateTime))
		{
			NextStatusUpdateTime = Time.time + 0.25f;
			if (IsPlayerOnBridge(out var _) != IsOnBridge && !HasPendingStatusChange)
			{
				HasPendingStatusChange = true;
				return;
			}
			HasPendingStatusChange = false;
			UpdateStatus();
		}
	}

	public static void UpdateStatus()
	{
		BridgeTriggerController bridgeTriggerController = null;
		bool flag = IsPlayerOnBridge(out var bridgeController);
		if (flag && VehicleHelper.IsInsideMotorVehicle())
		{
			bridgeTriggerController = FindSkipTrigger(bridgeController);
		}
		if ((bool)bridgeTriggerController)
		{
			SkipBridgeHelper.EnableSkipBridge(bridgeTriggerController.skipTargets);
		}
		else
		{
			SkipBridgeHelper.DisableSkipBridge();
		}
		if (flag != IsOnBridge)
		{
			IsOnBridge = flag;
			if (IsOnBridge)
			{
				AddPlayerToBridge(bridgeController);
			}
			else
			{
				RemovePlayerFromBridge();
			}
		}
	}

	private static BridgeTriggerController FindSkipTrigger(BridgeController bridgeController)
	{
		for (int i = 0; i < Instances.Count; i++)
		{
			BridgeTriggerController bridgeTriggerController = Instances[i];
			Transform[] array = bridgeTriggerController.skipTargets;
			if (array != null && array.Length > 0 && bridgeTriggerController.bridgeController == bridgeController)
			{
				return bridgeTriggerController;
			}
		}
		return null;
	}

	private static bool IsPlayerOnBridge(out BridgeController bridgeController)
	{
		bridgeController = null;
		for (int i = 0; i < Instances.Count; i++)
		{
			if (Instances[i]._containsPlayer)
			{
				bridgeController = Instances[i].bridgeController;
				return true;
			}
		}
		if (!VehicleHelper.IsInsideMotorVehicle())
		{
			return false;
		}
		VehicleController currentVehicleBase = VehicleHelper.GetCurrentVehicleBase();
		if (!currentVehicleBase)
		{
			return false;
		}
		CarFeatures carFeatures = currentVehicleBase.CarFeatures;
		if (carFeatures == null)
		{
			return false;
		}
		bridgeController = carFeatures.GetBridgeBelow();
		return bridgeController != null;
	}

	private static void AddPlayerToBridge(BridgeController bridgeController)
	{
		TrafficManager.Instance.SetMinDistanceToAdd(bridgeController.aiTrafficVehiclesSpawnRadius);
		TrafficManager.Instance.SetDistanceToRemove(bridgeController.aiTrafficVehiclesDeSpawnRadius);
		float[] steeringAnglesAssists = bridgeController.steeringAnglesAssists;
		foreach (float item in steeringAnglesAssists)
		{
			AngleAssist.ValidRoadAngles.Add(item);
		}
	}

	private static void RemovePlayerFromBridge()
	{
		TrafficManager.Instance.SetMinDistanceToAdd(CityManager.defaultAiTrafficVehiclesSpawnRadius);
		TrafficManager.Instance.SetDistanceToRemove(CityManager.defaultAiTrafficVehiclesDeSpawnRadius);
		AngleAssist.ResetRoadAngles();
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		Instances.Clear();
		IsOnBridge = false;
		HasPendingStatusChange = false;
		NextStatusUpdateTime = 0f;
	}
}
