using System;
using System.Collections;
using System.IO;
using System.Linq;
using BigAmbitions.InputSystem;
using DG.Tweening;
using Localizor.LanguageChangeEvent;
using Newtonsoft.Json;
using UI.Load;
using UnityEngine;

namespace UI;

public class FuneralUI : MonoBehaviour
{
	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private TextLocalizationComponent loadBackUpSaveLabel;

	[SerializeField]
	private TextLocalizationComponent backToMainMenuLabel;

	[SerializeField]
	private TextLocalizationComponent exitToDesktopLabel;

	private SaveGameManager.SaveGameStruct _backUpMetaData;

	private string _backUpSavePath;

	private void Start()
	{
		GlobalEvents.RegisterOnGameLoadedCallback(SetUpKeysLabels);
		GlobalEvents.onBindingsChanged = (Action)Delegate.Combine(GlobalEvents.onBindingsChanged, new Action(SetUpKeysLabels));
	}

	private void SetUpKeysLabels()
	{
		backToMainMenuLabel.Suffix = PlayerAction.SecondaryInteract.AsSuffix();
		exitToDesktopLabel.Suffix = PlayerAction.Cancel.AsSuffix();
		loadBackUpSaveLabel.Suffix = PlayerAction.Interact.AsSuffix();
	}

	private void OnEnable()
	{
		canvasGroup.alpha = 0f;
		_backUpMetaData = GetBackUpMetaData();
		if (_backUpMetaData == null)
		{
			loadBackUpSaveLabel.transform.parent.gameObject.SetActive(value: false);
		}
		else
		{
			_backUpMetaData.CharacterPath = SaveGamePathHelper.GetCharacterFolderPath(SaveGameManager.Current.characterId);
			loadBackUpSaveLabel.Arguments = new
			{
				age = 70
			};
		}
		canvasGroup.DOFade(1f, 1f);
	}

	private void Update()
	{
		if (PlayerAction.Interact.Pressed() && _backUpMetaData != null)
		{
			LoadBackUpSave();
		}
		if (PlayerAction.SecondaryInteract.Pressed())
		{
			BackToMainMenu();
		}
		if (PlayerAction.Cancel.Pressed())
		{
			ExitToDesktop();
		}
	}

	public void ExitToDesktop()
	{
		StartCoroutine(QuitToDesktopCoroutine());
	}

	private IEnumerator QuitToDesktopCoroutine()
	{
		yield return SaveGameManager.JoinSaveGameThreadsCoroutine();
		Application.Quit();
	}

	public void BackToMainMenu()
	{
		StartCoroutine(LoadScene.LoadMainMenuFromCity());
	}

	public void LoadBackUpSave()
	{
		if (_backUpMetaData != null)
		{
			StartCoroutine(LoadBackUpSaveCoroutine());
		}
	}

	private IEnumerator LoadBackUpSaveCoroutine()
	{
		yield return SaveGameManager.JoinSaveGameThreadsCoroutine();
		TransitionToSave.saveToLoadData = _backUpMetaData;
		if (File.Exists(_backUpSavePath) && !File.Exists(_backUpMetaData.FilePath))
		{
			File.Copy(_backUpSavePath, _backUpMetaData.FilePath);
		}
		LoadScene.LoadTransitionToSave();
	}

	private SaveGameManager.SaveGameStruct GetBackUpMetaData()
	{
		string path = Path.Combine(SaveGamePathHelper.GetCharacterFolderPath(SaveGameManager.Current.characterId), "OldAgeBackUp");
		if (!Directory.Exists(path))
		{
			return null;
		}
		_backUpSavePath = Directory.GetFiles(path).FirstOrDefault((string x) => x.ToLower().EndsWith(".hsg"));
		if (_backUpSavePath == null)
		{
			return null;
		}
		string path2 = _backUpSavePath + ".meta";
		if (File.Exists(path2))
		{
			try
			{
				SaveGameManager.SaveGameStruct saveGameStruct = JsonConvert.DeserializeObject<SaveGameManager.SaveGameStruct>(File.ReadAllText(path2));
				if (saveGameStruct != null)
				{
					return saveGameStruct;
				}
			}
			catch (Exception)
			{
			}
		}
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(_backUpSavePath);
		GameInstance gameInstance = SaveGameSerializationHelper.DeserializeBinaryData(_backUpSavePath);
		SaveGameManager.SaveGameStruct saveGameStruct2 = new SaveGameManager.SaveGameStruct
		{
			name = fileNameWithoutExtension,
			CharacterPath = SaveGamePathHelper.GetCharacterFolderPath(SaveGameManager.Current.characterId),
			characterData = gameInstance.charactersData[0],
			day = gameInstance.Day,
			saveGameType = SaveGameManager.SaveGameStruct.SaveGameType.binary,
			lastPlayedDate = File.GetLastWriteTime(_backUpSavePath),
			isRecoverSave = false
		};
		File.WriteAllText(path2, JsonConvert.SerializeObject(saveGameStruct2));
		return saveGameStruct2;
	}
}
