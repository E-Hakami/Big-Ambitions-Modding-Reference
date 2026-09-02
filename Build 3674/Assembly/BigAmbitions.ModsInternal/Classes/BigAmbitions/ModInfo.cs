// BigAmbitions.ModsInternal, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.ModInfo
using BigAmbitions.ModsInternal;
using Steamworks.Ugc;
using UnityEngine;

public class ModInfo
{
	public ulong steamItemId;

	public string modFolder;

	public string thumbnailUrl;

	public Sprite thumbnail;

	public string title;

	public string description;

	public int targetBuildNumber;

	public int modVersion;

	public string changeLog;

	public ModInfo()
	{
	}

	public ModInfo(Item item)
	{
		ModMetadata modMetadata = SteamModMetadataHandler.GetModMetadata(item);
		steamItemId = item.Id;
		thumbnailUrl = item.PreviewImageUrl;
		title = item.Title;
		description = item.Description;
		targetBuildNumber = modMetadata?.targetBuildNumber ?? 0;
		modVersion = modMetadata?.modVersion ?? 0;
	}
}
