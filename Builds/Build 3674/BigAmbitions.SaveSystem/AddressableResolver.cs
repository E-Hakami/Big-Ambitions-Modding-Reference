using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace BigAmbitions.SaveSystem;

public static class AddressableResolver
{
	private static readonly string[] CachedAddressableFolders = new string[2] { "Prefabs/", "ItemIcons/" };

	private static readonly Dictionary<string, string> CachedAddressLookups = new Dictionary<string, string>(StringComparer.Ordinal);

	public static void Init()
	{
		CachedAddressLookups.Clear();
		string[] cachedAddressableFolders = CachedAddressableFolders;
		for (int i = 0; i < cachedAddressableFolders.Length; i++)
		{
			GenerateLookup(cachedAddressableFolders[i]);
		}
	}

	public static AsyncOperationHandle<TObject> LoadAssetAsync<TObject>(string address)
	{
		return Addressables.LoadAssetAsync<TObject>(CachedAddressLookups.GetValueOrDefault(address.ToLowerInvariant(), address));
	}

	public static bool IsValidAddressableKey(string key)
	{
		if (!string.IsNullOrEmpty(key))
		{
			return CachedAddressLookups.ContainsKey(key.ToLowerInvariant());
		}
		return false;
	}

	private static void GenerateLookup(string addressableFolder)
	{
		foreach (IResourceLocator resourceLocator in Addressables.ResourceLocators)
		{
			foreach (object key in resourceLocator.Keys)
			{
				if (key is string text && text.StartsWith(addressableFolder, StringComparison.OrdinalIgnoreCase))
				{
					CachedAddressLookups[text.ToLowerInvariant()] = text;
				}
			}
		}
	}
}
