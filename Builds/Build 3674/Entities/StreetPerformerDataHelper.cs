using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Entities;

public static class StreetPerformerDataHelper
{
	private const string AddressableLabel = "StreetPerformerData";

	private static readonly List<StreetPerformerData> StreetPerformerDataList = new List<StreetPerformerData>();

	private static bool Initialized;

	private static void LoadStreetPerformers()
	{
		foreach (StreetPerformerData item in Addressables.LoadAssetsAsync<StreetPerformerData>("StreetPerformerData", null).WaitForCompletion())
		{
			StreetPerformerDataList.Add(item);
		}
		Initialized = true;
	}

	public static IReadOnlyList<StreetPerformerData> GetStreetPerformerDataList()
	{
		if (!Initialized)
		{
			LoadStreetPerformers();
		}
		return StreetPerformerDataList;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		StreetPerformerDataList.Clear();
		Initialized = false;
	}
}
