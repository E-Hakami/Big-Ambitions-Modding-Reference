using System;
using System.Collections.Generic;
using BAModAPI.Services;
using BigAmbitions.Items;
using BigAmbitions.SaveSystem;
using UnityEngine;
using UnityEngine.Pool;

namespace Helpers;

public static class PrefabHelper
{
	private static readonly Dictionary<string, ObjectPool<GameObject>> VisualsPool = new Dictionary<string, ObjectPool<GameObject>>();

	private static readonly Dictionary<string, ItemController> ItemControllerCache = new Dictionary<string, ItemController>();

	private static readonly Dictionary<string, UnityEngine.Object> PrefabCache = new Dictionary<string, UnityEngine.Object>();

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		PrefabCache.Clear();
		ItemControllerCache.Clear();
		ClearVisualPools();
	}

	private static UnityEngine.Object LoadPrefab(string path)
	{
		if (PrefabCache.TryGetValue(path, out var value))
		{
			if (value != null)
			{
				return value;
			}
			PrefabCache.Remove(path);
		}
		UnityEngine.Object obj = null;
		if (AddressableResolver.IsValidAddressableKey(path))
		{
			obj = AddressableResolver.LoadAssetAsync<GameObject>(path).WaitForCompletion();
		}
		if (obj == null)
		{
			int num = path.LastIndexOf('/');
			string fileName = ((num >= 0) ? path.Substring(num + 1) : path);
			obj = AssetService.FindAssetInAnyBundleByFileName<GameObject>(path);
			if (obj == null)
			{
				obj = AssetService.FindAssetInAnyBundleByFileName<GameObject>(fileName);
			}
		}
		if (obj != null)
		{
			PrefabCache.Add(path, obj);
		}
		return obj;
	}

	public static GameObject LoadPrefabAssetByName(string prefabName)
	{
		return (GameObject)LoadPrefab("Prefabs/" + prefabName + ".prefab");
	}

	public static ItemController LoadItemControllerFromPrefab(string itemName)
	{
		if (string.IsNullOrEmpty(itemName))
		{
			return null;
		}
		itemName = itemName.GetIdWithoutType();
		if (ItemControllerCache.TryGetValue(itemName, out var value))
		{
			if (value != null)
			{
				return value;
			}
			ItemControllerCache.Remove(itemName);
		}
		value = ((GameObject)LoadPrefab("Prefabs/" + itemName + ".prefab")).GetComponent<ItemController>();
		ItemControllerCache.Add(itemName, value);
		return value;
	}

	public static ItemController CreatePrefabItem(string itemName, Transform parent = null)
	{
		ItemController itemController = CreatePrefab<ItemController>(itemName.GetIdWithoutType(), parent);
		itemController.itemName = itemName;
		if (ItemsGetter.IsModItem(itemName))
		{
			AssetService.RemapShaders(itemController.gameObject);
		}
		return itemController;
	}

	public static void OnGameUnloaded()
	{
		PrefabCache.Clear();
		ItemControllerCache.Clear();
		ClearVisualPools();
	}

	private static void ClearVisualPools()
	{
		foreach (KeyValuePair<string, ObjectPool<GameObject>> item in VisualsPool)
		{
			item.Value.Dispose();
		}
		VisualsPool.Clear();
	}

	public static void ReturnVisualToPool(GameObject visuals)
	{
		if (VisualsPool.TryGetValue(visuals.name, out var value))
		{
			value.Release(visuals);
		}
	}

	public static GameObject CreateVisualForItemNamePooled(string itemName)
	{
		string itemNameId = itemName.GetIdWithoutType();
		if (!VisualsPool.ContainsKey(itemNameId))
		{
			VisualsPool.Add(itemNameId, new ObjectPool<GameObject>(delegate
			{
				GameObject gameObject2 = (GameObject)LoadPrefab("Prefabs/Visuals/" + itemNameId + ".prefab");
				if (gameObject2 == null)
				{
					gameObject2 = (GameObject)LoadPrefab("Prefabs/Visuals/CardboardBox.prefab");
				}
				return UnityEngine.Object.Instantiate(gameObject2);
			}, delegate(GameObject obj)
			{
				obj.transform.rotation = Quaternion.identity;
				obj.transform.SetParent(InstanceBehavior<BuildingManager>.Instance?.visualsContainer);
				obj.SetActive(value: true);
			}, delegate(GameObject obj)
			{
				obj.SetActive(value: false);
				obj.transform.SetParent(InstanceBehavior<BuildingManager>.Instance?.visualsContainer);
				obj.transform.eulerAngles = Vector3.zero;
			}));
		}
		GameObject gameObject = VisualsPool[itemNameId].Get();
		gameObject.name = itemNameId;
		return gameObject;
	}

	public static GameObject CreatePrefab(string prefabName, Transform parent = null)
	{
		GameObject obj = (GameObject)LoadPrefab("Prefabs/" + prefabName + ".prefab");
		if (obj == null)
		{
			throw new Exception("Prefab wasn't found: " + prefabName);
		}
		return UnityEngine.Object.Instantiate(obj, parent);
	}

	public static T CreatePrefab<T>(string prefabName, Transform parent = null) where T : UnityEngine.Object
	{
		UnityEngine.Object obj = LoadPrefab("Prefabs/" + prefabName + ".prefab");
		if (obj == null)
		{
			throw new Exception("Prefab wasn't found: " + prefabName);
		}
		GameObject gameObject = ((!(parent != null)) ? ((GameObject)UnityEngine.Object.Instantiate(obj)) : ((GameObject)UnityEngine.Object.Instantiate(obj, parent.position, parent.rotation, parent)));
		return gameObject.GetComponent<T>();
	}

	public static T CreatePrefab<T>(string prefabName, Vector3 position, Quaternion rotation, Transform parent = null) where T : UnityEngine.Object
	{
		UnityEngine.Object obj = LoadPrefab("Prefabs/" + prefabName + ".prefab");
		if (obj == null)
		{
			throw new Exception("Prefab wasn't found: " + prefabName);
		}
		return ((GameObject)UnityEngine.Object.Instantiate(obj, position, rotation, parent)).GetComponent<T>();
	}
}
