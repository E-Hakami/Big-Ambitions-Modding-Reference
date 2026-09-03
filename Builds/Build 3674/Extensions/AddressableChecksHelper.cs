using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;

namespace Extensions;

public static class AddressableChecksHelper
{
	public static bool IsValidAddressableKey(string key)
	{
		foreach (IResourceLocator resourceLocator in Addressables.ResourceLocators)
		{
			if (resourceLocator.Locate(key, typeof(Sprite), out var locations) && locations.Count > 0)
			{
				return true;
			}
		}
		return false;
	}
}
