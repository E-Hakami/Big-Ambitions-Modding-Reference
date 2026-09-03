// BigAmbitions.ModsInternal, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.ModsInternal.SteamModLoadingService
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BigAmbitions;
using BigAmbitions.ModsInternal;
using Helpers;
using Steamworks.Ugc;

public static class SteamModLoadingService
{
	public const string ModTag = "mod";

	public static async Task<List<ModInfo>> GetSubscribedMods()
	{
		return await FetchSubscribedMods(ensureInstalled: true);
	}

	public static async Task<PublishResult> UploadNewModToWorkshop(ModInfo modInfo, Action<float> onProgress)
	{
		return await SteamModUploader.UploadNewModToWorkshop(modInfo, onProgress);
	}

	public static async Task<PublishResult> UpdateExistingModOnWorkshop(ModInfo modInfo, Action<float> onProgress)
	{
		return await SteamModUploader.UpdateExistingModOnWorkshop(modInfo, onProgress);
	}

	public static async Task UpdateMod(long steamItemId)
	{
		await SteamModDownloader.EnsureModInstalled((ulong)steamItemId);
	}

	private static async Task<List<ModInfo>> FetchSubscribedMods(bool ensureInstalled)
	{
		List<ModInfo> mods = new List<ModInfo>();
		if (!SteamHelper.IsConnectedToSteam())
		{
			return mods;
		}
		List<Item> subscribedItems = await SteamModDownloader.GetSubscribedModItems();
		for (int i = 0; i < subscribedItems.Count; i++)
		{
			Item item = subscribedItems[i];
			if (ensureInstalled)
			{
				await SteamModDownloader.EnsureModInstalled(item.Id);
			}
			if (!string.IsNullOrWhiteSpace(item.Directory) && Directory.Exists(item.Directory))
			{
				ModInfo item2 = new ModInfo(item)
				{
					modFolder = item.Directory
				};
				mods.Add(item2);
			}
		}
		return mods;
	}
}
