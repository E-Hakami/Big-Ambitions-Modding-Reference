using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Tags;
using Buildings;
using Buildings.BuildingTypes.Special.GasStation;
using Buildings.Outdoors;
using Culling;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using Parking.UndergroundParking;
using Player.PlayerMissions;
using UI;
using UI.Notification;
using UI.Smartphone;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class CityBuildingController : ViewBlockingEntity
{
	private static CityBuildingController CurrentBuildingHighlighted;

	private static bool StaticInitFlag;

	private static readonly Vector3 EntranceDoorHalfExtents = new Vector3(1.5f, 0.3f, 1f);

	private static readonly int SelectionColorID = Shader.PropertyToID("_SelectionColor");

	private static readonly int EmissionExposureWeightID = Shader.PropertyToID("_EmissionExposureWeight");

	[Expandable]
	public Building building;

	public BuildingEntranceDoor[] entranceDoors;

	public Transform overridenCameraPositionForScreenshot;

	public DriveInEntrance[] driveInEntrances;

	public UndergroundParkingEntrance undergroundParkingEntrance;

	public bool blockPedestrianSpawn;

	[SerializeField]
	public MeshRenderer lodVersion;

	[SerializeField]
	private GameObject groundPlane;

	[SerializeField]
	private bool keepGroundPlaneAlways;

	[SerializeField]
	private List<BuildingSignController> buildingSigns;

	[SerializeField]
	private List<BuildingLogoSignController> buildingLogoSigns;

	[SerializeField]
	private bool skipFader;

	[NonSerialized]
	public BuildingRegistration buildingRegistration;

	[NonSerialized]
	public PointOfInterest poi;

	[NonSerialized]
	public Vector3 poiOffsetOnCityMap;

	[NonSerialized]
	public Vector3 poiOffsetOnStreet;

	private bool _highlighted;

	private BuildingOutsideMusicController _buildingOutsideMusicController;

	private BuildingOutsideHangoutZoneController _buildingOutsideHangoutZoneController;

	[Foldout("To remove")]
	public ViewBlockingEntityPart upperFloor;

	[Foldout("To remove")]
	public List<Transform> customPositions;

	[Foldout("To remove")]
	public ViewBlockingEntityPart bottomPlane;

	private Color32 _highlightColor;

	[Tooltip("Used be replaced by _renderers once all buildings have that list set up")]
	private List<Renderer> _lodRenderers;

	protected override int DefaultLayer => LayerHelper.BuildingLayerIndex;

	public bool HasHorizontalSigns => buildingSigns.Count > 0;

	public override void Awake()
	{
		if (!keepGroundPlaneAlways)
		{
			bottomPlane?.gameObject.SetActive(value: false);
		}
		if (base.gameObject.layer != DefaultLayer)
		{
			base.gameObject.layer = DefaultLayer;
		}
		foreach (ViewBlockingEntityPart cityBuildingPart in cityBuildingParts)
		{
			cityBuildingPart.gameObject.layer = DefaultLayer;
		}
		if (entranceDoors.Length != 0)
		{
			poiOffsetOnStreet = entranceDoors[0].doorTransform.position - (upperFloor ? upperFloor.transform.position : base.transform.position) + 2f * Vector3.up;
		}
		if (renderers.Any((Renderer x) => x == null))
		{
			Debug.LogError("Null renderers found in " + base.gameObject.name, this);
			renderers = renderers.Where((Renderer x) => x != null).ToArray();
		}
		if (renderersToFade.Any((Renderer x) => x == null))
		{
			Debug.LogError("Null renderersToFade found in " + base.gameObject.name, this);
			renderersToFade = renderersToFade.Where((Renderer x) => x != null).ToList();
		}
		if (renderersToHide.Any((Renderer x) => x == null))
		{
			Debug.LogError("Null renderersToHide found in " + base.gameObject.name, this);
			renderersToHide = renderersToHide.Where((Renderer x) => x != null).ToList();
		}
		_lodRenderers = new List<Renderer>();
		_lodRenderers.AddRange(renderers);
		_lodRenderers.AddRange(renderersToFade);
		_lodRenderers.AddRange(renderersToHide);
		_lodRenderers = _lodRenderers.Distinct().ToList();
	}

	public override void Start()
	{
		if (building != null)
		{
			buildingRegistration = BuildingHelper.GetBuildingRegistration(building.Address);
		}
		base.Start();
		if (skipFader)
		{
			ViewBlockingObjectManager.UnregisterEntity(this);
			base.enabled = false;
		}
		CreateOutsideControllers();
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool isOn)
		{
			if (isOn)
			{
				if ((bool)poi)
				{
					poi.offset = poiOffsetOnCityMap;
				}
			}
			else
			{
				SetHighlight(isOn: false);
				ClearOutlineColor();
				if ((bool)poi)
				{
					poi.offset = poiOffsetOnStreet;
				}
			}
			ToggleLodMode(isOn);
		});
		if (building == null)
		{
			return;
		}
		GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(UpdateIndoorOutdoorLight));
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, (Action<Address>)delegate(Address address)
		{
			if (!(address != building.Address))
			{
				UpdateIndoorOutdoorLight();
				UpdateSign();
				UpdatePoi();
				_buildingOutsideMusicController?.OnBuildingRegistrationChange();
				_buildingOutsideHangoutZoneController?.OnBuildingRegistrationChange();
			}
		});
		if (!StaticInitFlag)
		{
			GlobalEvents.onTimeMachineStarted = (Action)Delegate.Combine(GlobalEvents.onTimeMachineStarted, new Action(StopHighlight));
			StaticInitFlag = true;
		}
	}

	public virtual Transform GetPoiPosition()
	{
		if (!(upperFloor != null))
		{
			return base.transform;
		}
		return upperFloor.transform;
	}

	protected virtual void ToggleLodMode(bool active)
	{
		if (lodVersion == null)
		{
			return;
		}
		lodVersion.shadowCastingMode = (active ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly);
		foreach (Renderer lodRenderer in _lodRenderers)
		{
			if (!(lodRenderer.gameObject == lodVersion.gameObject))
			{
				lodRenderer.enabled = !active;
			}
		}
	}

	private void CreateOutsideControllers()
	{
		BuildingTypeData data = BuildingTypeHelper.GetData(building);
		if (data.HasTag(TagRef.Buildingtypetag.hasoutsidemusic))
		{
			_buildingOutsideMusicController = new BuildingOutsideMusicController(this);
		}
		if (data.HasTag(TagRef.Buildingtypetag.hasoutsidehangoutzone))
		{
			_buildingOutsideHangoutZoneController = new BuildingOutsideHangoutZoneController(this);
		}
	}

	public static void StopHighlight()
	{
		if (CurrentBuildingHighlighted != null)
		{
			CurrentBuildingHighlighted.ClearOutlineColor();
			CurrentBuildingHighlighted.SetHighlight(isOn: false);
		}
	}

	public void ClearHighlight()
	{
		if (CurrentBuildingHighlighted == this)
		{
			CurrentBuildingHighlighted = null;
		}
		_highlighted = false;
		StopAllCoroutines();
		UnsetOutlineLayer();
		ClearOutlineColor();
		if (poi != null && !poi.Permanent)
		{
			poi.gameObject.SetActive(value: false);
		}
	}

	public void GenerateOutsideImage(UnityAction<ScreenshotCaptureController.CaptureCommand> callback = null)
	{
		Texture2D texture2D = new Texture2D(900, 600, TextureFormat.ARGB32, mipChain: false)
		{
			hideFlags = HideFlags.HideAndDontSave
		};
		Vector3 vector = ((overridenCameraPositionForScreenshot != null) ? overridenCameraPositionForScreenshot.position : (entranceDoors[0].doorTransform.position + Vector3.up * 15f + entranceDoors[0].doorTransform.forward * 10f + entranceDoors[0].doorTransform.right * 10f));
		Quaternion rotation = ((overridenCameraPositionForScreenshot != null) ? overridenCameraPositionForScreenshot.rotation : Quaternion.LookRotation((entranceDoors[0].doorTransform.position - vector).normalized));
		ScreenshotCaptureController.CaptureCommand command = new ScreenshotCaptureController.CaptureCommand
		{
			width = texture2D.width,
			height = texture2D.height,
			outputRect = new Rect(0f, 0f, texture2D.width, texture2D.height),
			position = vector,
			rotation = rotation,
			outputTexture = texture2D,
			onSet = delegate
			{
				CityBuildingController[] cityBuildingControllers = InstanceBehavior<CityManager>.Instance.cityBuildingControllers;
				foreach (CityBuildingController obj in cityBuildingControllers)
				{
					obj.SetCameraBlockMode(isOn: false);
					obj.SetFadeSate(1f);
					obj.temporarilyDisableCameraBlock = true;
					obj.ToggleOutsideImageMode(active: true);
				}
				foreach (GasStationPartController allGasStationPartController in GasStationPartController.AllGasStationPartControllers)
				{
					allGasStationPartController.SetCameraBlockMode(isOn: false);
					allGasStationPartController.SetFadeSate(1f);
					allGasStationPartController.temporarilyDisableCameraBlock = true;
				}
				foreach (TreeController allTree in TreeController.AllTrees)
				{
					allTree.HideForScreenshot();
				}
				MouseController.currentTargetEntity?.OnIoExit();
				if (CityMap.IsOpen)
				{
					CityMapFilters.HideAllOutlines();
					InstanceBehavior<CityManager>.Instance.cityMap.TogglePostProductionVolume(isOn: false);
				}
			},
			onReset = delegate
			{
				CityBuildingController[] cityBuildingControllers = InstanceBehavior<CityManager>.Instance.cityBuildingControllers;
				foreach (CityBuildingController obj in cityBuildingControllers)
				{
					obj.temporarilyDisableCameraBlock = false;
					obj.timeSinceLastCameraBlock = -1f;
					obj.ToggleOutsideImageMode(active: false);
				}
				foreach (GasStationPartController allGasStationPartController2 in GasStationPartController.AllGasStationPartControllers)
				{
					allGasStationPartController2.temporarilyDisableCameraBlock = false;
					allGasStationPartController2.timeSinceLastCameraBlock = -1f;
				}
				foreach (TreeController allTree2 in TreeController.AllTrees)
				{
					allTree2.ShowForScreenshot();
				}
				foreach (GasStationPartController allGasStationPartController3 in GasStationPartController.AllGasStationPartControllers)
				{
					allGasStationPartController3.SetCameraBlockMode(isOn: false);
				}
			},
			onCaptured = delegate(ScreenshotCaptureController.CaptureCommand arg)
			{
				callback?.Invoke(arg);
				if (CityMap.IsOpen)
				{
					InstanceBehavior<CityManager>.Instance.cityMap.TogglePostProductionVolume(isOn: true);
					InstanceBehavior<UIs>.Instance.mapFilters.ApplyFilters();
				}
			}
		};
		InstanceBehavior<GameManager>.Instance.buildingOutdoorsCameraCaptureController.Capture(command);
	}

	protected virtual void ToggleOutsideImageMode(bool active)
	{
		if (CityMap.IsOpen)
		{
			ToggleLodMode(!active);
		}
	}

	public void UpdateSign()
	{
		if (buildingRegistration != null && building.BuildingType != "ba:buildingtype_residential" && (!string.IsNullOrWhiteSpace(buildingRegistration.BusinessName) || buildingRegistration.AvailableForRent))
		{
			foreach (BuildingLogoSignController buildingLogoSign in buildingLogoSigns)
			{
				if (buildingRegistration.AvailableForRent)
				{
					buildingRegistration.logoSettings = new LogoSettings();
					buildingRegistration.logoSettings.logoShape = "";
				}
				buildingLogoSign.UpdateSign(buildingRegistration);
				buildingLogoSign.gameObject.SetActive(value: true);
			}
			{
				foreach (BuildingSignController buildingSign in buildingSigns)
				{
					if (buildingRegistration.AvailableForRent)
					{
						buildingRegistration.signAppearanceSettings = new SignAppearanceSettings();
						buildingRegistration.logoSettings = new LogoSettings();
					}
					buildingSign.ConfigureSign(buildingRegistration);
					buildingSign.gameObject.SetActive(value: true);
				}
				return;
			}
		}
		buildingSigns.ForEach(delegate(BuildingSignController x)
		{
			x.gameObject.SetActive(value: false);
		});
		buildingLogoSigns.ForEach(delegate(BuildingLogoSignController x)
		{
			x.gameObject.SetActive(value: false);
		});
	}

	public void SetHighlight(bool isOn, Color32? color = null)
	{
		_highlighted = isOn;
		if (color.HasValue)
		{
			_highlightColor = color.Value;
		}
		if (isOn)
		{
			StopAllCoroutines();
			SetOutlineLayer();
		}
		else
		{
			OnIoExit();
		}
		if (poi != null && !poi.Permanent)
		{
			poi.gameObject.SetActive(isOn);
		}
		if (color.HasValue)
		{
			SetOutlineColor(color.Value);
		}
	}

	public void Unselect()
	{
		if (InstanceBehavior<UIs>.Instance.buildingResume.CityBuildingController == this)
		{
			InstanceBehavior<UIs>.Instance.buildingResume.UnselectBuilding(this);
		}
		if (MouseController.currentTargetEntity as CityBuildingController != this && !cityBuildingParts.Contains(MouseController.currentTargetEntity))
		{
			OnIoExit();
		}
	}

	public override void OnIoEnter()
	{
		if (!EventSystem.current.IsPointerOverGameObject() && !FullMenu.IsOpen && !SubwaySystem.IsRiding && !InstanceBehavior<UIs>.Instance.screenshot.enabled && !GameManager.IsAnyMiniGameActive() && ScreenshotController.uiIsVisible && !ScreenshotController.isInFreeLookMode)
		{
			CurrentBuildingHighlighted = this;
			StopAllCoroutines();
			SetOutlineLayer();
			if (CityMap.IsOpen)
			{
				SetOutlineColor(InstanceBehavior<GlobalReferences>.Instance.colors.white);
			}
			else if (InstanceBehavior<UIs>.IsInitialized)
			{
				InstanceBehavior<UIs>.Instance.buildingResume.HoverBuilding(this);
			}
		}
	}

	public override void OnIoExit()
	{
		CurrentBuildingHighlighted = null;
		if (base.gameObject.activeInHierarchy)
		{
			StartCoroutine(OnIoExitDelayedCoroutine());
		}
	}

	public override bool OnIoLeftClick()
	{
		if (EventSystem.current.IsPointerOverGameObject() || FullMenu.IsOpen)
		{
			return false;
		}
		if (!CityMap.IsOpen)
		{
			UpdateNavMeshTargets();
			return base.OnIoLeftClick();
		}
		InstanceBehavior<CityManager>.Instance.cityMap.SelectBuilding(this);
		return true;
	}

	public override void OnIoRightClick()
	{
		if (!SubwaySystem.IsRiding)
		{
			if (CityMap.IsOpen)
			{
				OnIoLeftClick();
			}
			else
			{
				InstanceBehavior<UIs>.Instance.buildingResume.ShowOptions(this);
			}
		}
	}

	private IEnumerator OnIoExitDelayedCoroutine()
	{
		if (CityMap.IsOpen && InstanceBehavior<UIs>.Instance.buildingResume.CityBuildingController == this)
		{
			yield return new WaitForSecondsRealtime(0.1f);
		}
		if (CityMap.IsOpen && InstanceBehavior<UIs>.Instance.buildingResume.CityBuildingController == this)
		{
			yield break;
		}
		if (_highlighted)
		{
			SetOutlineColor(_highlightColor);
			yield break;
		}
		UnsetOutlineLayer();
		if (!CityMap.IsOpen && InstanceBehavior<UIs>.IsInitialized)
		{
			InstanceBehavior<UIs>.Instance.buildingResume.UnHoverBuilding(this);
		}
	}

	protected virtual void UnsetOutlineLayer()
	{
		foreach (ViewBlockingEntityPart cityBuildingPart in cityBuildingParts)
		{
			if (cityBuildingPart.gameObject.layer != 0)
			{
				cityBuildingPart.gameObject.layer = LayerHelper.BuildingLayerIndex;
			}
		}
		if (lodVersion != null)
		{
			lodVersion.gameObject.layer = LayerHelper.BuildingLayerIndex;
		}
		if (base.gameObject.layer != 0)
		{
			base.gameObject.layer = LayerHelper.BuildingLayerIndex;
		}
		if (groundPlane != null)
		{
			groundPlane.layer = LayerHelper.BuildingLayerIndex;
		}
		if (!base.transform.Find("Billboards"))
		{
			return;
		}
		foreach (Transform item in base.transform.Find("Billboards"))
		{
			item.gameObject.layer = LayerHelper.BuildingLayerIndex;
		}
	}

	protected virtual void SetOutlineLayer()
	{
		foreach (ViewBlockingEntityPart cityBuildingPart in cityBuildingParts)
		{
			if (cityBuildingPart.gameObject.layer != 0)
			{
				cityBuildingPart.gameObject.layer = LayerHelper.BuildingOutlinedLayerIndex;
			}
		}
		if (lodVersion != null)
		{
			lodVersion.gameObject.layer = LayerHelper.BuildingOutlinedLayerIndex;
		}
		if (base.gameObject.layer != 0)
		{
			base.gameObject.layer = LayerHelper.BuildingOutlinedLayerIndex;
		}
		if (groundPlane != null)
		{
			groundPlane.layer = LayerHelper.BuildingOutlinedLayerIndex;
		}
		if (!base.transform.Find("Billboards"))
		{
			return;
		}
		foreach (Transform item in base.transform.Find("Billboards"))
		{
			item.gameObject.layer = LayerHelper.BuildingOutlinedLayerIndex;
		}
	}

	public override bool Interact()
	{
		if (building == null || InstanceBehavior<UIs>.Instance.gameSpeed.Paused)
		{
			return true;
		}
		PlayerMission currentPlayerMission = SaveGameManager.Current.currentPlayerMission;
		if (currentPlayerMission != null && currentPlayerMission.TryDeliverToAddress(building.Address))
		{
			return true;
		}
		if (!IsPlayerInAnAlwaysOpenEntranceDoor() && !BuildingHelper.CanEnterBuilding(building.Address))
		{
			if (buildingRegistration.AvailableForRent)
			{
				InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(building.Address, "Presentation");
			}
			else
			{
				Notifications.Show(NotificationType.Error, BuildingTypeHelper.GetData(building).HasTag(TagRef.Buildingtypetag.cantenterunlessrented) ? "buildingmanager_notification_private_property" : "buildingmanager_notification_business_closed", null, 4f, "BusinessCurrentlyClosed");
			}
			return true;
		}
		if (!building.requiredDLC.DlcIsOwned())
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { 
			{
				"dlc",
				building.requiredDLC.ToStringFast()
			} };
			Notifications.Show(NotificationType.Error, "common_requires_dlc", notificationData);
			return true;
		}
		GenericPersonalGoal genericPersonalGoal = InstanceBehavior<GameManager>.Instance.personalGoals.Find((GenericPersonalGoal x) => x.rewards.Exists((Reward r) => r is UnlockBuilding unlockBuilding && unlockBuilding.Address == building.Address));
		if ((bool)genericPersonalGoal && !SaveGameManager.Current.completedPersonalGoals.Contains(genericPersonalGoal.identifier))
		{
			Dictionary<string, string> notificationData2 = new Dictionary<string, string> { { "goal", genericPersonalGoal.title } };
			Notifications.Show(NotificationType.Error, "personal_goal_notification_personal_goal_required_to_enter_building", notificationData2, 4f, "Goal" + genericPersonalGoal.title + "Required");
			return true;
		}
		if (BuildingManager.IsBuildingBlockedByAnyService(building.Address))
		{
			Notifications.ShowError("cant_enter_building_while_interior_installation", "cant_enter_building_while_interior_installation");
			return true;
		}
		BuildingEntranceDoor closestEntranceDoor = entranceDoors[0];
		BuildingEntranceDoor[] array = entranceDoors;
		foreach (BuildingEntranceDoor buildingEntranceDoor in array)
		{
			if (buildingEntranceDoor != closestEntranceDoor && Vector3.SqrMagnitude(PlayerHelper.GetPosition() - buildingEntranceDoor.doorTransform.position) < Vector3.SqrMagnitude(PlayerHelper.GetPosition() - closestEntranceDoor.doorTransform.position))
			{
				closestEntranceDoor = buildingEntranceDoor;
			}
		}
		float entranceFee = buildingRegistration.GetEntranceFeeForPlayer();
		if (entranceFee > 0f)
		{
			LanguageChangeEventDataHolder bodyData = "buildingmanager_entrance_fee_confirm".Localize(new
			{
				businessName = buildingRegistration.BusinessName,
				entranceFee = entranceFee.ToCurrencyFormat()
			});
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
			{
				Dictionary<string, string> data = new Dictionary<string, string> { { "businessName", buildingRegistration.BusinessName } };
				TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_entrancefee", data);
				if (GameManager.ChangeMoneySafe(0f - entranceFee, transactionInfo, null, null, force: false, showNotification: true))
				{
					InstanceBehavior<BuildingManager>.Instance.EnterBuilding(building, useSaveGamePlayerPosition: false, inverseVehicleRotation: false, 0, closestEntranceDoor.doorId);
					if (buildingRegistration.businessTypeName == "ba:businesstype_nightclub")
					{
						NightclubBusinessHelper.OnEnterBuilding();
					}
				}
			});
		}
		else
		{
			InstanceBehavior<BuildingManager>.Instance.EnterBuilding(building, useSaveGamePlayerPosition: false, inverseVehicleRotation: false, 0, closestEntranceDoor.doorId);
		}
		return true;
	}

	private bool IsPlayerInAnAlwaysOpenEntranceDoor()
	{
		BuildingEntranceDoor[] array = entranceDoors;
		foreach (BuildingEntranceDoor buildingEntranceDoor in array)
		{
			if (buildingEntranceDoor.alwaysOpen && IsPlayerNearEntranceDoor(buildingEntranceDoor))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsPlayerNearEntranceDoor(BuildingEntranceDoor entranceDoor)
	{
		return Vector3.SqrMagnitude(PlayerHelper.GetPosition() - entranceDoor.doorTransform.position) < 4f;
	}

	public override bool SetCameraBlockMode(bool isOn)
	{
		if (!base.SetCameraBlockMode(isOn))
		{
			return false;
		}
		if (!keepGroundPlaneAlways)
		{
			bottomPlane?.gameObject.SetActive(isOn);
		}
		base.gameObject.layer = ((!isOn) ? LayerHelper.BuildingLayerIndex : 0);
		if (buildingRegistration == null || building.BuildingType == "ba:buildingtype_residential")
		{
			return true;
		}
		foreach (BuildingSignController buildingSign in buildingSigns)
		{
			if (buildingSign.signMeshRenderer.shadowCastingMode == ShadowCastingMode.Off)
			{
				buildingSign.signMeshRenderer.enabled = !isOn;
			}
			else
			{
				buildingSign.signMeshRenderer.shadowCastingMode = ((!isOn) ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly);
			}
			if (buildingSign.lightMeshRenderer.shadowCastingMode == ShadowCastingMode.Off)
			{
				buildingSign.lightMeshRenderer.enabled = !isOn;
			}
			else
			{
				buildingSign.lightMeshRenderer.shadowCastingMode = ((!isOn) ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly);
			}
		}
		return true;
	}

	public void UpdateIndoorOutdoorLight()
	{
		if (InstanceBehavior<GameManager>.Instance == null)
		{
			return;
		}
		int num = ((!BuildingTypeHelper.GetData(building).HasTag(TagRef.Buildingtypetag.hasoutdoorlight)) ? 1 : ((!InstanceBehavior<GameManager>.Instance.timeOfDayController.ShouldIndoorLightsBeOn() || !BusinessHelper.IsBusinessOpen(buildingRegistration)) ? 1 : 0));
		foreach (Renderer item in renderersToFade)
		{
			item.GetPropertyBlock(EntityController.PropertyBlockGetter);
			EntityController.PropertyBlockGetter.SetFloat(EmissionExposureWeightID, num);
			item.SetPropertyBlock(EntityController.PropertyBlockGetter);
		}
	}

	public override void SetOutlineColor(Color color)
	{
		try
		{
			foreach (Renderer item in renderersToHide)
			{
				item.GetPropertyBlock(EntityController.PropertyBlockGetter);
				EntityController.PropertyBlockGetter.SetColor(SelectionColorID, color);
				item.SetPropertyBlock(EntityController.PropertyBlockGetter);
			}
			if (lodVersion != null)
			{
				lodVersion.GetPropertyBlock(EntityController.PropertyBlockGetter);
				EntityController.PropertyBlockGetter.SetColor(SelectionColorID, color);
				lodVersion.SetPropertyBlock(EntityController.PropertyBlockGetter);
			}
		}
		catch (UnassignedReferenceException arg)
		{
			Debug.LogError($"{base.name} has renderers to hide with lost references. Error: {arg}", this);
		}
	}

	private void ClearOutlineColor()
	{
		try
		{
			foreach (Renderer item in renderersToHide)
			{
				item.SetPropertyBlock(null);
			}
			if (lodVersion != null)
			{
				lodVersion.SetPropertyBlock(null);
			}
		}
		catch (UnassignedReferenceException arg)
		{
			Debug.LogError($"{base.name} has renderers to hide with lost references. Error: {arg}", this);
		}
	}

	public void UpdatePoi(PointOfInterest pointOfInterest = null)
	{
		if (pointOfInterest == null)
		{
			if (poi == null)
			{
				CreatePOI();
			}
			pointOfInterest = poi;
		}
		bool permanent = buildingRegistration.RentedByPlayer || buildingRegistration.BuildingOwnedByPlayer;
		pointOfInterest.SetPermanent(permanent);
		pointOfInterest.SetOwnerStatus(buildingRegistration.BuildingOwnedByPlayer);
		pointOfInterest.SetRentStatus(buildingRegistration.RentedByPlayer);
		UpdatePoiIcon(pointOfInterest);
	}

	public void UpdatePoiIcon(PointOfInterest pointOfInterest)
	{
		pointOfInterest.SetIcon(buildingRegistration.GetPOIIcon(), buildingRegistration.GetPOIBackgroundColor());
	}

	public void CreatePOI()
	{
		poi = InstanceBehavior<CityManager>.Instance.cityMap.AddPoi((upperFloor != null) ? upperFloor.transform : base.transform, buildingRegistration.GetPOIIcon(), buildingRegistration.GetPOIBackgroundColor(), null, building.Address);
		poi.offset = (CityMap.IsOpen ? poiOffsetOnCityMap : poiOffsetOnStreet);
	}

	public override void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			OnIoExit();
			MouseController.currentTargetEntity = null;
			_buildingOutsideHangoutZoneController?.Cleanup();
			if (_buildingOutsideMusicController != null && InstanceBehavior<CullingManager>.IsInitialized)
			{
				InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.UnregisterCullable(_buildingOutsideMusicController);
			}
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		CurrentBuildingHighlighted = null;
		StaticInitFlag = false;
	}
}
