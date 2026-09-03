using System.Collections;
using System.Threading.Tasks;
using BAModAPI;
using UI.Load;
using UnityEngine;

public class TransitionToSave : MonoBehaviour
{
	public static SaveGameManager.SaveGameStruct saveToLoadData;

	public static bool saveGameLoadErrored;

	private IEnumerator Start()
	{
		while (LoadScene.isLoading)
		{
			yield return null;
		}
		yield return LoadSave();
	}

	private static IEnumerator LoadSave()
	{
		yield return null;
		Task<bool> loadTask = SaveGameManager.LoadAsync(saveToLoadData);
		while (!loadTask.IsCompleted)
		{
			yield return null;
		}
		if (loadTask.IsFaulted || loadTask.IsCanceled || !loadTask.Result)
		{
			if (loadTask.Exception != null)
			{
				Debug.LogException(loadTask.Exception);
			}
			saveGameLoadErrored = true;
			LoadScene.LoadMainMenu(ModActivationScope.City);
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	public static void ResetStaticData()
	{
		saveToLoadData = null;
		saveGameLoadErrored = false;
	}
}
