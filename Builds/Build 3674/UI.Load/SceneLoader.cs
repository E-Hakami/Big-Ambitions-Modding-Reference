using System;
using System.Collections;
using System.Collections.Generic;
using Localizor.LanguageChangeEvent;
using Scenes;
using UI.Elements;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace UI.Load;

public class SceneLoader
{
	private const ThreadPriority LoadingPriority = ThreadPriority.High;

	private readonly WaitForSecondsRealtime _progressUpdateWait = new WaitForSecondsRealtime(0.1f);

	private readonly ProgressBar _progressBar;

	private readonly TextLocalizationComponent _loadingText;

	public SceneLoader(ProgressBar progressBar, TextLocalizationComponent loadingText)
	{
		_progressBar = progressBar;
		_loadingText = loadingText;
	}

	public IEnumerator LoadScenes(GameScenes[] scenesToLoad)
	{
		GameScenes[] loadedScenes = GetLoadedScenes();
		GameScenes[] scenesToUnload = Except(loadedScenes, scenesToLoad);
		scenesToLoad = Except(scenesToLoad, loadedScenes);
		int totalSceneCount = scenesToLoad.Length + scenesToUnload.Length;
		int completedSceneCount = 0;
		if (scenesToUnload.Length != 0)
		{
			RemoveDuplicatedSingletons();
		}
		ThreadPriority previousLoadingPriority = Application.backgroundLoadingPriority;
		Application.backgroundLoadingPriority = ThreadPriority.High;
		try
		{
			GameScenes[] array = scenesToLoad;
			for (int i = 0; i < array.Length; i++)
			{
				GameScenes scene = array[i];
				AsyncOperation operation = SceneManager.LoadSceneAsync(scene.ToString(), LoadSceneMode.Additive);
				yield return ProcessScene(scene, operation, "loading", completedSceneCount, totalSceneCount);
				completedSceneCount++;
				yield return null;
			}
			array = scenesToUnload;
			for (int i = 0; i < array.Length; i++)
			{
				GameScenes scene2 = array[i];
				AsyncOperation operation2 = SceneManager.UnloadSceneAsync(scene2.ToString());
				yield return ProcessScene(scene2, operation2, "unloading", completedSceneCount, totalSceneCount);
				completedSceneCount++;
				yield return null;
			}
		}
		finally
		{
			Application.backgroundLoadingPriority = previousLoadingPriority;
		}
	}

	private static GameScenes[] Except(GameScenes[] source, GameScenes[] excluded)
	{
		HashSet<GameScenes> hashSet = new HashSet<GameScenes>(excluded);
		List<GameScenes> list = new List<GameScenes>();
		foreach (GameScenes item in source)
		{
			if (hashSet.Add(item))
			{
				list.Add(item);
			}
		}
		return list.ToArray();
	}

	private static GameScenes[] GetLoadedScenes()
	{
		GameScenes[] array = new GameScenes[SceneManager.sceneCount];
		for (int i = 0; i < SceneManager.sceneCount; i++)
		{
			if (Enum.TryParse<GameScenes>(SceneManager.GetSceneAt(i).name, out var result))
			{
				array[i] = result;
			}
			else
			{
				array[i] = GameScenes.MainScene;
			}
		}
		return array;
	}

	private IEnumerator ProcessScene(GameScenes scene, AsyncOperation operation, string state, int completedSceneCount, int totalSceneCount)
	{
		LogSceneTime(scene, operation, state);
		_loadingText.Key = "loading_scene_" + state;
		_loadingText.Arguments = new
		{
			scene = $"loading_screen_{scene}"
		};
		while (!operation.isDone)
		{
			float valueInPercent = ((float)completedSceneCount + operation.progress) / (float)totalSceneCount * 100f;
			_progressBar.SetValue(valueInPercent);
			yield return _progressUpdateWait;
		}
		float valueInPercent2 = ((float)completedSceneCount + 1f) / (float)totalSceneCount * 100f;
		_progressBar.SetValue(valueInPercent2);
	}

	private static void LogSceneTime(GameScenes scene, AsyncOperation operation, string state)
	{
	}

	private static void RemoveDuplicatedSingletons()
	{
		if ((bool)EventSystem.current)
		{
			UnityEngine.Object.Destroy(EventSystem.current.gameObject);
		}
		if ((bool)InstanceBehavior<GlobalReferences>.Instance)
		{
			UnityEngine.Object.Destroy(InstanceBehavior<GlobalReferences>.Instance);
		}
	}
}
