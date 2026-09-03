using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Scenes.MainMenu;

public class PersistentAddressablesPreloader : MonoBehaviour
{
	private static AsyncOperationHandle<IList<object>>[] LabelPreloadOperations;

	private static AsyncOperationHandle<object>[] SinglePreloadOperations;

	[SerializeField]
	private string[] addressableKeysToPreload;

	[SerializeField]
	private string[] addressableLabelsToPreload;

	[SerializeField]
	private bool logPreloadTime;

	private float _startTime;

	private void Start()
	{
		if (SinglePreloadOperations != null || LabelPreloadOperations != null)
		{
			Object.Destroy(base.gameObject);
			return;
		}
		_startTime = Time.realtimeSinceStartup;
		Object.DontDestroyOnLoad(base.gameObject);
		PreloadLabelAssets();
		PreloadSingleAssets();
		StartCoroutine(WaitForPreloadCompletion());
	}

	private void PreloadSingleAssets()
	{
		if (SinglePreloadOperations == null)
		{
			SinglePreloadOperations = new AsyncOperationHandle<object>[addressableKeysToPreload.Length];
			for (int i = 0; i < addressableKeysToPreload.Length; i++)
			{
				string key = addressableKeysToPreload[i];
				SinglePreloadOperations[i] = Addressables.LoadAssetAsync<object>(key);
			}
		}
	}

	private void PreloadLabelAssets()
	{
		if (LabelPreloadOperations == null)
		{
			LabelPreloadOperations = new AsyncOperationHandle<IList<object>>[addressableLabelsToPreload.Length];
			for (int i = 0; i < addressableLabelsToPreload.Length; i++)
			{
				string key = addressableLabelsToPreload[i];
				LabelPreloadOperations[i] = Addressables.LoadAssetsAsync<object>(key, null);
			}
		}
	}

	private IEnumerator WaitForPreloadCompletion()
	{
		AsyncOperationHandle<object>[] singlePreloadOperations = SinglePreloadOperations;
		foreach (AsyncOperationHandle<object> asyncOperationHandle in singlePreloadOperations)
		{
			yield return asyncOperationHandle;
		}
		AsyncOperationHandle<IList<object>>[] labelPreloadOperations = LabelPreloadOperations;
		foreach (AsyncOperationHandle<IList<object>> asyncOperationHandle2 in labelPreloadOperations)
		{
			yield return asyncOperationHandle2;
		}
		if (logPreloadTime)
		{
			float num = Time.realtimeSinceStartup - _startTime;
			Debug.Log($"Addressable assets preloaded in {num} seconds.");
		}
		Object.Destroy(base.gameObject);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		LabelPreloadOperations = null;
		SinglePreloadOperations = null;
	}
}
