using System;
using Helpers;
using UI;
using UI.Notification;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class CityHamptonsHouseController : CityBuildingController
{
	public HamptonsHouse hamptonsHouse;

	[SerializeField]
	private MeshRenderer outdoorVersion;

	[SerializeField]
	private MeshRenderer pool;

	[SerializeField]
	private Collider blockerCollider;

	public override void Start()
	{
		base.Start();
		ToggleLodMode(active: false);
		GlobalEvents.RegisterOnGameLoadedCallback(RefreshBlockerCollider);
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, new Action<bool>(OnCityMapToggle));
	}

	private void OnCityMapToggle(bool isOn)
	{
		if (!isOn && poi != null && InstanceBehavior<BuildingManager>.Instance.building == building)
		{
			poi.SetHidden(hide: true);
		}
	}

	private void OnEnterBuilding(Address address)
	{
		if (!(address != building.Address) && !(poi == null))
		{
			poi.SetHidden(hide: true);
		}
	}

	private void OnExitBuilding(Address address)
	{
		if (!(address != building.Address) && !(poi == null))
		{
			poi.SetHidden(hide: false);
		}
	}

	public void RefreshBlockerCollider()
	{
		bool flag = BuildingManager.IsInsideBuilding && building == InstanceBehavior<BuildingManager>.Instance.building;
		blockerCollider.enabled = !buildingRegistration.RentedByPlayer || buildingRegistration.IsOnSale() || (BuildingManager.IsBuildingBlockedByAnyService(building.Address) && !flag);
	}

	public override bool Interact()
	{
		return true;
	}

	protected override void ToggleOutsideImageMode(bool active)
	{
		if (active)
		{
			EnableOnlyOutdoorVersion();
		}
		else
		{
			ToggleLodMode(CityMap.IsOpen);
		}
	}

	private void EnableOnlyOutdoorVersion()
	{
		Renderer[] array = renderers;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].enabled = false;
		}
		outdoorVersion.enabled = true;
		pool.enabled = true;
	}

	protected override void ToggleLodMode(bool active)
	{
		bool rentedByPlayer = buildingRegistration.RentedByPlayer;
		lodVersion.enabled = true;
		lodVersion.shadowCastingMode = (active ? ShadowCastingMode.On : ShadowCastingMode.ShadowsOnly);
		Renderer[] array = renderers;
		foreach (Renderer renderer in array)
		{
			if (!(renderer.gameObject == lodVersion.gameObject))
			{
				renderer.enabled = !active & rentedByPlayer;
			}
		}
		if (!active && BuildingManager.IsInsideBuilding && building == InstanceBehavior<BuildingManager>.Instance.building)
		{
			hamptonsHouse.OnCurrentHeightChanged(hamptonsHouse.currentHeightIndex);
		}
		outdoorVersion.enabled = !active && !rentedByPlayer;
		pool.enabled = true;
	}

	public void OnRentedBuilding()
	{
		if (!CityMap.IsOpen)
		{
			ToggleLodMode(active: false);
		}
		RefreshBlockerCollider();
		BuildingManager.RequestHamptonsItemReloadIfLoaded(building.Address, applyInterior: true, withFade: false);
	}

	public void OnBuildingSold()
	{
		buildingRegistration.RentedByPlayer = false;
		if (!CityMap.IsOpen)
		{
			ToggleLodMode(active: false);
		}
		hamptonsHouse.allItemControllers.Clear();
		RefreshBlockerCollider();
		BuildingManager.RequestHamptonsItemReloadIfLoaded(building.Address, applyInterior: true);
	}

	protected override void SetOutlineLayer()
	{
		base.SetOutlineLayer();
		outdoorVersion.gameObject.layer = LayerHelper.BuildingOutlinedLayerIndex;
	}

	protected override void UnsetOutlineLayer()
	{
		base.UnsetOutlineLayer();
		outdoorVersion.gameObject.layer = LayerHelper.BuildingLayerIndex;
	}

	private void OnCollisionEnter(Collision other)
	{
		if ((other.transform.CompareTag("Player") || other.collider.gameObject.layer == LayerHelper.VehiclesLayerIndex) && (other.collider.gameObject.layer != LayerHelper.VehiclesLayerIndex || !(other.transform.GetComponentInParent<VehicleController>() != InstanceBehavior<GameManager>.Instance.selectedVehicle)))
		{
			if (buildingRegistration.RentedByPlayer && buildingRegistration.IsOnSale())
			{
				Notifications.ShowError("cant_enter_building_while_for_sale", "cant_enter_building_while_for_sale");
			}
			else if (BuildingManager.IsBuildingBlockedByAnyService(building.Address))
			{
				Notifications.ShowError("cant_enter_building_while_interior_installation", "cant_enter_building_while_interior_installation");
			}
			else
			{
				InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(building.Address, "Presentation");
			}
		}
	}

	public override Vector3 GetClosestNavMeshTargetPosition(Vector3 entityPosition)
	{
		Vector3 sourcePosition = hamptonsHouse.plotBounds.ClosestPointOnBounds(entityPosition);
		NavMeshQueryFilter filter = new NavMeshQueryFilter
		{
			agentTypeID = 1479372276,
			areaMask = -1
		};
		if (!NavMesh.SamplePosition(sourcePosition, out var hit, 2f, filter))
		{
			return base.GetClosestNavMeshTargetPosition(entityPosition);
		}
		return hit.position;
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onEnterBuilding, new Action<Address>(OnEnterBuilding));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnExitBuilding));
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Remove(GlobalEvents.onCityMapToggle, new Action<bool>(OnCityMapToggle));
	}
}
