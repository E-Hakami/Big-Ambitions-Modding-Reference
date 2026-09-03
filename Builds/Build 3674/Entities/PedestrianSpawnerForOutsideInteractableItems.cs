using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using Controllers;
using Culling;
using Extensions;
using NaughtyAttributes;
using UnityEngine;

namespace Entities;

public class PedestrianSpawnerForOutsideInteractableItems : MonoBehaviour, ICullable
{
	public Vector3 size;

	[MinMaxSlider(0f, 16f)]
	public Vector2Int spawnAmount = new Vector2Int(2, 4);

	[SerializeField]
	private BaseHumanPool pedestrianPool;

	private readonly HashSet<NpcSpawnerItem> _outsideInteractableItems = new HashSet<NpcSpawnerItem>();

	private readonly List<NpcSpawnerItem> _outsideInteractableItemsAux = new List<NpcSpawnerItem>();

	private readonly Collider[] _hits = new Collider[256];

	private readonly List<BaseHuman> _pedestrians = new List<BaseHuman>();

	private void Start()
	{
		InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.RegisterCullable(this);
	}

	private void Update()
	{
		foreach (NpcSpawnerItem outsideInteractableItem in _outsideInteractableItems)
		{
			outsideInteractableItem.UpdateItem();
		}
	}

	public void OnLod0()
	{
		if (!RainHelper.isRaining)
		{
			if (_outsideInteractableItems.Count == 0)
			{
				SetItems();
			}
			if (_pedestrians.Count == 0)
			{
				SpawnPedestrians();
			}
		}
	}

	private void SetItems()
	{
		int num = Physics.OverlapBoxNonAlloc(base.transform.position, size / 2f, _hits, base.transform.rotation);
		for (int i = 0; i < num; i++)
		{
			NpcSpawnerItem component = _hits[i].GetComponent<NpcSpawnerItem>();
			if (component != null)
			{
				_outsideInteractableItems.Add(component);
			}
		}
	}

	private void SpawnPedestrians()
	{
		int a = Random.Range(spawnAmount.x, spawnAmount.y + 1);
		a = Mathf.Min(a, _outsideInteractableItems.Count);
		_outsideInteractableItemsAux.Clear();
		foreach (NpcSpawnerItem outsideInteractableItem in _outsideInteractableItems)
		{
			_outsideInteractableItemsAux.Add(outsideInteractableItem);
		}
		for (int i = 0; i < a; i++)
		{
			NpcSpawnerItem random = _outsideInteractableItemsAux.GetRandom();
			_outsideInteractableItemsAux.Remove(random);
			BaseHuman baseHuman = pedestrianPool.GetPoolHandler().Get();
			random.OnNpcSpawn(baseHuman);
			_pedestrians.Add(baseHuman);
		}
	}

	public void OnLod1()
	{
		if (_pedestrians.Count == 0)
		{
			return;
		}
		ReleasePedestrians();
		foreach (NpcSpawnerItem outsideInteractableItem in _outsideInteractableItems)
		{
			outsideInteractableItem.OnNpcDespawn();
		}
	}

	private void ReleasePedestrians()
	{
		foreach (BaseHuman pedestrian in _pedestrians)
		{
			pedestrianPool.GetPoolHandler().Release(pedestrian);
		}
		_pedestrians.Clear();
	}

	public void OnLod2()
	{
		OnLod1();
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
