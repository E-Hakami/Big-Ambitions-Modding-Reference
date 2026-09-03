using System.Collections.Generic;
using System.Linq;
using BigAmbitions.SaveSystem;
using Buildings.BuildingTypes.Special.PrivateDriverService;
using Extensions;
using GleyTrafficSystem;
using UI;
using UI.Notification;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Device;
using Vehicles.VehicleTypes;

namespace Helpers;

public static class PrivateDriverHelpers
{
	private class PathData
	{
		public readonly List<Waypoint> points = new List<Waypoint>();

		public bool skipExpansion;

		public PathData(Waypoint waypoint)
		{
			points.Add(waypoint);
		}

		public PathData(PathData parent, Waypoint waypoint)
		{
			points.AddRange(parent.points);
			points.Add(waypoint);
		}
	}

	private const int ScreenPadding = 100;

	private const int PathfindingMaxSteps = 50;

	private const int PathfindingMaxPaths = 1000;

	private const float PathfindingCutoutDistanceSqr = 1000000f;

	private const float PathfindingCostPerWaypoint = 5f;

	private const float PathfindingDistanceDiffThreshold = 10f;

	private const float ScanRadius = 500f;

	private const string AddressableLabel = "PrivateDriverContracts";

	private static readonly List<PathData> TempPaths = new List<PathData>();

	private static readonly List<PathData> TempNewPaths = new List<PathData>();

	private static readonly Dictionary<Waypoint, (PathData, int)> VisitedWaypointPathIndex = new Dictionary<Waypoint, (PathData, int)>();

	private static VehicleComponent LastUsedVehicle;

	private static Dictionary<string, PrivateDriverContract> ContractCache;

	public static VehicleComponent GetAiVehiclePrefab(VehicleType vehicleType)
	{
		string idWithoutType = vehicleType.vehicleTypeName.GetIdWithoutType();
		GameObject gameObject = PrefabHelper.LoadPrefabAssetByName("Vehicles/" + idWithoutType);
		if (!gameObject)
		{
			Debug.LogError("Cannot find vehicle prefab for " + idWithoutType);
			return null;
		}
		VehicleComponent component = gameObject.GetComponent<VehicleComponent>();
		if ((bool)component)
		{
			return component;
		}
		Debug.LogError("Prefab " + idWithoutType + " does not have a VehicleComponent");
		return null;
	}

	public static PrivateDriverVehicle SummonPrivateDriverVehicle(VehicleInstance vehicleInstance)
	{
		VehicleComponent aiVehiclePrefab = GetAiVehiclePrefab(vehicleInstance.VehicleType);
		if (!aiVehiclePrefab)
		{
			return null;
		}
		if (BuildingManager.IsInsideBuilding)
		{
			return SummonPrivateDriverVehicleFromIndoors(vehicleInstance, aiVehiclePrefab);
		}
		PrivateDriverVehicle privateDriverVehicle = UseExistingPrivateDriverVehicle(vehicleInstance, aiVehiclePrefab);
		if ((bool)privateDriverVehicle)
		{
			return privateDriverVehicle;
		}
		Vector3 position = PlayerHelper.GetPosition();
		GetPath(aiVehiclePrefab, position, out var startingWaypoint, out var path);
		if (startingWaypoint == null)
		{
			Debug.LogWarning("No start waypoint found for private driver vehicle");
			return null;
		}
		VehicleComponent vehicleComponent = TrafficManager.Instance.LoadVehicle(aiVehiclePrefab.gameObject, startingWaypoint);
		if (!vehicleComponent)
		{
			Debug.LogWarning("Failed to spawn private driver vehicle");
			return null;
		}
		LastUsedVehicle = vehicleComponent;
		bool num = path != null;
		Waypoint targetWaypoint = (num ? TrafficManager.Instance.GetWaypoint(path.Last()) : null);
		if (num)
		{
			vehicleComponent.presetPath = path;
			Waypoint waypoint = TrafficManager.Instance.GetWaypoint(path[0]);
			vehicleComponent.transform.rotation = Quaternion.LookRotation(waypoint.position - startingWaypoint.position);
		}
		else
		{
			AIEvents.TriggerChangeDrivingStateEvent(vehicleComponent.GetIndex(), SpecialDriveActionTypes.StopNow, 0f);
		}
		if (vehicleComponent.TryGetComponent<PrivateDriverVehicle>(out var component))
		{
			Object.Destroy(component);
		}
		component = vehicleComponent.gameObject.AddComponent<PrivateDriverVehicle>();
		SetupVehicle(component, vehicleInstance);
		component.SetTargetWaypoint(targetWaypoint);
		return component;
	}

	private static PrivateDriverVehicle SummonPrivateDriverVehicleFromIndoors(VehicleInstance vehicleInstance, VehicleComponent prefab)
	{
		CityBuildingController cityBuildingController = InstanceBehavior<BuildingManager>.Instance.cityBuildingController;
		if (cityBuildingController.entranceDoors == null || cityBuildingController.entranceDoors.Length == 0)
		{
			return null;
		}
		Vector3 position = cityBuildingController.entranceDoors[0].doorTransform.position;
		Vector3 spotPosition = default(Vector3);
		Quaternion spotRotation = default(Quaternion);
		bool flag = false;
		if (InstanceBehavior<BuildingManager>.Instance.building.IsHamptonsHouse())
		{
			Waypoint closestWaypoint = TrafficManager.Instance.GetClosestWaypoint(position, 100f, TrafficManager.IsNotIntersection);
			if (closestWaypoint != null)
			{
				spotPosition = closestWaypoint.position;
				spotRotation = TrafficManager.Instance.GetWaypointOrientation(closestWaypoint);
				flag = true;
			}
			else
			{
				Debug.LogWarning("No waypoint found near Hamptons house entrance for private driver vehicle", cityBuildingController.entranceDoors[0].doorTransform);
			}
		}
		if (!flag && !TryGetClosestAvailableParkingSpot(Object.FindObjectsByType<ParkingLaneGenerator>(FindObjectsSortMode.None), position, out spotPosition, out spotRotation))
		{
			return null;
		}
		GameObject gameObject = ParkingSimulator.RequestParkedVehicle(vehicleInstance.vehicleTypeName.ToLowerInvariant());
		if (!gameObject)
		{
			return null;
		}
		LastUsedVehicle = null;
		gameObject.transform.SetPositionAndRotation(spotPosition, spotRotation);
		PrivateDriverVehicle privateDriverVehicle = gameObject.gameObject.AddComponent<PrivateDriverVehicle>();
		SetupVehicle(privateDriverVehicle, vehicleInstance);
		VehicleHelper.DestroyBlockingVehicles(prefab.gameObject, vehicleInstance.VehicleType, gameObject.transform);
		return privateDriverVehicle;
	}

	private static PrivateDriverVehicle UseExistingPrivateDriverVehicle(VehicleInstance vehicleInstance, VehicleComponent prefab)
	{
		if ((bool)LastUsedVehicle && LastUsedVehicle.prefab == prefab.gameObject && LastUsedVehicle.TryGetComponent<PrivateDriverVehicle>(out var component) && component.vehicleInstance == vehicleInstance && IsPositionOnScreen(LastUsedVehicle.transform.position, 0f))
		{
			VehicleComponent lastUsedVehicle = LastUsedVehicle;
			if (lastUsedVehicle.presetPath == null)
			{
				lastUsedVehicle.presetPath = new List<int>();
			}
			LastUsedVehicle.presetPath.Clear();
			component.RequestVehicleStop(hail: true);
			return component;
		}
		return null;
	}

	public static void SetupVehicle(PrivateDriverVehicle privateDriverVehicle, VehicleInstance vehicleInstance)
	{
		privateDriverVehicle.vehicleInstance = vehicleInstance;
		CarFeatures component = privateDriverVehicle.GetComponent<CarFeatures>();
		if ((bool)component)
		{
			component.SetDirtiness(0f);
			if (VehicleHelper.TryGetVehicleColor(vehicleInstance.vehicleColorName, out var resultVehicleColor))
			{
				component.SetColor(resultVehicleColor);
			}
		}
	}

	private static bool TryGetClosestAvailableParkingSpot(ParkingLaneGenerator[] parkingLaneGenerators, Vector3 entrancePosition, out Vector3 spotPosition, out Quaternion spotRotation)
	{
		spotPosition = Vector3.zero;
		spotRotation = Quaternion.identity;
		List<ParkingLaneGenerator> list = null;
		while (true)
		{
			float num = float.MaxValue;
			ParkingLaneGenerator parkingLaneGenerator = null;
			foreach (ParkingLaneGenerator parkingLaneGenerator2 in parkingLaneGenerators)
			{
				if (!parkingLaneGenerator2.isHandicapParking && parkingLaneGenerator2.chanceOfFreeSpot <= 99 && !parkingLaneGenerator2.ContainsDeliveryVehicle() && (list == null || !list.Contains(parkingLaneGenerator2)))
				{
					float sqrMagnitude = (parkingLaneGenerator2.GetWorldCenterPosition() - entrancePosition).sqrMagnitude;
					if (!(sqrMagnitude >= num))
					{
						num = sqrMagnitude;
						parkingLaneGenerator = parkingLaneGenerator2;
					}
				}
			}
			if (!parkingLaneGenerator)
			{
				return false;
			}
			if (parkingLaneGenerator.TryGetRandomFreeSpotForPlayerVehicle(out spotPosition, out spotRotation, 1 << LayerHelper.PlayerVehiclesLayerIndex))
			{
				break;
			}
			if (list == null)
			{
				list = new List<ParkingLaneGenerator>();
			}
			list.Add(parkingLaneGenerator);
		}
		return true;
	}

	private static void GetPath(VehicleComponent prefab, Vector3 playerPosition, out Waypoint startingWaypoint, out List<int> path)
	{
		startingWaypoint = null;
		path = null;
		List<Waypoint> list = new List<Waypoint>();
		for (float num = 0f; num < 360f; num += 90f)
		{
			Vector3 sampleDirection = Quaternion.Euler(0f, num, 0f) * Vector3.forward;
			Waypoint closestWaypoint = TrafficManager.Instance.GetClosestWaypoint(playerPosition, 100f, (Waypoint w) => IsGoodStartWaypoint(w) && IsWaypointWithDirection(w, sampleDirection, 45f));
			if (closestWaypoint != null && !list.Contains(closestWaypoint))
			{
				list.Add(closestWaypoint);
				LogWaypoint("playerClosestWaypoint", closestWaypoint);
				Waypoint startWaypoint = GetStartWaypoint(playerPosition, closestWaypoint, prefab);
				LogWaypoint("startWaypoint1", startWaypoint);
				List<int> pathToApproachPoint = GetPathToApproachPoint(startWaypoint, playerPosition);
				path = ChooseBestPath(path, pathToApproachPoint, playerPosition);
				LogPathEnd("path1" + ((path != null && path == pathToApproachPoint) ? " chosen" : ""), pathToApproachPoint);
				if (path != null && path == pathToApproachPoint)
				{
					startingWaypoint = startWaypoint;
				}
				Vector3 direction = TrafficManager.Instance.GetWaypointOrientation(closestWaypoint) * Vector3.back;
				Waypoint startWaypointFacing = GetStartWaypointFacing(playerPosition, direction, prefab);
				LogWaypoint("startWaypoint2", startWaypointFacing);
				List<int> pathToApproachPoint2 = GetPathToApproachPoint(startWaypointFacing, playerPosition);
				path = ChooseBestPath(path, pathToApproachPoint2, playerPosition);
				LogPathEnd("path2" + ((path != null && path == pathToApproachPoint2) ? " chosen" : ""), pathToApproachPoint2);
				if (path != null && path == pathToApproachPoint2)
				{
					startingWaypoint = startWaypointFacing;
				}
			}
		}
	}

	private static Waypoint GetStartWaypoint(Vector3 playerPosition, Waypoint playerClosestWaypoint, VehicleComponent prefab)
	{
		return TrafficManager.Instance.GetClosestWaypoint(playerPosition, 500f, (Waypoint w) => IsGoodStartWaypoint(w) && IsOffscreenWaypointWithDirection(w, playerClosestWaypoint), prefab) ?? TrafficManager.Instance.GetClosestWaypoint(playerPosition, 500f, (Waypoint w) => IsGoodStartWaypoint(w) && IsOffscreenWaypoint(w), prefab) ?? TrafficManager.Instance.GetClosestWaypoint(playerPosition, 500f, IsGoodStartWaypoint, prefab);
	}

	private static Waypoint GetStartWaypointFacing(Vector3 playerPosition, Vector3 direction, VehicleComponent prefab)
	{
		return TrafficManager.Instance.GetClosestWaypoint(playerPosition, 500f, (Waypoint w) => IsGoodStartWaypoint(w) && IsOffscreenWaypointWithDirection(w, direction), prefab) ?? TrafficManager.Instance.GetClosestWaypoint(playerPosition, 500f, (Waypoint w) => IsGoodStartWaypoint(w) && IsWaypointWithDirection(w, direction), prefab);
	}

	private static bool IsOffscreenWaypointWithDirection(Waypoint waypoint, Waypoint targetWaypoint)
	{
		if (!IsOffscreenWaypoint(waypoint))
		{
			return false;
		}
		Vector3 lhs = TrafficManager.Instance.GetWaypointOrientation(waypoint) * Vector3.forward;
		Vector3 normalized = (targetWaypoint.position - waypoint.position).normalized;
		if (Vector3.Dot(lhs, normalized) < 0.5f)
		{
			return false;
		}
		Vector3 rhs = TrafficManager.Instance.GetWaypointOrientation(targetWaypoint) * Vector3.forward;
		return Vector3.Dot(lhs, rhs) > 0.5f;
	}

	private static bool IsOffscreenWaypointWithDirection(Waypoint waypoint, Vector3 direction)
	{
		if (IsOffscreenWaypoint(waypoint))
		{
			return IsWaypointWithDirection(waypoint, direction);
		}
		return false;
	}

	private static bool IsOffscreenWaypoint(Waypoint waypoint)
	{
		if (waypoint.neighbors.Count == 0)
		{
			return false;
		}
		return !IsPositionOnScreen(waypoint.position, 100f);
	}

	private static bool IsPositionOnScreen(Vector3 position, float padding)
	{
		Vector3 vector = GameManager.GetMainCamera().WorldToScreenPoint(position);
		if (vector.z >= 0f && vector.x > 0f - padding && vector.x < (float)UnityEngine.Device.Screen.width + padding && vector.y > 0f - padding)
		{
			return vector.y < (float)UnityEngine.Device.Screen.height + padding;
		}
		return false;
	}

	private static bool IsGoodStartWaypoint(Waypoint waypoint)
	{
		float y = InstanceBehavior<GameManager>.Instance.playerController.transform.position.y;
		if (TrafficManager.WithinHeight(waypoint, y, 3f))
		{
			return TrafficManager.IsNotIntersection(waypoint);
		}
		return false;
	}

	private static bool IsWaypointWithDirection(Waypoint waypoint, Vector3 direction, float tolerance = 60f)
	{
		return Vector3.Angle(TrafficManager.Instance.GetWaypointOrientation(waypoint) * Vector3.forward, direction) < tolerance;
	}

	private static List<int> GetPathToApproachPoint(Waypoint startWaypoint, Vector3 target)
	{
		if (startWaypoint == null)
		{
			return null;
		}
		int bestWaypointIndex = -1;
		float bestWaypointDistanceSqr = float.MaxValue;
		TempPaths.Clear();
		VisitedWaypointPathIndex.Clear();
		foreach (int neighbor in startWaypoint.neighbors)
		{
			Waypoint waypoint = TrafficManager.Instance.GetWaypoint(neighbor);
			PathData pathData = new PathData(waypoint);
			TempPaths.Add(pathData);
			VisitedWaypointPathIndex[waypoint] = (pathData, 0);
			CheckWaypointForApproachPath(waypoint, target, ref bestWaypointIndex, ref bestWaypointDistanceSqr);
		}
		for (int i = 0; i < 50; i++)
		{
			TempNewPaths.Clear();
			foreach (PathData tempPath in TempPaths)
			{
				bool flag = false;
				if (!tempPath.skipExpansion)
				{
					foreach (int neighbor2 in tempPath.points.Last().neighbors)
					{
						Waypoint waypoint2 = TrafficManager.Instance.GetWaypoint(neighbor2);
						if ((!VisitedWaypointPathIndex.TryGetValue(waypoint2, out var value) || (value.Item1 != tempPath && value.Item2 > tempPath.points.Count)) && !((waypoint2.position - target).sqrMagnitude > 1000000f))
						{
							flag = true;
							PathData pathData2 = new PathData(tempPath, waypoint2);
							TempNewPaths.Add(pathData2);
							VisitedWaypointPathIndex[waypoint2] = (pathData2, tempPath.points.Count);
							CheckWaypointForApproachPath(waypoint2, target, ref bestWaypointIndex, ref bestWaypointDistanceSqr);
							if (TempNewPaths.Count >= 1000)
							{
								Debug.LogWarning($".GetPathToApproachPoint: Reached max path count ({1000})");
								break;
							}
						}
					}
				}
				if (!flag && tempPath.points.Any((Waypoint x) => x.listIndex == bestWaypointIndex))
				{
					tempPath.skipExpansion = true;
					TempNewPaths.Add(tempPath);
				}
				if (TempNewPaths.Count >= 1000)
				{
					break;
				}
			}
			TempPaths.Clear();
			TempPaths.AddRange(TempNewPaths);
		}
		TempNewPaths.Clear();
		VisitedWaypointPathIndex.Clear();
		if (bestWaypointIndex == -1)
		{
			TempPaths.Clear();
			return null;
		}
		PathData pathData3 = (from path in TempPaths
			where path.points.Any((Waypoint point) => point.listIndex == bestWaypointIndex)
			orderby path.points.FindIndex((Waypoint point) => point.listIndex == bestWaypointIndex)
			select path).First();
		List<int> list = new List<int>();
		foreach (Waypoint point in pathData3.points)
		{
			list.Add(point.listIndex);
			if (point.listIndex == bestWaypointIndex)
			{
				break;
			}
		}
		TempPaths.Clear();
		return list;
	}

	private static void CheckWaypointForApproachPath(Waypoint waypoint, Vector3 target, ref int bestWaypointIndex, ref float bestWaypointDistanceSqr)
	{
		if (bestWaypointIndex != waypoint.listIndex && TrafficManager.WithinHeight(waypoint, target.y, 3f))
		{
			float sqrMagnitude = (waypoint.position - target).sqrMagnitude;
			if (!(sqrMagnitude >= bestWaypointDistanceSqr))
			{
				bestWaypointIndex = waypoint.listIndex;
				bestWaypointDistanceSqr = sqrMagnitude;
			}
		}
	}

	private static List<int> ChooseBestPath(List<int> path1, List<int> path2, Vector3 playerPosition)
	{
		if ((path1 == null || path1.Count == 0) && (path2 == null || path2.Count == 0))
		{
			return null;
		}
		if (path1 == null || path1.Count == 0)
		{
			return path2;
		}
		if (path2 == null || path2.Count == 0)
		{
			return path1;
		}
		Waypoint waypoint = TrafficManager.Instance.GetWaypoint(path1.Last());
		Waypoint waypoint2 = TrafficManager.Instance.GetWaypoint(path2.Last());
		float magnitude = (waypoint.position - playerPosition).magnitude;
		float magnitude2 = (waypoint2.position - playerPosition).magnitude;
		if (Mathf.Abs(magnitude - magnitude2) > 10f)
		{
			if (!(magnitude < magnitude2))
			{
				return path2;
			}
			return path1;
		}
		float num = magnitude + (float)path1.Count * 5f;
		float num2 = magnitude2 + (float)path2.Count * 5f;
		if (!(num < num2))
		{
			return path2;
		}
		return path1;
	}

	private static void LogPathEnd(string name, List<int> path)
	{
	}

	private static void LogWaypoint(string name, Waypoint waypoint)
	{
	}

	public static Dictionary<string, PrivateDriverContract> GetContracts()
	{
		if (ContractCache != null)
		{
			return ContractCache;
		}
		IList<PrivateDriverContract> list = Addressables.LoadAssetsAsync<PrivateDriverContract>("PrivateDriverContracts", null).WaitForCompletion();
		ContractCache = new Dictionary<string, PrivateDriverContract>();
		foreach (PrivateDriverContract item in list)
		{
			ContractCache[item.key] = item;
		}
		return ContractCache;
	}

	public static PrivateDriverContract GetActiveContract()
	{
		if (string.IsNullOrEmpty(SaveGameManager.Current.activePrivateDriverContract))
		{
			return null;
		}
		return GetContracts().GetValueOrDefault(SaveGameManager.Current.activePrivateDriverContract);
	}

	public static void SetActiveContract(PrivateDriverContract contract)
	{
		SaveGameManager.Current.activePrivateDriverContract = (contract ? contract.key : null);
		if ((bool)InstanceBehavior<UIs>.Instance)
		{
			InstanceBehavior<UIs>.Instance.smartphoneUI.UpdatePrivateDriverEnabled();
		}
	}

	public static bool PayForActiveContract()
	{
		PrivateDriverContract activeContract = GetActiveContract();
		if (!activeContract || activeContract.costPerDay <= 0f)
		{
			SaveGameManager.Current.activePrivateDriverContractUnpaid = false;
			return true;
		}
		Dictionary<string, string> data = new Dictionary<string, string> { { "name", activeContract.key } };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_privatedriver", "ba:transaction_privatedriver", data);
		transactionInfo.SetTaxDeductibleName("ba:transaction_privatedriver_label");
		if (GameManager.ChangeMoneySafe(0f - activeContract.costPerDay, transactionInfo, SaveGameManager.Current.Day))
		{
			SaveGameManager.Current.activePrivateDriverContractUnpaid = false;
			return true;
		}
		Dictionary<string, string> notificationData = new Dictionary<string, string> { 
		{
			"fee",
			activeContract.costPerDay.ToShortCurrencyFormat()
		} };
		Notifications.Show(NotificationType.Error, "ba:private_driver_notification_unpaid", notificationData);
		SaveGameManager.Current.activePrivateDriverContractUnpaid = true;
		return false;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		LastUsedVehicle = null;
		ContractCache = null;
	}
}
