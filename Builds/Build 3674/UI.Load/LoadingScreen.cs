using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using BAModAPI;
using BigAmbitions.ModsInternal;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Localizor.LanguageChangeEvent;
using Scenes;
using UI.Elements;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UI.Load;

public class LoadingScreen : InstanceBehavior<LoadingScreen>
{
	private const float FadeInTime = 0.8f;

	private const float FadeOutTime = 0.8f;

	private const float SecondsBetweenHints = 10f;

	[SerializeField]
	private TextLocalizationComponent hintText;

	[SerializeField]
	private TextLocalizationComponent loadingText;

	[SerializeField]
	private ProgressBar progressBar;

	[SerializeField]
	private CanvasGroup canvasGroup;

	private SceneLoader _sceneLoader;

	private readonly string[] _hints = new string[20]
	{
		"loading_hint_handtrucks", "loading_hint_cityworkforce", "loading_hint_walkinpark", "loading_hint_purchasingagent", "loading_hint_coffee", "loading_hint_neighborhoodprices", "loading_hint_marketing", "loading_hint_towservice", "loading_hint_speedrun", "loading_hint_subway",
		"loading_hint_customerdialogs", "loading_hint_employeedemands", "loading_hint_storageshelf", "loading_hint_cleaningstation", "loading_hint_sleepingincar", "loading_hint_benches", "loading_hint_realestate", "loading_hint_contacts", "loading_hint_handtruck_in_trunk", "loading_hint_opening_hours"
	};

	private int _currentHint = -1;

	private float _nextHint;

	public AsyncOperationHandle<GameObject> handle;

	private void Update()
	{
		if (_nextHint <= 0f)
		{
			SetNewHint();
		}
		else
		{
			_nextHint -= Time.unscaledDeltaTime;
		}
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (handle.IsValid())
		{
			Addressables.Release(handle);
		}
	}

	private void Init()
	{
		progressBar.SetValue(0f);
		_sceneLoader = new SceneLoader(progressBar, loadingText);
		loadingText.Key = "loading_scene_loading";
		loadingText.Arguments = new
		{
			scene = "..."
		};
	}

	public void StartLoading(GameScenes[] scenesToLoad, bool skipFadeOut, ModActivationScope loadModScope, ModActivationScope unloadModScope)
	{
		LoadScene.isLoading = true;
		StartCoroutine(LoadAsyncScenes(scenesToLoad, skipFadeOut, loadModScope, unloadModScope));
	}

	private IEnumerator LoadAsyncScenes(GameScenes[] scenesToLoad, bool skipFadeOut, ModActivationScope loadModScope, ModActivationScope unloadModScope)
	{
		Init();
		yield return canvasGroup.DOFade(1f, 0.8f).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject)
			.WaitForCompletion();
		bool hasDiscoveredMods = ModDiscoveryRegistry.HasDiscoveredEntries;
		if (hasDiscoveredMods)
		{
			loadingText.Key = "loading_scene_unloading_mods";
			if (unloadModScope != ModActivationScope.None)
			{
				yield return WaitForTask(ModLifecycleLoader.UnloadScopeAsync(unloadModScope));
			}
		}
		yield return _sceneLoader.LoadScenes(scenesToLoad);
		yield return WaitForAsyncCallsToFinish();
		if (hasDiscoveredMods)
		{
			loadingText.Key = "loading_scene_loading_mods";
			if (loadModScope != ModActivationScope.None)
			{
				yield return WaitForTask(ModLifecycleLoader.LoadScopeAsync(loadModScope));
			}
		}
		LoadingAsyncTaskManager.ClearTasks();
		loadingText.Key = "loading_screen_loaded";
		progressBar.SetValue(100f);
		Time.timeScale = 1f;
		LoadScene.isLoading = false;
		if (scenesToLoad.Contains(GameScenes.MainMenu))
		{
			GameManager.isCitySceneBeingUnloaded = false;
		}
		GlobalEvents.InvokeOnGameLoaded();
		if (!skipFadeOut)
		{
			TweenerCore<float, float, FloatOptions> tween = canvasGroup.DOFade(0f, 0.8f).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
			float timer = 0f;
			while (tween.IsActive() && !tween.IsComplete() && timer < 1.6f)
			{
				timer += Time.unscaledDeltaTime;
				yield return null;
			}
			if (tween.IsActive())
			{
				tween.Kill();
			}
		}
		Object.Destroy(base.gameObject);
	}

	private static IEnumerator WaitForAsyncCallsToFinish()
	{
		yield return new WaitUntil(LoadingAsyncTaskManager.AreAllTasksCompleted);
	}

	private void SetNewHint()
	{
		if (_hints.Length > 1)
		{
			int num;
			for (num = _currentHint; num == _currentHint; num = Random.Range(0, _hints.Length))
			{
			}
			hintText.SetData(LanguageChangeEventDataHolder.Create(_hints[num], null, "loading_hint_prefix"));
			_currentHint = num;
			_nextHint = 10f;
		}
	}

	private static IEnumerator WaitForTask(Task task)
	{
		while (!task.IsCompleted)
		{
			yield return null;
		}
		if (!task.IsFaulted || task.Exception == null)
		{
			yield break;
		}
		throw task.Exception;
	}
}
