using System.Collections.Generic;
using Extensions;
using UnityEngine;

namespace Streets.Pedestrians;

public static class PedestrianSpawnerPositionProvider
{
	private static readonly List<StationaryAiSpawner> EligibleSpawners = new List<StationaryAiSpawner>();

	public static (Vector3, Quaternion) GetRandomSpawnerTarget(Vector3 fromPosition, float minimumDistance, float maximumDistance, bool occupyPosition = true)
	{
		EligibleSpawners.Clear();
		foreach (StationaryAiSpawner allPedestrianSpawner in StationaryAiSpawner.AllPedestrianSpawners)
		{
			if (!allPedestrianSpawner.disabled && allPedestrianSpawner.canPedestriansComeFromOutside)
			{
				float num = MathHelper.DistanceSqr(allPedestrianSpawner.transform.position, fromPosition);
				if (num > minimumDistance * minimumDistance && num < maximumDistance * maximumDistance)
				{
					EligibleSpawners.Add(allPedestrianSpawner);
				}
			}
		}
		if (EligibleSpawners.Count > 0 && TryGetRandomSpawnerPosition(EligibleSpawners, out var target, occupyPosition))
		{
			return target;
		}
		return default((Vector3, Quaternion));
	}

	private static bool TryGetRandomSpawnerPosition(List<StationaryAiSpawner> spawners, out (Vector3, Quaternion) target, bool occupyPosition)
	{
		target = default((Vector3, Quaternion));
		StationaryAiSpawner random = spawners.GetRandom();
		if (GetTarget(ref target, random, occupyPosition))
		{
			return true;
		}
		spawners.Shuffle();
		foreach (StationaryAiSpawner spawner in spawners)
		{
			if (GetTarget(ref target, spawner, occupyPosition))
			{
				return true;
			}
		}
		return false;
	}

	private static bool GetTarget(ref (Vector3 position, Quaternion rotation) target, StationaryAiSpawner chosenSpawner, bool occupyPosition)
	{
		if (!GetSamplePosition(chosenSpawner, out var randomSpawnerPosition, occupyPosition))
		{
			return false;
		}
		target.position = randomSpawnerPosition;
		target.rotation = GetRotation(chosenSpawner, randomSpawnerPosition);
		return true;
	}

	private static Quaternion GetRotation(StationaryAiSpawner chosenSpawner, Vector3 chosenSpawnerPosition)
	{
		Quaternion result = Quaternion.identity;
		Vector3 streetPerformerPosition = chosenSpawner.GetStreetPerformerPosition();
		if (streetPerformerPosition != default(Vector3))
		{
			result = Quaternion.LookRotation((streetPerformerPosition - chosenSpawnerPosition).normalized);
		}
		return result;
	}

	private static bool GetSamplePosition(StationaryAiSpawner spawner, out Vector3 randomSpawnerPosition, bool occupyPosition)
	{
		randomSpawnerPosition = default(Vector3);
		if (spawner.GetPosition(out var pos))
		{
			randomSpawnerPosition = pos;
			return true;
		}
		return false;
	}
}
