using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public static class SpecialNpcHelper
{
	private const string AddressableLabel = "SpecialNpcData";

	private static bool IsInitialized;

	public static Dictionary<string, SpecialNpcData> SpecialNpcDataDictionary { get; private set; }

	public static void Init()
	{
		if (IsInitialized)
		{
			return;
		}
		IList<SpecialNpcData> list = Addressables.LoadAssetsAsync<SpecialNpcData>("SpecialNpcData", null).WaitForCompletion();
		SpecialNpcDataDictionary = new Dictionary<string, SpecialNpcData>();
		foreach (SpecialNpcData item in list)
		{
			SpecialNpcDataDictionary.TryAdd(item.name, item);
		}
		IsInitialized = true;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		SpecialNpcDataDictionary = null;
		IsInitialized = false;
	}
}
