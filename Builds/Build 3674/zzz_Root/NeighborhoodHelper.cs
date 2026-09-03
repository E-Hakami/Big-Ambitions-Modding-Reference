using System.Collections.Generic;
using BigAmbitions.Neighborhoods;
using Buildings;
using Extensions;
using HGAttributes;
using UnityEngine;

public static class NeighborhoodHelper
{
	public const string AddressableLabel = "Neighborhoods";

	private static Dictionary<string, NeighborhoodData> NeighborhoodsDictionary = new Dictionary<string, NeighborhoodData>();

	[AutocompleteProvider("Neighborhoods")]
	private static IEnumerable<string> NeighborhoodsProvider => Neighborhoods;

	public static NeighborhoodData[] NeighborhoodsData { get; private set; }

	public static List<string> Neighborhoods { get; private set; }

	public static string CurrentNeighborhood { get; set; }

	public static void OnNeighborhoodsLoaded(IList<NeighborhoodData> neighborhoods)
	{
		if (NeighborhoodsDictionary == null)
		{
			NeighborhoodsDictionary = new Dictionary<string, NeighborhoodData>();
		}
		if (Neighborhoods == null)
		{
			Neighborhoods = new List<string>();
		}
		NeighborhoodsDictionary.Clear();
		Neighborhoods.Clear();
		NeighborhoodsDictionary.EnsureCapacity(neighborhoods.Count);
		foreach (NeighborhoodData neighborhood in neighborhoods)
		{
			if (NeighborhoodsDictionary.TryAdd(neighborhood.neighbourhood, neighborhood))
			{
				Neighborhoods.Add(neighborhood.neighbourhood);
			}
		}
		NeighborhoodsData = new NeighborhoodData[NeighborhoodsDictionary.Count];
		NeighborhoodsDictionary.Values.CopyTo(NeighborhoodsData, 0);
	}

	public static NeighborhoodData GetData(string neighborhood)
	{
		if (string.IsNullOrEmpty(neighborhood))
		{
			neighborhood = "ba:neighborhood_global";
		}
		return NeighborhoodsDictionary[neighborhood];
	}

	public static int TotalBuildings(string neighborhood)
	{
		return SaveGameManager.Current.BuildingRegistrations.CountWhere((BuildingRegistration x) => x.Neighborhood == neighborhood);
	}

	public static float AverageBuildingTypePrice(string neighborhood, string buildingType)
	{
		int num = 0;
		float num2 = 0f;
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			Building buildingCached = buildingRegistration.BuildingCached;
			if (!(buildingCached.Neighbourhood != neighborhood) && !(buildingCached.BuildingType != buildingType))
			{
				num2 += (UnitHelper.useImperial ? (buildingCached.GetMarketPricePerSqm() / 10.76391f) : buildingCached.GetMarketPricePerSqm());
				num++;
			}
		}
		if (num != 0)
		{
			return num2 / (float)num;
		}
		return 0f;
	}

	public static List<NeighbourhoodStats> GenerateNeighbourHoodStats()
	{
		List<NeighbourhoodStats> list = new List<NeighbourhoodStats>();
		NeighborhoodData[] neighborhoodsData = NeighborhoodsData;
		foreach (NeighborhoodData neighborhoodData in neighborhoodsData)
		{
			if (!string.IsNullOrEmpty(neighborhoodData.neighbourhood))
			{
				list.Add(new NeighbourhoodStats
				{
					name = neighborhoodData.neighbourhood,
					nextNewBusinessDay = Random.Range(2, 10),
					nextResidentialSwapDay = Random.Range(5, 15),
					nextWarehouseSwapDay = Random.Range(5, 20)
				});
			}
		}
		return list;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		NeighborhoodsDictionary = null;
		NeighborhoodsData = null;
		Neighborhoods = null;
	}
}
