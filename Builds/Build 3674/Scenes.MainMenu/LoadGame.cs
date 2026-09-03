using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BAModAPI;
using BigAmbitions.InputSystem;
using BigAmbitions.ModsInternal;
using Character.Customization;
using Extensions;
using Localizor;
using Localizor.LanguageChangeEvent;
using Newtonsoft.Json;
using UI.Load;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace Scenes.MainMenu;

public class LoadGame : MonoBehaviour
{
	[SerializeField]
	private MainMenuController mainMenu;

	[SerializeField]
	private Transform characterTemplate;

	[SerializeField]
	private GameObject startView;

	[SerializeField]
	private Sprite defaultSaveGameScreenshot;

	[SerializeField]
	private Toggle toggleRecoverSaves;

	[SerializeField]
	private GameObject brokenSaveGamesPopup;

	[SerializeField]
	private GameObject upgradeSaveGamesButton;

	[SerializeField]
	private ModBackupWarningUi modBackupWarningUi;

	private readonly List<Texture2D> _textures = new List<Texture2D>();

	private readonly List<Sprite> _sprites = new List<Sprite>();

	private bool _showRecoverSaves;

	private readonly List<string> _brokenSaveGames = new List<string>();

	private void OnEnable()
	{
		upgradeSaveGamesButton.SetActive(SaveGameCompatibilityHelper.HasSaveGamesInPreviousVersion());
	}

	public async void UpgradeSaveGames()
	{
		LoadingSpinner.Show();
		SaveGamePathHelper.CreateCurrentVersionSaveGameFolder();
		await SaveGameCompatibilityHelper.CopySaveGamesBetweenPreviousAndCurrentVersion();
		Notifications.Show(NotificationType.Success, "main_menu_savegames_upgraded_notification", null, 4f, null, null, notificationSound: true, trackOnSaveGame: false);
		InstanceBehavior<MainMenuController>.Instance.SetUpButtons();
		LoadSaveGames();
		LoadingSpinner.Hide();
	}

	private void Update()
	{
		if (PlayerAction.Cancel.Pressed())
		{
			if (HudConfirm.isOpen)
			{
				HudConfirm.onClose?.Invoke();
			}
			else
			{
				CloseLoadGames();
			}
		}
		if (PlayerAction.Confirm.Pressed() && HudConfirm.isOpen)
		{
			HudConfirm.onConfirm?.Invoke();
		}
		if ((Input.GetKey(KeyCode.LeftControl) && Input.GetKey(KeyCode.R)) || Input.GetKey(KeyCode.F5))
		{
			LoadSaveGames();
			mainMenu.SetUpButtons();
		}
	}

	public void CloseLoadGames()
	{
		base.gameObject.SetActive(value: false);
		startView.SetActive(value: true);
	}

	public void BrowseSaveGameFolder()
	{
		FolderHelper.OpenFolder(SaveGamePathHelper.CurrentVersionFolderPath());
	}

	public void LoadSaveGames()
	{
		if (TransitionToSave.saveGameLoadErrored)
		{
			_brokenSaveGames.Add(TransitionToSave.saveToLoadData.FilePath);
			ShowBrokenSaveGamesPopup();
		}
		CleanupTextures();
		characterTemplate.ResetTemplate();
		_showRecoverSaves = toggleRecoverSaves.isOn;
		if (!Directory.Exists(SaveGamePathHelper.CurrentVersionFolderPath()))
		{
			return;
		}
		List<CharacterSaveData> list = new List<CharacterSaveData>();
		string[] directories = Directory.GetDirectories(SaveGamePathHelper.CurrentVersionFolderPath());
		foreach (string text in directories)
		{
			List<SaveGameManager.SaveGameStruct> list2 = new List<SaveGameManager.SaveGameStruct>();
			foreach (string item in from x in Directory.GetFiles(text)
				where x.ToLower().EndsWith(".json") || x.ToLower().EndsWith(".hsg")
				select x)
			{
				try
				{
					string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(item);
					string path = item + ".meta";
					if (File.Exists(path))
					{
						try
						{
							SaveGameManager.SaveGameStruct saveGameStruct = JsonConvert.DeserializeObject<SaveGameManager.SaveGameStruct>(File.ReadAllText(path));
							if (saveGameStruct != null)
							{
								list2.Add(saveGameStruct);
								continue;
							}
							Debug.LogError("Savegame " + fileNameWithoutExtension + " could not be loaded");
						}
						catch (Exception)
						{
						}
					}
					if (Path.GetExtension(item) == ".hsg")
					{
						GameInstance gameInstance = SaveGameSerializationHelper.DeserializeBinaryData(item);
						List<CharacterData> charactersData = gameInstance.charactersData;
						CharacterData characterData = ((charactersData != null && charactersData.Count > 0) ? gameInstance.charactersData[0] : null);
						if (characterData == null)
						{
							Debug.LogWarning("Savegame " + fileNameWithoutExtension + " has no character data");
							continue;
						}
						SaveGameManager.SaveGameStruct saveGameStruct2 = new SaveGameManager.SaveGameStruct
						{
							name = fileNameWithoutExtension,
							CharacterPath = text,
							characterData = characterData,
							day = gameInstance.Day,
							saveGameType = SaveGameManager.SaveGameStruct.SaveGameType.binary,
							lastPlayedDate = File.GetLastWriteTime(item),
							isRecoverSave = (fileNameWithoutExtension.Length > 7 && fileNameWithoutExtension.Substring(0, 7) == "Recover")
						};
						File.WriteAllText(path, JsonConvert.SerializeObject(saveGameStruct2));
						list2.Add(saveGameStruct2);
					}
					else if (Path.GetExtension(item) == ".json")
					{
						GameInstance gameInstance2 = SaveGameSerializationHelper.DeserializeJsonData(item);
						List<CharacterData> charactersData = gameInstance2.charactersData;
						CharacterData characterData2 = ((charactersData != null && charactersData.Count > 0) ? gameInstance2.charactersData[0] : null);
						if (characterData2 == null)
						{
							Debug.LogWarning("Savegame " + fileNameWithoutExtension + " has no character data");
							continue;
						}
						SaveGameManager.SaveGameStruct saveGameStruct3 = new SaveGameManager.SaveGameStruct
						{
							name = fileNameWithoutExtension,
							CharacterPath = text,
							characterData = characterData2,
							day = gameInstance2.Day,
							saveGameType = SaveGameManager.SaveGameStruct.SaveGameType.json,
							lastPlayedDate = File.GetLastWriteTime(item),
							isRecoverSave = (fileNameWithoutExtension.Length > 7 && fileNameWithoutExtension.Substring(0, 7) == "Recover")
						};
						File.WriteAllText(path, JsonConvert.SerializeObject(saveGameStruct3));
						list2.Add(saveGameStruct3);
					}
				}
				catch (Exception arg)
				{
					Debug.LogError($"Couldn't load {item} due to: {arg}");
					_brokenSaveGames.Add(item);
				}
			}
			list2 = list2.OrderByDescending((SaveGameManager.SaveGameStruct x) => x.lastPlayedDate).ToList();
			list.Add(new CharacterSaveData
			{
				name = text,
				saveGames = list2
			});
		}
		foreach (CharacterSaveData item2 in list.OrderByDescending((CharacterSaveData x) => x.saveGames.FirstOrDefault()?.lastPlayedDate))
		{
			SetUpCharacter(item2);
		}
		if (_brokenSaveGames.Count != 0)
		{
			ShowBrokenSaveGamesPopup();
		}
		else
		{
			brokenSaveGamesPopup.SetActive(value: false);
		}
	}

	private void SetUpCharacter(CharacterSaveData character)
	{
		if (character.saveGames.Count != 0)
		{
			SaveGameManager.SaveGameStruct lastSaveGame = character.saveGames.FirstOrDefault((SaveGameManager.SaveGameStruct x) => !x.isRecoverSave) ?? character.saveGames[0];
			string characterName = "";
			if (lastSaveGame.characterData != null)
			{
				characterName = lastSaveGame.characterData.name;
			}
			if (characterName == null)
			{
				characterName = "No Name";
			}
			Transform entry = characterTemplate.CreateElement();
			LoadGameCharacterEntryView component = entry.GetComponent<LoadGameCharacterEntryView>();
			component.SetTitle(characterName);
			SetPortrait(lastSaveGame, component.PortraitImage);
			component.LoadLastGameModTooltip.Setup(lastSaveGame);
			component.LoadLastGameButton.onClick.AddListener(delegate
			{
				Load(lastSaveGame);
			});
			component.RemoveCharacterButton.onClick.AddListener(delegate
			{
				RemoveCharacter(characterName, character.name, entry);
			});
			component.Setup(character.saveGames, _showRecoverSaves, GetIcon, Load, DeleteSaveGame);
			entry.gameObject.SetActive(value: true);
		}
	}

	private void RemoveCharacter(string characterName, string characterPath, Transform entry)
	{
		LanguageChangeEventDataHolder bodyData = "main_menu_delete_character_confirm".Localize(new { characterName });
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			Directory.Delete(characterPath, recursive: true);
			UnityEngine.Object.Destroy(entry.gameObject);
			mainMenu.SetUpButtons();
		});
	}

	public void RecoverSaves()
	{
		_showRecoverSaves = toggleRecoverSaves.isOn;
		foreach (Transform item in characterTemplate.parent)
		{
			if (item.gameObject.activeSelf)
			{
				LoadGameCharacterEntryView component = item.GetComponent<LoadGameCharacterEntryView>();
				if (component.HasRecoverSaveEntries())
				{
					component.SetRecoverSavesVisible(_showRecoverSaves);
				}
			}
		}
	}

	private void DeleteSaveGame(SaveGameManager.SaveGameStruct saveGame, Transform gameEntry)
	{
		LanguageChangeEventDataHolder bodyData = "main_menu_delete_save_game_confirm".Localize(new
		{
			saveGameName = saveGame.name
		});
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			DeleteSaveFiles(saveGame.FilePath);
			Transform transform = gameEntry.parent?.parent?.parent;
			UnityEngine.Object.Destroy(gameEntry.gameObject);
			if (transform != null)
			{
				RefreshCharacterEntry(transform, saveGame.CharacterPath);
			}
			mainMenu.SetUpButtons();
		});
	}

	private void RefreshCharacterEntry(Transform characterEntry, string characterPath)
	{
		List<SaveGameManager.SaveGameStruct> list = new List<SaveGameManager.SaveGameStruct>();
		string[] files = Directory.GetFiles(characterPath, "*.meta");
		foreach (string path in files)
		{
			try
			{
				SaveGameManager.SaveGameStruct saveGameStruct = JsonConvert.DeserializeObject<SaveGameManager.SaveGameStruct>(File.ReadAllText(path));
				if (saveGameStruct != null)
				{
					list.Add(saveGameStruct);
				}
			}
			catch
			{
			}
		}
		if (list.Count == 0)
		{
			if (Directory.Exists(characterPath))
			{
				Directory.Delete(characterPath, recursive: true);
			}
			UnityEngine.Object.Destroy(characterEntry.gameObject);
			return;
		}
		SaveGameManager.SaveGameStruct lastSaveGame = null;
		for (int j = 0; j < list.Count; j++)
		{
			SaveGameManager.SaveGameStruct saveGameStruct2 = list[j];
			if (!saveGameStruct2.isRecoverSave && (lastSaveGame == null || saveGameStruct2.lastPlayedDate > lastSaveGame.lastPlayedDate))
			{
				lastSaveGame = saveGameStruct2;
			}
		}
		if (lastSaveGame == null)
		{
			for (int k = 0; k < list.Count; k++)
			{
				SaveGameManager.SaveGameStruct saveGameStruct3 = list[k];
				if (lastSaveGame == null || saveGameStruct3.lastPlayedDate > lastSaveGame.lastPlayedDate)
				{
					lastSaveGame = saveGameStruct3;
				}
			}
		}
		LoadGameCharacterEntryView component = characterEntry.GetComponent<LoadGameCharacterEntryView>();
		component.LoadLastGameButton.onClick.RemoveAllListeners();
		component.LoadLastGameButton.onClick.AddListener(delegate
		{
			Load(lastSaveGame);
		});
		component.LoadLastGameModTooltip.Setup(lastSaveGame);
		SetPortrait(lastSaveGame, component.PortraitImage);
	}

	private void DeleteSaveFiles(string filePath)
	{
		File.Delete(filePath);
		File.Delete(filePath + ".meta");
		File.Delete(Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + ".jpg"));
		File.Delete(Path.Combine(Path.GetDirectoryName(filePath), Path.GetFileNameWithoutExtension(filePath) + " portrait.jpg"));
	}

	public void DeleteBrokenSaves()
	{
		foreach (string brokenSaveGame in _brokenSaveGames)
		{
			DeleteSaveFiles(brokenSaveGame);
		}
		CloseBrokenSaveGameOverlay();
		LoadSaveGames();
	}

	public void MoveBrokenSaves()
	{
		foreach (string brokenSaveGame in _brokenSaveGames)
		{
			string text = brokenSaveGame.Replace("SaveGames", "BrokenSaveGames");
			Directory.CreateDirectory(Path.GetDirectoryName(text));
			if (File.Exists(text))
			{
				File.Delete(text);
			}
			File.Move(brokenSaveGame, text);
			if (File.Exists(text + ".meta"))
			{
				File.Delete(text + ".meta");
			}
			File.Move(brokenSaveGame + ".meta", text + ".meta");
			string text2 = Path.Combine(Path.GetDirectoryName(brokenSaveGame), Path.GetFileNameWithoutExtension(brokenSaveGame) + ".jpg");
			string text3 = Path.Combine(Path.GetDirectoryName(text), Path.GetFileNameWithoutExtension(text) + ".jpg");
			if (File.Exists(text2))
			{
				if (File.Exists(text3))
				{
					File.Delete(text3);
				}
				File.Move(text2, text3);
			}
		}
		CloseBrokenSaveGameOverlay();
		LoadSaveGames();
	}

	public void CloseBrokenSaveGameOverlay()
	{
		brokenSaveGamesPopup.SetActive(value: false);
		_brokenSaveGames.Clear();
		TransitionToSave.ResetStaticData();
		mainMenu.SetUpButtons();
	}

	public void Load(SaveGameManager.SaveGameStruct saveFile)
	{
		if (!saveFile.hasEverUsedMods && ModDiscoveryRegistry.AnyGameplayModsDiscovered())
		{
			modBackupWarningUi.Show(LoadTransition, saveFile.name + " (Modded)");
		}
		else if (saveFile.hasEverUsedMods && HasEnabledModsMismatch(saveFile.activeModsAtLastSave))
		{
			HudConfirm.Show(null, "mods_confirm_load_with_mods_mismatch", delegate
			{
				LoadTransition(null);
			}, null, "mods_confirm_load_anyway");
		}
		else
		{
			LoadTransition(null);
		}
		void LoadTransition(string fileName)
		{
			SaveGameManager.SaveGameStruct saveToLoadData = saveFile;
			if (!string.IsNullOrEmpty(fileName))
			{
				try
				{
					saveToLoadData = SaveGameManager.CreateSaveCopyWithNewName(saveFile, fileName);
				}
				catch (Exception arg)
				{
					Debug.LogError($"Could not create modded save copy from {saveFile.FilePath}: {arg}");
					return;
				}
			}
			TransitionToSave.saveToLoadData = saveToLoadData;
			LoadScene.LoadTransitionToSave();
		}
	}

	private static bool HasEnabledModsMismatch(List<SaveGameManager.SaveGameStruct.ActiveModAtSave> activeModsAtLastSave)
	{
		if (activeModsAtLastSave == null)
		{
			return false;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (KeyValuePair<ModActivationScope, List<DiscoveredModEntry>> entry in ModDiscoveryRegistry.Entries)
		{
			ModActivationScope key = entry.Key;
			if (key != ModActivationScope.Initialization && key != ModActivationScope.City)
			{
				continue;
			}
			List<DiscoveredModEntry> value = entry.Value;
			for (int i = 0; i < value.Count; i++)
			{
				DiscoveredModEntry discoveredModEntry = value[i];
				if (!string.IsNullOrWhiteSpace(discoveredModEntry?.ModId))
				{
					hashSet.Add(discoveredModEntry.ModId);
				}
			}
		}
		if (hashSet.Count != activeModsAtLastSave.Count)
		{
			return true;
		}
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int j = 0; j < activeModsAtLastSave.Count; j++)
		{
			SaveGameManager.SaveGameStruct.ActiveModAtSave activeModAtSave = activeModsAtLastSave[j];
			if (!string.IsNullOrWhiteSpace(activeModAtSave?.modId))
			{
				hashSet2.Add(activeModAtSave.modId);
			}
		}
		return !hashSet2.SetEquals(hashSet);
	}

	private void ShowBrokenSaveGamesPopup()
	{
		brokenSaveGamesPopup.SetActive(value: true);
		brokenSaveGamesPopup.transform.GetTmpInputByName("Window/Container/SaveGamesList").text = string.Join(Environment.NewLine, _brokenSaveGames);
	}

	public void CleanupTextures()
	{
		foreach (Texture2D texture in _textures)
		{
			UnityEngine.Object.Destroy(texture);
		}
		_textures.Clear();
		foreach (Sprite sprite in _sprites)
		{
			UnityEngine.Object.Destroy(sprite);
		}
		_sprites.Clear();
	}

	public Sprite GetIcon(SaveGameManager.SaveGameStruct saveGame)
	{
		string path = Path.Combine(saveGame.CharacterPath, saveGame.name + ".jpg");
		if (!File.Exists(path))
		{
			return defaultSaveGameScreenshot;
		}
		byte[] data = File.ReadAllBytes(path);
		Texture2D texture2D = new Texture2D(2, 2)
		{
			hideFlags = HideFlags.HideAndDontSave
		};
		_textures.Add(texture2D);
		if (texture2D.LoadImage(data))
		{
			Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
			_sprites.Add(sprite);
			return sprite;
		}
		return defaultSaveGameScreenshot;
	}

	public void SetPortrait(SaveGameManager.SaveGameStruct saveGame, Image image)
	{
		string characterPortraitPath = PortraitGenerator.GetCharacterPortraitPath(saveGame);
		if (!File.Exists(characterPortraitPath))
		{
			if (saveGame.characterData == null)
			{
				Debug.LogWarning(saveGame.FilePath + " has no character data, cannot generate portrait");
			}
			else
			{
				PortraitGenerator.Create(saveGame.characterData, PortraitGenerator.GetCharacterPortraitPath(saveGame), image);
			}
			return;
		}
		byte[] data = File.ReadAllBytes(characterPortraitPath);
		Texture2D texture2D = new Texture2D(2, 2)
		{
			hideFlags = HideFlags.HideAndDontSave
		};
		_textures.Add(texture2D);
		if (texture2D.LoadImage(data))
		{
			Sprite sprite = Sprite.Create(texture2D, new Rect(0f, 0f, texture2D.width, texture2D.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
			_sprites.Add(sprite);
			image.sprite = sprite;
		}
	}
}
