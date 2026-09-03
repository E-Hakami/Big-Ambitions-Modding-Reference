using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using Culling;
using Extensions;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AI;

namespace Entities;

public class PedestrianSpawnerForSwimmingPool : MonoBehaviour, ICullable
{
	public Vector3 size;

	[MinMaxSlider(0f, 16f)]
	public Vector2Int spawnAmount = new Vector2Int(2, 4);

	[SerializeField]
	private WaterPedestrianPool pedestrianPool;

	private readonly List<WaterPedestrian> _pedestrians = new List<WaterPedestrian>();

	private void Start()
	{
		InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.RegisterCullable(this);
	}

	public void OnLod0()
	{
		if (!RainHelper.isRaining && _pedestrians.Count == 0)
		{
			SpawnPedestrians();
		}
	}

	private void SpawnPedestrians()
	{
		int num = Random.Range(spawnAmount.x, spawnAmount.y + 1);
		for (int i = 0; i < num; i++)
		{
			if (NavMesh.SamplePosition(base.transform.position + new Vector3(Random.Range((0f - size.x) / 2f, size.x / 2f), 0f, Random.Range((0f - size.z) / 2f, size.z / 2f)), out var hit, 2f, NavMeshHelper.SwimmingAreaMask))
			{
				WaterPedestrian waterPedestrian = pedestrianPool.GetPoolHandler().Get();
				_pedestrians.Add(waterPedestrian);
				waterPedestrian.tpc.navmeshAgent.Warp(hit.position);
				waterPedestrian.Init();
			}
		}
	}

	public void OnLod1()
	{
		ReleasePedestrians();
	}

	public void OnLod2()
	{
		ReleasePedestrians();
	}

	private void ReleasePedestrians()
	{
		foreach (WaterPedestrian pedestrian in _pedestrians)
		{
			pedestrianPool.GetPoolHandler().Release(pedestrian);
		}
		_pedestrians.Clear();
	}

	public BoundingSphere GetCullingBoundingSphere()
	{
		float num = Mathf.Max(size.x, size.z);
		return new BoundingSphere(base.transform.position + Vector3.up * 2f, num / 2f);
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.red;
		Vector3 position = base.transform.position;
		position.y += size.y;
		Gizmos.DrawWireCube(base.transform.position, size);
		Gizmos.color = Color.white;
	}
}
