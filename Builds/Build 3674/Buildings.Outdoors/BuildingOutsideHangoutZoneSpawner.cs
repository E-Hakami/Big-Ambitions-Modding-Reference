using Extensions;
using Helpers;
using UnityEngine;

namespace Buildings.Outdoors;

public class BuildingOutsideHangoutZoneSpawner : MonoBehaviour
{
	public PoolHandler<BuildingOutsideHangoutZone> buildingOutsideHangoutZonePool;

	private int _poolIndex;

	public void Init()
	{
		buildingOutsideHangoutZonePool = new PoolHandler<BuildingOutsideHangoutZone>(GetNewBuildingOutsideHangoutZone, delegate(BuildingOutsideHangoutZone obj)
		{
			obj.gameObject.SetActive(value: true);
		}, delegate(BuildingOutsideHangoutZone obj)
		{
			obj.gameObject.SetActive(value: false);
		}, ObjectPoolHelper.DestroyPoolObject, collectionCheck: false, 5, 20);
		ObjectPoolHelper.PreWarmPool(buildingOutsideHangoutZonePool, 5);
	}

	private BuildingOutsideHangoutZone GetNewBuildingOutsideHangoutZone()
	{
		BuildingOutsideHangoutZone buildingOutsideHangoutZone = PrefabHelper.CreatePrefab<BuildingOutsideHangoutZone>("BuildingOutsideHangoutZone", base.transform);
		_poolIndex++;
		buildingOutsideHangoutZone.gameObject.name = "BuildingOutsideHangoutZone" + _poolIndex;
		return buildingOutsideHangoutZone;
	}

	private void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			buildingOutsideHangoutZonePool?.Clear();
		}
	}
}
