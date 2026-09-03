using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using BigAmbitions.GameAnalytics;
using BigAmbitions.Items;
using Buildings;
using Controllers;
using Extensions;
using Helpers;
using IngameDebugConsole;
using JimmysUnityUtilities;
using Localizor;
using NaughtyAttributes;
using Player.PlayerMissions;
using UI;
using UI.Notification;
using UnityEngine;
using Vehicles.VehicleTypes;

namespace Vehicles.DeliveryDriverJob;

public class DeliveryJobStartController : EntityController
{
	private static readonly List<DeliveryJobStartController> Instances = new List<DeliveryJobStartController>();

	private static bool AllowSameDayJobs;

	[NonSerialized]
	public Building building;

	private PointOfInterest _poi;

	private int _timeLimitMinutes;

	private List<DeliveryJobDestination> _generatedDestinations;

	private int _generatedDestinationsDay = -1;

	private Collider _collider;

	private bool _initialized;

	private DeliveryJobStartLocation Location => building.deliveryJobStartLocation;

	public override void Awake()
	{
		base.Awake();
		_collider = GetComponent<Collider>();
		randomAvailableNavMeshPositionGetter = new RandomAvailableNavMeshPositionGetter(navMeshTargets.Length);
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
		Instances.Add(this);
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		Instances.Remove(this);
		if ((bool)_poi)
		{
			UnityEngine.Object.Destroy(_poi.gameObject);
		}
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationChange));
	}

	public override void Start()
	{
		base.Start();
		if (!building)
		{
			Debug.LogError("DeliveryJobStartController: No building assigned to this controller.", this);
			return;
		}
		_initialized = true;
		CarFeatures component = GetComponent<CarFeatures>();
		if ((bool)component)
		{
			component.SetColor(Location.vehicleColor);
		}
		DeactivateIfNeeded();
	}

	private void OnEnable()
	{
		if (_initialized)
		{
			DeactivateIfNeeded();
		}
	}

	public void DeactivateIfNeeded()
	{
		if (!building)
		{
			return;
		}
		BuildingRegistration registration = building.GetRegistration();
		if (registration != null && registration.RentedByPlayer)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		if (SaveGameManager.Current?.currentPlayerMission != null && SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission && deliveryDriverMission.startAddress == building.Address)
		{
			base.gameObject.SetActive(value: false);
		}
		if (IsBlockedByVehicles())
		{
			Invoke("RetryActivate", 5f);
			base.gameObject.SetActive(value: false);
		}
	}

	public void OnClickToStartJob()
	{
		if (SaveGameManager.Current.currentPlayerMission != null)
		{
			Notifications.ShowError("notification_already_ongoing_mission");
			return;
		}
		if (VehicleHelper.IsInsideVehicle())
		{
			Notifications.ShowError("notification_must_exit_vehicle_before_action");
			return;
		}
		if (PlayerHelper.IsHoldingItem)
		{
			Notifications.ShowError("notification_need_empty_hands_to_interact");
			return;
		}
		HandTruck componentInChildren = InstanceBehavior<GameManager>.Instance.playerController.GetComponentInChildren<HandTruck>();
		if ((bool)componentInChildren)
		{
			componentInChildren.ExitVehicle();
		}
		InstanceBehavior<GameManager>.Instance.playerController.SetGoal(this, PromptJob);
	}

	private void PromptJob()
	{
		GameInstance current = SaveGameManager.Current;
		if (current.deliveryJobLocationsDoneToday == null)
		{
			current.deliveryJobLocationsDoneToday = new List<Address>();
		}
		if (SaveGameManager.Current.deliveryJobLocationsDoneToday.Contains(building.Address) && !AllowSameDayJobs)
		{
			Notifications.ShowError("notification_delivery_already_done_today", "notification_delivery_already_done_today");
			return;
		}
		if (_generatedDestinations == null || _generatedDestinations.Count == 0 || _generatedDestinationsDay != SaveGameManager.Current.Day)
		{
			_generatedDestinations = DeliveryJobHelper.GenerateDestinations(Location, base.transform.position);
			_generatedDestinationsDay = SaveGameManager.Current.Day;
		}
		if (_generatedDestinations == null || _generatedDestinations.Count == 0)
		{
			Notifications.ShowError("notification_no_available_destinations", "notification_no_available_destinations");
			return;
		}
		float totalLinearDistance = DeliveryJobHelper.GetTotalLinearDistance(_generatedDestinations);
		_timeLimitMinutes = Mathf.Clamp(Mathf.CeilToInt(totalLinearDistance * Location.minutesPerMeter), Location.minutesMin, Location.minutesMax);
		int timeLimitHours = _timeLimitMinutes / 60;
		int timeLimitMinutes = _timeLimitMinutes % 60;
		int count = _generatedDestinations.Count;
		float deliveryReward = Location.deliveryReward;
		InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.DeliveryDriverJob);
		HudConfirm.Show("ba:skill_deliverydriver".Localize(), "delivery_job_dialog".Localize(new
		{
			destinationsCount = count,
			timeLimitHours = timeLimitHours,
			timeLimitMinutes = timeLimitMinutes,
			deliveryReward = deliveryReward
		}), OnAcceptJob, OnDeclineJob, "dialog_accept_button", "dialog_decline_button");
	}

	private void OnAcceptJob()
	{
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.DeliveryDriverJob);
		if (PlayerHelper.IsHoldingItem)
		{
			Notifications.ShowError("notification_need_empty_hands_to_interact");
			return;
		}
		if (InstanceBehavior<UIs>.Instance.tasksUI.IsCollapsed)
		{
			InstanceBehavior<UIs>.Instance.tasksUI.SetCollapsedState(collapsed: false);
		}
		DeliveryVehicleInstance deliveryVehicleInstance = new DeliveryVehicleInstance(Location.vehicleTypeName)
		{
			id = UuidHelper.GenerateBase64Uuid(),
			vehicleColorName = Location.vehicleColor.name,
			fuel = VehicleTypeHelper.GetVehicleType(Location.vehicleTypeName).maxFuel,
			zeroSellingPrice = true,
			preventWarehouseAssign = true
		};
		base.gameObject.SetActive(value: false);
		VehicleController vehicle = VehicleHelper.CreateAndSpawnVehicle(deliveryVehicleInstance, base.transform.position, base.transform.rotation);
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			((CarController)vehicle).EnterVehicle();
		});
		Timestamp timestamp = TimeHelper.Now();
		timestamp.AddMinutes(_timeLimitMinutes);
		DeliveryDriverMission deliveryDriverMission = new DeliveryDriverMission
		{
			startTime = TimeHelper.Now(),
			endTime = timestamp,
			timeLimitMinutes = _timeLimitMinutes,
			vehicleId = deliveryVehicleInstance.id,
			startAddress = building.Address,
			destinations = _generatedDestinations,
			deliveryReward = Location.deliveryReward
		};
		SaveGameManager.Current.currentPlayerMission = deliveryDriverMission;
		InstanceBehavior<UIs>.Instance.tasksUI.deliveryJobUI.StartUpdateRoutine();
		SaveGameManager.Current.deliveryJobLocationsDoneToday.Add(building.Address);
		_generatedDestinations = null;
		GameEvent.Invoke("ba:gameevent_newjob");
		GameAnalytics.TrackDeliveryJob("started", 0);
		vehicle.vehicleInstance.TryToAddToCargo(new CargoInstance("ba:itemname_handtruck", 1, 0f));
		foreach (DeliveryJobDestination destination in deliveryDriverMission.destinations)
		{
			ItemAmountTarget[] itemAmounts = destination.itemAmounts;
			foreach (ItemAmountTarget itemAmountTarget in itemAmounts)
			{
				NestedCargoInstance item = new NestedCargoInstance(itemAmountTarget.itemName, itemAmountTarget.targetAmount, 0f, null);
				CargoInstance cargoInstance = new CargoInstance("ba:itemname_sealedcardboardbox", 1, 0f);
				cargoInstance.nestedCargoInstances.Add(item);
				if (!vehicle.vehicleInstance.TryToAddToCargo(cargoInstance))
				{
					Debug.LogError("add failed");
				}
			}
		}
	}

	private void OnDeclineJob()
	{
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.DeliveryDriverJob);
	}

	public static DeliveryJobStartController GetFirst()
	{
		if (Instances.Count != 0)
		{
			return Instances[0];
		}
		return null;
	}

	public static DeliveryJobStartController GetByAddress(Address address)
	{
		foreach (DeliveryJobStartController instance in Instances)
		{
			if ((bool)instance.building && instance.building.Address == address)
			{
				return instance;
			}
		}
		return null;
	}

	public static DeliveryJobStartController GetNearest(Vector3 position, float maxDistance)
	{
		DeliveryJobStartController result = null;
		float num = maxDistance * maxDistance;
		foreach (DeliveryJobStartController instance in Instances)
		{
			float num2 = Vector3.SqrMagnitude(position - instance.transform.position);
			if (!(num2 > num))
			{
				num = num2;
				result = instance;
			}
		}
		return result;
	}

	private void OnBuildingRegistrationChange(Address address)
	{
		if (address == building.Address)
		{
			OnBusinessChange(address);
		}
	}

	public static void OnBusinessChange(Address address)
	{
		if (SaveGameManager.Current.currentPlayerMission is DeliveryDriverMission deliveryDriverMission && deliveryDriverMission.startAddress == address)
		{
			return;
		}
		DeliveryJobStartController byAddress = GetByAddress(address);
		if (!byAddress)
		{
			return;
		}
		bool flag = byAddress.building.GetRegistration()?.RentedByPlayer ?? false;
		if (byAddress.gameObject.activeSelf == flag)
		{
			if (flag)
			{
				byAddress.gameObject.SetActive(value: false);
			}
			else
			{
				byAddress.RetryActivate();
			}
		}
	}

	public static void ActivateAll()
	{
		foreach (DeliveryJobStartController instance in Instances)
		{
			if (!instance.gameObject.activeSelf)
			{
				instance.gameObject.SetActive(value: true);
			}
		}
	}

	private bool IsBlockedByVehicles()
	{
		return Physics.CheckBox(base.transform.position, _collider.bounds.extents, base.transform.rotation, LayerHelper.vehicleSpawnPointMask);
	}

	private void RetryActivate()
	{
		if (!base.gameObject.activeSelf)
		{
			if (IsBlockedByVehicles())
			{
				Invoke("RetryActivate", 5f);
			}
			else
			{
				base.gameObject.SetActive(value: true);
			}
		}
	}

	private void UpdateMapPoi()
	{
		if (!_poi)
		{
			_poi = InstanceBehavior<CityManager>.Instance.cityMap.AddPoi(base.transform, Location.mapPoiIcon, Location.mapPoiColor);
		}
		_poi.SetHidden(!SaveGameManager.Current.SelectedCitymapFilters.Contains("ba:skill_deliverydriver"));
	}

	public static void UpdateMapPois()
	{
		foreach (DeliveryJobStartController instance in Instances)
		{
			instance.UpdateMapPoi();
		}
	}

	[ConsoleMethod("AllowSameDayDeliveryJobs", "Sets whether players can take multiple delivery jobs from the same location in a single day.", new string[] { })]
	public static void Command_AllowSameDayJobs(bool value)
	{
		AllowSameDayJobs = value;
		Debug.Log($"AllowSameDayDeliveryJobs set to {value}");
	}

	[Button(null, EButtonEnableMode.Always)]
	public void AutoSetupNav()
	{
		navMeshTargets = (from x in base.transform.GetComponentsInChildren<Transform>()
			where x.CompareTag("NavMeshTarget")
			select x).ToArray();
	}
}
