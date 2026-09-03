using Culling;
using Helpers;
using UnityEngine;

namespace Buildings.Outdoors;

public class BuildingOutsideHangoutZoneController : ICullable
{
	private readonly CityBuildingController _cityBuildingController;

	private BuildingOutsideHangoutZone _buildingOutsideHangoutZone;

	private bool _visible;

	public BuildingOutsideHangoutZoneController(CityBuildingController cityBuildingController)
	{
		_cityBuildingController = cityBuildingController;
		if (InstanceBehavior<CullingManager>.IsInitialized)
		{
			InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.RegisterCullable(this);
		}
	}

	public void Cleanup()
	{
		if (InstanceBehavior<CullingManager>.IsInitialized)
		{
			InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.UnregisterCullable(this);
		}
	}

	public void OnBuildingRegistrationChange()
	{
		if (_visible && HasPedestriansOutside() && !IsBuildingOutsideHangoutZoneSet())
		{
			SetBuildingOutsideHangoutZoneController();
		}
	}

	private bool IsBuildingOutsideHangoutZoneSet()
	{
		return _buildingOutsideHangoutZone != null;
	}

	private bool HasPedestriansOutside()
	{
		return BusinessTypeHelper.GetData(_cityBuildingController.buildingRegistration).hasPedestriansOutside;
	}

	public void OnLod0()
	{
		_visible = true;
		if (HasPedestriansOutside() && !IsBuildingOutsideHangoutZoneSet())
		{
			SetBuildingOutsideHangoutZoneController();
			_buildingOutsideHangoutZone.OnVisible();
		}
	}

	public void OnLod1()
	{
	}

	public void OnLod2()
	{
		_visible = false;
		if (IsBuildingOutsideHangoutZoneSet())
		{
			_buildingOutsideHangoutZone.OnNotVisible();
			_buildingOutsideHangoutZone.ReleaseFromPool();
			_buildingOutsideHangoutZone = null;
		}
	}

	public BoundingSphere GetCullingBoundingSphere()
	{
		return new BoundingSphere(_cityBuildingController.entranceDoors[0].doorTransform.transform.position + Vector3.up * 2f, 4f);
	}

	private void SetBuildingOutsideHangoutZoneController()
	{
		_buildingOutsideHangoutZone = InstanceBehavior<CityManager>.Instance.buildingOutsideHangoutZone.buildingOutsideHangoutZonePool.Get();
		_buildingOutsideHangoutZone.Init(_cityBuildingController);
	}
}
