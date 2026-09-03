using System;
using System.Linq;
using BigAmbitions.Characters;
using Extensions;
using UnityEngine.AddressableAssets;

namespace Character;

public static class SkinColorHelper
{
	private const string AddressableLabel = "SkinColors";

	private static SkinColor[] SkinColors;

	public static SkinColor GetRandom()
	{
		Initialize();
		return SkinColors.GetRandom();
	}

	public static SkinColor GetRandom(int seed)
	{
		Initialize();
		Random random = new Random(seed);
		return SkinColors[random.Next(SkinColors.Length)];
	}

	private static void Initialize()
	{
		if (SkinColors == null)
		{
			SkinColors = Addressables.LoadAssetsAsync<SkinColor>("SkinColors", null).WaitForCompletion().ToArray();
		}
	}
}
