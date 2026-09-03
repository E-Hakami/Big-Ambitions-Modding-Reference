using Seasons;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Load;

public class SeasonalSceneLoader : MonoBehaviour
{
	private void Start()
	{
		SeasonHelper.onSeasonalDecorationsOptionChanged.AddListener(OnSeasonalDecorationsOptionChanged);
	}

	private void OnDestroy()
	{
		SeasonHelper.onSeasonalDecorationsOptionChanged.RemoveListener(OnSeasonalDecorationsOptionChanged);
	}

	private static void OnSeasonalDecorationsOptionChanged(bool value)
	{
		Season seasonNonManipulated = SeasonHelper.GetSeasonNonManipulated();
		if (seasonNonManipulated == null || !seasonNonManipulated.hasSpecificScene)
		{
			return;
		}
		LoadingSpinner.Show();
		string text = seasonNonManipulated.sceneName.ToString();
		if (value)
		{
			SceneManager.LoadSceneAsync(seasonNonManipulated.sceneName.ToString(), LoadSceneMode.Additive);
			SceneManager.sceneLoaded += OnSceneLoaded;
			return;
		}
		for (int i = 0; i < SceneManager.loadedSceneCount; i++)
		{
			Scene sceneAt = SceneManager.GetSceneAt(i);
			if (!(sceneAt.name != text))
			{
				SceneManager.UnloadSceneAsync(sceneAt);
				SceneManager.sceneUnloaded += OnSceneUnloaded;
				return;
			}
		}
		LoadingSpinner.Hide();
	}

	private static void OnSceneUnloaded(Scene scene)
	{
		LoadingSpinner.Hide();
		SceneManager.sceneUnloaded -= OnSceneUnloaded;
	}

	private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		LoadingSpinner.Hide();
		SceneManager.sceneLoaded -= OnSceneLoaded;
	}
}
