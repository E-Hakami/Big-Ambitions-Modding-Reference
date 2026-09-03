using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using GleyTrafficSystem;
using Localizor;
using Player.HUD.ItemInfoOverlays;
using Player.HUD.SmartphoneUI;
using UI;
using UI.Guiders;
using UnityEngine;
using Vehicles.Taxis;
using Vehicles.VehicleTypes;

namespace Helpers;

public class PrivateDriverVehicle : EntityController, ITaxi
{
	private const float PlayerStopDistanceSqr = 100f;

	private const float PlayerFarStopDistanceSqr = 6400f;

	private const float WaypointStopDistanceSqr = 25f;

	private const float NavmeshTargetMargin = 1f;

	private const string NavmeshTargetTag = "NavMeshTarget";

	private const int LimoEnergyGain = 5;

	private const int LimoFoodGain = 10;

	[NonSerialized]
	public VehicleInstance vehicleInstance;

	private VehicleComponent _vehicle;

	private Waypoint _targetWaypoint;

	private MeshCollider _collider;

	private Rigidbody _rigidbody;

	private AiCarHorn _horn;

	private float _lastActionValue;

	private SpecialDriveActionTypes _lastDriveAction;

	private Timestamp _despawnTimestamp;

	public VehicleType VehicleType => vehicleInstance.VehicleType;

	protected override int DefaultLayer => LayerHelper.AiVehiclesLayerIndex;

	public override void Awake()
	{
		_vehicle = GetComponent<VehicleComponent>();
		_rigidbody = GetComponent<Rigidbody>();
		_horn = GetComponentInChildren<AiCarHorn>();
		if ((bool)_horn)
		{
			_horn.enabled = false;
		}
		simpleOverlayType = SimpleOverlayType.ClickToAction;
		detailedOverlayType = (DetailedOverlayType)0;
		customOverlayHeaderKey = "ba:private_driver";
		renderers = ((IEnumerable<MeshRenderer>)GetComponentsInChildren<MeshRenderer>()).Select((Func<MeshRenderer, Renderer>)((MeshRenderer x) => x)).ToArray();
		MeshCollider componentInChildren = GetComponentInChildren<MeshCollider>();
		if ((bool)componentInChildren && (bool)componentInChildren.sharedMesh)
		{
			navMeshTargets = new Transform[2];
			navMeshTargets[0] = CreateNavmeshTarget(componentInChildren, 1f);
			navMeshTargets[1] = CreateNavmeshTarget(componentInChildren, -1f);
			if ((bool)componentInChildren)
			{
				GameObject gameObject = new GameObject("MouseCollider");
				gameObject.layer = LayerHelper.InteractiveItemsLayerIndex;
				gameObject.transform.SetParent(base.transform);
				gameObject.transform.localPosition = Vector3.zero;
				gameObject.transform.localRotation = Quaternion.identity;
				gameObject.transform.localScale = Vector3.one;
				_collider = gameObject.AddComponent<MeshCollider>();
				_collider.sharedMesh = componentInChildren.sharedMesh;
				_collider.convex = componentInChildren.convex;
				_collider.isTrigger = false;
			}
		}
		_despawnTimestamp = TimeHelper.Now().AddHours(1);
		GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(CheckForDespawn));
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Combine(GlobalEvents.onTimeMachineEnded, new Action(CheckForDespawn));
		base.Awake();
	}

	private void OnDisable()
	{
		if ((bool)this)
		{
			UnityEngine.Object.Destroy(this);
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		if ((bool)InstanceBehavior<UIs>.Instance)
		{
			InstanceBehavior<UIs>.Instance.smartphoneUI.RebuildPrivateDriverUI();
			InstanceBehavior<UIs>.Instance.smartphoneUI.privateDriverUI.OnVehicleDestroyed(this);
		}
		GlobalEvents.onNewHour = (Action)Delegate.Remove(GlobalEvents.onNewHour, new Action(CheckForDespawn));
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Remove(GlobalEvents.onTimeMachineEnded, new Action(CheckForDespawn));
		if (!base.gameObject)
		{
			return;
		}
		base.gameObject.layer = DefaultLayer;
		if ((bool)InstanceBehavior<OverlayManager>.Instance && InstanceBehavior<OverlayManager>.Instance.IsShowingOverlayOverItem(this))
		{
			InstanceBehavior<OverlayManager>.Instance.HideSimpleOverlayAndClearCta();
		}
		if (!disableHighlightInteraction)
		{
			Renderer[] array = renderers;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].gameObject.layer = DefaultLayer;
			}
		}
		Transform[] array2 = navMeshTargets;
		foreach (Transform transform in array2)
		{
			if ((bool)transform)
			{
				UnityEngine.Object.Destroy(transform.gameObject);
			}
		}
		if ((bool)_collider)
		{
			UnityEngine.Object.Destroy(_collider.gameObject);
		}
		if ((bool)_horn)
		{
			_horn.enabled = true;
		}
	}

	private void Update()
	{
		if (SmartphonePrivateDriverUI.CurrentVehicle != this)
		{
			return;
		}
		GuidersManager.SetGuiderTarget(_rigidbody.worldCenterOfMass, customOverlayHeaderKey.GetLocalization(), InstanceBehavior<GlobalReferences>.Instance.vehiclePOIIcon, GuidersManager.GetGuiderColor(DirectionGuiderType.PrivateDriver), DirectionGuiderType.PrivateDriver);
		if (!_vehicle)
		{
			return;
		}
		if (_targetWaypoint == null)
		{
			TrafficManager.Instance.SetVehicleAction(_vehicle, SpecialDriveActionTypes.StopInPoint, clearOthers: true);
			return;
		}
		float sqrMagnitude = (base.transform.position - _targetWaypoint.position).sqrMagnitude;
		if (sqrMagnitude < 25f)
		{
			RequestVehicleStop(hail: true);
			return;
		}
		Vector3 vector = PlayerHelper.GetPosition() - base.transform.position;
		sqrMagnitude = vector.sqrMagnitude;
		if (sqrMagnitude < 100f)
		{
			RequestVehicleStop(hail: true, hardStop: true);
		}
		else if (sqrMagnitude < 6400f && Vector3.Dot(vector, base.transform.forward) < 0f && !Physics.Raycast(base.transform.position + Vector3.up, vector, vector.magnitude, LayerHelper.buildingsLayerMask, QueryTriggerInteraction.Ignore))
		{
			RequestVehicleStop(hail: true);
		}
	}

	private Transform CreateNavmeshTarget(MeshCollider referenceCollider, float xMultiplier)
	{
		Bounds bounds = referenceCollider.sharedMesh.bounds;
		Vector3 position = bounds.center + (bounds.extents.x + 1f) * xMultiplier * Vector3.right;
		Vector3 position2 = referenceCollider.transform.TransformPoint(position);
		position2.y = base.transform.position.y;
		Transform obj = new GameObject("NavMeshTarget").transform;
		obj.gameObject.tag = "NavMeshTarget";
		obj.SetParent(base.transform);
		obj.position = position2;
		return obj;
	}

	public void SetTargetWaypoint(Waypoint waypoint)
	{
		_targetWaypoint = waypoint;
	}

	public void RequestVehicleStop(bool hail, bool hardStop = false)
	{
		if (!_vehicle)
		{
			return;
		}
		if (_targetWaypoint != null)
		{
			_targetWaypoint = null;
			VehicleComponent vehicle = _vehicle;
			if (vehicle.presetPath == null)
			{
				vehicle.presetPath = new List<int>();
			}
			_vehicle.presetPath.Clear();
			(_lastActionValue, _lastDriveAction) = TrafficManager.Instance.GetCurrentDrivingState(_vehicle.GetIndex());
			if (hail && (bool)_horn)
			{
				_horn.Hail();
			}
		}
		if (hardStop)
		{
			AIEvents.TriggerChangeDrivingStateEvent(_vehicle.GetIndex(), SpecialDriveActionTypes.StopNow, 60f);
			_vehicle.rb.velocity = Vector3.zero;
		}
	}

	public void OnClickToSetDestination()
	{
		if (!VehicleHelper.IsInsideVehicle())
		{
			RequestVehicleStop(hail: false, hardStop: true);
			InstanceBehavior<GameManager>.Instance.playerController.SetGoal(this, delegate
			{
				InstanceBehavior<CityManager>.Instance.cityMap.SetTaxiMode(this);
			});
		}
	}

	private void CheckForDespawn()
	{
		if (!_despawnTimestamp.IsInTheFuture())
		{
			Vector3 vector = GameManager.GetMainCamera().WorldToScreenPoint(base.transform.position);
			if (vector.x < 0f || vector.x > (float)Screen.width || vector.y < 0f || vector.y > (float)Screen.height)
			{
				InstanceBehavior<UIs>.Instance.smartphoneUI.DismissPrivateDriver();
			}
		}
	}

	public int GetVehicleIndex()
	{
		if (!_vehicle)
		{
			return -1;
		}
		return _vehicle.GetIndex();
	}

	public void DriveAway()
	{
		if ((bool)_vehicle)
		{
			_vehicle.presetPath = null;
			TrafficManager.Instance.ForceSetVehicleActiveAction(_vehicle, _lastDriveAction);
			AIEvents.TriggerChangeDrivingStateEvent(_vehicle.GetIndex(), _lastDriveAction, _lastActionValue);
			TrafficManager.Instance.VehicleUpdateWaypoint(_vehicle);
		}
	}

	public float GetTimeMultiplier()
	{
		return 0.85f;
	}

	public string GetHappinessModifierName()
	{
		return "ba:happinessmodifier_privatedriver";
	}

	public VehicleComponent GetVehiclePrefab()
	{
		if (!_vehicle)
		{
			return PrivateDriverHelpers.GetAiVehiclePrefab(VehicleType);
		}
		return _vehicle.prefab.GetComponent<VehicleComponent>();
	}

	public VehicleComponent InstantiateVehicle(Waypoint waypoint)
	{
		InstanceBehavior<UIs>.Instance.smartphoneUI.DismissPrivateDriver(force: true, null, instantRemove: true);
		VehicleComponent vehiclePrefab = GetVehiclePrefab();
		VehicleComponent vehicleComponent = TrafficManager.Instance.LoadVehicle(vehiclePrefab.gameObject, waypoint);
		if (!vehicleComponent)
		{
			return null;
		}
		PrivateDriverHelpers.SetupVehicle(vehicleComponent.gameObject.AddComponent<PrivateDriverVehicle>(), vehicleInstance);
		return vehicleComponent;
	}

	public void OnTravelFinished()
	{
		if (!(vehicleInstance.vehicleTypeName != "ba:vehicletype_limo"))
		{
			HappinessHelper.AddModifier("ba:happinessmodifier_limo");
			EnergyHelper.GenerateEnergy(5f);
			InstanceBehavior<GameManager>.Instance.ChangeHunger(10);
		}
	}
}
