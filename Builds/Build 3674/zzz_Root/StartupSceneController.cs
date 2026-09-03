using DG.Tweening;
using JimmysUnityUtilities;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartupSceneController : MonoBehaviour
{
	[SerializeField]
	private GameObject compilingShadersPanel;

	[SerializeField]
	private CanvasGroup canvasGroup;

	private void Start()
	{
		compilingShadersPanel.SetActive(value: true);
		SceneManager.sceneLoaded += OnSceneLoaded;
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			SceneManager.LoadSceneAsync(1, LoadSceneMode.Additive);
		});
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (scene.buildIndex == 1)
		{
			SceneManager.sceneLoaded -= OnSceneLoaded;
			canvasGroup.DOFade(0f, 0.3f).OnComplete(delegate
			{
				SceneManager.UnloadSceneAsync(0);
			});
		}
	}
}
