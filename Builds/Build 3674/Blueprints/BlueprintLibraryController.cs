using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BigAmbitions.SaveSystem;
using Blueprints.Compatibility;
using BlueprintsUI;
using Helpers;
using Steam;
using Steamworks;
using Steamworks.Ugc;
using UnityEngine;

namespace Blueprints;

public class BlueprintLibraryController : BlueprintController
{
	private static List<Blueprint> LocalBlueprints = new List<Blueprint>();

	private ulong _recentlyRemovedBlueprintId;

	private readonly HashSet<ulong> _publishedIdSet = new HashSet<ulong>();

	public override void ClearCache()
	{
		base.ClearCache();
		LocalBlueprints.Clear();
		blueprintsCache.Clear();
		BlueprintsFolderLoader.ClearCache();
	}

	public BlueprintLibraryController()
	{
		SteamWorkshopBlueprint.onBlueprintUploaded = (Action<Blueprint>)Delegate.Combine(SteamWorkshopBlueprint.onBlueprintUploaded, new Action<Blueprint>(OnBlueprintUploaded));
		SteamWorkshopBlueprint.onBlueprintRemoved = (Action<Blueprint>)Delegate.Combine(SteamWorkshopBlueprint.onBlueprintRemoved, new Action<Blueprint>(OnBlueprintRemoved));
	}

	private void OnBlueprintUploaded(Blueprint blueprint)
	{
		workshopBlueprintsBySteamId.Add(blueprint.metadata.itemId, blueprint);
	}

	private void OnBlueprintRemoved(Blueprint blueprint)
	{
		_recentlyRemovedBlueprintId = blueprint.metadata.itemId;
		ClearCache();
	}

	public override async Task<List<Blueprint>> GetBlueprints(int page, BlueprintSortInfo sortInfo)
	{
		return await LoadBlueprintsLocal(page, sortInfo);
	}

	private async Task<List<Blueprint>> LoadBlueprintsLocal(int page, BlueprintSortInfo sortInfo)
	{
		if (sortInfo.HasChanged)
		{
			ClearCache();
			sortInfo.HasChanged = false;
		}
		if (blueprintsCache.TryGetValue(page, out var value))
		{
			return value;
		}
		LocalBlueprints = await BlueprintsFolderLoader.GetBlueprints();
		if (SteamHelper.IsConnectedToSteam())
		{
			if (!PlayerPrefs.GetBool(PlayerPref.HasAutoSubscribedBlueprints09))
			{
				await WorkshopBlueprints.AutoSubscribe09(LocalBlueprints);
				PlayerPrefs.SetBool(PlayerPref.HasAutoSubscribedBlueprints09, value: true);
			}
			await LoadUserUploadedItemIds();
			await VerifyLocalBlueprints();
			await VerifyWorkshopBlueprints();
		}
		if (LocalBlueprints.Count == 0)
		{
			return LocalBlueprints;
		}
		int buildNumber = GameVersion.GetCurrent().buildNumber;
		FilterAndSortBlueprints(ref LocalBlueprints, sortInfo);
		blueprintsCache.Clear();
		List<Blueprint> list = new List<Blueprint>();
		int num = 1;
		foreach (Blueprint localBlueprint in LocalBlueprints)
		{
			if (localBlueprint.metadata.buildNumber <= buildNumber)
			{
				list.Add(localBlueprint);
				if (list.Count == 16)
				{
					blueprintsCache[num] = list.Copy();
					list.Clear();
					num++;
				}
			}
		}
		if (list.Count > 0)
		{
			blueprintsCache[num] = list.Copy();
		}
		return (blueprintsCache.Count > 0) ? blueprintsCache[page] : new List<Blueprint>();
	}

	private async Task LoadUserUploadedItemIds()
	{
		List<Item> obj = await SteamWorkshopBlueprint.GetUserPublishedItems();
		_publishedIdSet.Clear();
		foreach (Item item in obj)
		{
			_publishedIdSet.Add(item.Id);
		}
	}

	private async Task VerifyWorkshopBlueprints()
	{
		int currentBuildNumber = GameVersion.GetCurrent().buildNumber;
		workshopBlueprintsBySteamId.Clear();
		List<Blueprint> blueprints = new List<Blueprint>();
		int i = 1;
		while (true)
		{
			List<Item> itemsOnPage = await SteamWorkshopBlueprint.GetUserSubscribedBlueprintItems(i);
			foreach (Item item in itemsOnPage)
			{
				if (_recentlyRemovedBlueprintId != 0L && item.Id == _recentlyRemovedBlueprintId)
				{
					_recentlyRemovedBlueprintId = 0uL;
					continue;
				}
				Blueprint blueprint = await BlueprintParser.ParseItemToBlueprint(item);
				if (blueprint == null || string.IsNullOrWhiteSpace(blueprint.name) || !workshopBlueprintsBySteamId.TryAdd(blueprint.metadata.itemId, blueprint) || blueprint.metadata.buildNumber > currentBuildNumber)
				{
					continue;
				}
				Blueprint blueprint2 = LocalBlueprints.FirstOrDefault((Blueprint x) => x.metadata.itemId == blueprint.metadata.itemId);
				if (blueprint2 != null && blueprint2.metadata.blueprintVersion == blueprint.metadata.blueprintVersion)
				{
					if (blueprint2.metadata.isWorkshopReviewPending)
					{
						blueprint2.metadata.isWorkshopReviewPending = false;
						await blueprint2.UpdateMetadata();
					}
				}
				else
				{
					blueprints.Add(blueprint);
				}
			}
			if (itemsOnPage.Count < 50)
			{
				break;
			}
			i++;
		}
		List<Blueprint> unsubscribedLocal = new List<Blueprint>();
		foreach (Blueprint bp in LocalBlueprints)
		{
			if ((bp.metadata.blueprintType != BlueprintType.SavedFromWorkshop && bp.metadata.blueprintType != BlueprintType.UploadedToWorkshop) || workshopBlueprintsBySteamId.ContainsKey(bp.metadata.itemId))
			{
				continue;
			}
			if (!workshopBlueprintsBySteamId.ContainsKey(bp.metadata.itemId) && _publishedIdSet.Contains(bp.metadata.itemId))
			{
				await SteamHelper.SubscribeToItem(bp.metadata.itemId);
			}
			else if (bp.metadata.blueprintType == BlueprintType.UploadedToWorkshop)
			{
				bp.metadata.blueprintType = BlueprintType.SavedLocally;
				bp.metadata.isWorkshopReviewPending = false;
				bp.metadata.itemId = 0uL;
				bp.metadata.blueprintVersion = 0;
				try
				{
					await bp.UpdateMetadata();
				}
				catch (Exception ex)
				{
					Debug.LogError("Failed to update metadata for blueprint '" + bp.name + "': " + ex.Message);
					unsubscribedLocal.Add(bp);
				}
			}
			else
			{
				unsubscribedLocal.Add(bp);
			}
		}
		if (unsubscribedLocal.Count > 0)
		{
			foreach (Blueprint item2 in unsubscribedLocal)
			{
				BlueprintsFolderLoader.RemoveLocalBlueprint(item2);
			}
		}
		if (blueprints.Count == 0)
		{
			return;
		}
		foreach (Blueprint item3 in blueprints)
		{
			await SyncBlueprintFromSteam(item3);
		}
	}

	private async Task VerifyLocalBlueprints()
	{
		foreach (Blueprint localBlueprint in LocalBlueprints)
		{
			if (localBlueprint.metadata.blueprintType == BlueprintType.UploadedToWorkshop && !_publishedIdSet.Contains(localBlueprint.metadata.itemId))
			{
				localBlueprint.metadata.blueprintType = BlueprintType.SavedLocally;
				localBlueprint.metadata.isWorkshopReviewPending = false;
				localBlueprint.metadata.itemId = 0uL;
				localBlueprint.metadata.blueprintVersion = 0;
				await localBlueprint.UpdateMetadata();
			}
		}
	}

	private static async Task SyncBlueprintFromSteam(Blueprint blueprint)
	{
		_ = 6;
		try
		{
			Item workshopItem = await SteamWorkshopBlueprint.GetWorkshopItem(blueprint.metadata.itemId);
			if (workshopItem.Result != Result.OK)
			{
				return;
			}
			string workshopBlueprintFolder = workshopItem.Directory;
			blueprint = await BlueprintParser.ParseItemToBlueprint(workshopItem);
			string text = blueprint.name;
			char[] illegalCharacters = Path.GetInvalidFileNameChars();
			if (text.Any((char x) => illegalCharacters.Contains(x)))
			{
				text = string.Concat(text.Split(Path.GetInvalidFileNameChars()));
			}
			string blueprintFolder = BlueprintsFolderLoader.GetBlueprintFolder(text);
			if (Directory.Exists(blueprintFolder))
			{
				if (await SteamWorkshopBlueprint.ThisUserIsOwner(blueprint))
				{
					return;
				}
				blueprint.CleanCachedThumbnail();
				FileSystemHelper.DeleteDirectory(blueprintFolder);
			}
			await FileSystemHelper.MoveFolderToDirectory(workshopBlueprintFolder, blueprintFolder);
			BlueprintMetadata metadata = blueprint.metadata;
			metadata.blueprintType = ((await SteamWorkshopBlueprint.ThisUserIsOwner(blueprint)) ? BlueprintType.UploadedToWorkshop : BlueprintType.SavedFromWorkshop);
			blueprint.metadata.isWorkshopReviewPending = false;
			await blueprint.metadata.Serialize(Path.Combine(blueprintFolder, "Metadata.json"));
			string layoutPath = Path.Combine(blueprintFolder, "Layout.json");
			await BlueprintsFolderLoader.ApplyCompatibilityFixes(blueprint, CompatibilityFixScope.Both, layoutPath);
			LocalBlueprints.Add(blueprint);
		}
		catch (Exception ex)
		{
			Debug.LogError($"Error syncing blueprint {blueprint?.name} ID: {blueprint?.metadata?.itemId} " + "from Steam Workshop: " + ex.Message);
		}
	}

	public override int GetMaxPageNumber()
	{
		return blueprintsCache.Count;
	}

	private static void FilterAndSortBlueprints(ref List<Blueprint> blueprints, BlueprintSortInfo sortInfo)
	{
		List<string> buildingTypeActiveTags = sortInfo.GetBuildingTypeActiveTags();
		List<string> buildingSizeActiveTags = sortInfo.GetBuildingSizeActiveTags();
		List<string> businessTypeActiveTags = sortInfo.GetBusinessTypeActiveTags();
		List<Blueprint> blueprints2 = new List<Blueprint>();
		foreach (Blueprint blueprint in blueprints)
		{
			try
			{
				if ((buildingTypeActiveTags.Count != 0 && !buildingTypeActiveTags.Contains(blueprint.metadata.buildingType) && !buildingTypeActiveTags.Contains(blueprint.metadata.buildingType.GetIdWithoutType())) || (buildingSizeActiveTags.Count != 0 && !buildingSizeActiveTags.Contains(blueprint.metadata.buildingSizeInfo.ToString())))
				{
					continue;
				}
				string text = null;
				foreach (BlueprintDataElement otherDatum in blueprint.metadata.otherData)
				{
					if (otherDatum.dataElement == DataElement.BusinessTypeName)
					{
						text = otherDatum.value.GetIdWithoutType();
						break;
					}
				}
				if ((businessTypeActiveTags.Count == 0 || (text != null && businessTypeActiveTags.Contains(text))) && sortInfo.MatchesBuildVersion(blueprint.metadata.buildNumber))
				{
					if (string.IsNullOrEmpty(sortInfo.SearchQuery))
					{
						goto IL_0151;
					}
					string value = sortInfo.SearchQuery.ToLowerInvariant();
					if (blueprint.name.ToLowerInvariant().Contains(value))
					{
						goto IL_0151;
					}
				}
				goto end_IL_0032;
				IL_0151:
				blueprints2.Add(blueprint);
				end_IL_0032:;
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
				Debug.LogError("Blueprint crashed: " + blueprint.name);
			}
		}
		sortInfo.SortBlueprints(ref blueprints2);
		blueprints = blueprints2;
	}

	public async Task<bool> CanUpdateToWorkshop(Blueprint blueprint)
	{
		if (blueprint.metadata.blueprintType != BlueprintType.UploadedToWorkshop)
		{
			return false;
		}
		if (blueprint.metadata.isWorkshopReviewPending)
		{
			return false;
		}
		if (!(await SteamWorkshopBlueprint.ThisUserIsOwner(blueprint)))
		{
			return false;
		}
		if (!workshopBlueprintsBySteamId.TryGetValue(blueprint.metadata.itemId, out var value))
		{
			return false;
		}
		return blueprint.metadata.blueprintVersion > value.metadata.blueprintVersion;
	}
}
