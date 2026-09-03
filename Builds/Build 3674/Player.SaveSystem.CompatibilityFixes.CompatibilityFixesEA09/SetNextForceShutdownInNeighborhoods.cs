using BigAmbitions.Neighborhoods;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class SetNextForceShutdownInNeighborhoods : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (NeighbourhoodStats neighbourhoodStat in gameInstance.NeighbourhoodStats)
		{
			neighbourhoodStat.nextForceShutdownDay = gameInstance.Day + Random.Range(2, 10);
		}
	}
}
