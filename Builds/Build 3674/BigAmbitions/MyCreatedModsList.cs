using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using Steamworks.Ugc;
using UnityEngine;
using UnityEngine.UI;

namespace BigAmbitions;

public class MyCreatedModsList : MonoBehaviour
{
	[SerializeField]
	private ModCreatorUI modCreatorUI;

	[SerializeField]
	private TextLocalizationComponent noPublishedModsText;

	[SerializeField]
	private Transform myModUiTemplate;

	[SerializeField]
	private Button createNewModButton;

	private Coroutine _refreshCoroutine;

	private readonly List<ModInfo> _modInfos = new List<ModInfo>();

	private Button _currentEditButton;

	private void OnEnable()
	{
		createNewModButton.interactable = true;
		if (_refreshCoroutine != null)
		{
			StopCoroutine(_refreshCoroutine);
		}
		_refreshCoroutine = StartCoroutine(RefreshModToUpdateDropdownCoroutine());
	}

	private void OnDisable()
	{
		if (_refreshCoroutine != null)
		{
			StopCoroutine(_refreshCoroutine);
		}
	}

	private IEnumerator RefreshModToUpdateDropdownCoroutine()
	{
		Task<List<Item>> publishedModsTask = SteamHelper.GetUserPublishedItems("mod");
		while (!publishedModsTask.IsCompleted)
		{
			yield return null;
		}
		if (publishedModsTask.IsFaulted)
		{
			Debug.LogError(publishedModsTask.Exception);
			PopulateModList(null);
		}
		else if (publishedModsTask.IsCanceled)
		{
			Debug.LogError("Fetching published Workshop items was canceled.");
			PopulateModList(null);
		}
		else
		{
			PopulateModList(publishedModsTask.Result);
		}
	}

	private void PopulateModList(IReadOnlyList<Item> publishedMods)
	{
		myModUiTemplate.ResetTemplate();
		_modInfos.Clear();
		if (publishedMods == null || publishedMods.Count == 0)
		{
			noPublishedModsText.gameObject.SetActive(value: true);
			return;
		}
		noPublishedModsText.gameObject.SetActive(value: false);
		foreach (Item publishedMod in publishedMods)
		{
			MyModUI component = myModUiTemplate.CreateElement().GetComponent<MyModUI>();
			ModInfo modInfo = new ModInfo(publishedMod);
			component.Setup(modInfo);
			component.SetOnEditModClick(OnModCreatorUIExpand);
			_modInfos.Add(modInfo);
		}
	}

	public void OnCreateNewModClick()
	{
		createNewModButton.interactable = false;
		createNewModButton.transform.localScale = Vector3.one;
		if ((bool)_currentEditButton)
		{
			_currentEditButton.interactable = true;
		}
		modCreatorUI.Expand(null);
	}

	private void OnModCreatorUIExpand(ulong steamId, Button button)
	{
		createNewModButton.interactable = true;
		if ((bool)_currentEditButton)
		{
			_currentEditButton.interactable = true;
		}
		_currentEditButton = button;
		_currentEditButton.interactable = false;
		_currentEditButton.transform.localScale = Vector3.one;
		modCreatorUI.Expand(_modInfos.Find((ModInfo m) => m.steamItemId == steamId));
	}
}
