using System.Collections.Generic;
using Culling;
using Entities;
using Extensions;
using NaughtyAttributes;
using UnityEngine;

public class StationaryAiSpawner : AiSpawnerZone, ICullable
{
	public static readonly List<StationaryAiSpawner> AllPedestrianSpawners = new List<StationaryAiSpawner>();

	private const float DefaultHomelessSpawnChance = 0.05f;

	public StationaryAiPool pedestrianPool;

	public StreetPerformerPool streetPerformerPool;

	public HomelessPool homelessPool;

	public bool canPedestriansComeFromOutside = true;

	public bool canSpawnStreetPerformers;

	[ShowIf("canSpawnStreetPerformers")]
	[Range(0f, 1f)]
	public float streetPerformerSpawnChance = 1f;

	[ShowIf("canSpawnStreetPerformers")]
	public BoundsPositionGiver streetPerformerBoundsPositionGiver;

	public bool canSpawnHomeless = true;

	[ShowIf("canSpawnHomeless")]
	[Range(0f, 1f)]
	public float homelessSpawnChance = 0.05f;

	[HideInInspector]
	public bool disabled;

	private List<StationaryAiBehavior> _dummyAIs;

	private StreetPerformer _streetPerformer;

	private Homeless _homeless;

	private void Start()
	{
		_dummyAIs = new List<StationaryAiBehavior>();
		InstanceBehavior<CullingManager>.Instance.generalCullingGroupController.RegisterCullable(this);
		AllPedestrianSpawners.Add(this);
	}

	public void ClearPedestrians()
	{
		foreach (StationaryAiBehavior dummyAI in _dummyAIs)
		{
			pedestrianPool.GetPoolHandler().Release(dummyAI);
		}
		if (_streetPerformer != null)
		{
			streetPerformerPool.GetPoolHandler().Release(_streetPerformer);
			_streetPerformer = null;
		}
		if (_homeless != null)
		{
			homelessPool.GetPoolHandler().Release(_homeless);
			_homeless = null;
		}
		_dummyAIs.Clear();
		usedPositions.Clear();
	}

	public void OnLod0()
	{
		bool flag = isVisible;
		isVisible = true;
		if (!CityMap.IsOpen && !disabled && !flag)
		{
			SpawnGroup();
		}
	}

	public void OnLod1()
	{
		if (isVisible)
		{
			HandleVisibilityAndClearPedestrians();
		}
	}

	public void OnLod2()
	{
		if (isVisible)
		{
			HandleVisibilityAndClearPedestrians();
		}
	}

	private void HandleVisibilityAndClearPedestrians()
	{
		isVisible = false;
		if (!disabled)
		{
			ClearPedestrians();
		}
	}

	public BoundingSphere GetCullingBoundingSphere()
	{
		return new BoundingSphere(base.transform.position + Vector3.up * 2f, 4f);
	}

	private void SpawnGroup()
	{
		if (!IsSpawningActive())
		{
			return;
		}
		SpawnStreetPerformerIfNeeded();
		SpawnHomelessIfNeeded();
		int num = spawnAmount.RandomValue();
		for (int i = 0; i != num; i++)
		{
			if (GetPosition(out var pos))
			{
				Spawn(pos);
			}
		}
	}

	private void SpawnHomelessIfNeeded()
	{
		if (canSpawnHomeless && homelessSpawnChance.Probability() && GetPosition(out var pos))
		{
			SpawnHomeless(pos);
		}
	}

	private void SpawnStreetPerformerIfNeeded()
	{
		if (canSpawnStreetPerformers && streetPerformerSpawnChance.Probability() && GetPosition(out var pos, streetPerformerBoundsPositionGiver))
		{
			SpawnStreetPerformer(pos);
		}
	}

	private void SpawnStreetPerformer(Vector3 position)
	{
		StreetPerformer streetPerformer = streetPerformerPool.GetPoolHandler().Get();
		streetPerformer.transform.position = position;
		streetPerformer.transform.rotation = streetPerformerBoundsPositionGiver.transform.rotation;
		IReadOnlyList<StreetPerformerData> streetPerformerDataList = StreetPerformerDataHelper.GetStreetPerformerDataList();
		streetPerformer.SetStreetPerformerData(streetPerformerDataList.GetRandom());
		streetPerformer.Enable();
		_streetPerformer = streetPerformer;
	}

	private void SpawnHomeless(Vector3 position)
	{
		Homeless homeless = homelessPool.GetPoolHandler().Get();
		homeless.transform.position = position;
		homeless.transform.rotation = Quaternion.Euler(0f, Random.Range(0, 360), 0f);
		_homeless = homeless;
	}

	private void Spawn(Vector3 position)
	{
		StationaryAiBehavior stationaryAiBehavior = pedestrianPool.GetPoolHandler().Get();
		stationaryAiBehavior.transform.position = position;
		FaceStreetPerformerIfSpawned(stationaryAiBehavior);
		_dummyAIs.Add(stationaryAiBehavior);
	}

	private void FaceStreetPerformerIfSpawned(StationaryAiBehavior dummyAI)
	{
		if (!(_streetPerformer == null))
		{
			Vector3 forward = _streetPerformer.transform.position - dummyAI.transform.position;
			forward.y = 0f;
			dummyAI.transform.rotation = Quaternion.LookRotation(forward);
		}
	}

	public Vector3 GetStreetPerformerPosition()
	{
		if (!(_streetPerformer == null))
		{
			return _streetPerformer.transform.position;
		}
		return default(Vector3);
	}

	[Button("Add street performers", EButtonEnableMode.Always)]
	public void AddStreetPerformers()
	{
		if (!(streetPerformerBoundsPositionGiver != null))
		{
			canSpawnStreetPerformers = true;
			GameObject obj = new GameObject("StreetPerformerBounds");
			BoundsPositionGiver boundsPositionGiver = obj.AddComponent<BoundsPositionGiver>();
			boundsPositionGiver.size = new Vector3(2f, 2f, 2f);
			boundsPositionGiver.bounds.size = boundsPositionGiver.size;
			boundsPositionGiver.gizmoColor = Color.blue;
			obj.transform.SetParent(base.transform);
			obj.transform.localPosition = Vector3.zero;
			obj.transform.localRotation = Quaternion.identity;
			streetPerformerBoundsPositionGiver = boundsPositionGiver;
		}
	}

	[Button("Remove street performers", EButtonEnableMode.Always)]
	public void RemoveStreetPerformers()
	{
		if (!(streetPerformerBoundsPositionGiver == null))
		{
			Object.DestroyImmediate(streetPerformerBoundsPositionGiver.gameObject);
			streetPerformerBoundsPositionGiver = null;
			canSpawnStreetPerformers = false;
		}
	}
}
