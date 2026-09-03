using System.Collections.Generic;
using System.Threading.Tasks;
using BlueprintsUI;
using Helpers;
using Player.SaveSystem.CompatibilityFixes;
using Steam;
using Steamworks.Ugc;
using UnityEngine;

namespace Blueprints;

public class BlueprintGalleryController : BlueprintController
{
	private bool _lastPageLoaded;

	public override void ClearCache()
	{
		base.ClearCache();
		_lastPageLoaded = false;
	}

	public override async Task<List<Blueprint>> GetBlueprints(int page, BlueprintSortInfo sortInfo)
	{
		if (!SteamHelper.IsConnectedToSteam())
		{
			return new List<Blueprint>();
		}
		return (await LoadBlueprintsFromWorkshop(page, sortInfo)) ?? new List<Blueprint>();
	}

	private async Task<List<Blueprint>> LoadBlueprintsFromWorkshop(int page, BlueprintSortInfo sortInfo)
	{
		int currentBuildNumber = GameVersion.GetCurrent().buildNumber;
		if (sortInfo.HasChanged)
		{
			ClearCache();
			sortInfo.HasChanged = false;
		}
		int originalPage = page;
		if (blueprintsCache.TryGetValue(page, out var cachedBlueprints))
		{
			if (cachedBlueprints.Count == 16 || GetMaxPageNumber() == page)
			{
				return cachedBlueprints;
			}
		}
		else
		{
			cachedBlueprints = new List<Blueprint>();
		}
		int steamPage = Mathf.CeilToInt((float)(page * 16) / 50f);
		while (true)
		{
			List<Item> list = await SteamWorkshopBlueprint.GetSortedItemsOnPage(steamPage, sortInfo);
			if (list == null || list.Count == 0)
			{
				_lastPageLoaded = true;
				if (cachedBlueprints.Count > 0)
				{
					blueprintsCache[page] = new List<Blueprint>(cachedBlueprints);
				}
				if (blueprintsCache.TryGetValue(originalPage, out var value) && value.Count > 0)
				{
					return blueprintsCache[originalPage];
				}
				return new List<Blueprint>();
			}
			if (list.Count < 50)
			{
				_lastPageLoaded = true;
			}
			foreach (Item item in list)
			{
				Blueprint blueprint = await BlueprintParser.ParseItemToBlueprint(item);
				if (blueprint != null && blueprint.metadata.buildNumber <= currentBuildNumber && CompatibilityBlueprintValidator.ShowInGallery(blueprint) && sortInfo.MatchesBuildVersion(blueprint.metadata.buildNumber))
				{
					cachedBlueprints.Add(blueprint);
					workshopBlueprintsBySteamId.TryAdd(blueprint.metadata.itemId, blueprint);
					if (cachedBlueprints.Count == 16)
					{
						blueprintsCache[page] = new List<Blueprint>(cachedBlueprints);
						cachedBlueprints.Clear();
						page++;
					}
				}
			}
			if (cachedBlueprints.Count > 0)
			{
				blueprintsCache[page] = new List<Blueprint>(cachedBlueprints);
			}
			if (blueprintsCache.TryGetValue(originalPage, out var value2) && value2.Count == 16)
			{
				return value2;
			}
			if (_lastPageLoaded)
			{
				break;
			}
			steamPage++;
		}
		return blueprintsCache.GetValueOrDefault(originalPage);
	}

	public override int GetMaxPageNumber()
	{
		if (!_lastPageLoaded)
		{
			return -1;
		}
		return blueprintsCache.Count;
	}
}
