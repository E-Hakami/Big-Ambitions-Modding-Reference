// BigAmbitions.ModsInternal, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.ModsInternal.SteamModMetadataHandler
using BigAmbitions;
using BigAmbitions.ModsInternal;
using Steamworks.Ugc;
using UnityEngine;

internal static class SteamModMetadataHandler
{
	public static string BuildModMetadataJson(ModInfo modInfo)
	{
		return JsonUtility.ToJson(new ModMetadata
		{
			targetBuildNumber = modInfo.targetBuildNumber,
			modVersion = modInfo.modVersion
		});
	}

	public static ModMetadata GetModMetadata(Item steamItem)
	{
		if (string.IsNullOrEmpty(steamItem.Metadata))
		{
			return null;
		}
		return JsonUtility.FromJson<ModMetadata>(steamItem.Metadata);
	}
}
