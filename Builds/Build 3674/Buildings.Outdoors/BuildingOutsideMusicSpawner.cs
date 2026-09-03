using Extensions;
using Helpers;
using UnityEngine;

namespace Buildings.Outdoors;

public class BuildingOutsideMusicSpawner : MonoBehaviour
{
	public PoolHandler<BuildingOutsideMusic> buildingOutsideMusicPool;

	private int _poolIndex;

	public void Init()
	{
		buildingOutsideMusicPool = new PoolHandler<BuildingOutsideMusic>(GetNewBuildingOutsideMusic, delegate(BuildingOutsideMusic obj)
		{
			obj.gameObject.SetActive(value: true);
		}, delegate(BuildingOutsideMusic obj)
		{
			obj.gameObject.SetActive(value: false);
		}, ObjectPoolHelper.DestroyPoolObject, collectionCheck: false, 5, 20);
		ObjectPoolHelper.PreWarmPool(buildingOutsideMusicPool, 5);
	}

	private BuildingOutsideMusic GetNewBuildingOutsideMusic()
	{
		BuildingOutsideMusic buildingOutsideMusic = PrefabHelper.CreatePrefab<BuildingOutsideMusic>("BuildingOutsideMusic", base.transform);
		_poolIndex++;
		buildingOutsideMusic.gameObject.name = "BuildingOutsideMusic" + _poolIndex;
		return buildingOutsideMusic;
	}

	private void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			buildingOutsideMusicPool?.Clear();
		}
	}
}
