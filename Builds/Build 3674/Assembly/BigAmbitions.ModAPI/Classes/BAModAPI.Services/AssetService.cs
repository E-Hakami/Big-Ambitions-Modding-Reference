// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BAModAPI.Services.AssetService
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BAModAPI.Services;
using UnityEngine;

public static class AssetService
{
	private static readonly Dictionary<string, AssetBundle> BundlesByKey = new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, string> BundlePathByKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, Dictionary<string, UnityEngine.Object>> AssetCache = new Dictionary<string, Dictionary<string, UnityEngine.Object>>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, Shader> ShaderRegistry = new Dictionary<string, Shader>(StringComparer.OrdinalIgnoreCase);

	private static bool _shaderRegistryReady;

	private static string NormalizeKey(string key)
	{
		if (!string.IsNullOrWhiteSpace(key))
		{
			return key.Replace('\\', '/');
		}
		return string.Empty;
	}

	private static string MakeModBundleKey(string modId, string bundleKey)
	{
		return NormalizeKey(Path.Combine(modId ?? string.Empty, bundleKey ?? string.Empty));
	}

	public static void RegisterBundle(string modId, string bundleKey, string absolutePath)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(bundleKey))
			{
				throw new ArgumentException("bundleKey");
			}
			if (string.IsNullOrWhiteSpace(absolutePath))
			{
				throw new ArgumentException("absolutePath");
			}
			BundlePathByKey[MakeModBundleKey(modId, NormalizeKey(bundleKey))] = absolutePath;
		}
		catch (Exception ex)
		{
			Debug.LogError($"AssetService.RegisterBundle failed. modId='{modId}', bundleKey='{bundleKey}', absolutePath='{absolutePath}'. Exception: {ex}");
		}
	}

	public static void UnloadAllBundles(bool unloadAllLoadedObjects)
	{
		try
		{
			foreach (AssetBundle value in BundlesByKey.Values)
			{
				if (value != null)
				{
					value.Unload(unloadAllLoadedObjects);
				}
			}
			BundlesByKey.Clear();
			BundlePathByKey.Clear();
			AssetCache.Clear();
		}
		catch (Exception arg)
		{
			Debug.LogError($"AssetService.UnloadAllBundles failed. Exception: {arg}");
		}
	}

	public static void UnloadBundle(string bundleKey, bool unloadAllLoadedObjects)
	{
		try
		{
			bundleKey = NormalizeKey(bundleKey);
			if (BundlesByKey.TryGetValue(bundleKey, out var value) && value != null)
			{
				value.Unload(unloadAllLoadedObjects);
			}
			BundlesByKey.Remove(bundleKey);
			BundlePathByKey.Remove(bundleKey);
			AssetCache.Remove(bundleKey);
		}
		catch (Exception arg)
		{
			Debug.LogError($"AssetService.UnloadBundle failed. bundleKey='{bundleKey}'. Exception: {arg}");
		}
	}

	public static bool IsBundleLoaded(string modId, string bundleKey)
	{
		try
		{
			return BundlesByKey.ContainsKey(MakeModBundleKey(modId, bundleKey));
		}
		catch (Exception arg)
		{
			Debug.LogError($"AssetService.IsBundleLoaded failed. modId='{modId}', bundleKey='{bundleKey}'. Exception: {arg}");
			return false;
		}
	}

	public static AssetBundle GetBundle(string modId, string bundleKey, bool autoLoadIfRegistered = true)
	{
		try
		{
			string text = MakeModBundleKey(modId, bundleKey);
			if (BundlesByKey.TryGetValue(text, out var value) && value != null)
			{
				return value;
			}
			if (!autoLoadIfRegistered)
			{
				return null;
			}
			if (!BundlePathByKey.TryGetValue(text, out var value2))
			{
				Debug.LogError("Bundle not registered. Requested modId='" + modId + "', bundleKey='" + bundleKey + "', combinedKey='" + text + "'. Registered keys: " + string.Join(", ", BundlePathByKey.Keys));
				return null;
			}
			return LoadBundle(text, value2);
		}
		catch (Exception ex)
		{
			Debug.LogError($"AssetService.GetBundle failed. modId='{modId}', bundleKey='{bundleKey}', autoLoadIfRegistered='{autoLoadIfRegistered}'. Exception: {ex}");
			return null;
		}
	}

	private static AssetBundle LoadBundle(string modBundleKey, string absolutePath)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(modBundleKey))
			{
				throw new ArgumentException("modBundleKey");
			}
			if (string.IsNullOrWhiteSpace(absolutePath))
			{
				throw new ArgumentException("absolutePath");
			}
			if (!File.Exists(absolutePath))
			{
				throw new FileNotFoundException("AssetBundle file not found", absolutePath);
			}
			modBundleKey = NormalizeKey(modBundleKey);
			if (BundlesByKey.TryGetValue(modBundleKey, out var value) && value != null)
			{
				return value;
			}
			AssetBundle assetBundle = AssetBundle.LoadFromFile(absolutePath);
			if (assetBundle == null)
			{
				throw new IOException("AssetBundle.LoadFromFile failed: " + absolutePath);
			}
			BundlesByKey[modBundleKey] = assetBundle;
			BundlePathByKey[modBundleKey] = absolutePath;
			if (!AssetCache.ContainsKey(modBundleKey))
			{
				AssetCache[modBundleKey] = new Dictionary<string, UnityEngine.Object>(StringComparer.OrdinalIgnoreCase);
			}
			return assetBundle;
		}
		catch (Exception arg)
		{
			Debug.LogError($"AssetService.LoadBundle failed. modBundleKey='{modBundleKey}', absolutePath='{absolutePath}'. Exception: {arg}");
			return null;
		}
	}

	private static async Task<AssetBundle> LoadBundleAsync(string modBundleKey, string absolutePath, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			ServiceHelper.EnsureInitialized();
			if (string.IsNullOrWhiteSpace(modBundleKey))
			{
				throw new ArgumentException("modBundleKey");
			}
			if (string.IsNullOrWhiteSpace(absolutePath))
			{
				throw new ArgumentException("absolutePath");
			}
			if (!File.Exists(absolutePath))
			{
				throw new FileNotFoundException("AssetBundle file not found", absolutePath);
			}
			modBundleKey = NormalizeKey(modBundleKey);
			if (BundlesByKey.TryGetValue(modBundleKey, out var value) && value != null)
			{
				return value;
			}
			AssetBundle assetBundle = await ServiceHelper.RunOnMainThreadAsync(async delegate
			{
				ct.ThrowIfCancellationRequested();
				AssetBundleCreateRequest request = AssetBundle.LoadFromFileAsync(absolutePath);
				return await ServiceHelper.AwaitAsyncOperation(request, () => request.assetBundle, ct).ConfigureAwait(continueOnCapturedContext: false);
			}).ConfigureAwait(continueOnCapturedContext: false);
			if (assetBundle == null)
			{
				throw new IOException("AssetBundle.LoadFromFileAsync failed: " + absolutePath);
			}
			BundlesByKey[modBundleKey] = assetBundle;
			BundlePathByKey[modBundleKey] = absolutePath;
			if (!AssetCache.ContainsKey(modBundleKey))
			{
				AssetCache[modBundleKey] = new Dictionary<string, UnityEngine.Object>(StringComparer.OrdinalIgnoreCase);
			}
			return assetBundle;
		}
		catch (OperationCanceledException)
		{
			return null;
		}
		catch (Exception arg)
		{
			Debug.LogError($"AssetService.LoadBundleAsync failed. modBundleKey='{modBundleKey}', absolutePath='{absolutePath}'. Exception: {arg}");
			return null;
		}
	}

	public static string[] GetAllAssetNames(string modId, string bundleKey)
	{
		try
		{
			AssetBundle bundle = GetBundle(modId, bundleKey);
			return (bundle == null) ? Array.Empty<string>() : bundle.GetAllAssetNames();
		}
		catch (Exception arg)
		{
			Debug.LogError($"AssetService.GetAllAssetNames failed. modId='{modId}', bundleKey='{bundleKey}'. Exception: {arg}");
			return Array.Empty<string>();
		}
	}

	public static bool HasAssetCached(string modId, string bundleKey, string assetName)
	{
		try
		{
			string key = MakeModBundleKey(modId, bundleKey);
			Dictionary<string, UnityEngine.Object> value;
			UnityEngine.Object value2;
			return AssetCache.TryGetValue(key, out value) && value.TryGetValue(assetName, out value2) && value2 != null;
		}
		catch (Exception ex)
		{
			Debug.LogError($"AssetService.HasAssetCached failed. modId='{modId}', bundleKey='{bundleKey}', assetName='{assetName}'. Exception: {ex}");
			return false;
		}
	}

	private static T LoadAsset<T>(string modId, string bundleKey, string assetName, bool cache = true) where T : UnityEngine.Object
	{
		try
		{
			if (string.IsNullOrWhiteSpace(assetName))
			{
				throw new ArgumentException("assetName");
			}
			string text = MakeModBundleKey(modId, bundleKey);
			AssetBundle bundle = GetBundle(modId, bundleKey);
			if (bundle == null)
			{
				Debug.LogError("Bundle not loaded or registered: " + text);
				return null;
			}
			if (!cache)
			{
				T val = bundle.LoadAsset<T>(assetName);
				if (val == null)
				{
					Debug.LogError("Asset '" + assetName + "' not found in bundle '" + text + "'");
				}
				return val;
			}
			if (!AssetCache.TryGetValue(text, out var value))
			{
				value = new Dictionary<string, UnityEngine.Object>(StringComparer.OrdinalIgnoreCase);
				AssetCache[text] = value;
			}
			if (value.TryGetValue(assetName, out var value2) && value2 != null)
			{
				return value2 as T;
			}
			T val2 = bundle.LoadAsset<T>(assetName);
			if (val2 == null)
			{
				Debug.LogError("Asset '" + assetName + "' not found in bundle '" + text + "'");
				return null;
			}
			value[assetName] = val2;
			return val2;
		}
		catch (Exception ex)
		{
			Debug.LogError($"AssetService.LoadAsset failed. modId='{modId}', bundleKey='{bundleKey}', assetName='{assetName}', cache='{cache}'. Exception: {ex}");
			return null;
		}
	}

	private static async Task<T> LoadAssetAsync<T>(string modId, string bundleKey, string assetName, bool cache = true, CancellationToken cancellationToken = default(CancellationToken)) where T : UnityEngine.Object
	{
		try
		{
			ServiceHelper.EnsureInitialized();
			if (string.IsNullOrWhiteSpace(assetName))
			{
				throw new ArgumentException("assetName");
			}
			string modBundleKey = MakeModBundleKey(modId, bundleKey);
			AssetBundle bundle = GetBundle(modId, bundleKey);
			if (bundle == null)
			{
				Debug.LogError("Bundle not loaded or registered: " + modBundleKey);
				return null;
			}
			if (cache && HasAssetCached(modId, bundleKey, assetName))
			{
				return (T)AssetCache[modBundleKey][assetName];
			}
			T val = await ServiceHelper.RunOnMainThreadAsync(async delegate
			{
				cancellationToken.ThrowIfCancellationRequested();
				AssetBundleRequest request = bundle.LoadAssetAsync<T>(assetName);
				return await ServiceHelper.AwaitAsyncOperation(request, () => request.asset as T, cancellationToken).ConfigureAwait(continueOnCapturedContext: false);
			}).ConfigureAwait(continueOnCapturedContext: false);
			if (val == null)
			{
				Debug.LogError("Asset '" + assetName + "' not found in bundle '" + modBundleKey + "'");
				return null;
			}
			if (!cache)
			{
				return val;
			}
			if (!AssetCache.TryGetValue(modBundleKey, out var value))
			{
				value = new Dictionary<string, UnityEngine.Object>(StringComparer.OrdinalIgnoreCase);
				AssetCache[modBundleKey] = value;
			}
			value[assetName] = val;
			return val;
		}
		catch (OperationCanceledException)
		{
			return null;
		}
		catch (Exception ex2)
		{
			Debug.LogError($"AssetService.LoadAssetAsync failed. modId='{modId}', bundleKey='{bundleKey}', assetName='{assetName}', cache='{cache}'. Exception: {ex2}");
			return null;
		}
	}

	private static GameObject LoadPrefab(string modId, string bundleKey, string prefabName, bool cache = true)
	{
		try
		{
			return LoadAsset<GameObject>(modId, bundleKey, prefabName, cache);
		}
		catch (Exception ex)
		{
			Debug.LogError($"AssetService.LoadPrefab failed. modId='{modId}', bundleKey='{bundleKey}', prefabName='{prefabName}', cache='{cache}'. Exception: {ex}");
			return null;
		}
	}

	public static T FindAssetInAnyBundleByFileName<T>(string fileName) where T : UnityEngine.Object
	{
		try
		{
			if (string.IsNullOrWhiteSpace(fileName))
			{
				return null;
			}
			string text = NormalizeKey(fileName);
			string fileName2 = Path.GetFileName(text);
			string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fileName2);
			foreach (KeyValuePair<string, string> item in BundlePathByKey)
			{
				if (!BundlesByKey.TryGetValue(item.Key, out var value))
				{
					value = LoadBundle(item.Key, item.Value);
				}
				if (value == null)
				{
					continue;
				}
				string[] allAssetNames = value.GetAllAssetNames();
				foreach (string text2 in allAssetNames)
				{
					if (NormalizeKey(text2).EndsWith(text, StringComparison.OrdinalIgnoreCase))
					{
						T val = value.LoadAsset<T>(text2);
						if (val != null)
						{
							return val;
						}
					}
					string fileName3 = Path.GetFileName(text2);
					if (string.Equals(fileName3, fileName2, StringComparison.OrdinalIgnoreCase))
					{
						T val2 = value.LoadAsset<T>(text2);
						if (val2 != null)
						{
							return val2;
						}
					}
					if (string.Equals(Path.GetFileNameWithoutExtension(fileName3), fileNameWithoutExtension, StringComparison.OrdinalIgnoreCase))
					{
						T val3 = value.LoadAsset<T>(text2);
						if (val3 != null)
						{
							return val3;
						}
					}
				}
			}
			return null;
		}
		catch (Exception arg)
		{
			Debug.LogError($"AssetService.FindAssetInAnyBundleByFileName failed. fileName='{fileName}'. Exception: {arg}");
			return null;
		}
	}

	public static GameObject Spawn(string modId, string bundleKey, string prefabName, Vector3 position, Quaternion rotation, Transform parent = null, bool worldPositionStays = true, bool active = true, bool remapShaders = true)
	{
		try
		{
			GameObject gameObject = LoadPrefab(modId, bundleKey, prefabName);
			if (gameObject == null)
			{
				return null;
			}
			GameObject gameObject2 = UnityEngine.Object.Instantiate(gameObject, position, rotation, parent);
			if (!worldPositionStays && parent != null)
			{
				gameObject2.transform.localPosition = Vector3.zero;
				gameObject2.transform.localRotation = Quaternion.identity;
			}
			if (remapShaders)
			{
				RemapShaders(gameObject2);
			}
			gameObject2.SetActive(active);
			return gameObject2;
		}
		catch (Exception ex)
		{
			Debug.LogError($"AssetService.Spawn failed. modId='{modId}', bundleKey='{bundleKey}', prefabName='{prefabName}'. Exception: {ex}");
			return null;
		}
	}

	public static async Task<GameObject> SpawnAsync(string modId, string bundleKey, string prefabName, Vector3 position, Quaternion rotation, Transform parent = null, bool worldPositionStays = true, bool active = true, bool remapShaders = true, CancellationToken ct = default(CancellationToken))
	{
		try
		{
			ServiceHelper.EnsureInitialized();
			GameObject prefab = await LoadAssetAsync<GameObject>(modId, bundleKey, prefabName, cache: true, ct).ConfigureAwait(continueOnCapturedContext: false);
			if (prefab == null)
			{
				return null;
			}
			return await ServiceHelper.RunOnMainThreadAsync(delegate
			{
				ct.ThrowIfCancellationRequested();
				GameObject gameObject = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);
				if (!worldPositionStays && parent != null)
				{
					gameObject.transform.localPosition = Vector3.zero;
					gameObject.transform.localRotation = Quaternion.identity;
				}
				if (remapShaders)
				{
					RemapShaders(gameObject);
				}
				gameObject.SetActive(active);
				return gameObject;
			}).ConfigureAwait(continueOnCapturedContext: false);
		}
		catch (OperationCanceledException)
		{
			return null;
		}
		catch (Exception ex2)
		{
			Debug.LogError($"AssetService.SpawnAsync failed. modId='{modId}', bundleKey='{bundleKey}', prefabName='{prefabName}'. Exception: {ex2}");
			return null;
		}
	}

	public static void RemapShaders(GameObject root, string fallbackShaderName = null)
	{
		try
		{
			if (root == null)
			{
				return;
			}
			EnsureShaderRegistry();
			Renderer[] componentsInChildren = root.GetComponentsInChildren<Renderer>(includeInactive: true);
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				Material[] materials = componentsInChildren[i].materials;
				foreach (Material material in materials)
				{
					if (material == null)
					{
						continue;
					}
					Shader shader = material.shader;
					if (!(shader == null) && !string.IsNullOrEmpty(shader.name))
					{
						if (!ShaderRegistry.TryGetValue(shader.name, out var value) && !string.IsNullOrWhiteSpace(fallbackShaderName))
						{
							ShaderRegistry.TryGetValue(fallbackShaderName, out value);
						}
						if (value == null)
						{
							Debug.LogWarning("[AssetService.RemapShaders] Could not remap shader '" + shader.name + "'. The shader may not be in use anywhere in the game build, or it may have been stripped.");
						}
						else if (value != shader)
						{
							material.shader = value;
						}
					}
				}
			}
		}
		catch (Exception arg)
		{
			Debug.LogError($"AssetService.RemapShaders failed. root='{root}', fallbackShaderName='{fallbackShaderName}'. Exception: {arg}");
		}
	}

	private static void EnsureShaderRegistry()
	{
		if (_shaderRegistryReady)
		{
			return;
		}
		_shaderRegistryReady = true;
		Shader[] array = Resources.FindObjectsOfTypeAll<Shader>();
		foreach (Shader shader in array)
		{
			if (!(shader == null) && !string.IsNullOrEmpty(shader.name) && !ShaderRegistry.ContainsKey(shader.name))
			{
				ShaderRegistry[shader.name] = shader;
			}
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		BundlesByKey.Clear();
		BundlePathByKey.Clear();
		AssetCache.Clear();
		ShaderRegistry.Clear();
		_shaderRegistryReady = false;
	}
}
