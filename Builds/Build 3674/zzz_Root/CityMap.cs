using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.PlacementSystem;
using Cinemachine;
using Controllers;
using Extensions;
using Helpers;
using Parking.UndergroundParking;
using Player.HUD.ItemInfoOverlays;
using UI;
using UI.InteriorDesigner;
using UI.MiniMenu;
using UI.Notification;
using UI.PurchaseVehicle;
using UI.Smartphone;
using UnityEngine;
using UnityEngine.Rendering;
using Vehicles.Taxis;

public class CityMap : MonoBehaviour
{
	private const float SelectionOutlineAlphaDefault = 0.3f;

	private const float SelectionOutlineAlphaInCityMap = 0.58f;

	private static readonly int InnerColor = Shader.PropertyToID("_InnerColor");

	public CityBuildingController buildingToFocus;

	public LayerMask cityMapMask;

	public LayerMask cityMapMaskLowDetail;

	public PointOfInterest poiTemplate;

	public List<PointOfInterest> pois = new List<PointOfInterest>();

	public Material outlineFullScreenMaterial;

	public bool isSubwayMode;

	[SerializeField]
	private AudioClip[] cityMapEnterSoundClips;

	[SerializeField]
	private AudioClip[] cityMapExitSoundClips;

	[SerializeField]
	private AnimationCurve lowPassCurve;

	[SerializeField]
	private AudioSource cityMapEnterExitSoundSource;

	[SerializeField]
	private Vector3 cameraPositionOffset = new Vector3(0f, 25.7f, -23.7f);

	public CityMapCam cityMapCam;

	public CityMapNeighborhoodZones cityMapNeighborhoodZones;

	private LayerMask _defaultCullingMask;

	private CinemachineVirtualCameraBase _initialCamera;

	private PointOfInterest[] _pointOfInterests = Array.Empty<PointOfInterest>();

	private bool _requirePOIRebuild = true;

	private Coroutine _toggleCoroutine;

	private Volume _volume;

	private Canvas _canvas;

	public ITaxi Taxi { get; private set; }

	public bool IsTaxiMode => Taxi != null;

	public static bool IsOpen { get; private set; }

	private void Start()
	{
		GlobalEvents.RegisterOnGameLoadedCallback(Init);
		GlobalEvents.onGameUnloaded = (Action)Delegate.Combine(GlobalEvents.onGameUnloaded, (Action)delegate
		{
			IsOpen = false;
		});
	}

	private void LateUpdate()
	{
		PermanentPointsOfInterest.HandlePermanentPOIs();
		if (!IsOpen)
		{
			return;
		}
		if (_requirePOIRebuild)
		{
			_pointOfInterests = InstanceBehavior<CityManager>.Instance.cityMap.pois.Where((PointOfInterest x) => !x.Permanent).ToArray();
			_requirePOIRebuild = false;
		}
		for (int num = 0; num < _pointOfInterests.Length; num++)
		{
			PointOfInterest pointOfInterest = _pointOfInterests[num];
			if (pointOfInterest.gameObject.activeSelf)
			{
				pointOfInterest.UpdatePosition(GameManager.GetMainCamera());
			}
		}
		float num2 = lowPassCurve.Evaluate(Mathf.InverseLerp(cityMapCam.minMaxDistance.x, cityMapCam.minMaxDistance.y, cityMapCam.distance));
		if (!InstanceBehavior<SfxManager>.Instance.audioMixer.GetFloat("CityMapAmbianceLowPassCutoff", out var value))
		{
			value = num2;
		}
		InstanceBehavior<SfxManager>.Instance.audioMixer.SetFloat("CityMapAmbianceLowPassCutoff", Mathf.Lerp(num2, value, Time.unscaledDeltaTime));
		InstanceBehavior<GameManager>.Instance.playerController?.UpdateCityMapPlayerIconRotation();
	}

	private void Init()
	{
		_volume = GetComponent<Volume>();
		cityMapCam = InstanceBehavior<GameManager>.Instance.citymapCamera?.GetComponentInParent<CityMapCam>();
		_defaultCullingMask = GameManager.GetMainCamera().cullingMask;
		poiTemplate.gameObject.SetActive(value: false);
		SetupPois();
	}

	public void UpdateNonePermanentPointOfInterests()
	{
		_requirePOIRebuild = true;
	}

	public static bool CanOpenMap()
	{
		if (InstanceBehavior<CityManager>.Instance != null && !MiniMenu.IsOpen && !PurchaseVehicleUI.runningShowcaseAnimation && !PurchaseVehicleUI.runningCancelShowcaseAnimation)
		{
			return !BuildingPreview.isPreviewing;
		}
		return false;
	}

	public void Toggle()
	{
		Toggle(Vector3.zero);
	}

	public void Toggle(Vector3 initialCamPosition, bool force = false, bool instant = false)
	{
		if (!force)
		{
			if (InstanceBehavior<GameManager>.Instance.playerController.awaitingRepositioning || SubwaySystem.IsRiding || InstanceBehavior<UIs>.Instance.timeMachine.isBlockingUi)
			{
				return;
			}
			if (PlacementSystem.IsInPlacementMode)
			{
				Notifications.ShowError("city_map_cant_use_while_in_placement_mode_notification", "city_map_cant_use_while_in_placement_mode_notification");
				return;
			}
		}
		if (_toggleCoroutine != null)
		{
			StopCoroutine(_toggleCoroutine);
		}
		_toggleCoroutine = StartCoroutine(Toggle(!IsOpen, initialCamPosition, instant));
	}

	public void Close()
	{
		if (IsOpen)
		{
			Toggle(Vector3.zero, force: true, instant: true);
		}
	}

	public IEnumerator Toggle(bool forceOpen, Vector3 camPosition, bool instant = false)
	{
		if (InstanceBehavior<BuildingManager>.Instance.enteringBuilding || InstanceBehavior<BuildingManager>.Instance.exitingBuilding)
		{
			yield break;
		}
		if (InteriorDesignerUI.IsOpen)
		{
			Notifications.ShowError("notification_citymap_interiordesigner_open");
			yield break;
		}
		InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
		Input.ResetInputAxes();
		IsOpen = forceOpen;
		_volume.enabled = forceOpen;
		InstanceBehavior<UIs>.Instance.mapFilters.gameObject.SetActive(forceOpen);
		InstanceBehavior<UIs>.Instance.mapFilters.ApplyFilters();
		InstanceBehavior<UIs>.Instance.cityMapSubwayStations.gameObject.SetActive(value: false);
		if (InstanceBehavior<GameManager>.Instance.timeOfDayController.ShouldOutdoorLightsBeOn(-1f, skipMapCheck: true))
		{
			VehicleHelper.ToggleAllVehicleLights(!forceOpen);
		}
		SetGlobalOutlineInnerAlpha(forceOpen ? 0.58f : 0.3f);
		GlobalEvents.onCityMapToggle?.Invoke(forceOpen);
		if (forceOpen)
		{
			InstanceBehavior<GameManager>.Instance.playerController.SetNavigationBlocker(NavigationBlocker.Map);
			GlobalEvents.outdoorLightsStatusChanged?.Invoke(obj: true);
			if (!instant)
			{
				cityMapEnterExitSoundSource.clip = cityMapEnterSoundClips.GetRandom();
				cityMapEnterExitSoundSource.Play();
			}
			GameManager.GetMainCamera().cullingMask = (PlayerPrefSettings.LowDetailCityMap ? cityMapMaskLowDetail : cityMapMask);
			InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: true, showOverlay: false);
			InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: true);
			InstanceBehavior<SfxManager>.Instance.cityMapSnapshot.TransitionTo(instant ? 0.1f : 1f);
			_initialCamera = CameraHelper.GetCurrentCamera();
			Vector3 newPos = InstanceBehavior<GameManager>.Instance.citymapCamera.transform.position;
			InstanceBehavior<UIs>.Instance.playerHUD.manageCargoUI.Close();
			bool flag = BuildingManager.IsInsideBuilding || UndergroundParkingManager.IsInsideParking;
			if (camPosition != Vector3.zero)
			{
				InstanceBehavior<UIs>.Instance.playerHUD.currentBuildingUI.Toggle(on: false);
				newPos.x = camPosition.x;
				newPos.z = camPosition.z;
			}
			else if (buildingToFocus != null)
			{
				InstanceBehavior<UIs>.Instance.playerHUD.currentBuildingUI.Toggle(on: false);
				Vector3 buildingToFocusPos = GetBuildingToFocusPos(buildingToFocus);
				SetCameraPosition(buildingToFocusPos);
				newPos.x = buildingToFocusPos.x;
				newPos.z = buildingToFocusPos.z;
				InstanceBehavior<UIs>.Instance.buildingResume.SelectBuilding(buildingToFocus);
				buildingToFocus = null;
				if (instant | flag)
				{
					yield return CameraHelper.SetCameraInstant(InstanceBehavior<GameManager>.Instance.dummyCamera);
				}
				else
				{
					yield return CameraHelper.SetCameraRoutine(InstanceBehavior<GameManager>.Instance.dummyCamera);
				}
			}
			else if (flag)
			{
				InstanceBehavior<UIs>.Instance.playerHUD.currentBuildingUI.Toggle(on: false);
				CityBuildingController cityBuildingController = (UndergroundParkingManager.IsInsideParking ? UndergroundParkingManager.currentParkingEntrance.parentCbc : InstanceBehavior<BuildingManager>.Instance.cityBuildingController);
				Vector3 buildingToFocusPos2 = GetBuildingToFocusPos(cityBuildingController);
				newPos.x = buildingToFocusPos2.x;
				newPos.z = buildingToFocusPos2.z;
				SetCameraPosition(buildingToFocusPos2);
				yield return CameraHelper.SetCameraInstant(InstanceBehavior<GameManager>.Instance.dummyCamera);
			}
			else
			{
				newPos.x = InstanceBehavior<GameManager>.Instance.playerController.transform.position.x;
				newPos.z = InstanceBehavior<GameManager>.Instance.playerController.transform.position.z;
			}
			if (BuildingManager.IsInsideBuilding || UndergroundParkingManager.IsInsideParking)
			{
				InstanceBehavior<BuildingManager>.Instance.indoorVolume.enabled = false;
			}
			InstanceBehavior<GameManager>.Instance.citymapCamera.transform.parent.position = newPos;
			cityMapCam.ForceUpdateCameraPosition();
			if (PlayerHelper.IsHoldingAMop)
			{
				MouseController.Reset();
			}
			if (instant)
			{
				yield return CameraHelper.SetCameraInstant(InstanceBehavior<GameManager>.Instance.citymapCamera);
			}
			else
			{
				yield return CameraHelper.SetCameraRoutine(InstanceBehavior<GameManager>.Instance.citymapCamera);
			}
		}
		else
		{
			if (!instant)
			{
				cityMapEnterExitSoundSource.clip = cityMapExitSoundClips.GetRandom();
				cityMapEnterExitSoundSource.Play();
			}
			buildingToFocus = null;
			if (BuildingManager.IsInsideBuilding && !GameManager.IsAnyMiniGameActive())
			{
				InstanceBehavior<UIs>.Instance.playerHUD.currentBuildingUI.Toggle(on: true);
			}
			VideoGameSetup.RepositionCamera();
			if (isSubwayMode)
			{
				ToggleSubwayMode(isOn: false);
				InstanceBehavior<CityManager>.Instance.subwaySystem.lastSubwayStation = null;
			}
			Taxi = null;
			GameManager.GetMainCamera().cullingMask = _defaultCullingMask;
			TogglePois(isOn: false);
			if ((BuildingManager.IsInsideBuilding && !InstanceBehavior<BuildingManager>.Instance.building.IsHamptonsHouse()) || UndergroundParkingManager.IsInsideParking)
			{
				InstanceBehavior<BuildingManager>.Instance.indoorVolume.enabled = true;
			}
			GlobalEvents.outdoorLightsStatusChanged?.Invoke(obj: true);
			yield return RestartTime(instant);
			GlobalEvents.outdoorLightsStatusChanged?.Invoke(obj: true);
			GlobalEvents.onCityMapClosed?.Invoke();
			InstanceBehavior<GameManager>.Instance.playerController.UnsetNavigationBlocker(NavigationBlocker.Map);
			if (PlayerHelper.IsHoldingAMop)
			{
				MouseController.cursorOnHover = CursorType.Mop;
				MouseController.SetRestrictedObjectTypes(InteractiveObjectType.Ground);
			}
		}
	}

	public void FocusOnBuilding(CityBuildingController buildingController)
	{
		if (IsOpen)
		{
			UpdateBuildingToFocus(buildingController);
			return;
		}
		buildingToFocus = buildingController;
		InstanceBehavior<UIs>.Instance.smartphoneUI.OpenApp(AppName.VoogleMaps);
	}

	private void UpdateBuildingToFocus(CityBuildingController newBuildingToFocus)
	{
		Vector3 buildingToFocusPos = GetBuildingToFocusPos(newBuildingToFocus);
		SetCameraPosition(buildingToFocusPos);
		InstanceBehavior<GameManager>.Instance.citymapCamera.transform.parent.position = buildingToFocusPos;
		InstanceBehavior<UIs>.Instance.buildingResume.SelectBuilding(newBuildingToFocus);
	}

	private Vector3 GetBuildingToFocusPos(CityBuildingController cityBuildingController)
	{
		return cityBuildingController.entranceDoors[0].doorTransform.position;
	}

	private void SetCameraPosition(Vector3 pos)
	{
		InstanceBehavior<GameManager>.Instance.dummyCamera.transform.position = pos + cameraPositionOffset;
		InstanceBehavior<GameManager>.Instance.dummyCamera.transform.LookAt(pos);
	}

	public void TemporarilyChangeState(bool cityMapEnabled)
	{
		GameManager.GetMainCamera().cullingMask = (cityMapEnabled ? cityMapMask : _defaultCullingMask);
		base.gameObject.SetActive(cityMapEnabled);
		if (!FullMenu.IsOpen)
		{
			InstanceBehavior<UIs>.Instance.mapFilters.gameObject.SetActive(cityMapEnabled);
		}
	}

	private void SetGlobalOutlineInnerAlpha(float alpha)
	{
		Color color = outlineFullScreenMaterial.GetColor(InnerColor);
		color.a = alpha;
		outlineFullScreenMaterial.SetColor(InnerColor, color);
	}

	private void TogglePois(bool isOn)
	{
		foreach (PointOfInterest poi in pois)
		{
			if (!poi.Permanent)
			{
				poi.gameObject.SetActive(isOn);
			}
		}
	}

	private IEnumerator RestartTime(bool instant)
	{
		if (!InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.IsDoingPurchase && (!InstanceBehavior<UIs>.Instance.playerHUD.dialogUI.isPanelOpen || CasinoBoatManager.IsOnCasinoBoat))
		{
			InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: false);
		}
		bool paused = InstanceBehavior<UIs>.Instance.gameSpeed.playerGameSpeed.paused;
		InstanceBehavior<SfxManager>.Instance.SetSoundSnapshot(paused);
		if (instant)
		{
			yield return CameraHelper.SetCameraInstant(_initialCamera);
		}
		else
		{
			yield return CameraHelper.SetCameraRoutine(_initialCamera);
			yield return new WaitForSecondsRealtime(0.1f);
		}
		if (!InstanceBehavior<UIs>.Instance.playerActivityUI.ShouldBePaused && !FullMenu.IsOpen && !MiniMenu.IsOpen && !InstanceBehavior<UIs>.Instance.playerHUD.purchaseUI.IsDoingPurchase && (!InstanceBehavior<UIs>.Instance.playerHUD.dialogUI.isPanelOpen || CasinoBoatManager.IsOnCasinoBoat))
		{
			InstanceBehavior<UIs>.Instance.gameSpeed.Reset();
		}
		GlobalEvents.onPause?.InvokeSafely(InstanceBehavior<UIs>.Instance.gameSpeed.Paused);
	}

	public void SelectBuilding(CityBuildingController target)
	{
		if (!(InstanceBehavior<UIs>.Instance.buildingResume.CityBuildingController == target))
		{
			if ((bool)InstanceBehavior<UIs>.Instance.buildingResume.CityBuildingController)
			{
				InstanceBehavior<UIs>.Instance.buildingResume.CityBuildingController.Unselect();
			}
			InstanceBehavior<UIs>.Instance.buildingResume.SelectBuilding(target);
		}
	}

	private void SetupPois()
	{
		poiTemplate.transform.ResetTemplate();
		CityBuildingController[] cityBuildingControllers = InstanceBehavior<CityManager>.Instance.cityBuildingControllers;
		foreach (CityBuildingController cityBuildingController in cityBuildingControllers)
		{
			if (cityBuildingController.poi != null || cityBuildingController.buildingRegistration.RentedByPlayer || cityBuildingController.buildingRegistration.BuildingOwnedByPlayer)
			{
				cityBuildingController.UpdatePoi();
				if (cityBuildingController.building.IsHamptonsHouse() && InstanceBehavior<BuildingManager>.Instance.building == cityBuildingController.building)
				{
					cityBuildingController.poi.SetHidden(hide: true);
				}
			}
		}
	}

	public PointOfInterest AddPoi(Transform target, Sprite icon, Color backgroundColor, string text = "", Address address = null)
	{
		PointOfInterest pointOfInterest = UnityEngine.Object.Instantiate(poiTemplate, poiTemplate.transform.parent);
		pointOfInterest.target = target;
		pointOfInterest.targetAddress = address;
		pointOfInterest.gameObject.SetActive(value: true);
		pointOfInterest.Initialize();
		pointOfInterest.SetIcon(icon, backgroundColor);
		pois.Add(pointOfInterest);
		pointOfInterest.gameObject.SetActive(value: false);
		pointOfInterest.SetText(text);
		return pointOfInterest;
	}

	public static void ToggleBuildingHighlights(IEnumerable<CityBuildingController> buildings, bool isOn, Color color, Sprite icon)
	{
		foreach (CityBuildingController building in buildings)
		{
			if (building.poi == null)
			{
				building.CreatePOI();
			}
			building.SetHighlight(isOn, color);
			building.poi.SetIcon(icon, color);
		}
	}

	public void ToggleSubwayMode(bool isOn)
	{
		isSubwayMode = isOn;
		ToggleSubwayStations(isOn);
		InstanceBehavior<UIs>.Instance.mapFilters.Toggle(show: false);
		InstanceBehavior<UIs>.Instance.cityMapSubwayStations.Toggle(isOn);
	}

	public void SetTaxiMode(ITaxi taxi)
	{
		if (!TaxiSystem.IsTraveling)
		{
			MonoBehaviour monoBehaviour = taxi as MonoBehaviour;
			if ((bool)monoBehaviour && monoBehaviour.gameObject.activeInHierarchy)
			{
				Taxi = taxi;
				Toggle();
			}
		}
	}

	public void ToggleSubwayStations(bool isOn)
	{
		SubwayStation.mapFilterIsOn = isOn;
		foreach (SubwayStation subwayStation in InstanceBehavior<CityManager>.Instance.subwayStations)
		{
			if (isOn)
			{
				subwayStation.SetOutline();
				subwayStation.SetOutlineColor((InstanceBehavior<CityManager>.Instance.subwaySystem.lastSubwayStation == subwayStation) ? ((Color)InstanceBehavior<GlobalReferences>.Instance.colors.lightRed) : InstanceBehavior<UIs>.Instance.mapFilters.subwayStationFilterColor);
				subwayStation.CityMapPoi.gameObject.SetActive(value: true);
			}
			else
			{
				subwayStation.OnIoExit();
				subwayStation.CityMapPoi.gameObject.SetActive(value: false);
			}
		}
	}

	public void TogglePostProductionVolume(bool isOn)
	{
		_volume.enabled = isOn;
	}

	public void SetCanvasEnabled(bool isEnabled)
	{
		if (!_canvas)
		{
			_canvas = GetComponent<Canvas>();
		}
		_canvas.enabled = isEnabled;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		IsOpen = false;
	}
}
