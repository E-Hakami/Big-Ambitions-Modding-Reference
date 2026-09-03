using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BlueprintsUI;
using Helpers;
using Steam;
using Steamworks;
using Steamworks.Ugc;
using UI.Notification;
using UnityEngine;

namespace Blueprints;

public static class WorkshopBlueprints
{
	public static async Task UploadBlueprintToWorkshop(Blueprint blueprint)
	{
		PublishResult publishResult = await SteamWorkshopBlueprint.UploadBlueprintToWorkshop(blueprint);
		if (publishResult.Result == Result.OK)
		{
			blueprint.metadata.itemId = publishResult.FileId;
			blueprint.metadata.blueprintType = BlueprintType.UploadedToWorkshop;
			blueprint.releaseDate = DateTime.Now;
			blueprint.metadata.isWorkshopReviewPending = true;
			await blueprint.UpdateMetadata();
			SteamWorkshopBlueprint.onBlueprintUploaded?.Invoke(blueprint);
			Notifications.Show(NotificationType.Success, "blueprints_ui_workshop_item_uploaded", null, 4f, null, null, notificationSound: true, trackOnSaveGame: false);
			Item? item = await SteamUGC.QueryFileAsync(publishResult.FileId);
			if (item.HasValue)
			{
				await item.Value.Subscribe();
			}
			SteamFriends.OpenWebOverlay($"steam://url/CommunityFilePage/{publishResult.FileId}");
		}
		else if (publishResult.Result == Result.FileNotFound)
		{
			Notifications.ShowError("blueprints_ui_workshop_item_upload_failed_file_not_found", null, trackOnSaveGame: false);
		}
		else
		{
			Notifications.ShowError("blueprints_ui_workshop_item_upload_failed", null, trackOnSaveGame: false);
			Debug.LogError("Error: " + publishResult.Result);
		}
	}

	public static async Task UpdateBlueprintToWorkshop(Blueprint blueprint)
	{
		PublishResult publishResult = await SteamWorkshopBlueprint.UpdateBlueprintToWorkshop(blueprint);
		if (publishResult.Result == Result.OK)
		{
			blueprint.releaseDate = DateTime.Now;
			blueprint.metadata.isWorkshopReviewPending = true;
			await blueprint.UpdateMetadata();
			if (BlueprintsListUI.LibraryController.workshopBlueprintsBySteamId.TryGetValue(blueprint.metadata.itemId, out var value))
			{
				value.metadata.blueprintVersion = blueprint.metadata.blueprintVersion;
			}
			else
			{
				Debug.Log($"Could not update workshop blueprint cache for itemId {blueprint.metadata.itemId}");
			}
			Notifications.Show(NotificationType.Success, "blueprints_ui_workshop_item_updated", null, 4f, null, null, notificationSound: true, trackOnSaveGame: false);
			SteamHelper.OpenSteamWithWorkshopItem(publishResult.FileId);
		}
		else if (publishResult.Result == Result.FileNotFound)
		{
			Notifications.ShowError("blueprints_ui_workshop_item_upload_failed_file_not_found", null, trackOnSaveGame: false);
		}
		else
		{
			Notifications.ShowError("blueprints_ui_workshop_item_upload_failed", null, trackOnSaveGame: false);
			Debug.LogError("Error: " + publishResult.Result);
		}
	}

	public static async Task RemoveBlueprintFromWorkshop(Blueprint blueprint)
	{
		if (await SteamWorkshopBlueprint.RemoveBlueprintFromWorkshop(blueprint))
		{
			blueprint.metadata.itemId = 0uL;
			blueprint.metadata.blueprintType = BlueprintType.SavedLocally;
			blueprint.releaseDate = DateTime.MinValue;
			await blueprint.UpdateMetadata();
			SteamWorkshopBlueprint.onBlueprintRemoved?.Invoke(blueprint);
			Notifications.Show(NotificationType.Success, "blueprints_ui_workshop_item_removed", null, 4f, null, null, notificationSound: true, trackOnSaveGame: false);
		}
		else
		{
			Notifications.ShowError("blueprints_ui_workshop_item_remove_failed", null, trackOnSaveGame: false);
		}
	}

	public static async Task AddToYourLibrary(Blueprint blueprint)
	{
		await SteamWorkshopBlueprint.DownloadBlueprint(blueprint);
		Item? item = await SteamUGC.QueryFileAsync(blueprint.metadata.itemId);
		if (item.HasValue)
		{
			await item.Value.Subscribe();
			Notifications.Show(NotificationType.Success, "blueprints_ui_workshop_item_added_to_library", null, 4f, null, null, notificationSound: true, trackOnSaveGame: false);
		}
		else
		{
			Notifications.ShowError("blueprints_ui_workshop_item_add_to_library_failed", null, trackOnSaveGame: false);
		}
	}

	public static async Task AutoSubscribe09(List<Blueprint> blueprints)
	{
		foreach (Blueprint blueprint in blueprints)
		{
			BlueprintType blueprintType = blueprint.metadata.blueprintType;
			if ((blueprintType == BlueprintType.UploadedToWorkshop || blueprintType == BlueprintType.SavedFromWorkshop) && blueprint.metadata.itemId != 0L)
			{
				Item? item = await SteamUGC.QueryFileAsync(blueprint.metadata.itemId);
				if (item.HasValue && !item.GetValueOrDefault().IsSubscribed && !(await item.Value.Subscribe()))
				{
					Debug.LogError($"Failed to auto-subscribe to workshop item {blueprint.metadata.itemId}");
				}
			}
		}
	}
}
