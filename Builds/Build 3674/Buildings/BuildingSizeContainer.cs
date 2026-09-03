using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Buildings;

public class BuildingSizeContainer : MonoBehaviour
{
	private class BuildingStructureState
	{
		public BuildingStructureEntry Entry { get; }

		public BuildingStructureController Instance { get; set; }

		public AsyncOperationHandle<GameObject> LoadHandle { get; set; }

		public BuildingStructureState(BuildingStructureEntry entry)
		{
			Entry = entry;
		}
	}

	[SerializeField]
	private BuildingStructureEntry[] buildingStructureEntries;

	private readonly Dictionary<int, BuildingStructureState> _buildingStructures = new Dictionary<int, BuildingStructureState>();

	private Transform _backgroundPreloadRoot;

	internal IReadOnlyList<BuildingStructureEntry> BuildingStructureEntries => buildingStructureEntries ?? Array.Empty<BuildingStructureEntry>();

	private void Awake()
	{
		BuildDictionary();
	}

	private void BuildDictionary()
	{
		if (buildingStructureEntries == null)
		{
			return;
		}
		for (int i = 0; i < buildingStructureEntries.Length; i++)
		{
			BuildingStructureEntry buildingStructureEntry = buildingStructureEntries[i];
			if (buildingStructureEntry == null)
			{
				Debug.LogError($"Building structure entry {i} in '{base.name}' is null.", this);
			}
			else if (!_buildingStructures.TryAdd(buildingStructureEntry.Version, new BuildingStructureState(buildingStructureEntry)))
			{
				Debug.LogError($"Duplicate building structure version {buildingStructureEntry.Version} in '{base.name}'.", this);
			}
		}
	}

	public AsyncOperationHandle<GameObject> LoadBuildingStructureAsync(int version)
	{
		if (!_buildingStructures.TryGetValue(version, out var value))
		{
			return default(AsyncOperationHandle<GameObject>);
		}
		if (value.Entry.PrefabReference == null || !value.Entry.PrefabReference.RuntimeKeyIsValid())
		{
			Debug.LogError($"Invalid building structure reference in '{base.name}', version {version}.", this);
			return default(AsyncOperationHandle<GameObject>);
		}
		if (!value.LoadHandle.IsValid())
		{
			value.LoadHandle = value.Entry.PrefabReference.LoadAssetAsync();
		}
		return value.LoadHandle;
	}

	public BuildingStructureController GetBuildingStructureController(int version)
	{
		if (!_buildingStructures.TryGetValue(version, out var value))
		{
			return null;
		}
		if (!value.Instance)
		{
			return InstantiateBuildingStructure(version, value, base.transform);
		}
		if (value.Instance.transform.parent == _backgroundPreloadRoot)
		{
			value.Instance.transform.SetParent(base.transform, worldPositionStays: false);
			ApplyEntryTransform(value.Instance.transform, value.Entry);
		}
		return value.Instance;
	}

	internal void PreloadBuildingStructureInstance(int version)
	{
		if (_buildingStructures.TryGetValue(version, out var value) && !value.Instance)
		{
			if (!_backgroundPreloadRoot)
			{
				_backgroundPreloadRoot = new GameObject("Background Preloaded Building Structures").transform;
				_backgroundPreloadRoot.SetParent(base.transform, worldPositionStays: false);
				_backgroundPreloadRoot.gameObject.SetActive(value: false);
			}
			InstantiateBuildingStructure(version, value, _backgroundPreloadRoot);
		}
	}

	private BuildingStructureController InstantiateBuildingStructure(int version, BuildingStructureState state, Transform parent)
	{
		BuildingStructureController loadedPrefab = GetLoadedPrefab(version);
		if (!loadedPrefab)
		{
			return null;
		}
		Vector3 position = parent.TransformPoint(state.Entry.localPosition);
		Quaternion rotation = parent.rotation * state.Entry.localRotation;
		BuildingStructureController buildingStructureController = UnityEngine.Object.Instantiate(loadedPrefab, position, rotation, parent);
		buildingStructureController.transform.localScale = state.Entry.localScale;
		state.Instance = buildingStructureController;
		return buildingStructureController;
	}

	private static void ApplyEntryTransform(Transform buildingTransform, BuildingStructureEntry entry)
	{
		buildingTransform.localPosition = entry.localPosition;
		buildingTransform.localRotation = entry.localRotation;
		buildingTransform.localScale = entry.localScale;
	}

	private BuildingStructureController GetLoadedPrefab(int version)
	{
		AsyncOperationHandle<GameObject> asyncOperationHandle = LoadBuildingStructureAsync(version);
		if (!asyncOperationHandle.IsValid())
		{
			return null;
		}
		if (!asyncOperationHandle.IsDone)
		{
			asyncOperationHandle.WaitForCompletion();
		}
		if (asyncOperationHandle.Status != AsyncOperationStatus.Succeeded || !asyncOperationHandle.Result)
		{
			Debug.LogError($"Failed to load building structure '{base.name}', version {version}.", this);
			return null;
		}
		if (!asyncOperationHandle.Result.TryGetComponent<BuildingStructureController>(out var component))
		{
			Debug.LogError("Addressable building structure '" + asyncOperationHandle.Result.name + "' has no BuildingStructureController component.", asyncOperationHandle.Result);
			return null;
		}
		return component;
	}

	public void DisableAllSizesAndLayouts()
	{
		foreach (BuildingStructureState value in _buildingStructures.Values)
		{
			if (!value.Instance)
			{
				continue;
			}
			value.Instance.gameObject.SetActive(value: false);
			Transform transform = value.Instance.transform.Find("Layouts");
			if (!transform)
			{
				continue;
			}
			foreach (Transform item in transform)
			{
				item.gameObject.SetActive(value: false);
			}
		}
	}

	private void OnDestroy()
	{
		foreach (BuildingStructureState value in _buildingStructures.Values)
		{
			if (value.LoadHandle.IsValid())
			{
				Addressables.Release(value.LoadHandle);
			}
		}
		_buildingStructures.Clear();
	}
}
