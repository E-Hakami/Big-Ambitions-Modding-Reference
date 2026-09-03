using System.Collections.Generic;
using System.Linq;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using MTAssets.SkinnedMeshCombiner;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

public class DummyPedestrianSpawnerEditorTest : MonoBehaviour
{
	public Vector3 size;

	[HideInInspector]
	public Bounds bounds;

	public float maxNavMeshSampleDistance = 2f;

	[MinMaxSlider(0f, 10000f)]
	public Vector2Int spawnAmount = new Vector2Int(100, 100);

	private readonly List<Vector3> _usedPositions = new List<Vector3>();

	private int _seed;

	private readonly NavMeshQueryFilter filter = new NavMeshQueryFilter
	{
		agentTypeID = 0,
		areaMask = -1
	};

	private void Start()
	{
		SpawnGroup();
	}

	private void OnValidate()
	{
		bounds = new Bounds(Vector3.zero, size);
	}

	private void SpawnGroup()
	{
		int num = spawnAmount.RandomValue();
		for (int i = 0; i != num; i++)
		{
			Vector3 pos = Vector3.zero;
			bool flag = false;
			for (int j = 0; j < 100; j++)
			{
				if (GetPosition(out pos))
				{
					flag = true;
					_usedPositions.Add(pos);
					Spawn(pos);
					break;
				}
			}
			if (!flag)
			{
				MonoBehaviour.print("Could not find a valid position for pedestrian");
			}
		}
	}

	private void Spawn(Vector3 position)
	{
		GameObject newDummyAi = GetNewDummyAi();
		newDummyAi.transform.position = position;
		SkinnedMeshCombiner componentInChildren = newDummyAi.GetComponentInChildren<SkinnedMeshCombiner>();
		componentInChildren.CombineMeshes();
		SkinnedMeshRenderer mergedMeshRenderer = componentInChildren.GetCombinedMeshSkinnedMeshRenderer();
		componentInChildren.resultMergeGameObject.layer = LayerHelper.HumanLayerIndex;
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if ((bool)mergedMeshRenderer)
			{
				mergedMeshRenderer.renderingLayerMask = 257u;
				mergedMeshRenderer.shadowCastingMode = ShadowCastingMode.Off;
			}
		});
	}

	private GameObject GetNewDummyAi()
	{
		BaseHuman baseHuman = PrefabHelper.CreatePrefab<BaseHuman>("Characters/DummyHumanEditorTest", base.transform);
		baseHuman.animator.SetFloat("Forward", 10f);
		return baseHuman.gameObject;
	}

	private bool GetPosition(out Vector3 pos)
	{
		Random.InitState(_seed++);
		pos = bounds.RandomPointInBounds();
		pos.y = 0f;
		pos += base.transform.position;
		pos.y += size.y;
		for (int i = 1; i < 11; i++)
		{
			if (NavMesh.SamplePosition(pos, out var hit, maxNavMeshSampleDistance * (float)i, filter) && bounds.Contains(hit.position - base.transform.position) && !Physics.CheckSphere(hit.position, 0.84f, LayerHelper.hangoutZoneConflictMask) && !_usedPositions.Any((Vector3 x) => Vector3.SqrMagnitude(x - hit.position) < 0.5f))
			{
				pos = hit.position;
				return true;
			}
		}
		return false;
	}
}
