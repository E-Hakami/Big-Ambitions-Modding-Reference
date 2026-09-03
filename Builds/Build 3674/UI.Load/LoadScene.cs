using System.Collections;
using BAModAPI;
using Player.Sound.Radio;
using Scenes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace UI.Load;

public static class LoadScene
{
	private const string LoadingScreenPath = "LoadingScreen";

	public static bool isLoading;

	public static void LoadIntro(bool skipFadeOut = false)
	{
		LoadScenes(GameScenes.Intro, skipFadeOut, ModActivationScope.Intro, ModActivationScope.MainMenu);
	}

	public static void LoadMainMenu(ModActivationScope unloadScope, bool skipFadeOut = false)
	{
		LoadScenes(GameScenes.MainMenu, skipFadeOut, ModActivationScope.MainMenu, unloadScope);
	}

	public static IEnumerator LoadMainMenuFromCity(bool skipFadeOut = false)
	{
		yield return SaveGameManager.JoinSaveGameThreadsCoroutine();
		GameManager.isCitySceneBeingUnloaded = true;
		GlobalEvents.onGameUnloaded?.Invoke();
		InstanceBehavior<UIs>.Instance.monologueUI.InstantClose();
		if ((bool)InstanceBehavior<LoudSpeakersManager>.Instance)
		{
			InstanceBehavior<LoudSpeakersManager>.Instance.OnExitBuilding(null);
		}
		Object.Destroy(InstanceBehavior<LoadingSpinner>.Instance);
		LoadMainMenu(ModActivationScope.City, skipFadeOut);
	}

	public static void LoadGame(ModActivationScope unloadScope, bool skipFadeOut = false)
	{
		LoadScenes(GameScenesHelper.GetAllCityScenes(), skipFadeOut, ModActivationScope.City, unloadScope);
	}

	public static void LoadTransitionToSave()
	{
		LoadScenes(GameScenes.TransitionToSave, skipFadeOut: true, ModActivationScope.None, ModActivationScope.Intro);
	}

	public static void LoadBlueprintCreator()
	{
		LoadScenes(GameScenes.LightsAndPostprocessing | GameScenes.Indoor | GameScenes.BlueprintCreator, skipFadeOut: false, ModActivationScope.BlueprintCreator, ModActivationScope.MainMenu);
	}

	public static void LoadScenes(GameScenes scenesToLoad, bool skipFadeOut = false, ModActivationScope loadModScope = ModActivationScope.None, ModActivationScope unloadModScope = ModActivationScope.None)
	{
		if (isLoading)
		{
			return;
		}
		isLoading = true;
		GameScenes[] scenesToLoadParsed = GameScenesHelper.GetScenesFromMask(scenesToLoad);
		AsyncOperationHandle<GameObject> asyncOperationHandle = Addressables.LoadAssetAsync<GameObject>("LoadingScreen");
		asyncOperationHandle.Completed += delegate(AsyncOperationHandle<GameObject> operation)
		{
			if (operation.Status != AsyncOperationStatus.Succeeded)
			{
				Debug.LogError("Failed to load loading screen prefab at address 'LoadingScreen'.");
				Addressables.Release(operation);
				isLoading = false;
			}
			else
			{
				GameObject gameObject = Object.Instantiate(operation.Result);
				Object.DontDestroyOnLoad(gameObject);
				LoadingScreen component = gameObject.GetComponent<LoadingScreen>();
				component.handle = operation;
				component.StartLoading(scenesToLoadParsed, skipFadeOut, loadModScope, unloadModScope);
			}
		};
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		isLoading = false;
	}
}
