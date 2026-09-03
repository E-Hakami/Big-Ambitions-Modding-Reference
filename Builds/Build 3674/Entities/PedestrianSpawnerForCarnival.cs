using System.Collections.Generic;
using Controllers;
using Culling;
using Extensions;
using NaughtyAttributes;
using UnityEngine;

namespace Entities;

public class PedestrianSpawnerForCarnival : MonoBehaviour, ICullable
{
	public Vector3 size;

	[MinMaxSlider(0f, 30f)]
	public Vector2Int spawnAmount = new Vector2Int(10, 30);

	[SerializeField]
	private CarnivalPedestrianPool pedestrianPool;

	private readonly List<CarnivalPedestrian> _pedestrians = new List<CarnivalPedestrian>();

	private readonly HashSet<ICarnivalNpcItem> _carnivalNpcItems = new HashSet<ICarnivalNpcItem>();

	private readonly List<ICarnivalNpcItem> _carnivalNpcItemsAux = new List<ICarnivalNpcItem>();

	private readonly Collider[] _hits = new Collider[256];

	private void Start()
	{
		InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.RegisterCullable(this);
	}

	public void OnLod0()
	{
		if (_carnivalNpcItems.Count == 0)
		{
			SetItems();
		}
		if (_pedestrians.Count != 0)
		{
			return;
		}
		foreach (ICarnivalNpcItem carnivalNpcItem in _carnivalNpcItems)
		{
			carnivalNpcItem.OnActivate();
		}
		SpawnPedestrians();
	}

	private void SetItems()
	{
		int num = Physics.OverlapBoxNonAlloc(base.transform.position, size / 2f, _hits, base.transform.rotation);
		for (int i = 0; i < num; i++)
		{
			ICarnivalNpcItem component = _hits[i].GetComponent<ICarnivalNpcItem>();
			if (component != null)
			{
				_carnivalNpcItems.Add(component);
			}
		}
	}

	private void SpawnPedestrians()
	{
		int num = Random.Range(spawnAmount.x, spawnAmount.y + 1);
		_carnivalNpcItemsAux.Clear();
		foreach (ICarnivalNpcItem carnivalNpcItem2 in _carnivalNpcItems)
		{
			_carnivalNpcItemsAux.Add(carnivalNpcItem2);
		}
		for (int i = 0; i < num; i++)
		{
			if (_carnivalNpcItemsAux.Count == 0)
			{
				break;
			}
			_carnivalNpcItemsAux.Shuffle();
			int num2 = _carnivalNpcItemsAux.Count - 1;
			while (num2 >= 0)
			{
				ICarnivalNpcItem carnivalNpcItem = _carnivalNpcItemsAux[num2];
				if (!carnivalNpcItem.CanPlaceNpc())
				{
					_carnivalNpcItemsAux.RemoveAt(num2);
					num2--;
					continue;
				}
				CarnivalPedestrian carnivalPedestrian = pedestrianPool.GetPoolHandler().Get();
				carnivalPedestrian.Init(_carnivalNpcItems);
				carnivalPedestrian.SetLastCarnivalNpcItem(carnivalNpcItem);
				carnivalNpcItem.PlaceNpcInstantly(carnivalPedestrian);
				_pedestrians.Add(carnivalPedestrian);
				break;
			}
		}
	}

	public void OnLod1()
	{
		ResetSpawner();
	}

	public void OnLod2()
	{
		ResetSpawner();
	}

	private void ResetSpawner()
	{
		foreach (ICarnivalNpcItem carnivalNpcItem in _carnivalNpcItems)
		{
			carnivalNpcItem.OnDeactivate();
		}
		ReleasePedestrians();
	}

	private void ReleasePedestrians()
	{
		foreach (CarnivalPedestrian pedestrian in _pedestrians)
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
