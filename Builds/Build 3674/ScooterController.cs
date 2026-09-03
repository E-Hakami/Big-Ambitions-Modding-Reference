using System;
using System.Collections;
using Helpers;
using NWH.VehiclePhysics2;
using NWH.VehiclePhysics2.Modules.FlipOver;
using NWH.VehiclePhysics2.Powertrain;
using NWH.WheelController3D;
using NaughtyAttributes;
using UI;
using UI.Notification;
using UnityEngine;

public class ScooterController : VehicleController
{
	private static readonly int OnScooter = Animator.StringToHash("OnScooter");

	private static MaterialPropertyBlock _propertyBlock;

	private static readonly int EmissiveExposureWeight = Shader.PropertyToID("_EmissiveExposureWeight");

	[BoxGroup("IK")]
	public Transform LHandIKAttachmentPoint;

	[BoxGroup("IK")]
	public Transform RHandIKAttachmentPoint;

	[SerializeField]
	private NWH.VehiclePhysics2.VehicleController vehicleController;

	[SerializeField]
	private GameObject paperBag;

	[SerializeField]
	private VehicleNavMeshObstacleToggler obstacleToggler;

	protected const int MaxDeadEngineFrameCount = 10;

	protected int deadEngineFrameCount;

	public override float CurrentSpeed => Mathf.Round(vehicleController.Speed);

	private static MaterialPropertyBlock PropertyBlock => _propertyBlock ?? (_propertyBlock = new MaterialPropertyBlock());

	public override void Awake()
	{
		obstacleToggler.autoToggleByVisibility = true;
		base.Awake();
		showCargoInItemPanel = true;
		vehicleController.soundManager.mixer = InstanceBehavior<GlobalReferences>.Instance.vehicleAudioMixer;
		vehicleController.onDisable.AddListener(delegate
		{
			foreach (WheelComponent wheel in vehicleController.powertrain.wheels)
			{
				wheel.wheelUAPI.enabled = false;
			}
			SetFreeze(isFrozen: true);
		});
		vehicleController.onEnable.AddListener(delegate
		{
			foreach (WheelComponent wheel2 in vehicleController.powertrain.wheels)
			{
				wheel2.wheelUAPI.enabled = true;
				((WheelController)wheel2.wheelUAPI).InitializeVehicleLayers();
			}
			SetFreeze(isFrozen: false);
		});
	}

	public override void Reset()
	{
		foreach (WheelComponent wheel in vehicleController.powertrain.wheels)
		{
			wheel.wheelUAPI.MotorTorque = 0f;
		}
		vehicleController.vehicleRigidbody.velocity = Vector3.zero;
		vehicleController.vehicleRigidbody.drag = 0f;
	}

	public override void Start()
	{
		base.Start();
		GlobalEvents.outdoorLightsStatusChanged = (Action<bool>)Delegate.Combine(GlobalEvents.outdoorLightsStatusChanged, new Action<bool>(OnOutdoorLightsStatusChanged));
		if ((bool)poi)
		{
			poi.offset = new Vector3(0f, 0.3f);
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
			while (true)
			{
				if (vehicleController.vehicleRigidbody.velocity.sqrMagnitude < 0.010000001f && vehicleController.vehicleRigidbody.angularVelocity.sqrMagnitude < 0.010000001f)
				{
					OnVehicleStationaryAfterLoad();
					yield return true;
				}
				yield return null;
			}
		}
	}

	private void OnOutdoorLightsStatusChanged(bool _)
	{
		if (controlledByPlayer)
		{
			vehicleController.effectsManager.lightsManager.lowBeamLights.SetState(base.ShouldLightsBeOn);
			vehicleController.effectsManager.lightsManager.tailLights.SetState(base.ShouldLightsBeOn);
		}
	}

	protected override void Update()
	{
		base.Update();
		if (!controlledByPlayer)
		{
			deadEngineFrameCount = 0;
			return;
		}
		EngineComponent engine = vehicleController.powertrain.engine;
		if (engine.IsRunning && engine.InputRPM == 0f)
		{
			deadEngineFrameCount++;
			if (deadEngineFrameCount >= 10)
			{
				deadEngineFrameCount = 0;
				engine.StopEngine();
				engine.StartEngine();
			}
		}
		else
		{
			deadEngineFrameCount = 0;
		}
		if (CurrentSpeed > 0f)
		{
			obstacleToggler.OnVehicleResume();
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.parkButton.interactable = false;
		}
		else
		{
			obstacleToggler.OnVehicleStopped();
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.parkButton.interactable = true;
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		GlobalEvents.outdoorLightsStatusChanged = (Action<bool>)Delegate.Remove(GlobalEvents.outdoorLightsStatusChanged, new Action<bool>(OnOutdoorLightsStatusChanged));
	}

	private void OnVehicleStationaryAfterLoad()
	{
		if (!controlledByPlayer)
		{
			vehicleController.enabled = false;
		}
	}

	public override bool Interact()
	{
		bool flag = base.Interact();
		if (SaveGameManager.Current.ActiveVehicleId == null && !CasinoBoatManager.boatSailInSequenceStarted && !flag)
		{
			flag = true;
			EnterVehicle();
		}
		return flag;
	}

	public override void EnterVehicle()
	{
		bool isHoldingBag = PlayerHelper.IsHoldingBag;
		if (PlayerHelper.ItemInstanceInHands != null && !isHoldingBag)
		{
			Notifications.Show(NotificationType.Warning, "escooter_cant_carry_item");
			return;
		}
		if (isHoldingBag)
		{
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.selectedItemInstance = null;
			InstanceBehavior<GameManager>.Instance.playerController.Character.HideHandContent();
			paperBag.SetActive(value: true);
		}
		base.EnterVehicle();
		vehicleCollider.gameObject.layer = LayerHelper.VehiclesLayerIndex;
		InstanceBehavior<GameManager>.Instance.playerController.AttachToObject(base.transform, NavigationBlocker.Scooter);
		string text = InstanceBehavior<BuildingManager>.Instance?.building?.StreetName ?? string.Empty;
		int valueOrDefault = (InstanceBehavior<BuildingManager>.Instance?.building?.StreetNumber).GetValueOrDefault();
		vehicleInstance.SetStreetData(text, valueOrDefault);
		vehicleController.effectsManager.lightsManager.lowBeamLights.SetState(base.ShouldLightsBeOn);
		vehicleController.effectsManager.lightsManager.tailLights.SetState(base.ShouldLightsBeOn);
		vehicleController.enabled = true;
		vehicleController.powertrain.engine.StartEngine();
		InstanceBehavior<GameManager>.Instance.playerController.Character.SetHandIKTargets(LHandIKAttachmentPoint, RHandIKAttachmentPoint);
		PlayerHelper.GetAnimator().SetBool(OnScooter, value: true);
		Renderer[] array = renderers;
		foreach (Renderer obj in array)
		{
			obj.GetPropertyBlock(PropertyBlock);
			PropertyBlock.SetFloat(EmissiveExposureWeight, 0f);
			obj.SetPropertyBlock(PropertyBlock);
		}
	}

	public override void ExitVehicle()
	{
		InstanceBehavior<GameManager>.Instance.playerController.DeattachFromObject(NavigationBlocker.Scooter);
		PlayerHelper.GetAnimator().SetBool(OnScooter, value: false);
		InstanceBehavior<GameManager>.Instance.playerController.Character.SetHandIKTargets(null, null);
		vehicleController.effectsManager.lightsManager.TurnOffAllLights();
		vehicleController.powertrain.engine.StopEngine();
		vehicleController.enabled = false;
		obstacleToggler.OnVehicleStopped();
		base.ExitVehicle();
		vehicleCollider.gameObject.layer = LayerHelper.InteractiveItemsLayerIndex;
		Renderer[] array = renderers;
		foreach (Renderer obj in array)
		{
			obj.GetPropertyBlock(PropertyBlock);
			PropertyBlock.SetFloat(EmissiveExposureWeight, 1f);
			obj.SetPropertyBlock(PropertyBlock);
		}
		if (PlayerHelper.ItemInstanceInHands != null)
		{
			paperBag.SetActive(value: false);
			InstanceBehavior<GameManager>.Instance.playerController.Character.ShowHandContent();
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetItemInstance(PlayerHelper.ItemInstanceInHands, spawnIntoPlayerHands: false, changeOnlyVisibility: true);
		}
	}
}
