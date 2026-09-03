using System.Collections.Generic;
using Extensions;
using Helpers;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AI;

public abstract class AiSpawnerZone : MonoBehaviour
{
	public float maxNavMeshSampleDistance = 2f;

	[MinMaxSlider(0f, 16f)]
	public Vector2Int spawnAmount = new Vector2Int(2, 4);

	public BoundsPositionGiver boundsPositionGiver;

	protected readonly List<Vector3> usedPositions = new List<Vector3>();

	protected bool isVisible;

	public bool GetPosition(out Vector3 pos, BoundsPositionGiver positionGiver = null, bool occupyPosition = true)
	{
		if (positionGiver == null)
		{
			positionGiver = boundsPositionGiver;
		}
		pos = Vector3.zero;
		for (int i = 1; i < 11; i++)
		{
			pos = positionGiver.GetRandomPointInBounds();
			if (NavMesh.SamplePosition(pos, out var hit, maxNavMeshSampleDistance, NavMeshHelper.NpcNavMeshFilter) && !Physics.CheckSphere(hit.position, 0.84f, LayerHelper.hangoutZoneConflictMask) && !usedPositions.Exists((Vector3 x) => Vector3.SqrMagnitude(x - hit.position) < 1f))
			{
				pos = hit.position;
				if (occupyPosition)
				{
					usedPositions.Add(pos);
				}
				return true;
			}
		}
		return false;
	}

	protected bool IsSpawningActive()
	{
		return InstanceBehavior<CityManager>.Instance.pedestrianSpawner.spawningActive;
	}
}
