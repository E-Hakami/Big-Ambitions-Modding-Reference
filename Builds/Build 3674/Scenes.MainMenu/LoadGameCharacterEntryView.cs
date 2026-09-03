using System;
using System.Collections.Generic;
using Extensions;
using TMPro;
using UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Scenes.MainMenu;

public class LoadGameCharacterEntryView : MonoBehaviour
{
	[SerializeField]
	private Button collapseButton;

	[SerializeField]
	private Button loadLastGameButton;

	[SerializeField]
	private Button removeCharacterButton;

	[SerializeField]
	private TMP_Text titleLabel;

	[SerializeField]
	private Image portraitImage;

	[SerializeField]
	private ModTooltip loadLastGameModTooltip;

	[SerializeField]
	private RectTransform collapseIconRect;

	[SerializeField]
	private GameObject saveGamesRoot;

	[SerializeField]
	private Transform saveGameTemplate;

	[SerializeField]
	private Transform recoverSaveGameTemplate;

	[SerializeField]
	private GameObject recoverSavesListRoot;

	private bool _isCollapsed;

	private bool _showRecoverSaves;

	private int _normalSaveCount;

	private int _recoverSaveCount;

	public Button LoadLastGameButton => loadLastGameButton;

	public Button RemoveCharacterButton => removeCharacterButton;

	public ModTooltip LoadLastGameModTooltip => loadLastGameModTooltip;

	public Image PortraitImage => portraitImage;

	public void SetTitle(string characterName)
	{
		titleLabel.text = characterName;
	}

	public void ToggleCollapse()
	{
		SetCollapsed(!_isCollapsed);
	}

	private void SetCollapsed(bool collapsed)
	{
		_isCollapsed = collapsed;
		saveGamesRoot.SetActive(!collapsed && _normalSaveCount > 0);
		recoverSavesListRoot.SetActive(!collapsed && _showRecoverSaves && _recoverSaveCount > 0);
		collapseIconRect.localEulerAngles = new Vector3(0f, 0f, (!collapsed) ? 90 : 0);
	}

	public void SetRecoverSavesVisible(bool isVisible)
	{
		_showRecoverSaves = isVisible;
		SetCollapsed(_isCollapsed);
	}

	public bool HasRecoverSaveEntries()
	{
		return recoverSaveGameTemplate.parent.childCount >= 3;
	}

	public void Setup(List<SaveGameManager.SaveGameStruct> saveGames, bool showRecoverSaves, Func<SaveGameManager.SaveGameStruct, Sprite> getIcon, Action<SaveGameManager.SaveGameStruct> onLoad, Action<SaveGameManager.SaveGameStruct, Transform> onDelete)
	{
		List<SaveGameManager.SaveGameStruct> list = new List<SaveGameManager.SaveGameStruct>();
		List<SaveGameManager.SaveGameStruct> list2 = new List<SaveGameManager.SaveGameStruct>();
		foreach (SaveGameManager.SaveGameStruct saveGame2 in saveGames)
		{
			if (saveGame2.isRecoverSave)
			{
				list2.Add(saveGame2);
			}
			else
			{
				list.Add(saveGame2);
			}
		}
		_normalSaveCount = list.Count;
		_recoverSaveCount = list2.Count;
		saveGameTemplate.ResetTemplate();
		foreach (SaveGameManager.SaveGameStruct saveGame in list)
		{
			Transform saveGameEntry = saveGameTemplate.CreateElement();
			saveGameEntry.GetComponent<LoadGameSaveEntryView>().Setup(saveGame, getIcon(saveGame), delegate
			{
				onLoad(saveGame);
			}, delegate
			{
				onDelete(saveGame, saveGameEntry);
			});
		}
		recoverSaveGameTemplate.ResetTemplate();
		SetRecoverSavesVisible((list2.Count > 0) & showRecoverSaves);
		foreach (SaveGameManager.SaveGameStruct recoverSaveGame in list2)
		{
			Transform recoverSaveGameEntry = recoverSaveGameTemplate.CreateElement();
			recoverSaveGameEntry.GetComponent<LoadGameSaveEntryView>().Setup(recoverSaveGame, getIcon(recoverSaveGame), delegate
			{
				onLoad(recoverSaveGame);
			}, delegate
			{
				onDelete(recoverSaveGame, recoverSaveGameEntry);
			});
		}
		SetCollapsed(_isCollapsed);
	}
}
