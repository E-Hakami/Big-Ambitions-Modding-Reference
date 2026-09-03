using System;
using System.Collections;
using Extensions;
using GleyTrafficSystem;
using Helpers;
using NWH.Common.CoM;
using UI;
using UI.Notification;
using UnityEngine;

public class VehicleParkingHelper : MonoBehaviour
{
	private const float SpotCheckHeight = 2f;

	private const int MaxOverlapResults = 16;

	private const float MinDistanceToOfferSpot = 1.5f;

	private const float MinDistanceToOfferSpotSqr = 2.25f;

	private const float SpotSelectionInterval = 0.1f;

	private static readonly Collider[] OverlapColliders = new Collider[16];

	private static readonly Collider[] SpotScanColliders = new Collider[16];

	public AutoParkSpot availableAutoParkSpot;

	public Collider parallelParkingCollider;

	private CarController _carController;

	private VariableCenterOfMass _vcom;

	private SphereCollider _detectionSphere;

	private Vector3 _lastAutoParkPosition = Vector3.positiveInfinity;

	private float _nextSpotSelectionTime;

	private void Awake()
	{
		parallelParkingCollider.enabled = false;
		_detectionSphere = GetComponent<SphereCollider>();
	}

	private void Start()
	{
		_carController = GetComponentInParent<CarController>();
		_vcom = _carController.GetComponent<VariableCenterOfMass>();
	}

	private void SelectNearestSpot()
	{
		if (Time.time < _nextSpotSelectionTime)
		{
			return;
		}
		_nextSpotSelectionTime = Time.time + 0.1f;
		AutoParkSpot autoParkSpot = FindNearestFreeSpot();
		if (!(autoParkSpot == availableAutoParkSpot))
		{
			if (availableAutoParkSpot != null)
			{
				availableAutoParkSpot.visuals.enabled = false;
			}
			availableAutoParkSpot = autoParkSpot;
		}
	}

	private AutoParkSpot FindNearestFreeSpot()
	{
		Vector3 position = _carController.transform.position;
		if (MathHelper.DistanceSqr(position, _lastAutoParkPosition) < 2.25f)
		{
			return null;
		}
		_lastAutoParkPosition = Vector3.positiveInfinity;
		float num = InstanceBehavior<GlobalReferences>.Instance.autoParkSettings.VehiclePadding * 2f;
		float num2 = _vcom.dimensions.z + num;
		int num3 = Physics.OverlapSphereNonAlloc(base.transform.TransformPoint(_detectionSphere.center), _detectionSphere.radius, SpotScanColliders, 1 << LayerHelper.AutoParkSpotsLayerIndex, QueryTriggerInteraction.Collide);
		AutoParkSpot result = null;
		float num4 = float.MaxValue;
		for (int i = 0; i < num3; i++)
		{
			if (SpotScanColliders[i].TryGetComponent<AutoParkSpot>(out var component) && !(component.maxVehicleLength < num2))
			{
				float num5 = MathHelper.DistanceSqr(position, component.transform.position);
				if (!(num5 < 2.25f) && !(num5 >= num4) && !IsSpotOccupied(component))
				{
					result = component;
					num4 = num5;
				}
			}
		}
		return result;
	}

	private void OnTriggerExit(Collider other)
	{
		AutoParkSpot component = other.GetComponent<AutoParkSpot>();
		if ((bool)component)
		{
			component.visuals.enabled = false;
			if (availableAutoParkSpot != null && availableAutoParkSpot.gameObject == other.gameObject)
			{
				availableAutoParkSpot = null;
			}
		}
	}

	private bool IsSpotOccupied(AutoParkSpot spot)
	{
		return IsSpotOccupied(spot.transform.position, spot.transform.parent.rotation, spot.visuals.size.y);
	}

	private bool IsSpotOccupied(Vector3 spotPosition, Quaternion spotRotation, float spotWidth)
	{
		Vector3 halfExtents = new Vector3(spotWidth, 2f, _vcom.dimensions.z) * 0.5f;
		int num = Physics.OverlapBoxNonAlloc(spotPosition, halfExtents, OverlapColliders, spotRotation, LayerHelper.freeParkingSpotDetectionMask, QueryTriggerInteraction.Ignore);
		if (num == 16)
		{
			return true;
		}
		Transform parent = _carController.transform;
		for (int i = 0; i < num; i++)
		{
			if (!OverlapColliders[i].transform.IsChildOf(parent))
			{
				return true;
			}
		}
		return false;
	}

	public IEnumerator ParkInClosestSpot()
	{
		if (availableAutoParkSpot == null)
		{
			yield break;
		}
		if (Math.Abs(_carController.GetCurrentCondition() - 1f) < Mathf.Epsilon)
		{
			Notifications.Show(NotificationType.Error, "vehicleautoparksystem_vehicle_to_broken", null, 4f, "auto-park");
			yield break;
		}
		AutoParkSpot autoParkSpot = availableAutoParkSpot;
		availableAutoParkSpot = null;
		if (IsSpotOccupied(autoParkSpot))
		{
			yield break;
		}
		Vector3 spotPosition = autoParkSpot.transform.position;
		Quaternion spotRotation = autoParkSpot.transform.parent.rotation;
		float spotWidth = autoParkSpot.visuals.size.y;
		autoParkSpot.visuals.enabled = false;
		yield return UiFader.Fade();
		if (IsSpotOccupied(spotPosition, spotRotation, spotWidth))
		{
			yield return UiFader.UnFade();
			yield break;
		}
		_lastAutoParkPosition = spotPosition;
		VehicleHelper.TeleportVehicleToGround(_carController, spotPosition, spotRotation);
		yield return UiFader.UnFade();
		_carController.vehicleInstance.parkingState = ParkingState.Legal;
		if (_carController.poi != null)
		{
			_carController.poi.SetBackground(InstanceBehavior<GlobalReferences>.Instance.vehiclePOIBackgroundColor);
		}
		_carController.UpdateParkingZone();
	}

	private void Update()
	{
		if (_carController.controlledByPlayer)
		{
			bool flag = _carController.CurrentSpeed < 10f;
			if (flag && _carController.vehicleType.autoParkSupported)
			{
				SelectNearestSpot();
			}
			bool flag2 = availableAutoParkSpot != null;
			if (InstanceBehavior<UIs>.IsInitialized && _carController.vehicleType.autoParkSupported)
			{
				InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.autoParkButton.interactable = flag2 & flag;
			}
			if (_carController.vehicleType.autoParkSupported & flag2)
			{
				availableAutoParkSpot.visuals.enabled = _carController.vehicleType.autoParkSupported & flag2 & flag;
			}
			bool num = parallelParkingCollider.enabled && !flag2;
			parallelParkingCollider.enabled = flag2;
			if (num)
			{
				Manager.TriggerColliderRemovedEvent(parallelParkingCollider);
			}
		}
	}

	public void OnEnterVehicle()
	{
		base.enabled = true;
		parallelParkingCollider.enabled = true;
	}

	public void OnExitVehicle()
	{
		base.enabled = false;
		availableAutoParkSpot = null;
		parallelParkingCollider.enabled = false;
		Manager.TriggerColliderRemovedEvent(parallelParkingCollider);
	}

	public static bool TryGetRandomParkingGarageSpot(string parkingGarageRootPath, out Vector3 spotPosition, out Quaternion spotRotation)
	{
		spotPosition = default(Vector3);
		spotRotation = default(Quaternion);
		if (string.IsNullOrEmpty(parkingGarageRootPath))
		{
			return false;
		}
		GameObject gameObject = GameObject.Find(parkingGarageRootPath);
		if (gameObject == null)
		{
			return false;
		}
		ParkingLaneGenerator[] componentsInChildren = gameObject.GetComponentsInChildren<ParkingLaneGenerator>(includeInactive: true);
		if (componentsInChildren == null || componentsInChildren.Length == 0)
		{
			return false;
		}
		int num = UnityEngine.Random.Range(0, componentsInChildren.Length);
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			int num2 = (num + i) % componentsInChildren.Length;
			ParkingLaneGenerator parkingLaneGenerator = componentsInChildren[num2];
			if (!(parkingLaneGenerator == null) && parkingLaneGenerator.TryGetRandomFreeSpotForPlayerVehicle(out spotPosition, out spotRotation))
			{
				return true;
			}
		}
		return false;
	}
}
