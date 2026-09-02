// BigAmbitions.ModsInternal, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.ModsInternal.SteamModDownloader
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BigAmbitions.ModsInternal;
using Helpers;
using Steamworks;
using Steamworks.Ugc;

internal static class SteamModDownloader
{
	public static async Task<List<Item>> GetSubscribedModItems()
	{
		List<Item> subscribedItems = new List<Item>();
		if (!SteamHelper.IsConnectedToSteam())
		{
			return subscribedItems;
		}
		int page = 1;
		while (SteamHelper.IsConnectedToSteam())
		{
			List<Item> list = await SteamHelper.GetUserSubscribedItems(page, "mod");
			if (list.Count == 0)
			{
				break;
			}
			subscribedItems.AddRange(list);
			if (list.Count < 50)
			{
				break;
			}
			page++;
		}
		return subscribedItems;
	}

	public static async Task EnsureModInstalled(ulong itemId)
	{
		if (!SteamHelper.IsConnectedToSteam())
		{
			return;
		}
		Item? item = await Item.GetAsync(itemId);
		if (!item.HasValue)
		{
			return;
		}
		Item modItem = item.Value;
		bool flag = false;
		if (string.IsNullOrWhiteSpace(modItem.Directory) || !Directory.Exists(modItem.Directory))
		{
			flag = await SteamUGC.DownloadAsync(modItem.Id);
			if (!flag)
			{
				return;
			}
		}
		if (flag && !string.IsNullOrWhiteSpace(modItem.Directory) && Directory.Exists(modItem.Directory))
		{
			ModManifest.Add(modItem.Id);
		}
	}
}
