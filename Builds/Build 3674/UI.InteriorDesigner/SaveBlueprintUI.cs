using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BigAmbitions.BlueprintCreator;
using BigAmbitions.Items;
using BigAmbitions.ModsInternal;
using Blueprints;
using BlueprintsUI;
using Buildings;
using BusinessLayoutSets;
using IngameDebugConsole;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Notification;
using UnityEngine;

namespace UI.InteriorDesigner;

public class SaveBlueprintUI : MonoBehaviour
{
	[Serializable]
	public enum LayoutSaveOption
	{
		Local,
		Business,
		Interior
	}

	public static Action<bool> onClosed;

	private static List<string> Options;

	[SerializeField]
	private TMP_InputField blueprintNameInputField;

	private string _blueprintName;

	public bool IsOpen => base.gameObject.activeSelf;

	private void Awake()
	{
		Options = new List<string>
		{
			"common_blueprint".GetLocalization(),
			"blueprintcategory_devbusinesslayouts".GetLocalization(),
			"blueprintcategory_devinteriordesigns".GetLocalization()
		};
	}

	public void OpenPanel()
	{
		blueprintNameInputField.text = InstanceBehavior<BuildingManager>.Instance.buildingRegistration.blueprintName;
		base.gameObject.SetActive(value: true);
	}

	public void ClosePanel(bool hasSaved = false)
	{
		if (IsOpen)
		{
			base.gameObject.SetActive(value: false);
			onClosed?.Invoke(hasSaved);
		}
	}

	public void OnSaveLayoutAsBlueprint()
	{
		if (InstanceBehavior<BuildingManager>.Instance.allItemControllers.Any((ItemController x) => ItemsGetter.GetByName(x.itemName).isSpecialGift))
		{
			Notifications.ShowError("blueprint_notification_cannot_contain_special_gifts");
			return;
		}
		string text = blueprintNameInputField.text;
		if (string.IsNullOrEmpty(text) || text.Any((char x) => Path.GetInvalidFileNameChars().Contains(x) || x == '.'))
		{
			Notifications.ShowError("blueprints_ui_invalid_name");
			return;
		}
		_blueprintName = text;
		if (GameManager.IsDevMode)
		{
			DropdownSelector.Show(Options, OnSave, "common_save_as");
		}
		else
		{
			OnSave();
		}
	}

	private void OnSave(string saveChoice = null)
	{
		char[] illegalChars = Path.GetInvalidFileNameChars();
		if (_blueprintName.Any((char x) => illegalChars.Contains(x)))
		{
			string fixedName = string.Concat(_blueprintName.Split(Path.GetInvalidFileNameChars()));
			HudConfirm.Show("blueprints_ui_illegal_characters_warning".Localize(new { fixedName }), default(LanguageChangeEventDataHolder), delegate
			{
				BlueprintsFolderLoader.UnloadPlayerBlueprints();
				Save(fixedName, saveChoice);
			}, null, "common_accept", "common_retry");
		}
		else if (BlueprintsFolderLoader.IsBlueprintNameInUse(_blueprintName))
		{
			LanguageChangeEventDataHolder bodyData = "blueprints_ui_name_overwrite_confirm".Localize();
			HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
			{
				BlueprintsFolderLoader.UnloadPlayerBlueprints();
				Save(_blueprintName, saveChoice);
			});
		}
		else
		{
			BlueprintsFolderLoader.UnloadPlayerBlueprints();
			Save(_blueprintName, saveChoice);
		}
	}

	private async Task Save(string blueprintName, string saveChoice = null)
	{
		LayoutSaveOption option = LayoutSaveOption.Local;
		if (saveChoice == Options[1])
		{
			option = LayoutSaveOption.Business;
		}
		else if (saveChoice == Options[2])
		{
			option = LayoutSaveOption.Interior;
		}
		await SaveLayoutInternal(blueprintName, option);
	}

	private async Task SaveLayoutInternal(string blueprintName, LayoutSaveOption option)
	{
		LoadingSpinner.Show();
		BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
		Building building = InstanceBehavior<BuildingManager>.Instance.building;
		buildingRegistration.blueprintName = blueprintName;
		BusinessLayoutSet businessLayoutSet = BusinessLayoutSetHelper.Collect();
		businessLayoutSet.LayoutName = blueprintName;
		businessLayoutSet.BusinessType = buildingRegistration.businessTypeName;
		businessLayoutSet.BuildingSize = building.BuildingSize;
		businessLayoutSet.BuildingVersion = building.BuildingVersion;
		foreach (HashSet<string> value in ModLifecycleLoader.ActiveModIdsByScope.Values)
		{
			foreach (string item2 in value)
			{
				string item = (item2.All(char.IsDigit) ? item2 : "unknown");
				if (!businessLayoutSet.requiredModIds.Contains(item))
				{
					businessLayoutSet.requiredModIds.Add(item);
				}
			}
		}
		BuildingSizeInfo sizeInfo = new BuildingSizeInfo(building);
		switch (option)
		{
		case LayoutSaveOption.Local:
			await SaveLocalBlueprint(blueprintName, businessLayoutSet, buildingRegistration, building);
			break;
		case LayoutSaveOption.Business:
		{
			string thumbnailPath2 = BlueprintBusinessLayoutsController.GetThumbnailPath(building.BuildingType, sizeInfo, buildingRegistration.businessTypeName, blueprintName);
			string layoutPath2 = BlueprintBusinessLayoutsController.GetLayoutPath(sizeInfo, buildingRegistration.businessTypeName, blueprintName);
			await SaveDevBlueprint(thumbnailPath2, layoutPath2, businessLayoutSet, building);
			break;
		}
		case LayoutSaveOption.Interior:
		{
			string thumbnailPath = BlueprintInteriorDesignsController.GetThumbnailPath(building.BuildingType, sizeInfo, buildingRegistration.businessTypeName, blueprintName);
			string layoutPath = BlueprintInteriorDesignsController.GetLayoutPath(building.BuildingType, sizeInfo, buildingRegistration.businessTypeName, blueprintName);
			await SaveDevBlueprint(thumbnailPath, layoutPath, businessLayoutSet, building);
			break;
		}
		}
		if (option != LayoutSaveOption.Local)
		{
			ClosePanel(hasSaved: true);
			LoadingSpinner.Hide();
		}
	}

	private async Task SaveLocalBlueprint(string blueprintName, BusinessLayoutSet layoutSet, BuildingRegistration reg, Building bld)
	{
		int blueprintVersion = 0;
		ulong itemId = 0uL;
		BlueprintType blueprintType = BlueprintType.SavedLocally;
		Blueprint openWithBlueprint = BlueprintCreatorSystem.OpenWithBlueprint;
		if (openWithBlueprint != null && BlueprintsFolderLoader.IsBlueprintNameInUse(blueprintName))
		{
			blueprintVersion = ++openWithBlueprint.metadata.blueprintVersion;
			itemId = openWithBlueprint.metadata.itemId;
			blueprintType = openWithBlueprint.metadata.blueprintType;
		}
		BlueprintMetadata metadata = new BlueprintMetadata
		{
			blueprintVersion = blueprintVersion,
			itemId = itemId,
			blueprintType = blueprintType,
			buildNumber = GameVersion.GetCurrent().buildNumber,
			price = layoutSet.Items.Sum((BusinessLayoutSets.Item i) => ItemHelper.GetDefaultMarketPrice(i.itemName)),
			buildingType = bld.BuildingType,
			buildingSizeInfo = new BuildingSizeInfo(bld.BuildingSize, bld.BuildingVersion),
			requiredModIds = layoutSet.requiredModIds,
			otherData = reg.GetBlueprintDataElements(bld)
		};
		string folder = BlueprintsFolderLoader.GetBlueprintFolder(blueprintName);
		Directory.CreateDirectory(folder);
		string thumbnailPath = Path.Combine(folder, "Thumbnail.png");
		BuildingSizeInfo sizeInfo = new BuildingSizeInfo(bld);
		await InstanceBehavior<BuildingManager>.Instance.layoutScreenshotGenerator.GenerateThumbnails(thumbnailPath, sizeInfo);
		await metadata.Serialize(Path.Combine(folder, "Metadata.json"));
		await layoutSet.Serialize(Path.Combine(folder, "Layout.json"));
		ClosePanel(hasSaved: true);
		await BlueprintsFolderLoader.GetBlueprints();
		LoadingSpinner.Hide();
	}

	private static async Task SaveDevBlueprint(string thumbnailPath, string layoutPath, BusinessLayoutSet layoutSet, Building bld)
	{
		string directoryName = Path.GetDirectoryName(thumbnailPath);
		if (!string.IsNullOrEmpty(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		BuildingSizeInfo sizeInfo = new BuildingSizeInfo(bld);
		await InstanceBehavior<BuildingManager>.Instance.layoutScreenshotGenerator.GenerateThumbnails(thumbnailPath, sizeInfo);
		string directoryName2 = Path.GetDirectoryName(layoutPath);
		if (!string.IsNullOrEmpty(directoryName2))
		{
			Directory.CreateDirectory(directoryName2);
			await layoutSet.Serialize(layoutPath);
		}
		else
		{
			Debug.LogError("Failed to create layout directory for " + layoutPath);
		}
	}

	[ConsoleMethod("GenerateBlueprintThumbnail", "Generates a thumbnail for the current blueprint in Dev Mode. ", new string[] { })]
	public static void Command_GenerateBlueprintThumbnail(BlueprintType blueprintType)
	{
		if (!GameManager.IsDevMode)
		{
			Debug.LogError("GenerateBlueprintThumbnail only works in Dev Mode.");
			return;
		}
		if (blueprintType != BlueprintType.DevBusinessLayout && blueprintType != BlueprintType.DevInteriorDesign)
		{
			Debug.LogError("GenerateBlueprintThumbnail cannot be used with Local option.");
			return;
		}
		BuildingRegistration buildingRegistration = InstanceBehavior<BuildingManager>.Instance.buildingRegistration;
		Building building = InstanceBehavior<BuildingManager>.Instance.building;
		string text = buildingRegistration?.blueprintName;
		if (string.IsNullOrEmpty(text) || building == null)
		{
			Debug.LogError("Cannot generate thumbnail: missing blueprint name or building instance.");
		}
		else
		{
			CoroutineUtility.Run(GenerateDevThumbnailCoroutine(blueprintType, text, building, buildingRegistration.businessTypeName));
		}
	}

	private static IEnumerator GenerateDevThumbnailCoroutine(BlueprintType blueprintType, string blueprintName, Building bld, string businessType)
	{
		BuildingSizeInfo sizeInfo = new BuildingSizeInfo(bld);
		string thumbPath = ((blueprintType == BlueprintType.DevBusinessLayout) ? BlueprintBusinessLayoutsController.GetThumbnailPath(bld.BuildingType, sizeInfo, businessType, blueprintName) : BlueprintInteriorDesignsController.GetThumbnailPath(bld.BuildingType, sizeInfo, businessType, blueprintName));
		string directoryName = Path.GetDirectoryName(thumbPath);
		if (!string.IsNullOrEmpty(directoryName))
		{
			Directory.CreateDirectory(directoryName);
		}
		Task thumbnailTask = InstanceBehavior<BuildingManager>.Instance.layoutScreenshotGenerator.GenerateThumbnails(thumbPath, sizeInfo);
		yield return new WaitUntil(() => thumbnailTask.IsCompleted);
		if (thumbnailTask.Exception != null)
		{
			Debug.LogError($"Failed to generate thumbnail: {thumbnailTask.Exception}");
		}
		else
		{
			Debug.Log($"{blueprintType} thumbnail generated at '{thumbPath}'.");
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		Options = null;
		onClosed = null;
	}
}
