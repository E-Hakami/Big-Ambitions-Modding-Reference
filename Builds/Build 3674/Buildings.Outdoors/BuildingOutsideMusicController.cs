using Culling;
using Helpers;
using UnityEngine;

namespace Buildings.Outdoors;

public class BuildingOutsideMusicController : ICullable
{
	private readonly CityBuildingController _cityBuildingController;

	private BuildingOutsideMusic _buildingOutsideMusic;

	private bool _visible;

	public BuildingOutsideMusicController(CityBuildingController cityBuildingController)
	{
		_cityBuildingController = cityBuildingController;
		InstanceBehavior<CullingManager>.Instance?.generalCullingGroupController.RegisterCullable(this);
	}

	public void OnBuildingRegistrationChange()
	{
		if (_visible && HasMusicOutside() && !IsBuildingOutsideMusicSet())
		{
			SetBuildingOutsideMusic();
		}
	}

	private bool IsBuildingOutsideMusicSet()
	{
		return _buildingOutsideMusic != null;
	}

	private bool HasMusicOutside()
	{
		return BusinessTypeHelper.GetData(_cityBuildingController.buildingRegistration).hasMusicOutside;
	}

	public void OnLod0()
	{
		_visible = true;
		if (HasMusicOutside() && !IsBuildingOutsideMusicSet())
		{
			SetBuildingOutsideMusic();
		}
	}

	public void OnLod1()
	{
		_visible = false;
		if (IsBuildingOutsideMusicSet())
		{
			_buildingOutsideMusic.ReleaseFromPool();
			_buildingOutsideMusic = null;
		}
	}

	public void OnLod2()
	{
		OnLod1();
	}

	public BoundingSphere GetCullingBoundingSphere()
	{
		return new BoundingSphere(_cityBuildingController.entranceDoors[0].doorTransform.transform.position + Vector3.up * 2f, 4f);
	}

	private void SetBuildingOutsideMusic()
	{
		_buildingOutsideMusic = InstanceBehavior<CityManager>.Instance.buildingOutsideMusicSpawner.buildingOutsideMusicPool.Get();
		_buildingOutsideMusic.Init(_cityBuildingController);
	}
}
