using System;
using System.Collections.Generic;
using Extensions;
using GleyTrafficSystem;
using Helpers;
using NaughtyAttributes;
using Streets.Pedestrians;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/PedestrianPool")]
public class PedestrianPool : Pool<Pedestrian>
{
	[Serializable]
	public class AnimatorOverride
	{
		public RuntimeAnimatorController overrideController;

		[MinMaxSlider(0f, 10f)]
		public Vector2 moveSpeed;
	}

	private const float DefaultMoveSpeed = 2f;

	[SerializeField]
	private List<AnimatorOverride> animatorOverrides;

	private int _poolIndex;

	protected override string GetPrefabName()
	{
		return "Characters/Pedestrian";
	}

	public override void CreatePool(Transform parent)
	{
		base.CreatePool(parent);
		_poolIndex = 0;
	}

	protected override Pedestrian CreateFunc(Transform parent)
	{
		Vector3 navmeshSafePositionForNpcs = NavMeshHelper.GetNavmeshSafePositionForNpcs();
		Pedestrian pedestrian = PrefabHelper.CreatePrefab<Pedestrian>(GetPrefabName(), navmeshSafePositionForNpcs, Quaternion.identity, parent);
		InitPedestrian(pedestrian);
		return pedestrian;
	}

	private void InitPedestrian(Pedestrian pedestrian)
	{
		_poolIndex++;
		pedestrian.gameObject.name = inspectorGameObjectName + _poolIndex;
	}

	protected override void ActionOnGet(Pedestrian pedestrian)
	{
		if (ObjectPoolHelper.isPrewarming)
		{
			pedestrian.gameObject.SetActive(value: true);
			return;
		}
		pedestrian.gameObject.SetActive(value: true);
		InstanceBehavior<CityManager>.Instance.pedestrianSpawner.activePedestrians.Add(pedestrian);
		AnimatorOverride random = animatorOverrides.GetRandom();
		float moveSpeed = ((random == null) ? 2f : UnityEngine.Random.Range(random.moveSpeed.x, random.moveSpeed.y));
		pedestrian.OnSpawn(random?.overrideController, moveSpeed);
	}

	protected override void ActionOnRelease(Pedestrian pedestrian)
	{
		if (ObjectPoolHelper.isPrewarming)
		{
			pedestrian.gameObject.SetActive(value: false);
			return;
		}
		InstanceBehavior<CityManager>.Instance.pedestrianSpawner.activePedestrians.Remove(pedestrian);
		Manager.TriggerColliderRemovedEvent(pedestrian.tpc.capsuleCollider);
		pedestrian.OnRelease();
		pedestrian.gameObject.SetActive(value: false);
	}

	protected override void ActionOnDestroy(Pedestrian pedestrian)
	{
		if ((bool)pedestrian)
		{
			Manager.TriggerColliderRemovedEvent(pedestrian.tpc.capsuleCollider);
			UnityEngine.Object.Destroy(pedestrian.gameObject);
		}
	}
}
