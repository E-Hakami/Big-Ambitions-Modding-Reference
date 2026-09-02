// BigAmbitions.ModsInternal, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.ModsInternal.SteamModUploader
using System;
using System.IO;
using System.Threading.Tasks;
using BigAmbitions;
using BigAmbitions.ModsInternal;
using Helpers;
using Steamworks;
using Steamworks.Ugc;

internal static class SteamModUploader
{
	public static async Task<PublishResult> UploadNewModToWorkshop(ModInfo modInfo, Action<float> onProgress)
	{
		if (modInfo == null)
		{
			return new PublishResult
			{
				Result = Result.Fail
			};
		}
		if (!SteamHelper.IsConnectedToSteam())
		{
			return new PublishResult
			{
				Result = Result.ConnectFailed
			};
		}
		if (string.IsNullOrWhiteSpace(modInfo.modFolder) || !Directory.Exists(modInfo.modFolder) || (!string.IsNullOrWhiteSpace(modInfo.thumbnailUrl) && !File.Exists(modInfo.thumbnailUrl)))
		{
			return new PublishResult
			{
				Result = Result.FileNotFound
			};
		}
		if (modInfo.steamItemId != 0L)
		{
			return new PublishResult
			{
				Result = Result.DuplicateRequest
			};
		}
		string t = SteamModMetadataHandler.BuildModMetadataJson(modInfo);
		bool num = !string.IsNullOrWhiteSpace(modInfo.thumbnailUrl);
		string movedThumbnailPath = MoveThumbnailOutsideModFolder(modInfo);
		Editor editor = Editor.NewCommunityFile.WithTag("mod").WithTitle(string.IsNullOrWhiteSpace(modInfo.title) ? Path.GetFileName(modInfo.modFolder) : modInfo.title).WithDescription(modInfo.description)
			.WithContent(modInfo.modFolder)
			.WithMetaData(t)
			.WithPublicVisibility();
		if (num)
		{
			editor = editor.WithPreviewFile((!string.IsNullOrEmpty(movedThumbnailPath)) ? movedThumbnailPath : modInfo.thumbnailUrl);
		}
		PublishResult result;
		try
		{
			Progress<float> progress = new Progress<float>(onProgress);
			result = await editor.SubmitAsync(progress);
		}
		finally
		{
			RestoreMovedThumbnail(modInfo, movedThumbnailPath);
		}
		if (result.Result != Result.OK)
		{
			return result;
		}
		await SteamHelper.SubscribeToItem(result.FileId);
		ModManifest.Add(result.FileId);
		return result;
	}

	public static async Task<PublishResult> UpdateExistingModOnWorkshop(ModInfo modInfo, Action<float> onProgress)
	{
		if (modInfo == null)
		{
			return new PublishResult
			{
				Result = Result.Fail
			};
		}
		if (!SteamHelper.IsConnectedToSteam())
		{
			return new PublishResult
			{
				Result = Result.ConnectFailed
			};
		}
		if (string.IsNullOrWhiteSpace(modInfo.modFolder) || !Directory.Exists(modInfo.modFolder))
		{
			return new PublishResult
			{
				Result = Result.FileNotFound
			};
		}
		if (modInfo.steamItemId == 0L)
		{
			return new PublishResult
			{
				Result = Result.InvalidParam
			};
		}
		SteamId? steamId = await SteamHelper.GetOwnerIdByItemId(modInfo.steamItemId);
		if (!steamId.HasValue || (ulong)steamId.Value != (ulong)SteamClient.SteamId)
		{
			return new PublishResult
			{
				Result = Result.AccessDenied
			};
		}
		string t = SteamModMetadataHandler.BuildModMetadataJson(modInfo);
		bool num = !string.IsNullOrWhiteSpace(modInfo.thumbnailUrl);
		string movedThumbnailPath = MoveThumbnailOutsideModFolder(modInfo);
		Editor editor = new Editor(modInfo.steamItemId).WithTitle(string.IsNullOrWhiteSpace(modInfo.title) ? Path.GetFileName(modInfo.modFolder) : modInfo.title).WithDescription(modInfo.description).WithContent(modInfo.modFolder)
			.WithMetaData(t)
			.WithChangeLog(modInfo.changeLog)
			.WithTag("mod")
			.WithPublicVisibility();
		if (num)
		{
			editor = editor.WithPreviewFile((!string.IsNullOrEmpty(movedThumbnailPath)) ? movedThumbnailPath : modInfo.thumbnailUrl);
		}
		try
		{
			Progress<float> progress = new Progress<float>(onProgress);
			return await editor.SubmitAsync(progress);
		}
		finally
		{
			RestoreMovedThumbnail(modInfo, movedThumbnailPath);
		}
	}

	private static string MoveThumbnailOutsideModFolder(ModInfo modInfo)
	{
		if (string.IsNullOrWhiteSpace(modInfo.thumbnailUrl))
		{
			return string.Empty;
		}
		string fullPath = Path.GetFullPath(modInfo.thumbnailUrl);
		string fullPath2 = Path.GetFullPath(modInfo.modFolder);
		if (!fullPath.StartsWith(fullPath2, StringComparison.OrdinalIgnoreCase))
		{
			return string.Empty;
		}
		string text = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(fullPath) + "_workshop_upload_" + Guid.NewGuid().ToString("N") + Path.GetExtension(fullPath));
		if (File.Exists(text))
		{
			File.Delete(text);
		}
		File.Move(fullPath, text);
		return text;
	}

	private static void RestoreMovedThumbnail(ModInfo modInfo, string movedThumbnailPath)
	{
		if (!string.IsNullOrEmpty(movedThumbnailPath) && File.Exists(movedThumbnailPath))
		{
			File.Move(movedThumbnailPath, modInfo.thumbnailUrl);
		}
	}
}
