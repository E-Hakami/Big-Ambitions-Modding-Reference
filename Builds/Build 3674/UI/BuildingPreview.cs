using System;
using System.Collections;
using System.Collections.Generic;
using BigAmbitions.InputSystem;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using Blueprints;
using Buildings;
using Buildings.Indoors;
using BusinessLayoutSets;
using CameraControllers;
using Cinemachine;
using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using Parking.UndergroundParking;
using Player.HUD.ItemInfoOverlays;
using Player.HUD.ItemWarningIcons;
using Streets;
using TMPro;
using UI.Guiders;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI;

public class BuildingPreview : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent cancelPreviewButtonLabel;

	[SerializeField]
	private TextMeshProUGUI headlineLabel;

	[Header("Wall button stuff")]
	public Image wallsButtonImage;

	public Sprite wallsVisibleSprite;

	public Sprite wallsHiddenSprite;

	public Sprite wallsPartlyHiddenSprite;

	private Building _buildingOnPreview;

	private MultipleHeightsBuildingController _multipleHeightsBuildingController;

	private CinemachineVirtualCameraBase _currentCamera;

	private bool _wasCityMapEnabledBeforePreview;

	private bool _isPreviewingSameBuildingAsItsIn;

	private BuildingSizeInfo _buildingInfo;

	private int _previousGlobalHeightIndex = -1;

	public static bool isPreviewing;

	public Building GetCurrentBuildingOnPreview => _buildingOnPreview;

	public MultipleHeightsBuildingController GetMultipleHeightsBuildingController => _multipleHeightsBuildingController;

	public string GetCurrentBuildingSize { get; private set; }

	public string GetCurrentBuildingType { get; private set; }

	private void Start()
	{
		SetUpKeysLabels();
		GlobalEvents.onBindingsChanged = (Action)Delegate.Combine(GlobalEvents.onBindingsChanged, new Action(SetUpKeysLabels));
		WallsVisibilityHelper.onWallsVisibilityChanged = (Action<WallsVisibility>)Delegate.Combine(WallsVisibilityHelper.onWallsVisibilityChanged, new Action<WallsVisibility>(UpdateWallsVisibilityIcon));
		UpdateWallsVisibilityIcon(WallsVisibilityHelper.currentWallsVisibility);
	}

	private void SetUpKeysLabels()
	{
		cancelPreviewButtonLabel.Suffix = PlayerAction.Cancel.AsSuffix();
	}

	public void Toggle(bool state)
	{
		InstanceBehavior<OverlayManager>.Instance.gameObject.SetActive(!state);
		InstanceBehavior<ItemWarningIconManager>.Instance.gameObject.SetActive(!state);
		InstanceBehavior<UIs>.Instance.tasksUI.ChangeVisibility(!state);
		InstanceBehavior<UIs>.Instance.topBar.gameObject.SetActive(!state);
		InstanceBehavior<UIs>.Instance.playerHUD.gameObject.SetActive(!state);
		GuidersManager.UpdateGuidersVisibility();
		PlayerController playerController = InstanceBehavior<GameManager>.Instance.playerController;
		if (state)
		{
			playerController.SetNavigationBlocker(NavigationBlocker.BuildingPreview);
			InstanceBehavior<UIs>.Instance.smartphoneUI.gameObject.SetActive(value: false);
		}
		else
		{
			playerController.UnsetNavigationBlocker(NavigationBlocker.BuildingPreview);
			if (_buildingOnPreview == null)
			{
				InstanceBehavior<UIs>.Instance.smartphoneUI.gameObject.SetActive(value: true);
			}
		}
		base.gameObject.SetActive(state);
	}

	public void ToggleWalls()
	{
		WallsVisibilityHelper.ToggleWalls();
	}

	private void UpdateWallsVisibilityIcon(WallsVisibility wallsVisibility)
	{
		Image image = wallsButtonImage;
		image.sprite = wallsVisibility switch
		{
			WallsVisibility.AllVisible => wallsVisibleSprite, 
			WallsVisibility.AllHidden => wallsHiddenSprite, 
			_ => wallsPartlyHiddenSprite, 
		};
	}

	public void PreviewBuilding(Building building)
	{
		if (BuildingManager.IsInsideBuilding && InstanceBehavior<BuildingManager>.Instance.building.BuildingSize == building.BuildingSize && InstanceBehavior<BuildingManager>.Instance.building.BuildingVersion == building.BuildingVersion)
		{
			Notifications.ShowError("buildingpreview_notification_you_are_already_in_the_same_building");
			return;
		}
		_buildingOnPreview = building;
		GetCurrentBuildingSize = building.BuildingSize;
		GetCurrentBuildingType = building.BuildingType;
		isPreviewing = true;
		Transform transform = InstanceBehavior<BuildingManager>.Instance.ToggleBuildingLayout(building, state: true);
		InteriorElement[] componentsInChildren = transform.GetComponentsInChildren<InteriorElement>();
		InitMultipleHeightsBuildingController(transform);
		BuildingManager.ApplyInteriorDesign(building, componentsInChildren);
		Transform transform2 = transform.Find("PreviewCameraData") ?? transform.Find("Structure/PreviewCameraData");
		BuildingSizeInfo buildingSizeInfo = new BuildingSizeInfo(building);
		if (transform2 == null)
		{
			Debug.LogError("Preview camera data not found for " + buildingSizeInfo.ToString() + ". Aborting preview");
			InstanceBehavior<BuildingManager>.Instance.ToggleBuildingLayout(building, state: false);
			_buildingOnPreview = null;
			isPreviewing = false;
			return;
		}
		BuildingPreviewHandle component = transform2.GetComponent<BuildingPreviewHandle>();
		transform.GetComponent<BuildingGridBase>().HideGrid(GridType.Both);
		if (CityMap.IsOpen)
		{
			_wasCityMapEnabledBeforePreview = true;
			InstanceBehavior<CityManager>.Instance.cityMap.TemporarilyChangeState(cityMapEnabled: false);
		}
		_currentCamera = CameraHelper.GetCurrentCamera();
		Transform transform3 = component.transform;
		InstanceBehavior<GameManager>.Instance.buildingPreviewCamera.Follow = transform3;
		InstanceBehavior<GameManager>.Instance.buildingPreviewCamera.LookAt = transform3;
		BuildingPreviewCam component2 = InstanceBehavior<GameManager>.Instance.buildingPreviewCamera.GetComponent<BuildingPreviewCam>();
		component2.offset = component.direction;
		component2.minMaxDistance = component.minMaxDistance;
		ForceDayLighting();
		InstanceBehavior<GameManager>.Instance.timeOfDayController.UpdateLights();
		InstanceBehavior<UIs>.Instance.fullMenu.Toggle(show: false);
		headlineLabel.text = building.Address.ToFormattedString() + " (" + buildingSizeInfo.ToString() + ")";
		Toggle(state: true);
		CameraHelper.SetCamera(InstanceBehavior<GameManager>.Instance.buildingPreviewCamera);
	}

	private void InitMultipleHeightsBuildingController(Transform currentBuildingVersion)
	{
		_multipleHeightsBuildingController = currentBuildingVersion.GetComponent<MultipleHeightsBuildingController>();
		if (_multipleHeightsBuildingController != null)
		{
			_multipleHeightsBuildingController.OnCurrentHeightChanged(1, skipPlayerCheck: true);
			return;
		}
		_previousGlobalHeightIndex = MultipleHeightsBuildingController.GetGlobalHeightShaderValue();
		MultipleHeightsBuildingController.SetGlobalHeightShaderValue(0);
	}

	private static void ForceDayLighting()
	{
		InstanceBehavior<GameManager>.Instance.timeOfDayController.SetEnvironmentSettings(12f, forceInsideBuilding: true);
		InstanceBehavior<GameManager>.Instance.timeOfDayController.UpdateHourlyValues(12f, forceInsideBuilding: true, forceUpdateEnvironmentalValues: true);
	}

	public void PreviewLayout(BusinessLayoutSet layoutSet)
	{
		isPreviewing = true;
		_buildingInfo = new BuildingSizeInfo(layoutSet);
		GetCurrentBuildingSize = layoutSet.BuildingSize;
		GetCurrentBuildingType = BusinessTypeHelper.GetSuitableBuildingType(layoutSet.BusinessType);
		InstanceBehavior<GameManager>.Instance.playerController.Character.ToggleVisibility(show: false);
		_isPreviewingSameBuildingAsItsIn = BuildingManager.IsInsideBuilding && InstanceBehavior<BuildingManager>.Instance.building.BuildingSize == layoutSet.BuildingSize && InstanceBehavior<BuildingManager>.Instance.building.BuildingVersion == layoutSet.BuildingVersion;
		InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer.ClearChildren();
		Transform transform;
		if (_isPreviewingSameBuildingAsItsIn)
		{
			transform = InstanceBehavior<BuildingManager>.Instance.currentBuildingVersion;
		}
		else
		{
			InstanceBehavior<BuildingManager>.Instance.ToggleLayout(new BuildingSizeInfo(InstanceBehavior<BuildingManager>.Instance.building), state: false);
			transform = InstanceBehavior<BuildingManager>.Instance.ToggleLayout(_buildingInfo, state: true);
		}
		InitMultipleHeightsBuildingController(transform);
		BuildingPreviewHandle component = transform.Find("PreviewCameraData").GetComponent<BuildingPreviewHandle>();
		transform.GetComponent<BuildingGridBase>().HideGrid(GridType.Both);
		headlineLabel.text = layoutSet.LayoutName + " (" + _buildingInfo.ToString() + ")";
		Toggle(state: true);
		_currentCamera = CameraHelper.GetCurrentCamera();
		Transform transform2 = component.transform;
		InstanceBehavior<GameManager>.Instance.buildingPreviewCamera.Follow = transform2;
		InstanceBehavior<GameManager>.Instance.buildingPreviewCamera.LookAt = transform2;
		BuildingPreviewCam component2 = InstanceBehavior<GameManager>.Instance.buildingPreviewCamera.GetComponent<BuildingPreviewCam>();
		component2.offset = component.direction;
		component2.minMaxDistance = component.minMaxDistance;
		ForceDayLighting();
		InstanceBehavior<GameManager>.Instance.timeOfDayController.UpdateLights();
		CameraHelper.SetCamera(InstanceBehavior<GameManager>.Instance.buildingPreviewCamera);
		InteriorElement[] componentsInChildren = transform.GetComponentsInChildren<InteriorElement>();
		BuildingManager.ApplyInteriorDesign(layoutSet.interiorDesigns, componentsInChildren);
		IEnumerable<ItemInstance> itemInstancesFromLayoutItems = BusinessLayoutSetHelper.GetItemInstancesFromLayoutItems(layoutSet.Items);
		InstanceBehavior<BuildingManager>.Instance.InstantiateInstances(itemInstancesFromLayoutItems, null, onlyVisual: true);
	}

	public void CancelPreview()
	{
		if (_buildingOnPreview != null)
		{
			CancelBuildingPreview();
		}
		else
		{
			CancelLayoutPreview();
		}
	}

	private void CancelBuildingPreview()
	{
		InstanceBehavior<BuildingManager>.Instance.ToggleBuildingLayout(_buildingOnPreview, state: false);
		if (BuildingManager.IsInsideBuilding)
		{
			InstanceBehavior<BuildingManager>.Instance.ToggleBuildingLayout(InstanceBehavior<BuildingManager>.Instance.building, state: true);
		}
		else if (UndergroundParkingManager.IsInsideParking)
		{
			InstanceBehavior<BuildingManager>.Instance.ToggleLayout(new BuildingSizeInfo("ba:buildingsize_parking", UndergroundParkingManager.currentParkingEntrance.parkingVersion), state: true);
		}
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(_buildingOnPreview.Address, "Presentation");
		ResetBuildingPreview();
		Toggle(state: false);
		_buildingOnPreview = null;
		InstanceBehavior<BuildingManager>.Instance.StartCoroutine(CancelPreviewTransition());
	}

	private void ResetBuildingPreview()
	{
		if (_multipleHeightsBuildingController != null)
		{
			_multipleHeightsBuildingController.OnCurrentHeightChanged(0, skipPlayerCheck: true);
			_multipleHeightsBuildingController = null;
		}
		else
		{
			MultipleHeightsBuildingController.SetGlobalHeightShaderValue(_previousGlobalHeightIndex);
		}
		isPreviewing = false;
		InstanceBehavior<GameManager>.Instance.timeOfDayController.UpdateHourlyValues(SaveGameManager.Current.Hour, forceInsideBuilding: false, forceUpdateEnvironmentalValues: true);
		InstanceBehavior<GameManager>.Instance.timeOfDayController.UpdateLights();
	}

	private void CancelLayoutPreview()
	{
		InstanceBehavior<BuildingManager>.Instance.IndoorItemContainer.ClearChildren();
		if (!_isPreviewingSameBuildingAsItsIn)
		{
			InstanceBehavior<BuildingManager>.Instance.ToggleLayout(_buildingInfo, state: false);
		}
		InstanceBehavior<BuildingManager>.Instance.LoadBuilding();
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			InstanceBehavior<BuildingManager>.Instance.LoadItems();
		}
		StartCoroutine(InstanceBehavior<BuildingManager>.Instance.DelayedEnterBuildingActions());
		InstanceBehavior<GameManager>.Instance.playerController.Character.ToggleVisibility(show: true);
		_isPreviewingSameBuildingAsItsIn = false;
		ResetBuildingPreview();
		Toggle(state: false);
		InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: false);
		InstanceBehavior<UIs>.Instance.gameSpeed.Reset();
		InstanceBehavior<BuildingManager>.Instance.StartCoroutine(CancelPreviewTransition());
	}

	private IEnumerator CancelPreviewTransition()
	{
		yield return CameraHelper.SetCameraRoutine(_currentCamera);
		if (_wasCityMapEnabledBeforePreview)
		{
			InstanceBehavior<CityManager>.Instance.cityMap.TemporarilyChangeState(cityMapEnabled: true);
			_wasCityMapEnabledBeforePreview = false;
		}
	}

	private void OnDestroy()
	{
		WallsVisibilityHelper.onWallsVisibilityChanged = (Action<WallsVisibility>)Delegate.Remove(WallsVisibilityHelper.onWallsVisibilityChanged, new Action<WallsVisibility>(UpdateWallsVisibilityIcon));
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		isPreviewing = false;
	}
}
