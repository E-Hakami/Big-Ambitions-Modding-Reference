using Entities;
using Helpers;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/BuildingStationaryAiPool")]
public class BuildingStationaryAiPool : Pool<BuildingStationaryAiBehavior>
{
	private int _poolIndex;

	protected override string GetPrefabName()
	{
		return "Characters/BuildingStationaryAi";
	}

	public override void CreatePool(Transform parent)
	{
		base.CreatePool(parent);
		_poolIndex = 0;
	}

	protected override BuildingStationaryAiBehavior CreateFunc(Transform parent)
	{
		BuildingStationaryAiBehavior buildingStationaryAiBehavior = PrefabHelper.CreatePrefab<BuildingStationaryAiBehavior>(GetPrefabName(), parent);
		_poolIndex++;
		buildingStationaryAiBehavior.name = inspectorGameObjectName + _poolIndex;
		return buildingStationaryAiBehavior;
	}

	protected override void ActionOnGet(BuildingStationaryAiBehavior stationaryAiBehavior)
	{
		if (ObjectPoolHelper.isPrewarming)
		{
			stationaryAiBehavior.gameObject.SetActive(value: true);
			return;
		}
		stationaryAiBehavior.gameObject.SetActive(value: true);
		stationaryAiBehavior.Enable();
	}

	protected override void ActionOnRelease(BuildingStationaryAiBehavior stationaryAiBehavior)
	{
		if (ObjectPoolHelper.isPrewarming)
		{
			stationaryAiBehavior.gameObject.SetActive(value: false);
			return;
		}
		stationaryAiBehavior.Disable();
		stationaryAiBehavior.gameObject.SetActive(value: false);
	}

	protected override void ActionOnDestroy(BuildingStationaryAiBehavior stationaryAiBehavior)
	{
		if ((bool)stationaryAiBehavior)
		{
			Object.Destroy(stationaryAiBehavior.gameObject);
		}
	}
}
