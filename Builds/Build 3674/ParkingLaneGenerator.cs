using System;
using System.Collections.Generic;
using Buildings;
using Extensions;
using GleyTrafficSystem;
using Helpers;
using IngameDebugConsole;
using JimmysUnityUtilities;
using NaughtyAttributes;
using UnityEngine;
using Vehicles.DeliveryDriverJob;

[RequireComponent(typeof(BoxCollider))]
public sealed class ParkingLaneGenerator : MonoBehaviour
{
	private struct LaneObstacle
	{
		public float start;

		public float end;
	}

	private struct LaneSegment
	{
		public Transform transform;

		public BoxCollider collider;

		public float startDistance;

		public float length;
	}

	private readonly struct LaneSpan(Transform transform, Vector3 centerLocal, float length)
	{
		public readonly Transform transform = transform;

		public readonly Vector3 centerLocal = centerLocal;

		public readonly float length = length;

		public readonly Vector3 start = transform.TransformPoint(centerLocal) - transform.forward * length / 2f;
	}

	private const float RegenerationSearchDistance = 6f;

	private const float LaneVerticalTolerance = 3f;

	private const int MaxNearbyLanes = 32;

	private const string SegmentNamePrefix = "Lane Segment ";

	public static bool spawningActive = true;

	public static bool pendingDestroyBlockingParkedVehicles;

	private static readonly Dictionary<Building, ParkingLaneGenerator> DeliveryVanSpots = new Dictionary<Building, ParkingLaneGenerator>();

	private static readonly Collider[] NearbyLaneColliders = new Collider[32];

	private static readonly ParkingLaneGenerator[] ProcessedLanes = new ParkingLaneGenerator[32];

	[Header("References")]
	public Building building;

	public string neighbourhood;

	[Header("Parking Lane Layout")]
	[SerializeField]
	private float parkingSpotDistance = 5f;

	[MinValue(0)]
	[SerializeField]
	private int spotCountOverride;

	[Space]
	[SerializeField]
	private bool sideBySideParkingSpots;

	[EnableIf("sideBySideParkingSpots")]
	[SerializeField]
	private float sideBySideAngle;

	[EnableIf("sideBySideParkingSpots")]
	[SerializeField]
	private float depthRandomOffset = 0.4f;

	[EnableIf("sideBySideParkingSpots")]
	[MinValue(0)]
	[MaxValue(100)]
	[SerializeField]
	private int inverseParkingChance = 57;

	[Header("Curve")]
	[SerializeField]
	private bool curvedLane;

	[EnableIf("curvedLane")]
	[SerializeField]
	private float curveAngle = 45f;

	[EnableIf("curvedLane")]
	[MinValue(1)]
	[SerializeField]
	private int curveSegmentCount = 4;

	[Header("Vehicle Placement")]
	[Range(0f, 100f)]
	public int chanceOfFreeSpot = 75;

	[MinValue(0)]
	[SerializeField]
	private int minFreeSpots;

	[DisableIf("sideBySideParkingSpots")]
	[MinMaxSlider(0f, 2f)]
	[SerializeField]
	private Vector2 parkingSpotSizeRandomRange = new Vector2(0.2f, 0.8f);

	[SerializeField]
	private float extraAngleRotation = 2.5f;

	[SerializeField]
	private float lateralRandomOffset = 0.3f;

	[Header("Business Hours")]
	[Range(0f, 100f)]
	[SerializeField]
	private int closedBusinessFreeSpotChance = 97;

	[Header("Specific Vehicles")]
	public bool designatedDeliveryVanSpot;

	[SerializeField]
	private string[] specificVehicles;

	[Header("Rental")]
	[SerializeField]
	private bool removeVehiclesIfRentedByPlayer;

	[Header("Handicap")]
	public bool isHandicapParking;

	public List<AutoParkSpot> autoParkSpots = new List<AutoParkSpot>();

	public Action<GameObject> onGenerateVehicle;

	public Action<GameObject> onReleaseVehicle;

	private AutoParkSettings _autoParkSettings;

	private BoxCollider _collider;

	private bool _initialized;

	private bool _lastOpeningState;

	private GameObject _specialVehicle;

	private Vector3 _laneCenterPoint;

	private Vector3 _start;

	private Vector3 _end;

	private List<Vector3> _startPoints = new List<Vector3>();

	private List<Vector3> _endPoints = new List<Vector3>();

	private readonly List<BoxCollider> _reserveObstacles = new List<BoxCollider>();

	private readonly List<LaneObstacle> _obstacles = new List<LaneObstacle>();

	private readonly List<LaneSegment> _segments = new List<LaneSegment>();

	private float _totalCurveLength;

	private float Width
	{
		get
		{
			if (!IsCurved)
			{
				if (!sideBySideParkingSpots)
				{
					return _collider.size.z;
				}
				return _collider.size.x;
			}
			return _totalCurveLength;
		}
	}

	private float SlotSpan
	{
		get
		{
			if (spotCountOverride <= 0)
			{
				return Width;
			}
			return (float)spotCountOverride * parkingSpotDistance;
		}
	}

	private float SlotStartOffset
	{
		get
		{
			if (spotCountOverride <= 0)
			{
				return 0f;
			}
			return Mathf.Max(0f, (Width - SlotSpan) * 0.5f);
		}
	}

	private bool IsCurved
	{
		get
		{
			if (curvedLane)
			{
				return curveSegmentCount > 0;
			}
			return false;
		}
	}

	private bool IsEditor
	{
		get
		{
			if (Application.isEditor)
			{
				return !Application.isPlaying;
			}
			return false;
		}
	}

	public Vector3 GetWorldCenterPosition()
	{
		if (!_collider && !TryGetComponent<BoxCollider>(out _collider))
		{
			return base.transform.position;
		}
		if (!IsCurved)
		{
			return base.transform.TransformPoint(_collider.center);
		}
		return EvaluateCurveWorldPoint(_totalCurveLength * 0.5f, 0f);
	}

	public bool TryGetRandomFreeSpotForPlayerVehicle(out Vector3 spotPosition, out Quaternion spotRotation)
	{
		return TryGetRandomFreeSpotForPlayerVehicle(out spotPosition, out spotRotation, LayerHelper.freeParkingSpotDetectionMask);
	}

	public bool TryGetRandomFreeSpotForPlayerVehicle(out Vector3 spotPosition, out Quaternion spotRotation, LayerMask layerMask)
	{
		spotPosition = default(Vector3);
		spotRotation = default(Quaternion);
		if (_collider == null && !TryGetComponent<BoxCollider>(out _collider))
		{
			return false;
		}
		if (IsCurved && _segments.Count == 0)
		{
			SyncCurveSegments();
		}
		int num = Mathf.FloorToInt(SlotSpan / parkingSpotDistance);
		if (num <= 0)
		{
			return false;
		}
		int num2 = UnityEngine.Random.Range(0, num);
		for (int i = 0; i < num; i++)
		{
			int slotIndex = (num2 + i) % num;
			GetPlayerSpotPose(slotIndex, out var center, out var rotation);
			Vector3 halfExtents = (sideBySideParkingSpots ? new Vector3(parkingSpotDistance * 0.5f, 0.5f, _collider.size.z * 0.5f) : new Vector3(_collider.size.x * 0.5f, 0.5f, parkingSpotDistance * 0.5f));
			if (!Physics.CheckBox(center, halfExtents, rotation, layerMask))
			{
				spotPosition = center;
				spotRotation = rotation;
				return true;
			}
		}
		return false;
	}

	private void GetPlayerSpotPose(int slotIndex, out Vector3 center, out Quaternion rotation)
	{
		if (IsCurved && TryResolveSegment(SlotStartOffset + ((float)slotIndex + 0.5f) * parkingSpotDistance, out var segment, out var localDistance))
		{
			float num = localDistance - segment.length * 0.5f;
			Vector3 position = (sideBySideParkingSpots ? new Vector3(num, 0f, 0f) : new Vector3(0f, 0f, num));
			center = segment.transform.TransformPoint(position) + base.transform.up * 0.1f;
			rotation = (sideBySideParkingSpots ? (segment.transform.rotation * Quaternion.Euler(0f, sideBySideAngle, 0f)) : segment.transform.rotation);
			return;
		}
		Vector3 center2 = _collider.center;
		if (sideBySideParkingSpots)
		{
			center2.x += _collider.size.x * 0.5f - SlotStartOffset - ((float)slotIndex + 0.5f) * parkingSpotDistance;
		}
		else
		{
			center2.z += SlotStartOffset - _collider.size.z * 0.5f + ((float)slotIndex + 0.5f) * parkingSpotDistance;
		}
		center = base.transform.TransformPoint(center2) + base.transform.up * 0.1f;
		rotation = (sideBySideParkingSpots ? (base.transform.rotation * Quaternion.Euler(0f, sideBySideAngle, 0f)) : base.transform.rotation);
	}

	public bool TryReserveSpot(out Vector3 spotPosition, out Quaternion spotRotation, LayerMask layerMask, int layerIndex)
	{
		if (!TryGetRandomFreeSpotForPlayerVehicle(out spotPosition, out spotRotation, layerMask))
		{
			return false;
		}
		GameObject obj = new GameObject("Reserved Parking Spot");
		obj.layer = layerIndex;
		obj.transform.parent = base.transform;
		obj.transform.position = spotPosition;
		obj.transform.rotation = spotRotation;
		float num = Mathf.Max(1f, parkingSpotDistance * 0.5f - 1f);
		float num2 = Mathf.Max(1f, _collider.size.z * 0.5f - 1f);
		Vector3 vector = (sideBySideParkingSpots ? new Vector3(num, 0.5f, num2) : new Vector3(num2, 0.5f, num));
		BoxCollider boxCollider = obj.AddComponent<BoxCollider>();
		boxCollider.size = vector * 2f;
		_reserveObstacles.Add(boxCollider);
		return true;
	}

	public void ReleaseReservedSpots()
	{
		foreach (BoxCollider reserveObstacle in _reserveObstacles)
		{
			if ((bool)reserveObstacle)
			{
				reserveObstacle.enabled = false;
				UnityEngine.Object.Destroy(reserveObstacle.gameObject);
			}
		}
		_reserveObstacles.Clear();
	}

	private void Awake()
	{
		if (designatedDeliveryVanSpot)
		{
			ParkingLaneGenerator value;
			if (!building)
			{
				Debug.LogWarning("Designated Delivery Van Spot without assigned building: " + base.name, this);
			}
			else if (DeliveryVanSpots.TryGetValue(building, out value) && (bool)value && value != this)
			{
				Debug.LogWarning($"Duplicate deliveryVanSpot for {building.Address}: {base.name}", this);
				Debug.LogWarning($"Original deliveryVanSpot for {building.Address}: {value}", value);
			}
			else
			{
				DeliveryVanSpots[building] = this;
			}
		}
	}

	private void Start()
	{
		if (!_initialized)
		{
			GlobalEvents.RegisterOnGameLoadedCallback(Init);
		}
	}

	public void Init()
	{
		_initialized = true;
		ClampAllChances();
		CleanupParkedVehicles(force: true);
		TryGetComponent<BoxCollider>(out _collider);
		SyncCurveSegments();
		if (building != null)
		{
			GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
			GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
		}
		if (Application.isPlaying && (bool)building)
		{
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(building.Address);
			if (buildingRegistration.RentedByPlayer && removeVehiclesIfRentedByPlayer)
			{
				return;
			}
			string businessTypeName = buildingRegistration.businessTypeName;
			if (string.IsNullOrEmpty(businessTypeName) || businessTypeName == "ba:businesstype_empty")
			{
				InitNonBusinessParkingLane();
				return;
			}
			bool flag = BusinessHelper.IsBusinessOpen(buildingRegistration);
			GenerateParkedVehicles(GetFreeSpotChanceForBusiness(flag), force: true);
			DeferDestroyBlockingParkedVehicles();
			_lastOpeningState = flag;
			GlobalEvents.onNewHour = (Action)Delegate.Remove(GlobalEvents.onNewHour, new Action(OnNewHour));
			GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(OnNewHour));
		}
		else
		{
			InitNonBusinessParkingLane();
		}
		GenerateAutoParkSpots();
	}

	private void ClampAllChances()
	{
		chanceOfFreeSpot = Mathf.Clamp(chanceOfFreeSpot, 0, 100);
		closedBusinessFreeSpotChance = Mathf.Clamp(closedBusinessFreeSpotChance, 0, 100);
	}

	private void SyncCurveSegments()
	{
		if (!_collider && !TryGetComponent<BoxCollider>(out _collider))
		{
			return;
		}
		_segments.Clear();
		_totalCurveLength = 0f;
		if (!IsCurved)
		{
			_collider.enabled = true;
			if ((bool)base.transform.Find(GetSegmentName(0)))
			{
				DestroySurplusSegments(0);
			}
			return;
		}
		_collider.enabled = false;
		for (int i = 0; i < curveSegmentCount; i++)
		{
			BoxCollider orCreateSegmentCollider = GetOrCreateSegmentCollider(i);
			ApplySegmentGeometry(orCreateSegmentCollider, i);
			float num = (sideBySideParkingSpots ? orCreateSegmentCollider.size.x : orCreateSegmentCollider.size.z);
			_segments.Add(new LaneSegment
			{
				transform = orCreateSegmentCollider.transform,
				collider = orCreateSegmentCollider,
				startDistance = _totalCurveLength,
				length = num
			});
			_totalCurveLength += num;
		}
		DestroySurplusSegments(curveSegmentCount);
		Physics.SyncTransforms();
	}

	private BoxCollider GetOrCreateSegmentCollider(int index)
	{
		Transform transform = base.transform.Find(GetSegmentName(index));
		if (!transform)
		{
			GameObject obj = new GameObject(GetSegmentName(index));
			obj.layer = LayerHelper.ParkingAreaLayerIndex;
			obj.transform.SetParent(base.transform, worldPositionStays: false);
			return obj.AddComponent<BoxCollider>();
		}
		transform.gameObject.layer = LayerHelper.ParkingAreaLayerIndex;
		if (!transform.TryGetComponent<BoxCollider>(out var component))
		{
			return transform.gameObject.AddComponent<BoxCollider>();
		}
		return component;
	}

	private void ApplySegmentGeometry(BoxCollider segmentCollider, int index)
	{
		GetSegmentPose(index, out var position, out var rotation, out var length);
		segmentCollider.transform.SetPositionAndRotation(position, rotation);
		segmentCollider.isTrigger = true;
		segmentCollider.center = Vector3.zero;
		segmentCollider.size = GetSegmentSize(length);
	}

	private Vector3 GetSegmentSize(float length)
	{
		if (!sideBySideParkingSpots)
		{
			return new Vector3(_collider.size.x, _collider.size.y, length);
		}
		return new Vector3(length, _collider.size.y, _collider.size.z);
	}

	private Vector3 GetCurvePoint(int index)
	{
		float num = (sideBySideParkingSpots ? _collider.size.x : _collider.size.z);
		float num2 = num / (float)curveSegmentCount;
		float num3 = curveAngle / (float)curveSegmentCount;
		Vector3 vector = (sideBySideParkingSpots ? Vector3.right : Vector3.forward);
		Vector3 result = _collider.center - vector * (num * 0.5f);
		for (int i = 0; i < index; i++)
		{
			result += Quaternion.AngleAxis(((float)i + 0.5f) * num3, Vector3.up) * vector * num2;
		}
		return result;
	}

	private void GetSegmentPose(int index, out Vector3 position, out Quaternion rotation, out float length)
	{
		Vector3 vector = base.transform.TransformPoint(GetCurvePoint(index));
		Vector3 vector2 = base.transform.TransformPoint(GetCurvePoint(index + 1));
		length = Vector3.Distance(vector, vector2);
		rotation = Quaternion.LookRotation((vector2 - vector).normalized, base.transform.up);
		if (sideBySideParkingSpots)
		{
			rotation *= Quaternion.Euler(0f, -90f, 0f);
		}
		position = (vector + vector2) * 0.5f;
	}

	private void DestroySurplusSegments(int segmentCount)
	{
		Transform[] children = base.transform.GetChildren();
		foreach (Transform transform in children)
		{
			if (transform.name.StartsWith("Lane Segment ") && (!int.TryParse(transform.name.Substring("Lane Segment ".Length), out var result) || result >= segmentCount))
			{
				DestroyLaneChild(transform.gameObject);
			}
		}
	}

	private static string GetSegmentName(int index)
	{
		return "Lane Segment " + index;
	}

	private bool TryResolveSegment(float distance, out LaneSegment segment, out float localDistance)
	{
		if (_segments.Count == 0)
		{
			segment = default(LaneSegment);
			localDistance = 0f;
			return false;
		}
		distance = Mathf.Clamp(distance, 0f, _totalCurveLength);
		for (int i = 0; i < _segments.Count - 1; i++)
		{
			LaneSegment laneSegment = _segments[i];
			if (!(distance > laneSegment.startDistance + laneSegment.length))
			{
				segment = laneSegment;
				localDistance = distance - laneSegment.startDistance;
				return true;
			}
		}
		List<LaneSegment> segments = _segments;
		segment = segments[segments.Count - 1];
		localDistance = distance - segment.startDistance;
		return true;
	}

	private Vector3 EvaluateCurveWorldPoint(float distance, float lateralOffset)
	{
		if (!TryResolveSegment(distance, out var segment, out var localDistance))
		{
			return base.transform.position;
		}
		float num = localDistance - segment.length * 0.5f;
		Vector3 position = (sideBySideParkingSpots ? new Vector3(num, 0f, lateralOffset) : new Vector3(lateralOffset, 0f, num));
		return segment.transform.TransformPoint(position);
	}

	private Vector3 GetClosestLanePoint(Vector3 worldPoint)
	{
		if (!IsCurved)
		{
			return _collider.ClosestPoint(worldPoint);
		}
		Vector3 result = base.transform.position;
		float num = float.MaxValue;
		foreach (LaneSegment segment in _segments)
		{
			Vector3 vector = segment.collider.ClosestPoint(worldPoint);
			float num2 = MathHelper.DistanceSqr(vector, worldPoint);
			if (!(num2 >= num))
			{
				result = vector;
				num = num2;
			}
		}
		return result;
	}

	private int GetFreeSpotChanceForBusiness(bool isOpen)
	{
		if (!isOpen)
		{
			return closedBusinessFreeSpotChance;
		}
		return chanceOfFreeSpot;
	}

	private float GetSideBySideDepthOffset(float vehicleHalfDepth)
	{
		float num = _collider.size.z / 2f - vehicleHalfDepth;
		if (num <= 0f)
		{
			return 0f;
		}
		return Mathf.Clamp(UnityEngine.Random.Range(0f - depthRandomOffset, depthRandomOffset), 0f - num, num);
	}

	private void InitNonBusinessParkingLane()
	{
		GenerateParkedVehicles(chanceOfFreeSpot, force: true);
		DeferDestroyBlockingParkedVehicles();
		ParkingSimulator.ParkingLaneRegeneration.RemoveListener(OnParkingLaneRegeneration);
		ParkingSimulator.ParkingLaneRegeneration.AddListener(OnParkingLaneRegeneration);
	}

	private void OnNewHour()
	{
		bool flag = BusinessHelper.IsBusinessOpen(BuildingHelper.GetBuildingRegistration(building.Address));
		if (_lastOpeningState != flag)
		{
			ParkingSimulator.parkingQueueWorker.AddWork(this);
		}
	}

	private void OnParkingLaneRegeneration()
	{
		ParkingSimulator.parkingQueueWorker.AddWork(this);
	}

	private void OnDestroy()
	{
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
		GlobalEvents.onNewHour = (Action)Delegate.Remove(GlobalEvents.onNewHour, new Action(OnNewHour));
		ParkingSimulator.ParkingLaneRegeneration.RemoveListener(OnParkingLaneRegeneration);
	}

	private void DeferDestroyBlockingParkedVehicles()
	{
		if (pendingDestroyBlockingParkedVehicles)
		{
			return;
		}
		pendingDestroyBlockingParkedVehicles = true;
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			VehicleHelper.DestroyBlockingParkedVehicles();
			pendingDestroyBlockingParkedVehicles = false;
			foreach (VehicleController allPlayerVehicle in VehicleHelper.AllPlayerVehicles)
			{
				if ((bool)allPlayerVehicle)
				{
					RegenerateAutoParkSpotsNear(allPlayerVehicle.transform.position);
				}
			}
		});
	}

	private void OnBuildingRegistrationChange(Address address)
	{
		if (address != building.Address)
		{
			return;
		}
		DeliveryJobStartLocation deliveryJobStartLocation = building.deliveryJobStartLocation;
		if ((object)deliveryJobStartLocation != null)
		{
			string[] possibleItems = deliveryJobStartLocation.possibleItems;
			if (possibleItems != null && possibleItems.Length > 0)
			{
				GenerateAutoParkSpots();
			}
		}
		if (BuildingHelper.GetBuildingRegistration(address).RentedByPlayer && removeVehiclesIfRentedByPlayer)
		{
			CleanupParkedVehicles(force: true);
			GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
		}
	}

	public bool ContainsDeliveryVehicle()
	{
		if (building != null)
		{
			DeliveryJobStartLocation deliveryJobStartLocation = building.deliveryJobStartLocation;
			if ((object)deliveryJobStartLocation != null)
			{
				string[] possibleItems = deliveryJobStartLocation.possibleItems;
				if (possibleItems != null && possibleItems.Length > 0)
				{
					return !BuildingHelper.GetBuildingRegistration(building.Address).RentedByPlayer;
				}
			}
		}
		return false;
	}

	public static void RegenerateAutoParkSpotsNear(Vector3 position)
	{
		int num = Physics.OverlapSphereNonAlloc(position, 6f, NearbyLaneColliders, LayerHelper.parkingAreaLayerMask, QueryTriggerInteraction.Collide);
		int num2 = 0;
		for (int i = 0; i < num; i++)
		{
			ParkingLaneGenerator componentInParent = NearbyLaneColliders[i].GetComponentInParent<ParkingLaneGenerator>();
			if ((bool)componentInParent && (bool)componentInParent._collider && !IsProcessed(componentInParent, num2))
			{
				ProcessedLanes[num2] = componentInParent;
				num2++;
				componentInParent.GenerateAutoParkSpots();
			}
		}
	}

	private static bool IsProcessed(ParkingLaneGenerator lane, int processedCount)
	{
		for (int i = 0; i < processedCount; i++)
		{
			if (ProcessedLanes[i] == lane)
			{
				return true;
			}
		}
		return false;
	}

	[Button("Clean and generate AutoPark spots", EButtonEnableMode.Always)]
	private void GenerateAutoParkSpots()
	{
		if (sideBySideParkingSpots)
		{
			return;
		}
		GlobalReferences instance = InstanceBehavior<GlobalReferences>.Instance;
		if (instance == null)
		{
			return;
		}
		_autoParkSettings = instance.autoParkSettings;
		foreach (AutoParkSpot autoParkSpot in autoParkSpots)
		{
			if ((bool)autoParkSpot)
			{
				autoParkSpot.Destroy();
			}
		}
		autoParkSpots.Clear();
		_startPoints.Clear();
		_endPoints.Clear();
		if (ContainsDeliveryVehicle())
		{
			return;
		}
		if (IsCurved && _segments.Count == 0)
		{
			SyncCurveSegments();
		}
		Physics.SyncTransforms();
		MeshCollider[] componentsInChildren = base.transform.GetComponentsInChildren<MeshCollider>();
		if (IsCurved)
		{
			_start = EvaluateCurveWorldPoint(0f, 0f);
			_end = EvaluateCurveWorldPoint(_totalCurveLength, 0f);
			{
				foreach (LaneSegment segment in _segments)
				{
					GenerateAutoParkSpotsForSpan(new LaneSpan(segment.transform, Vector3.zero, segment.length), componentsInChildren);
				}
				return;
			}
		}
		_laneCenterPoint = base.transform.TransformPoint(_collider.center);
		float z = _collider.size.z;
		_start = _laneCenterPoint - base.transform.forward * z / 2f;
		_end = _laneCenterPoint + base.transform.forward * z / 2f;
		GenerateAutoParkSpotsForSpan(new LaneSpan(base.transform, _collider.center, z), componentsInChildren);
	}

	private void GenerateAutoParkSpotsForSpan(LaneSpan span, MeshCollider[] parkedVehicles)
	{
		_obstacles.Clear();
		AddSceneryVehicleObstacles(span, parkedVehicles);
		AddPlayerVehicleObstacles(span);
		_obstacles.Sort(CompareObstacleStart);
		float num = 0f;
		foreach (LaneObstacle obstacle in _obstacles)
		{
			CreateSpotInGap(span, num, obstacle.start);
			num = Mathf.Max(num, obstacle.end);
		}
		CreateSpotInGap(span, num, span.length);
	}

	private void AddSceneryVehicleObstacles(LaneSpan span, MeshCollider[] parkedVehicles)
	{
		foreach (MeshCollider obj in parkedVehicles)
		{
			Transform transform = obj.transform;
			float num = obj.sharedMesh.bounds.size.z / 2f;
			if (IsInsideLane(span, transform.position, num))
			{
				AddObstacle(span, transform.position, transform.forward, num);
			}
		}
	}

	private void AddPlayerVehicleObstacles(LaneSpan span)
	{
		foreach (VehicleController allPlayerVehicle in VehicleHelper.AllPlayerVehicles)
		{
			if ((bool)allPlayerVehicle && !allPlayerVehicle.controlledByPlayer && VehicleHelper.TryGetBodyColliderBounds(allPlayerVehicle.transform, out var localBounds, out var bodyCollider))
			{
				float num = localBounds.size.z / 2f;
				Vector3 center = bodyCollider.bounds.center;
				if (IsInsideLane(span, center, num))
				{
					Vector3 vehicleForward = (allPlayerVehicle.TryGetComponent<Rigidbody>(out var component) ? (component.rotation * Vector3.forward) : allPlayerVehicle.transform.forward);
					AddObstacle(span, center, vehicleForward, num);
				}
			}
		}
	}

	private void AddObstacle(LaneSpan span, Vector3 vehiclePosition, Vector3 vehicleForward, float halfLength)
	{
		Vector3 forward = span.transform.forward;
		float a = Vector3.Dot(vehiclePosition + vehicleForward * halfLength - span.start, forward);
		float b = Vector3.Dot(vehiclePosition - vehicleForward * halfLength - span.start, forward);
		float obstaclePadding = _autoParkSettings.ObstaclePadding;
		LaneObstacle item = new LaneObstacle
		{
			start = Mathf.Clamp(Mathf.Min(a, b) - obstaclePadding, 0f, span.length),
			end = Mathf.Clamp(Mathf.Max(a, b) + obstaclePadding, 0f, span.length)
		};
		_obstacles.Add(item);
	}

	private static int CompareObstacleStart(LaneObstacle left, LaneObstacle right)
	{
		return left.start.CompareTo(right.start);
	}

	private bool IsInsideLane(LaneSpan span, Vector3 position, float endMargin)
	{
		return new Bounds(span.centerLocal, new Vector3(_collider.size.x, 6f, span.length + endMargin * 2f)).Contains(span.transform.InverseTransformPoint(position));
	}

	private void CreateSpotInGap(LaneSpan span, float gapStart, float gapEnd)
	{
		if (!(gapEnd <= gapStart))
		{
			Vector3 vector = span.start + span.transform.forward * gapStart;
			Vector3 vector2 = span.start + span.transform.forward * gapEnd;
			SetupAutoParkSpot(span, vector, vector2);
			_startPoints.Add(vector);
			_endPoints.Add(vector2);
		}
	}

	public void ProcessParkingLane()
	{
		if (!IsEditor && _collider == null)
		{
			return;
		}
		if ((bool)building)
		{
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(building.Address);
			if (buildingRegistration.RentedByPlayer && removeVehiclesIfRentedByPlayer)
			{
				return;
			}
			if (BusinessTypeHelper.GetData(buildingRegistration) == null)
			{
				ProcessNonBusinessParkingLane();
				return;
			}
			bool flag = BusinessHelper.IsBusinessOpen(BuildingHelper.GetBuildingRegistration(building.Address));
			if (_lastOpeningState == flag)
			{
				return;
			}
			_lastOpeningState = flag;
			CleanupParkedVehicles();
			GenerateParkedVehicles(GetFreeSpotChanceForBusiness(flag));
		}
		else
		{
			ProcessNonBusinessParkingLane();
		}
		Physics.SyncTransforms();
		VehicleHelper.DestroyBlockingParkedVehicles(skipPlayerMountedVehicles: true);
		GenerateAutoParkSpots();
	}

	private void ProcessNonBusinessParkingLane()
	{
		CleanupParkedVehicles();
		GenerateParkedVehicles(chanceOfFreeSpot);
	}

	private void SetupAutoParkSpot(LaneSpan span, Vector3 startPosition, Vector3 endPosition)
	{
		float minSpotLength = _autoParkSettings.MinSpotLength;
		float num = Vector3.SqrMagnitude(startPosition - endPosition);
		if (!(num < minSpotLength * minSpotLength))
		{
			num = Mathf.Sqrt(num);
			AutoParkSpot component = UnityEngine.Object.Instantiate(InstanceBehavior<GlobalReferences>.Instance.autoParkSpot, span.transform).GetComponent<AutoParkSpot>();
			component.transform.position = startPosition + span.transform.forward * (num / 2f) + span.transform.up * 0.1f;
			Vector3 localPosition = component.transform.localPosition;
			localPosition.x = span.centerLocal.x;
			component.transform.localPosition = localPosition;
			component.visuals.size = new Vector2(num, component.visuals.size.y);
			component.boxCollider.size = new Vector3(num, component.visuals.size.y, 0.1f);
			component.maxVehicleLength = num;
			autoParkSpots.Add(component);
		}
	}

	[Button("Cleanup Parking Lane", EButtonEnableMode.Always)]
	public void CleanupParkedVehicles(bool force = false)
	{
		if (!force && !IsEditor && !BuildingManager.IsInsideBuilding && MathHelper.DistanceSqr(GetClosestLanePoint(InstanceBehavior<GameManager>.Instance.playerController.transform.position), InstanceBehavior<GameManager>.Instance.playerController.transform.position) <= 6400f)
		{
			return;
		}
		Transform[] children = base.transform.GetChildren();
		foreach (Transform transform in children)
		{
			if (transform.gameObject.layer == LayerHelper.ParkedVehiclesLayerIndex)
			{
				onReleaseVehicle?.Invoke(transform.gameObject);
				ParkingSimulator.ReleaseParkedVehicle(transform.gameObject);
			}
			else if (transform.gameObject.layer == LayerHelper.AutoParkSpotsLayerIndex)
			{
				DestroyLaneChild(transform.gameObject);
			}
			else if (transform.gameObject.layer == LayerHelper.ParkingAreaLayerIndex)
			{
				CleanupSegmentAutoParkSpots(transform);
			}
		}
	}

	private void CleanupSegmentAutoParkSpots(Transform segment)
	{
		Transform[] children = segment.GetChildren();
		foreach (Transform transform in children)
		{
			if (transform.gameObject.layer == LayerHelper.AutoParkSpotsLayerIndex)
			{
				DestroyLaneChild(transform.gameObject);
			}
		}
	}

	private void DestroyLaneChild(GameObject child)
	{
		if (IsEditor)
		{
			UnityEngine.Object.DestroyImmediate(child);
		}
		else
		{
			UnityEngine.Object.Destroy(child);
		}
	}

	private void GenerateParkedVehicles(int freeSpotChance, bool force = false)
	{
		if (!spawningActive)
		{
			return;
		}
		freeSpotChance = Mathf.Clamp(freeSpotChance, 0, 100);
		bool flag = ContainsDeliveryVehicle();
		if (flag && DeliveryVanSpots.TryGetValue(building, out var value) && (bool)value && value != this)
		{
			flag = false;
		}
		if (flag)
		{
			DeliveryJobStartController byAddress = DeliveryJobStartController.GetByAddress(building.Address);
			if ((bool)byAddress && byAddress.gameObject != _specialVehicle)
			{
				flag = false;
			}
		}
		if (!IsEditor && !force && !BuildingManager.IsInsideBuilding && MathHelper.DistanceSqr(GetClosestLanePoint(InstanceBehavior<GameManager>.Instance.playerController.transform.position), InstanceBehavior<GameManager>.Instance.playerController.transform.position) <= 6400f)
		{
			return;
		}
		int num = 0;
		List<GameObject> list = new List<GameObject>();
		float num2 = 0f;
		while (num2 + parkingSpotDistance / 2f < SlotSpan)
		{
			bool flag2 = flag && num2 == 0f;
			if (flag2 && (bool)_specialVehicle)
			{
				if (!_specialVehicle.gameObject.activeSelf)
				{
					_specialVehicle.gameObject.SetActive(value: true);
				}
				num2 += parkingSpotDistance;
				continue;
			}
			if (!flag2 && RngHelper.Chance(freeSpotChance))
			{
				num++;
				num2 += parkingSpotDistance;
				continue;
			}
			int num3 = 0;
			while (num3 < 20)
			{
				num3++;
				GameObject gameObject;
				if (flag2)
				{
					gameObject = (_specialVehicle = PrefabHelper.CreatePrefab("Vehicles/ParkedVehicles/DeliveryJobVan"));
					gameObject.GetComponent<DeliveryJobStartController>().building = building;
					gameObject.SetActive(value: true);
				}
				else
				{
					string[] array = specificVehicles;
					gameObject = ParkingSimulator.RequestParkedVehicle(((array != null && array.Length > 0) ? specificVehicles.GetRandom() : ("ba:vehicletype_" + InstanceBehavior<GlobalReferences>.Instance.vehiclePool.GetWeightedRandomVehicle((CarType carType) => carType.canBeRandomlyParked).vehiclePrefab.name)).ToLowerInvariant());
				}
				gameObject.transform.parent = base.transform;
				MeshCollider component = gameObject.GetComponent<MeshCollider>();
				float num4 = (sideBySideParkingSpots ? parkingSpotDistance : component.sharedMesh.bounds.size.z);
				float num5 = (sideBySideParkingSpots ? 0f : UnityEngine.Random.Range(parkingSpotSizeRandomRange.x, parkingSpotSizeRandomRange.y));
				if (IsCurved)
				{
					if (!TryPlaceCurvedVehicle(gameObject, component, flag2, SlotStartOffset + num2 + num5, num4))
					{
						ParkingSimulator.ReleaseParkedVehicle(gameObject.gameObject);
						if (num3 >= 20)
						{
							num2 += parkingSpotDistance;
						}
						continue;
					}
				}
				else
				{
					Vector3 vector = new Vector3(UnityEngine.Random.Range(0f - lateralRandomOffset, lateralRandomOffset), 0f, SlotStartOffset - _collider.size.z / 2f + num4 / 2f + num2 + num5);
					if (sideBySideParkingSpots)
					{
						float z = component.sharedMesh.bounds.extents.z;
						float x = component.sharedMesh.bounds.extents.x;
						float f = sideBySideAngle * (MathF.PI / 180f);
						float num6 = Mathf.Abs(Mathf.Cos(f));
						float num7 = Mathf.Abs(Mathf.Sin(f));
						float num8 = z * num6 + x * num7;
						if (!flag2 && num8 > _collider.size.z / 2f)
						{
							ParkingSimulator.ReleaseParkedVehicle(gameObject.gameObject);
							if (num3 >= 20)
							{
								num2 += parkingSpotDistance;
							}
							continue;
						}
						float sideBySideDepthOffset = GetSideBySideDepthOffset(num8);
						float num9 = _collider.size.x / 2f - SlotStartOffset - num4 / 2f - num2;
						float x2 = ((num6 > 0.001f) ? Mathf.Clamp((num9 + num7 * sideBySideDepthOffset) / num6, (0f - _collider.size.x) / 2f, _collider.size.x / 2f) : num9);
						vector = new Vector3(x2, 0f, sideBySideDepthOffset);
					}
					gameObject.transform.localPosition = vector + _collider.center;
					if (!flag2 && !sideBySideParkingSpots && gameObject.transform.localPosition.z + component.sharedMesh.bounds.extents.z > _collider.center.z + _collider.size.z / 2f)
					{
						ParkingSimulator.ReleaseParkedVehicle(gameObject.gameObject);
						if (num3 >= 20)
						{
							num2 += parkingSpotDistance;
						}
						continue;
					}
					Vector3 euler = new Vector3(0f, UnityEngine.Random.Range(0f - extraAngleRotation, extraAngleRotation), 0f);
					if (sideBySideParkingSpots)
					{
						euler.y += sideBySideAngle;
						if (RngHelper.Chance(inverseParkingChance))
						{
							euler.y += 180f;
						}
					}
					gameObject.transform.localRotation = Quaternion.Euler(euler);
					if (sideBySideParkingSpots)
					{
						Vector3 vector2 = gameObject.transform.localRotation * component.sharedMesh.bounds.center;
						gameObject.transform.localPosition -= new Vector3(vector2.x, 0f, vector2.z);
					}
				}
				if (!sideBySideParkingSpots)
				{
					num2 += num5;
				}
				num2 += num4;
				if (flag2)
				{
					gameObject.transform.SetParent(null);
					gameObject.GetComponent<DeliveryJobStartController>().DeactivateIfNeeded();
				}
				else
				{
					list.Add(gameObject);
				}
				onGenerateVehicle?.Invoke(gameObject);
				break;
			}
		}
		EnsureMinFreeSpots(num, list);
		GenerateAutoParkSpots();
	}

	private void EnsureMinFreeSpots(int freeSpotCount, List<GameObject> spawnedVehicles)
	{
		int num = Mathf.Min(minFreeSpots - freeSpotCount, spawnedVehicles.Count);
		for (int i = 0; i < num; i++)
		{
			int index = UnityEngine.Random.Range(0, spawnedVehicles.Count);
			GameObject gameObject = spawnedVehicles[index];
			spawnedVehicles.RemoveAt(index);
			onReleaseVehicle?.Invoke(gameObject);
			ParkingSimulator.ReleaseParkedVehicle(gameObject);
		}
	}

	private bool TryPlaceCurvedVehicle(GameObject vehicle, MeshCollider vehicleCollider, bool isDeliveryVehicle, float spotStartDistance, float spotSize)
	{
		float num = spotStartDistance + spotSize * 0.5f;
		if (!TryResolveSegment(num, out var segment, out var localDistance))
		{
			return false;
		}
		float num2 = localDistance - segment.length * 0.5f;
		if (sideBySideParkingSpots)
		{
			return TryPlaceCurvedSideBySideVehicle(vehicle, vehicleCollider, isDeliveryVehicle, segment, num2);
		}
		if (!isDeliveryVehicle && num + vehicleCollider.sharedMesh.bounds.extents.z > _totalCurveLength)
		{
			return false;
		}
		Vector3 position = new Vector3(UnityEngine.Random.Range(0f - lateralRandomOffset, lateralRandomOffset), 0f, num2);
		Quaternion quaternion = Quaternion.Euler(0f, UnityEngine.Random.Range(0f - extraAngleRotation, extraAngleRotation), 0f);
		vehicle.transform.SetPositionAndRotation(segment.transform.TransformPoint(position), segment.transform.rotation * quaternion);
		return true;
	}

	private bool TryPlaceCurvedSideBySideVehicle(GameObject vehicle, MeshCollider vehicleCollider, bool isDeliveryVehicle, LaneSegment segment, float alongLocal)
	{
		Bounds bounds = vehicleCollider.sharedMesh.bounds;
		float f = sideBySideAngle * (MathF.PI / 180f);
		float num = Mathf.Abs(Mathf.Cos(f));
		float num2 = Mathf.Abs(Mathf.Sin(f));
		float num3 = bounds.extents.z * num + bounds.extents.x * num2;
		if (!isDeliveryVehicle && num3 > _collider.size.z * 0.5f)
		{
			return false;
		}
		float sideBySideDepthOffset = GetSideBySideDepthOffset(num3);
		float num4 = ((num > 0.001f) ? Mathf.Clamp((alongLocal + num2 * sideBySideDepthOffset) / num, (0f - segment.length) * 0.5f, segment.length * 0.5f) : alongLocal);
		float num5 = UnityEngine.Random.Range(0f - extraAngleRotation, extraAngleRotation) + sideBySideAngle;
		if (RngHelper.Chance(inverseParkingChance))
		{
			num5 += 180f;
		}
		Quaternion quaternion = Quaternion.Euler(0f, num5, 0f);
		Vector3 vector = quaternion * bounds.center;
		Vector3 position = new Vector3(num4 - vector.x, 0f, sideBySideDepthOffset - vector.z);
		vehicle.transform.SetPositionAndRotation(segment.transform.TransformPoint(position), segment.transform.rotation * quaternion);
		return true;
	}

	[ConsoleMethod("ToggleParkedVehicles", "Toggle parked vehicles spawning", new string[] { })]
	public static void Command_ToggleSpawning()
	{
		spawningActive = !spawningActive;
		ParkingLaneGenerator[] array = UnityEngine.Object.FindObjectsByType<ParkingLaneGenerator>(FindObjectsSortMode.None);
		if (!spawningActive)
		{
			ParkingLaneGenerator[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].CleanupParkedVehicles(force: true);
			}
		}
		else
		{
			ParkingLaneGenerator[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				array2[i].Init();
			}
		}
		Debug.Log("Parked Vehicles Spawning is now " + (spawningActive ? "active" : "inactive"));
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		DeliveryVanSpots.Clear();
		spawningActive = true;
		pendingDestroyBlockingParkedVehicles = false;
	}
}
