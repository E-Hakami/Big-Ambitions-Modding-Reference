using System.Collections;
using System.Collections.Generic;
using Blueprints;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Buildings;

public class BuildingSizeResolver : MonoBehaviour
{
	private sealed class BackgroundPreloadRequest
	{
		public BuildingSizeContainer container;

		public BuildingStructureEntry entry;
	}

	private readonly Dictionary<string, BuildingSizeContainer> _buildingSizeContainersDictionary = new Dictionary<string, BuildingSizeContainer>();

	private int _foregroundLoads;

	private int _foregroundPriorityUntilFrame;

	private void Awake()
	{
		UpdateBuildingSizes();
		GlobalEvents.RegisterOnGameLoadedLateCallback(StartBackgroundPreloading);
	}

	private void UpdateBuildingSizes()
	{
		foreach (Transform item in base.transform)
		{
			if (item.TryGetComponent<BuildingSizeContainer>(out var component))
			{
				_buildingSizeContainersDictionary.Add(component.name, component);
			}
		}
	}

	public BuildingStructureController GetInstantiatedBuilding(BuildingSizeInfo buildingInfo)
	{
		return GetInstantiatedBuildingInternal(buildingInfo);
	}

	public AsyncOperationHandle<GameObject> LoadBuildingAsync(BuildingSizeInfo buildingInfo)
	{
		_foregroundPriorityUntilFrame = Time.frameCount + 2;
		AsyncOperationHandle<GameObject> result = (_buildingSizeContainersDictionary.TryGetValue(buildingInfo.GetSizeShort(), out var value) ? value.LoadBuildingStructureAsync(buildingInfo.buildingVersion) : default(AsyncOperationHandle<GameObject>));
		if (result.IsValid() && !result.IsDone)
		{
			_foregroundLoads++;
			result.Completed += delegate
			{
				_foregroundLoads = Mathf.Max(0, _foregroundLoads - 1);
			};
		}
		return result;
	}

	private BuildingStructureController GetInstantiatedBuildingInternal(BuildingSizeInfo buildingInfo)
	{
		_foregroundPriorityUntilFrame = Time.frameCount + 1;
		if (_buildingSizeContainersDictionary.TryGetValue(buildingInfo.GetSizeShort(), out var value))
		{
			return value.GetBuildingStructureController(buildingInfo.buildingVersion);
		}
		return null;
	}

	public void DisableAllSizesAndLayouts()
	{
		foreach (BuildingSizeContainer value in _buildingSizeContainersDictionary.Values)
		{
			value.DisableAllSizesAndLayouts();
		}
	}

	private void StartBackgroundPreloading()
	{
		if ((bool)this)
		{
			StartCoroutine(PreloadInBackground());
		}
	}

	private IEnumerator PreloadInBackground()
	{
		yield return null;
		List<BackgroundPreloadRequest> backgroundPreloadRequests = GetBackgroundPreloadRequests();
		foreach (BackgroundPreloadRequest request in backgroundPreloadRequests)
		{
			while (ForegroundLoadingHasPriority())
			{
				yield return null;
			}
			AsyncOperationHandle<GameObject> asyncOperationHandle = request.container.LoadBuildingStructureAsync(request.entry.Version);
			if (!asyncOperationHandle.IsValid())
			{
				Debug.LogError("[BuildingSizeResolver] Failed to start background loading for " + $"'{request.container.name}', version {request.entry.Version}.", request.container);
				continue;
			}
			if (!asyncOperationHandle.IsDone)
			{
				yield return asyncOperationHandle;
			}
			if (request.entry.PreloadMode != BuildingPreloadMode.PreloadAndInstantiate)
			{
				yield return null;
				continue;
			}
			yield return null;
			while (ForegroundLoadingHasPriority())
			{
				yield return null;
			}
			request.container.PreloadBuildingStructureInstance(request.entry.Version);
			yield return null;
		}
	}

	private List<BackgroundPreloadRequest> GetBackgroundPreloadRequests()
	{
		List<BackgroundPreloadRequest> list = new List<BackgroundPreloadRequest>();
		foreach (BuildingSizeContainer value in _buildingSizeContainersDictionary.Values)
		{
			foreach (BuildingStructureEntry buildingStructureEntry in value.BuildingStructureEntries)
			{
				if (buildingStructureEntry.PreloadMode != BuildingPreloadMode.Lazy)
				{
					list.Add(new BackgroundPreloadRequest
					{
						container = value,
						entry = buildingStructureEntry
					});
				}
			}
		}
		list.Sort((BackgroundPreloadRequest requestA, BackgroundPreloadRequest requestB) => requestB.entry.PreloadPriority.CompareTo(requestA.entry.PreloadPriority));
		return list;
	}

	private bool ForegroundLoadingHasPriority()
	{
		if (_foregroundLoads <= 0 && Time.frameCount > _foregroundPriorityUntilFrame)
		{
			if ((bool)InstanceBehavior<BuildingManager>.Instance)
			{
				return InstanceBehavior<BuildingManager>.Instance.enteringBuilding;
			}
			return false;
		}
		return true;
	}
}
