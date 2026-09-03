using System.Linq;
using BigAmbitions.Neighborhoods;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class AddMissingNeighborhoodStats : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		NeighborhoodData[] neighborhoodsData = NeighborhoodHelper.NeighborhoodsData;
		foreach (NeighborhoodData neighborhoodData in neighborhoodsData)
		{
			if (!string.IsNullOrEmpty(neighborhoodData.neighbourhood) && !gameInstance.NeighbourhoodStats.Any((NeighbourhoodStats x) => x.name == neighborhoodData.neighbourhood))
			{
				NeighbourhoodStats item = new NeighbourhoodStats
				{
					name = neighborhoodData.neighbourhood,
					nextNewBusinessDay = Random.Range(gameInstance.Day + 2, gameInstance.Day + 10),
					nextResidentialSwapDay = Random.Range(gameInstance.Day + 5, gameInstance.Day + 15),
					nextWarehouseSwapDay = Random.Range(gameInstance.Day + 5, gameInstance.Day + 20)
				};
				gameInstance.NeighbourhoodStats.Add(item);
			}
		}
	}
}
