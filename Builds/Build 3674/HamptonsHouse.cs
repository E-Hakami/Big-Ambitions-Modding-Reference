using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using BigAmbitions.DayNightCycle;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using Buildings;
using Buildings.Indoors;
using Buildings.Indoors.InteriorDesign;
using Buildings.Outdoors;
using Culling;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using NaughtyAttributes;
using UnityEngine;

public class HamptonsHouse : MultipleHeightsBuildingController, ICullable
{
	[SerializeField]
	private Building building;

	[SerializeField]
	private BuildingGridBase buildingGrid;

	[SerializeField]
	private Collider[] hoverColliders;

	[SerializeField]
	private PlayerVolumeFormedByMultipleColliders houseVolume;

	[SerializeField]
	private PlayerVolumeFormedByMultipleColliders plotVolume;

	public SizeOrientedBounds plotBounds;

	public Transform itemsContainer;

	public InteriorElement[] interiorElements;

	[NonSerialized]
	public List<ItemController> allItemControllers = new List<ItemController>();

	private bool _isPlayerInsideHouse;

	private bool _isPlayerInsidePlot;

	private Coroutine _itemsLoadCoroutine;

	private BuildingRegistration _buildingRegistration;

	private bool _hamptonsItemReloadInProgress;

	private bool _cityMapOpenOrClosing;

	private readonly HashSet<ItemController> _itemsVisible = new HashSet<ItemController>();

	private readonly List<ItemInstance> _itemsToInstantiate = new List<ItemInstance>();

	public bool IsHouseLoaded { get; private set; }

	public IEnumerator ReloadHouseCoroutine(bool applyInterior)
	{
		if (!(itemsContainer == null))
		{
			if (_itemsLoadCoroutine != null)
			{
				StopCoroutine(_itemsLoadCoroutine);
				_itemsLoadCoroutine = null;
			}
			itemsContainer.ClearChildren();
			allItemControllers.Clear();
			_hamptonsItemReloadInProgress = true;
			_itemsLoadCoroutine = StartCoroutine(LoadHamptonsItems());
			if (applyInterior)
			{
				BuildingManager.ApplyInteriorDesign(building, interiorElements);
			}
			_hamptonsItemReloadInProgress = false;
			yield return _itemsLoadCoroutine;
			_itemsLoadCoroutine = null;
		}
	}

	private void Start()
	{
		if (!InteriorDesignerHelper.BlueprintCreatorMode)
		{
			GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onEnterVehicle, new Action<VehicleController>(OnEnterVehicle));
			GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, new Action<VehicleController>(OnExitVehicle));
			GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, new Action<bool>(OnCityMapToggle));
			GlobalEvents.onCityMapClosed = (Action)Delegate.Combine(GlobalEvents.onCityMapClosed, new Action(OnCityMapClosed));
			_buildingRegistration = (building ? building.GetRegistration() : null);
			if ((bool)buildingGrid)
			{
				buildingGrid.HideGrid(GridType.Both);
			}
			InstanceBehavior<CullingManager>.Instance.hamptonsHousesCullingGroupController.RegisterCullable(this);
			WallsVisibilityHelper.onWallsVisibilityChanged = (Action<WallsVisibility>)Delegate.Combine(WallsVisibilityHelper.onWallsVisibilityChanged, new Action<WallsVisibility>(OnWallsVisibilityChanged));
			OnCurrentHeightChanged(2, skipPlayerCheck: true);
			BuildingManager.ApplyInteriorDesign(building, interiorElements);
		}
	}

	private void OnCityMapToggle(bool toggle)
	{
		_cityMapOpenOrClosing = true;
		if (!IsHouseLoaded)
		{
			return;
		}
		Collider[] array;
		if (toggle)
		{
			HideItems();
			array = hoverColliders;
			for (int i = 0; i < array.Length; i++)
			{
				array[i].enabled = true;
			}
			return;
		}
		array = hoverColliders;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = !_isPlayerInsidePlot;
		}
		foreach (ItemController item in _itemsVisible)
		{
			if ((bool)item)
			{
				item.Show();
			}
		}
	}

	private void OnCityMapClosed()
	{
		_cityMapOpenOrClosing = false;
		if (IsHouseLoaded && _buildingRegistration.RentedByPlayer && base.ShouldCheckHeight())
		{
			plotVolume.ResetInsideCount();
			if (VehicleHelper.IsInsideVehicle())
			{
				plotVolume.ForceColliderDetectionForVehicle();
			}
			else
			{
				plotVolume.ForceColliderDetectionForPlayer();
			}
			CheckIfPlayerIsInsidePlot();
			if (_isPlayerInsidePlot)
			{
				currentHeightIndex = -1;
				CheckHeight();
			}
		}
	}

	private void HideItems()
	{
		_itemsVisible.Clear();
		foreach (ItemController allItemController in allItemControllers)
		{
			if (allItemController.visible)
			{
				allItemController.Hide();
				_itemsVisible.Add(allItemController);
			}
		}
	}

	private void OnEnterVehicle(VehicleController vehicleController)
	{
		if (vehicleController.vehicleType.spawnInPlayerObject)
		{
			return;
		}
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if (IsHouseLoaded)
			{
				plotVolume.ResetInsideCount();
				plotVolume.ForceColliderDetectionForVehicle();
			}
			if (_isPlayerInsidePlot)
			{
				houseVolume.ResetInsideCount();
				houseVolume.ForceColliderDetectionForVehicle();
			}
		});
	}

	private void OnExitVehicle(VehicleController vehicleController)
	{
		if (vehicleController.vehicleType.spawnInPlayerObject)
		{
			return;
		}
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if (_isPlayerInsideHouse)
			{
				houseVolume.ResetInsideCount();
			}
			if (_isPlayerInsidePlot)
			{
				plotVolume.ResetInsideCount();
			}
		});
	}

	private void OnWallsVisibilityChanged(WallsVisibility newWallsVisibility)
	{
		if (_isPlayerInsidePlot && !_isPlayerInsideHouse)
		{
			if (newWallsVisibility == WallsVisibility.AllVisible)
			{
				RoomWallOcclusionManager.Disable();
			}
			else
			{
				RoomWallOcclusionManager.Enable();
			}
		}
	}

	public void OnEnterPlot()
	{
		_isPlayerInsidePlot = true;
		currentHeightIndex = -1;
		Collider[] array = hoverColliders;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = false;
		}
		EnableWallOcclusionManagerIfNeeded();
		if (!BuildingManager.IsInsideBuilding)
		{
			InstanceBehavior<BuildingManager>.Instance.EnterBuilding(building);
		}
	}

	private void OnExitPlot()
	{
		_isPlayerInsidePlot = false;
		Collider[] array = hoverColliders;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = true;
		}
		RoomWallOcclusionManager.Disable();
		OnCurrentHeightChanged(2, skipPlayerCheck: true);
		InstanceBehavior<BuildingManager>.Instance.ExitFromBuilding(0);
	}

	protected override void LateUpdate()
	{
		if (IsHouseLoaded && _buildingRegistration.RentedByPlayer && base.ShouldCheckHeight() && !_cityMapOpenOrClosing)
		{
			CheckIfPlayerIsInsidePlot();
			if (ShouldCheckHeight() && _isPlayerInsidePlot)
			{
				CheckIfPlayerIsInsideHouse();
				CheckHeight();
			}
		}
	}

	protected override bool ShouldCheckHeight()
	{
		if (_isPlayerInsidePlot)
		{
			return base.ShouldCheckHeight();
		}
		return false;
	}

	private void CheckIfPlayerIsInsideHouse()
	{
		if (_isPlayerInsideHouse != houseVolume.IsInside)
		{
			_isPlayerInsideHouse = houseVolume.IsInside;
			if (_isPlayerInsideHouse)
			{
				RoomWallOcclusionManager.Disable();
				RainHelper.OnEnterBuilding();
			}
			else
			{
				EnableWallOcclusionManagerIfNeeded();
				RainHelper.OnExitBuilding();
			}
		}
	}

	private void EnableWallOcclusionManagerIfNeeded()
	{
		if (SaveGameManager.Current != null && SaveGameManager.Current.wallsVisibility != WallsVisibility.AllVisible && !_isPlayerInsideHouse)
		{
			RoomWallOcclusionManager.Enable();
		}
	}

	private void CheckIfPlayerIsInsidePlot()
	{
		if (_isPlayerInsidePlot == plotVolume.IsInside)
		{
			return;
		}
		if (plotVolume.IsInside)
		{
			if (BuildingManager.IsBuildingBlockedByAnyService(_buildingRegistration.Address) || _buildingRegistration.IsOnSale())
			{
				return;
			}
			OnEnterPlot();
		}
		else
		{
			OnExitPlot();
		}
		_isPlayerInsidePlot = plotVolume.IsInside;
	}

	private void OnDestroy()
	{
		GlobalEvents.onCityMapClosed = (Action)Delegate.Remove(GlobalEvents.onCityMapClosed, new Action(OnCityMapClosed));
		WallsVisibilityHelper.onWallsVisibilityChanged = (Action<WallsVisibility>)Delegate.Remove(WallsVisibilityHelper.onWallsVisibilityChanged, new Action<WallsVisibility>(OnWallsVisibilityChanged));
	}

	public void OnLod0()
	{
		LoadItemsIfNeeded();
	}

	public void OnLod1()
	{
		UnloadItemsIfNeeded();
	}

	public void OnLod2()
	{
		UnloadItemsIfNeeded();
	}

	private void LoadItemsIfNeeded()
	{
		if (!IsHouseLoaded && !(itemsContainer == null))
		{
			_itemsLoadCoroutine = StartCoroutine(LoadHamptonsItems());
			IsHouseLoaded = true;
		}
	}

	private IEnumerator LoadHamptonsItems()
	{
		_itemsToInstantiate.Clear();
		_itemsToInstantiate.AddRange(_buildingRegistration.itemInstances.Values);
		yield return InstantiateInstancesCoroutine(_itemsToInstantiate);
		InstanceBehavior<BuildingManager>.Instance.SetUpItemControllersParents(allItemControllers);
	}

	private IEnumerator InstantiateInstancesCoroutine(IEnumerable<ItemInstance> instances)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		float roofY = BuildingSizeHelper.GetBuildingRoofPosition(buildingSize, 0);
		foreach (ItemInstance instance in instances)
		{
			ItemController itemController = InstanceBehavior<BuildingManager>.Instance.InstantiateSingleInstance(instance, itemsContainer);
			itemController.loadedInHamptonsHouse = true;
			allItemControllers.Add(itemController);
			if (CityMap.IsOpen)
			{
				itemController.Hide();
				_itemsVisible.Add(itemController);
			}
			if (_isPlayerInsidePlot)
			{
				ToggleSecondFloorItem(currentHeightIndex == 1, itemController, roofY);
			}
			else if (!_hamptonsItemReloadInProgress && (float)stopwatch.ElapsedMilliseconds > 0.5f)
			{
				yield return null;
				stopwatch.Restart();
			}
		}
	}

	private void UnloadItemsIfNeeded()
	{
		if (IsHouseLoaded)
		{
			if (_itemsLoadCoroutine != null)
			{
				StopCoroutine(_itemsLoadCoroutine);
			}
			itemsContainer?.ClearChildren();
			IsHouseLoaded = false;
			allItemControllers.Clear();
			_itemsVisible.Clear();
		}
	}

	public BoundingSphere GetCullingBoundingSphere()
	{
		return new BoundingSphere(base.transform.position + Vector3.up * 2f, 60f);
	}

	[Button("Fill Interior Elements", EButtonEnableMode.Always)]
	public void FillInteriorElements()
	{
		interiorElements = GetComponentsInChildren<InteriorElement>();
	}
}
