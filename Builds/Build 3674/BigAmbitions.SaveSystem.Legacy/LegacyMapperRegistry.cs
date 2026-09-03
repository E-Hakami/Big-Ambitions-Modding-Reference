using UnityEngine;

namespace BigAmbitions.SaveSystem.Legacy;

public static class LegacyMapperRegistry
{
	public static bool SuppressErrors { get; set; }

	internal static string Map(ILegacyMapper mapper, int legacy, bool logErrors = true)
	{
		if (mapper.TryMap(legacy, out var value))
		{
			return value;
		}
		if (logErrors && !SuppressErrors)
		{
			Debug.LogError($"Could not map legacy value {legacy} for {mapper}");
		}
		return string.Empty;
	}
}
