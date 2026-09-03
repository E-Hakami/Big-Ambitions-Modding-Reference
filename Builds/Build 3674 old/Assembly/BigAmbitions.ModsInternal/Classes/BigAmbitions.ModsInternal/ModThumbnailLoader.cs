// BigAmbitions.ModsInternal, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.ModsInternal.ModThumbnailLoader
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BigAmbitions;
using UnityEngine;
using UnityEngine.Networking;

public static class ModThumbnailLoader
{
	private static readonly SemaphoreSlim ThumbnailLoadSemaphore = new SemaphoreSlim(8, 8);

	private static readonly object ThumbnailCacheLock = new object();

	private static readonly Dictionary<ulong, Sprite> ThumbnailSpriteCache = new Dictionary<ulong, Sprite>();

	private static readonly Dictionary<ulong, Task<Sprite>> ThumbnailInFlightLoads = new Dictionary<ulong, Task<Sprite>>();

	public static void ClearCache()
	{
		lock (ThumbnailCacheLock)
		{
			ThumbnailSpriteCache.Clear();
			ThumbnailInFlightLoads.Clear();
		}
	}

	public static void LoadThumbnailAsync(ModInfo modInfo, Action<ModInfo, Sprite> onLoaded)
	{
		if (modInfo == null)
		{
			return;
		}
		if (modInfo.thumbnail != null)
		{
			onLoaded?.Invoke(modInfo, modInfo.thumbnail);
		}
		else
		{
			if (modInfo.steamItemId == 0L)
			{
				return;
			}
			Task<Sprite> value2;
			lock (ThumbnailCacheLock)
			{
				if (ThumbnailSpriteCache.TryGetValue(modInfo.steamItemId, out var value) && value != null)
				{
					modInfo.thumbnail = value;
					onLoaded?.Invoke(modInfo, value);
					return;
				}
				if (!ThumbnailInFlightLoads.TryGetValue(modInfo.steamItemId, out value2))
				{
					value2 = LoadThumbnailSpriteInternal(modInfo);
					ThumbnailInFlightLoads.Add(modInfo.steamItemId, value2);
				}
			}
			InvokeCallbackWhenReady(modInfo, value2, onLoaded);
		}
	}

	private static async Task InvokeCallbackWhenReady(ModInfo modInfo, Task<Sprite> loadTask, Action<ModInfo, Sprite> onLoaded)
	{
		Sprite sprite;
		try
		{
			sprite = await loadTask;
		}
		catch
		{
			sprite = null;
		}
		if (!(sprite == null))
		{
			modInfo.thumbnail = sprite;
			onLoaded?.Invoke(modInfo, sprite);
		}
	}

	private static async Task<Sprite> LoadThumbnailSpriteInternal(ModInfo modInfo)
	{
		await ThumbnailLoadSemaphore.WaitAsync();
		try
		{
			_ = 1;
			Sprite sprite;
			try
			{
				sprite = await GetOrCreateThumbnailSprite(modInfo);
			}
			catch
			{
				sprite = null;
			}
			if (sprite == null)
			{
				return null;
			}
			lock (ThumbnailCacheLock)
			{
				ThumbnailSpriteCache[modInfo.steamItemId] = sprite;
				ThumbnailInFlightLoads.Remove(modInfo.steamItemId);
			}
			return sprite;
		}
		finally
		{
			ThumbnailLoadSemaphore.Release();
			lock (ThumbnailCacheLock)
			{
				if (ThumbnailInFlightLoads.TryGetValue(modInfo.steamItemId, out var value) && value.IsCompleted)
				{
					ThumbnailInFlightLoads.Remove(modInfo.steamItemId);
				}
			}
		}
	}

	private static async Task<Sprite> GetOrCreateThumbnailSprite(ModInfo modInfo)
	{
		if (modInfo == null)
		{
			return null;
		}
		if (string.IsNullOrWhiteSpace(modInfo.thumbnailUrl))
		{
			return null;
		}
		byte[] array = await DownloadBytes(modInfo.thumbnailUrl);
		if (array == null || array.Length == 0)
		{
			return null;
		}
		return CreateSpriteFromPngBytes(array);
	}

	private static Sprite CreateSpriteFromPngBytes(byte[] pngBytes)
	{
		Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
		if (!texture2D.LoadImage(pngBytes, markNonReadable: false))
		{
			UnityEngine.Object.Destroy(texture2D);
			return null;
		}
		Rect rect = new Rect(0f, 0f, texture2D.width, texture2D.height);
		Vector2 pivot = new Vector2(0.5f, 0.5f);
		return Sprite.Create(texture2D, rect, pivot, 100f);
	}

	private static async Task<byte[]> DownloadBytes(string url)
	{
		using UnityWebRequest request = UnityWebRequest.Get(url);
		request.downloadHandler = new DownloadHandlerBuffer();
		UnityWebRequestAsyncOperation operation = request.SendWebRequest();
		while (!operation.isDone)
		{
			await Task.Yield();
		}
		return (request.result != UnityWebRequest.Result.Success) ? null : request.downloadHandler.data;
	}
}
