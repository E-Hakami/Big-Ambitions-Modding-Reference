using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BigAmbitions.SaveSystem;
using Blueprints;
using BlueprintsUI;
using Extensions;
using Helpers;
using Steamworks;
using Steamworks.Ugc;
using UnityEngine;

namespace Steam;

public static class SteamWorkshopBlueprint
{
	private const string BlueprintTag = "Blueprint";

	public static Action<Blueprint> onBlueprintUploaded;

	public static Action<Blueprint> onBlueprintRemoved;

	public static async Task<PublishResult> UploadBlueprintToWorkshop(Blueprint blueprint)
	{
		if (!SteamHelper.IsConnectedToSteam())
		{
			Debug.LogError("Not connected to Steam.");
			return new PublishResult
			{
				Result = Result.ConnectFailed
			};
		}
		string blueprintFolder = BlueprintsFolderLoader.GetBlueprintFolder(blueprint.name);
		if (!Directory.Exists(blueprintFolder))
		{
			return new PublishResult
			{
				Result = Result.FileNotFound
			};
		}
		string t = Path.Combine(blueprintFolder, "Thumbnail.png");
		string t2 = BlueprintParser.ParseBlueprintIntoItemMetadata(blueprint);
		string id = blueprint.metadata.GetDataElementValue(DataElement.BusinessTypeName) ?? "ba:businesstype_empty";
		return await Editor.NewCommunityFile.WithTitle(blueprint.name).WithPreviewFile(t).WithMetaData(t2)
			.WithContent(blueprintFolder)
			.WithTag("Blueprint")
			.WithTag(blueprint.metadata.buildingType.GetIdWithoutType())
			.WithTag(blueprint.metadata.buildingSizeInfo.ToString())
			.WithTag(id.GetIdWithoutType())
			.WithPublicVisibility()
			.SubmitAsync()
			.TimeoutAfter(SteamHelper.SteamResultTimeout);
	}

	public static async Task<PublishResult> UpdateBlueprintToWorkshop(Blueprint blueprint)
	{
		if (!SteamHelper.IsConnectedToSteam())
		{
			Debug.LogError("Not connected to Steam.");
			return new PublishResult
			{
				Result = Result.ConnectFailed
			};
		}
		if (!(await ThisUserIsOwner(blueprint)))
		{
			return new PublishResult
			{
				Result = Result.AccessDenied
			};
		}
		string blueprintFolder = BlueprintsFolderLoader.GetBlueprintFolder(blueprint.name);
		if (!Directory.Exists(blueprintFolder))
		{
			return new PublishResult
			{
				Result = Result.FileNotFound
			};
		}
		string t = Path.Combine(blueprintFolder, "Thumbnail.png");
		string t2 = BlueprintParser.ParseBlueprintIntoItemMetadata(blueprint);
		string id = blueprint.metadata.GetDataElementValue(DataElement.BusinessTypeName) ?? "ba:businesstype_empty";
		return await new Editor(blueprint.metadata.itemId).WithTitle(blueprint.name).WithPreviewFile(t).WithMetaData(t2)
			.WithContent(blueprintFolder)
			.WithTag("Blueprint")
			.WithTag(blueprint.metadata.buildingType.GetIdWithoutType())
			.WithTag(blueprint.metadata.buildingSizeInfo.ToString())
			.WithTag(id.GetIdWithoutType())
			.WithPublicVisibility()
			.SubmitAsync()
			.TimeoutAfter(SteamHelper.SteamResultTimeout);
	}

	public static async Task<bool> RemoveBlueprintFromWorkshop(Blueprint blueprint)
	{
		if (!(await ThisUserIsOwner(blueprint)))
		{
			return false;
		}
		Item? item = await SteamUGC.QueryFileAsync(blueprint.metadata.itemId).TimeoutAfter(SteamHelper.SteamResultTimeout);
		if (item.HasValue && !(await item.Value.Unsubscribe()))
		{
			Debug.LogError($"Failed to unsubscribe from workshop item {blueprint.metadata.itemId}");
			return false;
		}
		return await SteamUGC.DeleteFileAsync(blueprint.metadata.itemId).TimeoutAfter(SteamHelper.SteamResultTimeout);
	}

	public static async Task<bool> ThisUserIsOwner(Blueprint blueprint)
	{
		if (!SteamHelper.IsConnectedToSteam())
		{
			return false;
		}
		if (blueprint.ownerId != 0L)
		{
			return blueprint.ownerId == (ulong)SteamClient.SteamId;
		}
		if (blueprint.metadata.itemId == 0L)
		{
			return false;
		}
		return (ulong?)(await SteamHelper.GetOwnerIdByItemId(blueprint.metadata.itemId)) == (ulong)SteamClient.SteamId;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		onBlueprintUploaded = null;
		onBlueprintRemoved = null;
	}

	public static async Task<List<Item>> GetSortedItemsOnPage(int page, BlueprintSortInfo sortInfo)
	{
		if (!SteamHelper.IsConnectedToSteam())
		{
			Debug.LogError("Not connected to Steam.");
			return null;
		}
		ResultPage? resultPage = await GetQueryForSortInfo(sortInfo).WithMetadata(b: true).GetPageAsync(page).TimeoutAfter(SteamHelper.SteamResultTimeout);
		return (!resultPage.HasValue) ? new List<Item>() : FilterBlueprintItems(resultPage.Value.Entries);
	}

	public static async Task DownloadBlueprint(Blueprint blueprint)
	{
		if (!SteamHelper.IsConnectedToSteam())
		{
			Debug.LogError("Not connected to Steam.");
			return;
		}
		Item item = await GetWorkshopItem(blueprint.metadata.itemId);
		if (item.Id == 0uL)
		{
			Debug.LogError("Couldn't find installed blueprint '" + blueprint.name + "'");
			return;
		}
		string directory = item.Directory;
		string text = blueprint.name;
		if (HasInvalidFileNameCharacter(text))
		{
			text = string.Concat(text.Split(Path.GetInvalidFileNameChars()));
		}
		string blueprintFolder = BlueprintsFolderLoader.GetBlueprintFolder(text);
		if (Directory.Exists(blueprintFolder))
		{
			blueprintFolder += $" (ID {blueprint.metadata.itemId})";
		}
		await FileSystemHelper.MoveFolderToDirectory(directory, blueprintFolder);
		if (BlueprintsPanel.cancellationTokenSource.Token.IsCancellationRequested)
		{
			BlueprintsPanel.OnBlueprintsLoadingCancelled();
			return;
		}
		blueprint.metadata.blueprintType = BlueprintType.SavedFromWorkshop;
		await blueprint.metadata.Serialize(Path.Combine(blueprintFolder, "Metadata.json"));
		if (BlueprintsPanel.cancellationTokenSource.Token.IsCancellationRequested)
		{
			BlueprintsPanel.OnBlueprintsLoadingCancelled();
		}
	}

	public static async Task<Item> GetWorkshopItem(ulong itemId)
	{
		return await SteamHelper.DownloadAndGetWorkshopItem(itemId, "Blueprint");
	}

	public static async Task<List<Item>> GetUserPublishedItems(SteamId steamId = default(SteamId))
	{
		return FilterBlueprintItems(await SteamHelper.GetUserPublishedItems("Blueprint", null, steamId));
	}

	public static async Task<List<Item>> GetUserSubscribedBlueprintItems(int page)
	{
		return FilterBlueprintItems(await SteamHelper.GetUserSubscribedItems(page, "Blueprint"));
	}

	private static Query GetQueryForSortInfo(BlueprintSortInfo sortInfo)
	{
		Query query = default(Query);
		if (!string.IsNullOrEmpty(sortInfo.SearchQuery))
		{
			query = query.WhereSearchText(sortInfo.SearchQuery);
		}
		query = query.WithTag("Blueprint").MatchAllTags();
		query = RequireTagGroup(query, sortInfo.GetBuildingSizeActiveTags());
		query = RequireTagGroup(query, sortInfo.GetBuildingTypeActiveTags());
		query = RequireTagGroup(query, sortInfo.GetBusinessTypeActiveTags());
		return ApplyRanking(query, sortInfo.SortByOption);
	}

	private static Query RequireTagGroup(Query query, List<string> tags)
	{
		if (tags == null || tags.Count == 0)
		{
			return query;
		}
		int num = 0;
		foreach (string tag in tags)
		{
			if (!string.IsNullOrEmpty(tag))
			{
				num++;
			}
		}
		if (num == 0)
		{
			return query;
		}
		string[] array = new string[num];
		int num2 = 0;
		foreach (string tag2 in tags)
		{
			if (!string.IsNullOrEmpty(tag2))
			{
				array[num2] = tag2;
				num2++;
			}
		}
		return query.WithTagGroup(array);
	}

	private static Query ApplyRanking(Query query, SortByOption sortBy)
	{
		return sortBy switch
		{
			SortByOption.Popularity => query.RankedByTotalUniqueSubscriptions(), 
			SortByOption.UploadDate => query.RankedByPublicationDate(), 
			SortByOption.Rating => query.SortByVoteScore(), 
			_ => query, 
		};
	}

	private static List<Item> FilterBlueprintItems(IEnumerable<Item> items)
	{
		List<Item> list = new List<Item>();
		foreach (Item item in items)
		{
			if (IsBlueprintItem(item))
			{
				list.Add(item);
			}
		}
		return list;
	}

	private static bool IsBlueprintItem(Item item)
	{
		if (!item.HasTag("Blueprint"))
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(item.Metadata))
		{
			return false;
		}
		if (item.Metadata.Contains($"{DataElement.BuildingType}:"))
		{
			return item.Metadata.Contains($"{DataElement.BuildingSize}:");
		}
		return false;
	}

	private static bool HasInvalidFileNameCharacter(string text)
	{
		char[] invalidFileNameChars = Path.GetInvalidFileNameChars();
		for (int i = 0; i < text.Length; i++)
		{
			for (int j = 0; j < invalidFileNameChars.Length; j++)
			{
				if (text[i] == invalidFileNameChars[j])
				{
					return true;
				}
			}
		}
		return false;
	}
}
