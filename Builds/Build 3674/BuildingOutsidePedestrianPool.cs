using Extensions;
using GleyTrafficSystem;
using Helpers;
using Streets.Pedestrians;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/BuildingOutsidePedestrianPool")]
public class BuildingOutsidePedestrianPool : Pool<BuildingOutsidePedestrian>
{
	public string prefabPath;

	private int _poolIndex;

	protected override string GetPrefabName()
	{
		return prefabPath;
	}

	public override void CreatePool(Transform parent)
	{
		base.CreatePool(parent);
		_poolIndex = 0;
	}

	protected override BuildingOutsidePedestrian CreateFunc(Transform parent)
	{
		Vector3 navmeshSafePositionForNpcs = NavMeshHelper.GetNavmeshSafePositionForNpcs();
		BuildingOutsidePedestrian buildingOutsidePedestrian = PrefabHelper.CreatePrefab<BuildingOutsidePedestrian>(GetPrefabName(), navmeshSafePositionForNpcs, Quaternion.identity, parent);
		InitPedestrian(buildingOutsidePedestrian);
		return buildingOutsidePedestrian;
	}

	private void InitPedestrian(BuildingOutsidePedestrian pedestrian)
	{
		_poolIndex++;
		pedestrian.gameObject.name = inspectorGameObjectName + _poolIndex;
		pedestrian.Init();
	}

	protected override void ActionOnGet(BuildingOutsidePedestrian pedestrian)
	{
		pedestrian.gameObject.SetActive(value: true);
	}

	protected override void ActionOnRelease(BuildingOutsidePedestrian pedestrian)
	{
		if (ObjectPoolHelper.isPrewarming)
		{
			pedestrian.gameObject.SetActive(value: false);
			return;
		}
		Manager.TriggerColliderRemovedEvent(pedestrian.tpc.capsuleCollider);
		pedestrian.gameObject.SetActive(value: false);
	}

	protected override void ActionOnDestroy(BuildingOutsidePedestrian pedestrian)
	{
		if ((bool)pedestrian)
		{
			Manager.TriggerColliderRemovedEvent(pedestrian.tpc.capsuleCollider);
			Object.Destroy(pedestrian.gameObject);
		}
	}
}
