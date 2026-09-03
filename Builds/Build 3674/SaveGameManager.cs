using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using BAModAPI;
using BigAmbitions.ModsInternal;
using BigAmbitions.PlacementSystem;
using BigAmbitions.Rivals;
using BigAmbitions.SaveSystem;
using Blueprints;
using Character.Customization;
using Entities;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Newtonsoft.Json;
using OdinSerializer;
using Player.SaveSystem.CompatibilityFixes;
using PlayerActivity;
using UI;
using UI.InteriorDesigner;
using UI.Load;
using UI.Notification;
using UnityEngine;
using UnityEngine.Serialization;

public static class SaveGameManager
{
	[Serializable]
	public class SaveGameStruct
	{
		[Serializable]
		public enum SaveGameType
		{
			json,
			binary
		}

		public class ActiveModAtSave
		{
			public string modId;

			public string modDisplayName;
		}

		[FormerlySerializedAs("Name")]
		public string name;

		public string characterId;

		public CharacterData characterData;

		public SaveGameType saveGameType;

		public DateTime lastPlayedDate;

		public bool isRecoverSave;

		public int day;

		public string description;

		public string alias;

		public bool isTemporary;

		public List<string> tags;

		public bool hasEverUsedMods;

		public List<ActiveModAtSave> activeModsAtLastSave;

		public string CharacterPath
		{
			get
			{
				return SaveGamePathHelper.GetCharacterFolderPath(characterId);
			}
			set
			{
				characterId = value.Split('\\', '/')[^1];
			}
		}

		[IgnoreDataMember]
		public string FilePath => Path.Combine(CharacterPath, name + "." + ((saveGameType == SaveGameType.json) ? "json" : "hsg"));
	}

	private struct SaveGameThreadWrapper
	{
		public GameInstance SaveGame;

		public string Path;

		public bool SaveAsJson;
	}

	private struct CompressionThreadWrapper
	{
		public string inputPath;

		public string outputPath;
	}

	public enum SaveType
	{
		Default,
		RecoverSave,
		OldAgeBackUp,
		MidnightSave
	}

	private static GameInstance _current;

	private static bool _hasChangeSinceLastSave;

	private static int _saveProcessesRunning;

	private static Thread _saveGameSaveThread;

	public static GameInstance Current
	{
		get
		{
			return _current;
		}
		set
		{
			_current = value;
			_hasChangeSinceLastSave = false;
		}
	}

	public static bool IsModdedSave
	{
		get
		{
			if (!ModLifecycleLoader.AnyGameplayModsLoaded)
			{
				return _current?.hasEverUsedMods ?? false;
			}
			return true;
		}
	}

	public static bool SavingGameInProgress => _saveProcessesRunning > 0;

	public static bool Save(SaveType saveType, string saveGameName = null, string characterFolder = null)
	{
		if (!CanSave())
		{
			if (saveType == SaveType.Default && (bool)InstanceBehavior<UIs>.Instance)
			{
				Notifications.ShowError("notification_save_not_allowed");
			}
			return false;
		}
		Thread saveGameSaveThread = _saveGameSaveThread;
		if (saveGameSaveThread != null && saveGameSaveThread.ThreadState == System.Threading.ThreadState.Running)
		{
			_saveGameSaveThread.Join();
		}
		_saveProcessesRunning = 2;
		if (ModLifecycleLoader.AnyGameplayModsLoaded)
		{
			Current.hasEverUsedMods = true;
		}
		GlobalEvents.onSaveGame?.Invoke();
		Current.buildNumberAtLastSave = GameVersion.GetCurrent().buildNumber;
		if (saveGameName != null)
		{
			saveGameName = FileSystemHelper.MakeValidFilename(saveGameName);
		}
		switch (saveType)
		{
		case SaveType.RecoverSave:
			saveGameName = $"Recover #{Current.currentAutoSaveNumber}";
			Current.currentAutoSaveNumber++;
			if (Current.currentAutoSaveNumber >= PlayerPrefSettings.MaxAutoSavesPerGame)
			{
				Current.currentAutoSaveNumber = 0;
			}
			break;
		case SaveType.MidnightSave:
			saveGameName = "Recover Midnight";
			break;
		default:
			Current.SaveGameName = saveGameName;
			break;
		}
		SaveGamePathHelper.CreateCurrentVersionSaveGameFolder();
		if (characterFolder == null)
		{
			characterFolder = SaveGamePathHelper.GetCharacterFolderPath(Current.characterId);
		}
		PortraitGenerator.Create(Current.charactersData.First(), PortraitGenerator.GetCharacterPortraitPath(Current, characterFolder));
		GenerateSaveGameTexture(Path.Combine(characterFolder, saveGameName + ".jpg"));
		string text = Path.Combine(characterFolder, saveGameName + "." + (InstanceBehavior<GameManager>.Instance.useSaveGameTypeJson ? "json" : "hsg"));
		SaveGameThreadWrapper saveGameThreadWrapper = new SaveGameThreadWrapper
		{
			Path = SaveGamePathHelper.GetTempSavePath(),
			SaveGame = Current,
			SaveAsJson = InstanceBehavior<GameManager>.Instance.useSaveGameTypeJson
		};
		File.Delete(saveGameThreadWrapper.Path);
		SerializeSaveGame(saveGameThreadWrapper);
		_saveGameSaveThread = new Thread(CompressSaveGame)
		{
			Priority = System.Threading.ThreadPriority.Highest,
			Name = "SaveGame Compress Thread"
		};
		_saveGameSaveThread.Start(new CompressionThreadWrapper
		{
			inputPath = saveGameThreadWrapper.Path,
			outputPath = text
		});
		List<SaveGameStruct.ActiveModAtSave> list = new List<SaveGameStruct.ActiveModAtSave>();
		foreach (var activeMod in ModLifecycleLoader.GetActiveMods())
		{
			SaveGameStruct.ActiveModAtSave activeModAtSave = new SaveGameStruct.ActiveModAtSave();
			(activeModAtSave.modId, activeModAtSave.modDisplayName) = activeMod;
			list.Add(activeModAtSave);
		}
		SaveGameStruct saveGameStruct = new SaveGameStruct
		{
			name = saveGameName,
			CharacterPath = characterFolder,
			characterData = Current.charactersData[0],
			saveGameType = ((!InstanceBehavior<GameManager>.Instance.useSaveGameTypeJson) ? SaveGameStruct.SaveGameType.binary : SaveGameStruct.SaveGameType.json),
			lastPlayedDate = DateTime.Now,
			isRecoverSave = (saveType == SaveType.RecoverSave || saveType == SaveType.MidnightSave),
			day = Current.Day,
			hasEverUsedMods = Current.hasEverUsedMods,
			activeModsAtLastSave = list
		};
		File.WriteAllText(text + ".meta", JsonConvert.SerializeObject(saveGameStruct));
		if (saveType == SaveType.Default)
		{
			PlayerPrefSettings.LastSaveGameName = GetSaveGamePrettyName(saveGameStruct);
		}
		_hasChangeSinceLastSave = false;
		return true;
	}

	private static void SerializeSaveGame(object data)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		try
		{
			SaveGameThreadWrapper saveGameThreadWrapper = (SaveGameThreadWrapper)data;
			if (saveGameThreadWrapper.SaveAsJson ? SaveGameSerializationHelper.SerializeJsonData(saveGameThreadWrapper.Path, saveGameThreadWrapper.SaveGame) : SaveGameSerializationHelper.SerializeBinaryData(saveGameThreadWrapper.Path, saveGameThreadWrapper.SaveGame, compressed: false))
			{
				UnityEngine.Debug.Log($"Game {saveGameThreadWrapper.Path} saved in {stopwatch.ElapsedMilliseconds} ms");
			}
			stopwatch.Stop();
		}
		catch (Exception exception)
		{
			UnityEngine.Debug.Log("Save game serialization failed:");
			UnityEngine.Debug.LogException(exception);
		}
	}

	private static void CompressSaveGame(object data)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		CompressionThreadWrapper compressionThreadWrapper = (CompressionThreadWrapper)data;
		if (SaveGameSerializationHelper.CompressBinaryData(compressionThreadWrapper.inputPath, compressionThreadWrapper.outputPath))
		{
			UnityEngine.Debug.Log($"Compressed to {compressionThreadWrapper.outputPath} in {stopwatch.ElapsedMilliseconds} ms");
		}
		stopwatch.Stop();
		_saveProcessesRunning--;
	}

	public static string GetSaveGamePrettyName(SaveGameStruct savegame)
	{
		return savegame.CharacterPath.Replace(SaveGamePathHelper.CurrentVersionFolderPath() + "\\", "") + "/" + savegame.name;
	}

	public static bool Load(SaveGameStruct saveGame, bool loadScene = true)
	{
		if (!TryPrepareLoad(ref saveGame, out var loadStopwatch))
		{
			return false;
		}
		try
		{
			Current = ((saveGame.saveGameType == SaveGameStruct.SaveGameType.json) ? SaveGameSerializationHelper.DeserializeJsonData(saveGame.FilePath) : SaveGameSerializationHelper.DeserializeBinaryData(saveGame.FilePath));
			InitializeLoadedSave();
		}
		catch (Exception exception)
		{
			UnityEngine.Debug.LogException(exception);
			return false;
		}
		return CompleteLoad(saveGame, loadScene, loadStopwatch);
	}

	public static async Task<bool> LoadAsync(SaveGameStruct saveGame, bool loadScene = true)
	{
		if (!TryPrepareLoad(ref saveGame, out var loadStopwatch))
		{
			return false;
		}
		try
		{
			GameInstance current = ((saveGame.saveGameType != SaveGameStruct.SaveGameType.json) ? (await SaveGameSerializationHelper.DeserializeBinaryDataAsync(saveGame.FilePath)) : (await SaveGameSerializationHelper.DeserializeJsonDataAsync(saveGame.FilePath)));
			Current = current;
			InitializeLoadedSave();
		}
		catch (Exception exception)
		{
			UnityEngine.Debug.LogException(exception);
			return false;
		}
		return CompleteLoad(saveGame, loadScene, loadStopwatch);
	}

	private static bool TryPrepareLoad(ref SaveGameStruct saveGame, out Stopwatch loadStopwatch)
	{
		loadStopwatch = null;
		if (saveGame == null)
		{
			string saveGameName = PlayerPrefSettings.LastSaveGameName;
			saveGame = SaveGamePathHelper.GetAllSaveGamesFromVersion().Find((SaveGameStruct s) => GetSaveGamePrettyName(s) == saveGameName);
		}
		if (saveGame == null || !File.Exists(saveGame.FilePath))
		{
			return false;
		}
		GlobalEvents.Init();
		AddressableLoader.RegisterAndLoadAll();
		return true;
	}

	private static void InitializeLoadedSave()
	{
		ItemHelper.Init();
		SaveGameCompatibilityFixes.ApplyCompatibilityFixes(Current);
	}

	private static bool CompleteLoad(SaveGameStruct saveGame, bool loadScene, Stopwatch loadStopwatch = null)
	{
		Current.SaveGameName = saveGame.name;
		if (string.IsNullOrEmpty(Current.characterId))
		{
			Current.characterId = UuidHelper.GenerateBase64Uuid();
			string characterFolderPath = SaveGamePathHelper.GetCharacterFolderPath(Current.characterId);
			string text = Path.Combine(characterFolderPath, Current.SaveGameName + ".json");
			List<CharacterData> charactersData = Current.charactersData;
			if (charactersData != null && charactersData.Count > 0)
			{
				string characterFolderPath2 = SaveGamePathHelper.GetCharacterFolderPath(Current.charactersData[0].name);
				string text2 = Path.Combine(characterFolderPath2, Current.SaveGameName + ".json");
				if (File.Exists(text2))
				{
					File.Move(text2, text);
				}
				string text3 = text2.Replace(".json", ".jpg");
				string destFileName = text.Replace(".json", ".jpg");
				if (File.Exists(text3))
				{
					File.Move(text3, destFileName);
				}
				if (!Directory.EnumerateFileSystemEntries(characterFolderPath2).Any())
				{
					Directory.Delete(characterFolderPath2);
				}
			}
			SaveGameThreadWrapper saveGameThreadWrapper = new SaveGameThreadWrapper
			{
				Path = text,
				SaveGame = CreateSaveSnapshot(Current),
				SaveAsJson = true
			};
			_saveGameSaveThread = new Thread(SerializeSaveGame)
			{
				Priority = System.Threading.ThreadPriority.Highest,
				Name = "SaveGame Serialisation Thread"
			};
			_saveGameSaveThread.Start(saveGameThreadWrapper);
			PlayerPrefSettings.LastSaveGameName = GetSaveGamePrettyName(new SaveGameStruct
			{
				name = saveGame.name,
				CharacterPath = characterFolderPath
			});
		}
		if (loadScene)
		{
			LoadScene.LoadGame(ModActivationScope.MainMenu);
		}
		else
		{
			LoadScene.isLoading = true;
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				LoadScene.isLoading = false;
				GlobalEvents.InvokeOnGameLoaded();
			});
		}
		return true;
	}

	private static void GenerateSaveGameTexture(string screenshotPath)
	{
		if ((bool)InstanceBehavior<GameManager>.Instance && (bool)InstanceBehavior<GameManager>.Instance.saveGameCameraCaptureController)
		{
			Texture2D renderResult = new Texture2D(TextureHelper.SaveGameTextureSize.x, TextureHelper.SaveGameTextureSize.y, TextureFormat.ARGB32, mipChain: false)
			{
				hideFlags = HideFlags.HideAndDontSave
			};
			Transform transform = GameManager.GetMainCamera().transform;
			ScreenshotCaptureController.CaptureCommand command = new ScreenshotCaptureController.CaptureCommand
			{
				width = TextureHelper.SaveGameTextureSize.x,
				height = TextureHelper.SaveGameTextureSize.y,
				outputRect = new Rect(0f, 0f, renderResult.width, renderResult.height),
				position = transform.position,
				rotation = transform.rotation,
				outputTexture = renderResult,
				onCaptured = delegate(ScreenshotCaptureController.CaptureCommand captureCommand)
				{
					File.WriteAllBytes(screenshotPath, renderResult.EncodeToJPG(80));
					_saveProcessesRunning--;
					UnityEngine.Object.Destroy(captureCommand.outputTexture);
				}
			};
			InstanceBehavior<GameManager>.Instance.saveGameCameraCaptureController.Capture(command);
		}
	}

	private static GameInstance CreateSaveSnapshot(GameInstance gameInstance)
	{
		return (GameInstance)SerializationUtility.CreateCopy(gameInstance);
	}

	public static void New(GameVariables difficulty)
	{
		int buildNumber = GameVersion.GetCurrent().buildNumber;
		Current = new GameInstance
		{
			buildNumberAtStart = buildNumber,
			buildNumberAtLastSave = buildNumber,
			characterId = UuidHelper.GenerateBase64Uuid(),
			seed = RngHelper.GenerateGameSeed(),
			Day = 1,
			Hour = 8,
			Minute = 0f,
			Money = difficulty.startingMoney,
			Energy = (difficulty.tutorialEnabled ? 50f : 100f),
			Hunger = 100f,
			Happiness = 100f,
			CurrentStreetName = string.Empty,
			CurrentStreetNumber = 0,
			BuildingRegistrations = new List<BuildingRegistration>(),
			VehicleInstances = new List<VehicleInstance>(),
			gameVariables = difficulty
		};
		AddressableLoader.RegisterAndLoadAll();
		EmployeePreset employeePreset = BusinessTypeHelper.GetData("ba:businesstype_giftshop").uniforms[0].Copy();
		employeePreset.name = "common_default".GetLocalization();
		Current.employeePresets = new List<EmployeePreset> { employeePreset };
		if (difficulty.allCoursesUnlocked)
		{
			EducationHelper.UnlockAllCourses();
		}
		if (difficulty.disableHappiness)
		{
			Current.happinessModifiers.Add(new HappinessModifierData
			{
				type = "ba:happinessmodifier_cheat",
				hoursLeft = -1
			});
			HappinessHelper.UpdateHappiness();
		}
		if (difficulty.disableEnergy)
		{
			Current.Energy = 100f;
		}
		Current.Contacts = new List<Contact>();
		if (difficulty.tutorialEnabled)
		{
			TutorialHelper.NewTutorial();
		}
		Current.specialRivalStates = new List<SpecialRivalState>();
		Current.NeighbourhoodStats = NeighborhoodHelper.GenerateNeighbourHoodStats();
		RivalsHelper.GenerateRivals(Current);
		HappinessHelper.AddModifier("ba:happinessmodifier_no_home");
		HappinessHelper.AddModifier("ba:happinessmodifier_first_day_on_ny");
		if (difficulty.tutorialEnabled)
		{
			HappinessHelper.AddModifier("ba:happinessmodifier_a_fresh_start");
		}
		Current.SelectedCitymapFilters.Add("buildingresume_rented_by_you");
	}

	public static void ApplyNewDifficulty(GameVariables gameVariables)
	{
		GameVariables gameVariables2 = Current.gameVariables;
		Current.gameVariables = gameVariables;
		Current.gameVariables.startingAge = gameVariables2.startingAge;
		Current.gameVariables.startingMoney = gameVariables2.startingMoney;
		Current.gameVariables.daysPerYear = gameVariables2.daysPerYear;
		Current.gameVariables.tutorialEnabled = gameVariables2.tutorialEnabled;
		if (gameVariables.allCoursesUnlocked && !gameVariables2.allCoursesUnlocked)
		{
			EducationHelper.UnlockAllCourses();
		}
		if (gameVariables.allContactsUnlocked && !gameVariables2.allContactsUnlocked)
		{
			ContactsHelper.UnlockAllContacts();
		}
		if (gameVariables.disableHappiness != gameVariables2.disableHappiness)
		{
			if (gameVariables.disableHappiness)
			{
				Current.happinessModifiers.Add(new HappinessModifierData
				{
					type = "ba:happinessmodifier_cheat",
					hoursLeft = -1
				});
			}
			else
			{
				Current.happinessModifiers.RemoveAll((HappinessModifierData x) => x.type == "ba:happinessmodifier_cheat");
			}
			HappinessHelper.UpdateHappiness();
		}
		if (gameVariables.disableEnergy)
		{
			Current.Energy = 100f;
		}
		if (!Mathf.Approximately(gameVariables.bankInterestMultiplier, gameVariables2.bankInterestMultiplier))
		{
			LoanHelper.RecalculateLoanPayments(Current);
		}
		if (gameVariables.disableVehicleFuel != gameVariables2.disableVehicleFuel)
		{
			ApplyFuelMode(gameVariables.disableVehicleFuel);
		}
	}

	public static void JoinSaveGameThreads()
	{
		Thread saveGameSaveThread = _saveGameSaveThread;
		if (saveGameSaveThread != null && saveGameSaveThread.ThreadState == System.Threading.ThreadState.Running)
		{
			_saveGameSaveThread.Join();
		}
	}

	public static IEnumerator JoinSaveGameThreadsCoroutine()
	{
		JoinSaveGameThreads();
		yield return new WaitUntil(() => !SavingGameInProgress);
	}

	private static bool CanSave()
	{
		if (CasinoBoatManager.IsOnCasinoBoat)
		{
			return false;
		}
		if (InteriorDesignerUI.IsOpen)
		{
			return false;
		}
		if (PlayerActivityUI.IsPanelOpen && BuildingManager.IsInsideBuilding && InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName == "ba:businesstype_school")
		{
			return false;
		}
		if (PlacementSystem.IsInPlacementMode)
		{
			return false;
		}
		return true;
	}

	public static bool MarkChange()
	{
		if (_hasChangeSinceLastSave)
		{
			return false;
		}
		_hasChangeSinceLastSave = true;
		return true;
	}

	public static bool HasChangesSinceLastSave()
	{
		if (_hasChangeSinceLastSave)
		{
			return true;
		}
		if (Current == null)
		{
			return false;
		}
		return (Current.LastPlayerPosition - PlayerHelper.GetPosition()).sqrMagnitude > 1f;
	}

	public static SaveGameStruct CreateSaveCopyWithNewName(SaveGameStruct originalSave, string requestedSaveName)
	{
		string extension = Path.GetExtension(originalSave.FilePath);
		string text = FileSystemHelper.MakeValidFilename(requestedSaveName);
		if (string.IsNullOrWhiteSpace(text))
		{
			throw new InvalidOperationException("Invalid save name");
		}
		string availableSaveName = GetAvailableSaveName(originalSave.CharacterPath, text, extension);
		string text2 = Path.Combine(originalSave.CharacterPath, availableSaveName + extension);
		File.Copy(originalSave.FilePath, text2, overwrite: false);
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalSave.FilePath);
		string text3 = Path.Combine(originalSave.CharacterPath, fileNameWithoutExtension + ".jpg");
		string text4 = Path.Combine(originalSave.CharacterPath, fileNameWithoutExtension + " portrait.jpg");
		string destFileName = Path.Combine(originalSave.CharacterPath, availableSaveName + ".jpg");
		string destFileName2 = Path.Combine(originalSave.CharacterPath, availableSaveName + " portrait.jpg");
		if (File.Exists(text3))
		{
			File.Copy(text3, destFileName, overwrite: false);
		}
		if (File.Exists(text4))
		{
			File.Copy(text4, destFileName2, overwrite: false);
		}
		SaveGameStruct saveGameStruct = new SaveGameStruct
		{
			name = availableSaveName,
			CharacterPath = originalSave.CharacterPath,
			characterData = originalSave.characterData,
			saveGameType = originalSave.saveGameType,
			lastPlayedDate = DateTime.Now,
			isRecoverSave = originalSave.isRecoverSave,
			day = originalSave.day,
			description = originalSave.description,
			alias = originalSave.alias,
			isTemporary = originalSave.isTemporary,
			tags = ((originalSave.tags == null) ? null : new List<string>(originalSave.tags)),
			hasEverUsedMods = true,
			activeModsAtLastSave = originalSave.activeModsAtLastSave
		};
		File.WriteAllText(text2 + ".meta", JsonConvert.SerializeObject(saveGameStruct));
		return saveGameStruct;
	}

	private static string GetAvailableSaveName(string characterPath, string baseName, string extension)
	{
		string text = baseName;
		int num = 1;
		while (File.Exists(Path.Combine(characterPath, text + extension)))
		{
			text = $"{baseName} {num}";
			num++;
		}
		return text;
	}

	private static void ApplyFuelMode(bool disableVehicleFuel)
	{
		CarController[] array = UnityEngine.Object.FindObjectsByType<CarController>(FindObjectsSortMode.None);
		foreach (CarController carController in array)
		{
			if (Current.VehicleInstances.Contains(carController.vehicleInstance))
			{
				carController.fuelModule.useFuel = !disableVehicleFuel;
				if (disableVehicleFuel)
				{
					carController.fuelModule.amount = carController.fuelModule.capacity;
					carController.vehicleInstance.fuel = carController.fuelModule.amount;
				}
			}
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		_current = null;
		_hasChangeSinceLastSave = false;
		_saveProcessesRunning = 0;
		_saveGameSaveThread = null;
	}
}
