using System.Collections.Generic;
using System.Linq;
using Extensions;
using UnityEngine;

namespace Controllers;

public class RandomAvailableNavMeshPositionGetter
{
	private readonly Dictionary<Vector3, bool> _navmeshTargetPositionsAvailable = new Dictionary<Vector3, bool>();

	private readonly Vector3[] _navmeshPositions;

	public RandomAvailableNavMeshPositionGetter(int numberOfNavmeshTargets)
	{
		_navmeshPositions = new Vector3[numberOfNavmeshTargets];
	}

	public void Reset()
	{
		_navmeshTargetPositionsAvailable.Clear();
	}

	public bool TryGetPosition(out Vector3 navMeshPosition, Transform[] navmeshTargets, Vector3 itemPosition)
	{
		navmeshTargets.Select((Transform x) => x.position).ToList().CopyTo(_navmeshPositions);
		_navmeshPositions.Shuffle();
		Vector3[] navmeshPositions = _navmeshPositions;
		foreach (Vector3 vector in navmeshPositions)
		{
			if (_navmeshTargetPositionsAvailable.TryGetValue(vector, out var value))
			{
				if (value)
				{
					navMeshPosition = vector;
					return true;
				}
				continue;
			}
			if (!PathHelper.IsThereAWallBetweenTargetAndEntity(vector, itemPosition))
			{
				_navmeshTargetPositionsAvailable[vector] = true;
				navMeshPosition = vector;
				return true;
			}
			_navmeshTargetPositionsAvailable[vector] = false;
		}
		navMeshPosition = Vector3.zero;
		return false;
	}
}
