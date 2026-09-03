using System;
using System.Collections;
using System.Collections.Generic;
using BigAmbitions.Characters;
using BigAmbitions.DayNightCycle;
using BigAmbitions.InputSystem;
using BigAmbitions.Items;
using BigAmbitions.SoundSystem;
using BigAmbitions.Tags;
using Buildings;
using CameraControllers;
using Entities;
using Enums;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Damage;
using NWH.VehiclePhysics2.Effects;
using NWH.VehiclePhysics2.Modules.FlipOver;
using NWH.VehiclePhysics2.Modules.Fuel;
using NWH.VehiclePhysics2.Powertrain;
using NaughtyAttributes;
using Parking.UndergroundParking;
using UI;
using UI.ItemPanel;
using UI.Load;
using UI.Notification;
using UnityEngine;
using UnityEngine.AI;
using Vehicles.Components;

public class CarController : VehicleController
{
	private const float TimeBetweenNotDriveableSounds = 5f;

	private const float LowFuelWarningThreshold = 0.1f;

	private const float RepairWarningThreshold = 0.9f;

	public NWH.VehiclePhysics2.VehicleController vehicleController;

	public VehicleDeformationController vehicleDeformationController;

	[HideInInspector]
	public bool isCheckedForParkingZone;

	public Transform boxPlaceholder;

	public VehicleNavMeshObstacleToggler obstacleToggler;

	[SerializeField]
	private Transform vehicleRefuelingPosition;

	[SerializeField]
	private DamageHandler damageHandler;

	[SerializeField]
	private VehicleParkingHelper vehicleParkingHelper;

	[SerializeField]
	private SoundType brokenStartupSoundType;

	[SerializeField]
	private SoundType fuelEmptyStartupSoundType;

	[SerializeField]
	private VehicleBlinker blinkers;

	[SerializeField]
	private ParticleSystem smokeParticleSystem;

	[BoxGroup("Driver model")]
	[SerializeField]
	private AppearanceSetter driverAppearanceSetter;

	[BoxGroup("Driver model")]
	[SerializeField]
	private Animator driverAnimator;

	[BoxGroup("Car wash")]
	[SerializeField]
	[MinMaxSlider(1f, 10f)]
	private Vector2 washDuration = new Vector2(3f, 4f);

	[BoxGroup("Car wash")]
	[SerializeField]
	private AudioSource washAudioSource;

	[BoxGroup("Car wash")]
	[SerializeField]
	private AudioClip washSoundLoop;

	[BoxGroup("Car wash")]
	[SerializeField]
	private AudioClip washSoundEnd;

	private int _lastWarehouseSlotIndex = -1;

	private bool _reverseCamera;

	private bool _shouldUpdateCamera;

	private float _timeSinceLastNotDriveableSound;

	private bool _wasMoving;

	private AngleAssist _angleAssist;

	private bool _hasRepairTask;

	private bool _hasFuelTask;

	[NonSerialized]
	public FuelModule fuelModule;

	[NonSerialized]
	public bool isWashing;

	protected override int DefaultLayer => LayerHelper.PlayerVehiclesLayerIndex;

	public override float CurrentSpeed => Mathf.Round(vehicleController.Speed);

	public event Action<bool> onMovingChanged;

	public override void Awake()
	{
		obstacleToggler.autoToggleByVisibility = true;
		if (TryGetComponent<FuelModuleWrapper>(out var component))
		{
			fuelModule = (FuelModule)component.GetModule();
			fuelModule.onOutOfFuel.AddListener(delegate
			{
				FuelEmptyNotification();
			});
		}
		vehicleController.soundManager.mixer = InstanceBehavior<GlobalReferences>.Instance.vehicleAudioMixer;
		vehicleController.onCollision.AddListener(OnVehicleCollision);
		vehicleController.onDisable.AddListener(delegate
		{
			foreach (WheelComponent wheel in vehicleController.powertrain.wheels)
			{
				wheel.wheelUAPI.enabled = false;
			}
			SetFreeze(isFrozen: true);
		});
		vehicleController.onLODChanged.AddListener(delegate
		{
			int lodIndex = vehicleController.stateSettings.GetDefinition(typeof(EffectManager).FullName).lodIndex;
			if (vehicleController.activeLODIndex <= lodIndex)
			{
				SetLightsState();
			}
		});
		vehicleController.onEnable.AddListener(delegate
		{
			foreach (WheelComponent wheel2 in vehicleController.powertrain.wheels)
			{
				wheel2.wheelUAPI.enabled = true;
			}
			SetFreeze(isFrozen: false);
		});
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		_angleAssist = new AngleAssist(base.transform, vehicleController);
		base.Awake();
	}

	private void OnEnterBuilding(Address address)
	{
		if (controlledByPlayer)
		{
			isCheckedForParkingZone = false;
		}
		TogglePoiIfInHamptonsHouse(address, hide: true);
	}

	private void OnExitBuilding(Address address)
	{
		if (!controlledByPlayer)
		{
			TogglePoiIfInHamptonsHouse(address, hide: false);
		}
	}

	private void TogglePoiIfInHamptonsHouse(Address address, bool hide)
	{
		Building building = BuildingHelper.GetBuilding(address);
		if (!(building == null) && building.IsHamptonsHouse() && address == vehicleInstance.Address)
		{
			poi.SetHidden(hide);
		}
	}

	public override void Reset()
	{
		foreach (WheelComponent wheel in vehicleController.powertrain.wheels)
		{
			wheel.wheelUAPI.MotorTorque = 0f;
		}
		vehicleController.vehicleRigidbody.velocity = Vector3.zero;
		vehicleController.vehicleRigidbody.angularVelocity = Vector3.zero;
		vehicleController.vehicleRigidbody.drag = 0f;
	}

	public override void SetVehicleInstance(VehicleInstance vehicleInstance)
	{
		base.SetVehicleInstance(vehicleInstance);
		if (vehicleInstance != null)
		{
			damageHandler.SetDamage(vehicleInstance.damage);
		}
	}

	public override void Start()
	{
		GlobalEvents.outdoorLightsStatusChanged = (Action<bool>)Delegate.Combine(GlobalEvents.outdoorLightsStatusChanged, new Action<bool>(OnOutdoorLightsStatusChanged));
		base.Start();
		vehicleController.vehicleRigidbody.isKinematic = true;
		if (fuelModule != null)
		{
			fuelModule.capacity = vehicleType.maxFuel;
			if (vehicleInstance != null)
			{
				fuelModule.amount = vehicleInstance.fuel;
			}
			if (SaveGameManager.Current == null || SaveGameManager.Current.gameVariables.disableVehicleFuel)
			{
				fuelModule.useFuel = false;
			}
		}
		if ((bool)washAudioSource)
		{
			washAudioSource.outputAudioMixerGroup = InstanceBehavior<GlobalReferences>.Instance.playerVehicleMixerGroup;
		}
		StartCoroutine(WaitForStationary());
		IEnumerator WaitForStationary()
		{
			yield return null;
			FlipOverModule flipOverModule = vehicleController.GetComponent<FlipOverModuleWrapper>().module;
			if (flipOverModule != null)
			{
				while (flipOverModule.flippedOver)
				{
					yield return null;
				}
			}
			while (!(vehicleController.vehicleRigidbody.velocity.sqrMagnitude < 0.010000001f) || !(vehicleController.vehicleRigidbody.angularVelocity.sqrMagnitude < 0.010000001f))
			{
				yield return null;
			}
			OnVehicleStationaryAfterLoad();
		}
	}

	private void OnOutdoorLightsStatusChanged(bool _)
	{
		if (controlledByPlayer)
		{
			SetLightsState();
		}
	}

	private void SetLightsState()
	{
		vehicleController.effectsManager.lightsManager.lowBeamLights.SetState(base.ShouldLightsBeOn);
		vehicleController.effectsManager.lightsManager.tailLights.SetState(base.ShouldLightsBeOn);
	}

	protected override void Update()
	{
		base.Update();
		if (!controlledByPlayer)
		{
			return;
		}
		for (int i = 0; i < vehicleController.powertrain.wheels.Count; i++)
		{
			vehicleController.powertrain.wheels[i].wheelUAPI.Damage = 0f;
		}
		if ((bool)smokeParticleSystem)
		{
			if (damageHandler.Damage >= 1f)
			{
				if (!smokeParticleSystem.isPlaying)
				{
					smokeParticleSystem.Play();
				}
			}
			else if (smokeParticleSystem.isPlaying)
			{
				smokeParticleSystem.Stop();
			}
		}
		PlayCarNotDriveableSoundIfNeeded();
		if (PlayerPrefSettings.SteeringAssist)
		{
			_angleAssist.Run();
		}
		else
		{
			vehicleController.steering.externallyAddedAngle = 0f;
		}
		if (!InstanceBehavior<UIs>.IsInitialized || InstanceBehavior<UIs>.Instance.screenshot.enabled || GameManager.ShouldBlockKeyboardShortcuts() || CityMap.IsOpen)
		{
			return;
		}
		if (PlayerAction.ReverseCarCam.Pressed())
		{
			_shouldUpdateCamera = true;
			_reverseCamera = true;
		}
		else if (PlayerAction.ReverseCarCam.Released())
		{
			_shouldUpdateCamera = true;
			_reverseCamera = false;
		}
		vehicleController.input.Horn = PlayerAction.VehicleHorn.Pressing();
		if (PlayerAction.VehicleLeftBlinker.Pressed())
		{
			ToggleBlinker(left: true);
		}
		if (PlayerAction.VehicleRightBlinker.Pressed())
		{
			ToggleBlinker(left: false);
		}
		bool flag = CurrentSpeed > 0f;
		UpdateMovingState(flag);
		if (flag)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.vehicleInfo.SetParkingZone(ParkingState.NotAvailable);
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.parkButton.interactable = false;
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.sellButton.interactable = false;
			isCheckedForParkingZone = false;
			obstacleToggler.OnVehicleResume();
		}
		else
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.sellButton.interactable = !InstanceBehavior<UIs>.Instance.gameSpeed.Paused;
			if (!isCheckedForParkingZone)
			{
				UpdateParkingZone();
				InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.parkButton.interactable = true;
				isCheckedForParkingZone = true;
				obstacleToggler.OnVehicleStopped();
			}
		}
		if (_shouldUpdateCamera)
		{
			UpdateCamera();
			_shouldUpdateCamera = false;
		}
		vehicleController.powertrain.engine.Damage = ((damageHandler.Damage >= 1f) ? 1 : 0);
		vehicleController.powertrain.transmission.Damage = ((damageHandler.Damage >= 1f) ? 1 : 0);
		UpdateDirtiness();
		UpdateTodoTasks();
	}

	private void UpdateMovingState(bool isMoving)
	{
		if (_wasMoving != isMoving)
		{
			_wasMoving = isMoving;
			onMovingChanged?.Invoke(isMoving);
		}
	}

	private void UpdateDirtiness()
	{
		if (vehicleType.canGetDirty && !BuildingManager.IsInsideBuilding && (controlledByPlayer || RainHelper.AreRainDropsFalling))
		{
			float num = vehicleInstance.dirtiness;
			if (RainHelper.AreRainDropsFalling)
			{
				num = Mathf.Clamp01(vehicleInstance.dirtiness - Time.fixedDeltaTime / vehicleType.cleanByRainTimer);
			}
			else if (Mathf.Abs(CurrentSpeed) > 0.1f)
			{
				num = Mathf.Clamp01(vehicleInstance.dirtiness + Time.fixedDeltaTime / vehicleType.dirtinessTimer);
			}
			if (!Mathf.Approximately(num, vehicleInstance.dirtiness))
			{
				vehicleInstance.dirtiness = num;
				base.CarFeatures.SetDirtiness(vehicleInstance.dirtiness);
			}
		}
	}

	private void OnApplicationFocus(bool hasFocus)
	{
		if (!(!InstanceBehavior<CityManager>.IsInitialized | hasFocus) && controlledByPlayer && !CityMap.IsOpen && !LoadScene.isLoading)
		{
			CameraHelper.SetCamera((BuildingManager.IsInsideBuilding || UndergroundParkingManager.IsInsideParking) ? InstanceBehavior<GameManager>.Instance.indoorVehicleCamera : InstanceBehavior<GameManager>.Instance.vehicleCamera);
		}
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("WarehouseVehicleSlotZone"))
		{
			_lastWarehouseSlotIndex = other.GetComponent<WarehouseSlotController>().slotIndex;
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.CompareTag("WarehouseVehicleSlotZone"))
		{
			_lastWarehouseSlotIndex = -1;
		}
	}

	private void OnVehicleCollision(Collision collision)
	{
		if (DamageHandler.IsCollisionValid(collision))
		{
			CoroutineUtility.RunAfterOneFrame(OnVehicleCollision);
		}
	}

	private void OnVehicleCollision()
	{
		vehicleInstance.damage = damageHandler.Damage;
		if (SaveGameManager.Current != null && SaveGameManager.Current.gameVariables.disableVehicleDamage)
		{
			Repair();
		}
		if (IsCarBroken() && controlledByPlayer)
		{
			Notifications.Show(NotificationType.Error, "notification_vehicle_to_damaged_to_drive", null, 4f, "notification_vehicle_to_damaged_to_drive");
			InstanceBehavior<SfxManager>.Instance.PlayAudio(brokenStartupSoundType, base.transform.position);
		}
		GlobalEvents.onVehicleVariablesChanged?.Invoke();
	}

	private bool IsCarBroken()
	{
		return Math.Abs(GetCurrentCondition() - 1f) < Mathf.Epsilon;
	}

	private bool IsCarFueled()
	{
		return vehicleInstance.fuel > 0f;
	}

	private bool IsCarDriveable()
	{
		if (!IsCarBroken())
		{
			return IsCarFueled();
		}
		return false;
	}

	private void OnVehicleStationaryAfterLoad()
	{
		damageHandler.SetDamage(vehicleInstance.damage);
		if (!controlledByPlayer)
		{
			vehicleController.enabled = false;
		}
	}

	private void ToggleBlinker(bool left)
	{
		if (left)
		{
			blinkers.ToggleLeftBlinker();
		}
		else
		{
			blinkers.ToggleRightBlinker();
		}
	}

	public void Wash()
	{
		StartCoroutine(WashCoroutine(washDuration.RandomValue()));
	}

	private IEnumerator WashCoroutine(float duration)
	{
		isWashing = true;
		PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
		playerController.SetVehicleNavigationBlocker(VehicleNavigationBlocker.WashingVehicle);
		SetFreeze(isFrozen: true);
		washAudioSource.clip = washSoundLoop;
		washAudioSource.loop = true;
		washAudioSource.Play();
		float startTime = Time.time;
		while (Time.time - startTime < duration)
		{
			float num = (Time.time - startTime) / duration;
			SetDirtiness(1f - num);
			yield return null;
		}
		washAudioSource.clip = washSoundEnd;
		washAudioSource.loop = false;
		washAudioSource.Play();
		SetFreeze(isFrozen: false);
		playerController.UnsetVehicleNavigationBlocker(VehicleNavigationBlocker.WashingVehicle);
		isWashing = false;
	}

	public override void UpdateCamera()
	{
		if (_reverseCamera)
		{
			CameraHelper.SetCamera((BuildingManager.IsInsideBuilding || UndergroundParkingManager.IsInsideParking) ? InstanceBehavior<GameManager>.Instance.indoorVehicleCameraReverse : InstanceBehavior<GameManager>.Instance.vehicleCameraReverse);
		}
		else
		{
			CameraHelper.SetCamera((BuildingManager.IsInsideBuilding || UndergroundParkingManager.IsInsideParking) ? InstanceBehavior<GameManager>.Instance.indoorVehicleCamera : InstanceBehavior<GameManager>.Instance.vehicleCamera);
		}
	}

	public override void EnterVehicle()
	{
		base.EnterVehicle();
		InstanceBehavior<GameManager>.Instance.playerController.Character.transform.SetParent(base.transform);
		InstanceBehavior<GameManager>.Instance.playerController.Character.transform.localPosition = Vector3.zero;
		InstanceBehavior<GameManager>.Instance.playerController.Character.transform.localRotation = Quaternion.identity;
		InstanceBehavior<GameManager>.Instance.playerController.transform.Find("Rain")?.SetParent(base.transform);
		InstanceBehavior<GameManager>.Instance.playerController.Character.ToggleVisibility(show: false);
		if ((bool)driverAppearanceSetter)
		{
			driverAppearanceSetter.gameObject.SetActive(value: true);
			driverAppearanceSetter.SetRandomAppearance(PlayerHelper.CharacterData.gender);
			driverAppearanceSetter.UpdateVisuals(PlayerHelper.CharacterData.elements);
			driverAnimator?.SetBool(PermanentAnimationType.SitDeliveryTruck);
		}
		InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.Vehicle);
		vehicleController.effectsManager.lightsManager.lowBeamLights.SetState(base.ShouldLightsBeOn);
		vehicleController.effectsManager.lightsManager.tailLights.SetState(base.ShouldLightsBeOn);
		StartEngine();
		UpdateParkingZone();
		FuelModule fuelModule = this.fuelModule;
		if (fuelModule != null && fuelModule.amount == 0f)
		{
			FuelEmptyNotification(!IsCarBroken());
		}
		if (IsCarBroken())
		{
			Notifications.Show(NotificationType.Error, "notification_vehicle_to_damaged_to_drive", null, 4f, "notification_vehicle_to_damaged_to_drive");
			InstanceBehavior<SfxManager>.Instance.PlayAudio(brokenStartupSoundType, base.transform.position);
		}
		_timeSinceLastNotDriveableSound = 0f;
		vehicleParkingHelper.OnEnterVehicle();
		blinkers.onEnterCar.Invoke();
		UpdateTodoTasks(force: true);
	}

	public override void ExitVehicle()
	{
		base.ExitVehicle();
		float num = 0f;
		num = ((!_reverseCamera) ? ((BuildingManager.IsInsideBuilding || UndergroundParkingManager.IsInsideParking) ? InstanceBehavior<GameManager>.Instance.indoorVehicleCamera : InstanceBehavior<GameManager>.Instance.vehicleCamera).transform.eulerAngles.y : ((BuildingManager.IsInsideBuilding || UndergroundParkingManager.IsInsideParking) ? InstanceBehavior<GameManager>.Instance.indoorVehicleCameraReverse : InstanceBehavior<GameManager>.Instance.vehicleCameraReverse).transform.eulerAngles.y);
		InstanceBehavior<GameManager>.Instance.indoorCamera.GetComponent<PedestrianCam>().angle = num;
		InstanceBehavior<GameManager>.Instance.pedestrianCamera.GetComponent<PedestrianCam>().angle = num;
		_reverseCamera = false;
		SaveGameManager.Current.ActiveVehicleId = null;
		SavePosition();
		UpdateNavMeshTargets();
		driverAppearanceSetter?.gameObject.SetActive(value: false);
		InstanceBehavior<GameManager>.Instance.playerController.Character.ToggleVisibility(show: true);
		base.transform.Find("Rain")?.SetParent(InstanceBehavior<GameManager>.Instance.playerController.transform);
		InstanceBehavior<GameManager>.Instance.playerController.transform.SetParent(InstanceBehavior<GameManager>.Instance.transform);
		InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.Vehicle);
		vehicleController.effectsManager.lightsManager.TurnOffAllLights();
		StopEngine();
		if (!ItemPanelUI.IsSelling)
		{
			CheckIfWarehouseSlotShouldBeSet();
		}
		vehicleParkingHelper.OnExitVehicle();
		blinkers.onExitCar.Invoke();
		DestroyTodoTasks();
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			Vector3 navMeshTargetPosition = GetNavMeshTargetPosition();
			if (!InstanceBehavior<GameManager>.Instance.playerController.Character.navmeshAgent.Warp(navMeshTargetPosition))
			{
				EnterVehicle();
				Notifications.Show(NotificationType.Warning, "carcontroller_no_exitpositon_found");
			}
		});
	}

	private void StartEngine()
	{
		vehicleController.vehicleRigidbody.isKinematic = false;
		vehicleController.enabled = true;
		vehicleController.powertrain.engine.StartEngine();
	}

	private void StopEngine()
	{
		vehicleController.powertrain.engine.StopEngine();
		vehicleController.powertrain.engine.starterActive = false;
		vehicleController.enabled = false;
		vehicleController.vehicleRigidbody.isKinematic = true;
	}

	public void CheckIfWarehouseSlotShouldBeSet()
	{
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && InstanceBehavior<BuildingManager>.Instance.building.BuildingType == "ba:buildingtype_warehouse" && !vehicleInstance.preventWarehouseAssign && _lastWarehouseSlotIndex != -1)
		{
			vehicleInstance.UnAssignFromWarehouse(showNotification: false);
			Warehouse obj = (Warehouse)InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
			obj.ClearSlotAssignments(vehicleInstance.id);
			EmployeeInstance employeeInstance = obj.vehicleSlots[_lastWarehouseSlotIndex - 1].AssignVehicle(vehicleInstance);
			Dictionary<string, string> notificationData = new Dictionary<string, string>
			{
				{ "type", vehicleInstance.vehicleTypeName },
				{
					"index",
					_lastWarehouseSlotIndex.ToString()
				}
			};
			Notifications.Show(NotificationType.Success, "carcontroller_notification_warehouse_vehicle_assigned_slot", notificationData);
			if (employeeInstance != null)
			{
				notificationData = new Dictionary<string, string>
				{
					{
						"employeeName",
						employeeInstance.characterData.name
					},
					{
						"slotNumber",
						_lastWarehouseSlotIndex.ToString()
					}
				};
				Notifications.Show(NotificationType.Success, "carcontroller_notification_warehouse_employee_unassigned_slot", notificationData);
			}
			GameEvent.Invoke("ba:gameevent_enteredbuilding");
		}
	}

	public void UpdateParkingZone()
	{
		bool flag = true;
		string neighbourhood = string.Empty;
		if (!BuildingManager.IsInsideBuilding)
		{
			foreach (WheelComponent wheel in vehicleController.powertrain.wheels)
			{
				Transform transform = wheel.wheelUAPI.transform;
				Collider[] array = Physics.OverlapBox(transform.position + transform.up * -0.1f, new Vector3(0.1f, wheel.wheelUAPI.Radius * 2f, 0.1f), transform.rotation, LayerHelper.parkingAreaLayerMask);
				if (array.Length == 0)
				{
					flag = false;
					break;
				}
				ParkingLaneGenerator componentInParent = array[0].GetComponentInParent<ParkingLaneGenerator>();
				if (!componentInParent || componentInParent.isHandicapParking)
				{
					flag = false;
					break;
				}
				neighbourhood = componentInParent.neighbourhood;
			}
		}
		InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.vehicleInfo.SetParkingZone((!flag) ? ParkingState.Illegal : ParkingState.Legal, neighbourhood);
	}

	public override void SavePosition()
	{
		base.SavePosition();
		if (fuelModule != null)
		{
			vehicleInstance.fuel = fuelModule.amount;
		}
	}

	private void FuelEmptyNotification(bool playSound = true)
	{
		Notifications.Show(NotificationType.Error, "carcontroller_notification_fuel_empty", null, 4f, "carcontroller_notification_fuel_empty");
		if (playSound)
		{
			InstanceBehavior<SfxManager>.Instance.PlayAudio(fuelEmptyStartupSoundType, base.transform.position);
		}
	}

	public override float GetCurrentFuel()
	{
		return fuelModule.amount;
	}

	public override void SetFuel(float value)
	{
		fuelModule.amount = value;
		fuelModule.amount = Mathf.Clamp(fuelModule.amount, 0f, fuelModule.capacity);
		vehicleInstance.fuel = fuelModule.amount;
		GlobalEvents.onVehicleVariablesChanged?.Invoke();
	}

	public override void AddFuel(float value)
	{
		SetFuel(Math.Min(fuelModule.amount + value, fuelModule.capacity));
	}

	public override float GetCurrentCondition()
	{
		return damageHandler.Damage;
	}

	public override bool Interact()
	{
		if (!ShouldUseJerryCan())
		{
			return base.Interact();
		}
		AddFuel(10f);
		PlayerHelper.ItemInstanceInHands = null;
		return true;
	}

	public override Vector3 GetClosestNavMeshTargetPosition(Vector3 entityPosition)
	{
		if (ShouldUseJerryCan() && NavMesh.SamplePosition(vehicleRefuelingPosition.position, out var hit, 3f, -1))
		{
			return hit.position;
		}
		return base.GetClosestNavMeshTargetPosition(entityPosition);
	}

	public bool ShouldUseJerryCan()
	{
		Item itemInHands = PlayerHelper.ItemInHands;
		if ((object)itemInHands != null && itemInHands.HasTag(TagRef.Itemtag.isfuelcontainer))
		{
			return !Mathf.Approximately(fuelModule.capacity, GetCurrentFuel());
		}
		return false;
	}

	public override void Repair()
	{
		base.Repair();
		damageHandler.Repair();
		vehicleDeformationController?.Reset();
	}

	public void SetDamage(float damageAmount)
	{
		vehicleInstance.damage = damageAmount;
		damageHandler.SetDamage(damageAmount);
	}

	[ContextMenu("Destroy underneath")]
	public void DestroyVehiclesUnderneath()
	{
		VehicleHelper.DestroyBlockingVehicles(base.gameObject, vehicleInstance.VehicleType);
	}

	public override void UpdateCargoCount()
	{
		base.UpdateCargoCount();
		if ((bool)boxPlaceholder)
		{
			int num = vehicleInstance.cargoInstances?.Count ?? 0;
			for (int i = 0; i < boxPlaceholder.childCount; i++)
			{
				boxPlaceholder.GetChild(i).gameObject.SetActive(num > i);
			}
		}
	}

	private void PlayCarNotDriveableSoundIfNeeded()
	{
		if (!IsCarDriveable() && IsPlayerTryingToMoveCar() && _timeSinceLastNotDriveableSound >= 5f)
		{
			SoundType type = (IsCarBroken() ? brokenStartupSoundType : fuelEmptyStartupSoundType);
			InstanceBehavior<SfxManager>.Instance.PlayAudio(type, base.transform.position);
			_timeSinceLastNotDriveableSound = 0f;
		}
		if (!IsCarDriveable())
		{
			_timeSinceLastNotDriveableSound += Time.deltaTime;
		}
		else
		{
			_timeSinceLastNotDriveableSound = 0f;
		}
	}

	private bool IsPlayerTryingToMoveCar()
	{
		if (vehicleController.input.Throttle == 0f)
		{
			return vehicleController.input.Brakes != 0f;
		}
		return true;
	}

	private void UpdateTodoTasks(bool force = false)
	{
		bool flag = vehicleType.damageIntensity > 0f && vehicleInstance.damage >= 0.9f;
		if ((flag != _hasRepairTask) | force)
		{
			_hasRepairTask = flag;
			if (flag)
			{
				InstanceBehavior<UIs>.Instance.tasksUI.CreateTodoTask(TodoTaskType.VehicleNeedsRepair, null, null, null, null, Priority.High, 1);
			}
			else
			{
				TodoTask taskOfType = TodoTask.GetTaskOfType(TodoTaskType.VehicleNeedsRepair);
				if (taskOfType != null)
				{
					InstanceBehavior<UIs>.Instance.tasksUI.CompleteTodoTask(taskOfType);
				}
			}
		}
		FuelModule fuelModule = this.fuelModule;
		bool flag2 = fuelModule != null && fuelModule.useFuel && this.fuelModule.FuelPercentage <= 0.1f;
		if (!((flag2 != _hasFuelTask) | force))
		{
			return;
		}
		_hasFuelTask = flag2;
		if (flag2)
		{
			InstanceBehavior<UIs>.Instance.tasksUI.CreateTodoTask(TodoTaskType.VehicleNeedsFuel, null, null, null, null, Priority.High, 1);
			return;
		}
		TodoTask taskOfType2 = TodoTask.GetTaskOfType(TodoTaskType.VehicleNeedsFuel);
		if (taskOfType2 != null)
		{
			InstanceBehavior<UIs>.Instance.tasksUI.CompleteTodoTask(taskOfType2);
		}
	}

	private void DestroyTodoTasks()
	{
		if ((!PlayerHelper.IsUsingVehicle || !(SaveGameManager.Current.ActiveVehicleId != vehicleInstance.id)) && (bool)InstanceBehavior<UIs>.Instance)
		{
			TodoTask taskOfType = TodoTask.GetTaskOfType(TodoTaskType.VehicleNeedsRepair);
			if (taskOfType != null)
			{
				InstanceBehavior<UIs>.Instance.tasksUI.InstantlyCompleteTodoTask(taskOfType);
			}
			TodoTask taskOfType2 = TodoTask.GetTaskOfType(TodoTaskType.VehicleNeedsFuel);
			if (taskOfType2 != null)
			{
				InstanceBehavior<UIs>.Instance.tasksUI.InstantlyCompleteTodoTask(taskOfType2);
			}
			_hasRepairTask = false;
			_hasFuelTask = false;
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		GlobalEvents.outdoorLightsStatusChanged = (Action<bool>)Delegate.Remove(GlobalEvents.outdoorLightsStatusChanged, new Action<bool>(OnOutdoorLightsStatusChanged));
	}
}
