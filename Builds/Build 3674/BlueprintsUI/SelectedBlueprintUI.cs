using System;
using System.IO;
using System.Threading.Tasks;
using BigAmbitions.BlueprintCreator;
using Blueprints;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using Player.SaveSystem.CompatibilityFixes;
using Steamworks;
using Steamworks.Ugc;
using TMPro;
using UI.Load;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace BlueprintsUI;

public class SelectedBlueprintUI : MonoBehaviour
{
	[SerializeField]
	private TMP_Text titleLabel;

	[SerializeField]
	private TextLocalizationComponent authorLabel;

	[SerializeField]
	private Image thumbnail;

	[SerializeField]
	private TMP_Text downloadsAmountLabel;

	[SerializeField]
	private TMP_Text releasedLabel;

	[SerializeField]
	private Image ratingImage;

	[SerializeField]
	private Transform otherDataTransform;

	[SerializeField]
	private Button addToYourLibraryButton;

	[SerializeField]
	private GameObject steamInfoGameObject;

	[SerializeField]
	private GameObject missingModsWarning;

	[SerializeField]
	private BasicTooltip olderGameVersionWarningTooltip;

	[Header("Buttons")]
	[SerializeField]
	private GameObject topButtonsSection;

	[SerializeField]
	private Button openInBlueprintCreatorButton;

	[SerializeField]
	private Button uploadToWorkshopButton;

	[SerializeField]
	private Button removeFromWorkshopButton;

	[SerializeField]
	private Button removeFromLibraryButton;

	[SerializeField]
	private BasicTooltip uploadToWorkshopTooltip;

	private Blueprint _selectedBlueprint;

	public Action reloadBlueprints;

	public bool IsOpen => base.gameObject.activeSelf;

	private void OnEnable()
	{
		CheckMissingModsWarning();
	}

	public void Show(Blueprint blueprint)
	{
		ShowAsync(blueprint);
	}

	private async Task ShowAsync(Blueprint blueprint)
	{
		_selectedBlueprint = blueprint;
		HandleOlderGameVersionWarning(blueprint.metadata.buildNumber);
		if (_selectedBlueprint.metadata.blueprintType != BlueprintType.SavedLocally && SteamHelper.IsConnectedToSteam())
		{
			SetUpSteamInfo();
		}
		else
		{
			steamInfoGameObject.SetActive(value: false);
		}
		titleLabel.text = blueprint.name;
		SetUpAuthor(blueprint);
		blueprint.ShowThumbnail(thumbnail);
		otherDataTransform.ResetTemplate();
		SelectedBlueprintElementUI component = otherDataTransform.CreateElement().GetComponent<SelectedBlueprintElementUI>();
		component.label.Key = "common_building_size";
		component.value.Key = "common_placeholder";
		component.value.Arguments = new
		{
			data = blueprint.metadata.buildingSizeInfo.ToString()
		};
		SelectedBlueprintElementUI component2 = otherDataTransform.GetComponent<SelectedBlueprintElementUI>();
		component2.label.Key = "common_building_type";
		component2.value.Key = blueprint.metadata.buildingType;
		SelectedBlueprintElementUI component3 = otherDataTransform.CreateElement().GetComponent<SelectedBlueprintElementUI>();
		component3.label.Key = "common_price";
		component3.value.Key = "common_placeholder";
		string data = blueprint.metadata.FullPriceWithInstallation.ToShortCurrencyFormat();
		component3.value.Arguments = new { data };
		foreach (BlueprintDataElement otherDatum in blueprint.metadata.otherData)
		{
			SelectedBlueprintElementUI component4 = otherDataTransform.CreateElement().GetComponent<SelectedBlueprintElementUI>();
			component4.label.Key = otherDatum.dataElement.GetLocalizeKey();
			string data2 = ((otherDatum.dataElement == DataElement.BusinessTypeName) ? otherDatum.value.GetLocalization() : otherDatum.value);
			component4.value.Key = "common_placeholder";
			component4.value.Arguments = new
			{
				data = data2
			};
		}
		addToYourLibraryButton.gameObject.SetActive(blueprint.metadata.blueprintType == BlueprintType.Workshop && !BlueprintsFolderLoader.IsWorkshopBlueprintInLibrary(blueprint.metadata.itemId));
		BlueprintType blueprintType = blueprint.metadata.blueprintType;
		int num;
		if (blueprintType != BlueprintType.SavedLocally && blueprintType != BlueprintType.SavedFromWorkshop && blueprintType != BlueprintType.UploadedToWorkshop)
		{
			blueprintType = blueprint.metadata.blueprintType;
			if ((blueprintType != BlueprintType.DevBusinessLayout && blueprintType != BlueprintType.DevInteriorDesign && blueprintType != BlueprintType.FeedbackSystem) || !GameManager.IsDevMode)
			{
				num = 0;
				goto IL_02c6;
			}
		}
		num = ((InstanceBehavior<MainMenuController>.Instance != null) ? 1 : 0);
		goto IL_02c6;
		IL_02c6:
		bool canOpenInBlueprintCreator = (byte)num != 0;
		bool canUploadToWorkshop = blueprint.metadata.blueprintType == BlueprintType.SavedLocally;
		bool canUpdateToWorkshop = await BlueprintsListUI.LibraryController.CanUpdateToWorkshop(blueprint);
		blueprintType = blueprint.metadata.blueprintType;
		bool canRemoveFromLibrary = blueprintType == BlueprintType.SavedLocally || blueprintType == BlueprintType.SavedFromWorkshop || blueprintType == BlueprintType.FeedbackSystem;
		bool canRemoveFromWorkshop = blueprint.metadata.blueprintType == BlueprintType.UploadedToWorkshop;
		openInBlueprintCreatorButton.gameObject.SetActive(canOpenInBlueprintCreator);
		uploadToWorkshopButton.gameObject.SetActive(canUploadToWorkshop | canUpdateToWorkshop);
		removeFromLibraryButton.gameObject.SetActive(canRemoveFromLibrary);
		removeFromWorkshopButton.gameObject.SetActive(canRemoveFromWorkshop);
		if (canUpdateToWorkshop | canUploadToWorkshop)
		{
			BusinessLayoutSet layoutSet = await blueprint.GetLayout(validate: false);
			uploadToWorkshopButton.interactable = !CompatibilityBlueprintValidator.ContainsInvalidItems(layoutSet);
		}
		uploadToWorkshopTooltip.descriptionKey = (canUpdateToWorkshop ? "blueprint_info_tooltip_update_to_workshop" : "blueprint_info_tooltip_upload_to_workshop");
		topButtonsSection.SetActive(canOpenInBlueprintCreator | canUploadToWorkshop | canUpdateToWorkshop | canRemoveFromLibrary | canRemoveFromWorkshop);
		base.gameObject.SetActive(value: true);
	}

	private void SetUpSteamInfo()
	{
		steamInfoGameObject.SetActive(value: true);
		downloadsAmountLabel.text = _selectedBlueprint.downloads.ToFormattedNumber();
		releasedLabel.text = _selectedBlueprint.releaseDate.ToShortDateString();
		ratingImage.fillAmount = _selectedBlueprint.rating;
	}

	private void SetUpAuthor(Blueprint blueprint)
	{
		BlueprintType blueprintType = _selectedBlueprint.metadata.blueprintType;
		string text = ((blueprintType == BlueprintType.SavedLocally || blueprintType == BlueprintType.UploadedToWorkshop) ? "blueprints_author_you" : (blueprint.author.IsNullOrEmpty() ? "common_unknown" : blueprint.author));
		if (text.IsNullOrEmpty() || text == "common_unknown")
		{
			authorLabel.SetData("blueprints_author".Localize(new
			{
				author = "common_unknown"
			}));
			Action<string> onReceiveUsername = delegate(string username)
			{
				blueprint.author = username;
				authorLabel.SetData("blueprints_author".Localize(new
				{
					author = username
				}));
			};
			if (blueprint.ownerId != 0)
			{
				SteamAPI.RequestOwnerSteamUsername(blueprint.ownerId, onReceiveUsername);
			}
			else
			{
				SteamAPI.RequestOwnerSteamUsernameByItem(blueprint.metadata.itemId, onReceiveUsername);
			}
		}
		else
		{
			authorLabel.SetData("blueprints_author".Localize(new
			{
				author = text
			}));
		}
	}

	private void CheckMissingModsWarning()
	{
		missingModsWarning.SetActive(_selectedBlueprint.IsMissingMods());
	}

	private void HandleOlderGameVersionWarning(int buildNumber)
	{
		if (!(olderGameVersionWarningTooltip == null))
		{
			bool flag = GameVersion.IsBuildFromOlderVersionGroup(buildNumber);
			olderGameVersionWarningTooltip.gameObject.SetActive(flag);
			if (flag)
			{
				olderGameVersionWarningTooltip.descriptionKey = "blueprint_old_version";
				olderGameVersionWarningTooltip.localizationArguments = new
				{
					gameVersion = GameVersion.GetVersionString(buildNumber, useBlueprintPreVersionSystem: true)
				};
			}
		}
	}

	public async void AddToYourLibrary()
	{
		if (!SteamHelper.IsConnectedToSteam())
		{
			Notifications.ShowError("blueprints_workshop_cant_add_to_library_not_connected");
			return;
		}
		LoadingSpinner.Show();
		await WorkshopBlueprints.AddToYourLibrary(_selectedBlueprint);
		if (!BlueprintsPanel.cancellationTokenSource.Token.IsCancellationRequested)
		{
			BlueprintsFolderLoader.UnloadPlayerBlueprints();
			await BlueprintsFolderLoader.GetBlueprints();
			if (!BlueprintsPanel.cancellationTokenSource.Token.IsCancellationRequested)
			{
				reloadBlueprints();
				LoadingSpinner.Hide();
			}
		}
	}

	public void OpenInBlueprintCreator()
	{
		OpenInBlueprintCreatorAsync();
	}

	private void OpenInBlueprintCreatorAsync()
	{
		BlueprintCreatorSystem.OpenWithBlueprint = _selectedBlueprint;
		LoadScene.LoadBlueprintCreator();
	}

	public void UploadToSteamWorkshop()
	{
		UploadToSteamWorkshopAsync();
	}

	private async Task UploadToSteamWorkshopAsync()
	{
		if (!SteamHelper.IsConnectedToSteam())
		{
			Notifications.ShowError("blueprints_workshop_cant_upload_not_connected");
			return;
		}
		LoadingSpinner.Show();
		_selectedBlueprint.metadata.RemoveDataElement(DataElement.CompatBlueprint);
		await _selectedBlueprint.UpdateMetadata();
		if (!(await BlueprintsListUI.LibraryController.CanUpdateToWorkshop(_selectedBlueprint)))
		{
			await WorkshopBlueprints.UploadBlueprintToWorkshop(_selectedBlueprint);
		}
		else
		{
			await WorkshopBlueprints.UpdateBlueprintToWorkshop(_selectedBlueprint);
		}
		reloadBlueprints();
		LoadingSpinner.Hide();
	}

	public void ReadWorkshopTermsOfService()
	{
		SteamFriends.OpenWebOverlay("http://steamcommunity.com/sharedfiles/workshoplegalagreement");
	}

	public void RemoveFromYourLibrary()
	{
		if (_selectedBlueprint.metadata.blueprintType == BlueprintType.SavedFromWorkshop && !SteamHelper.IsConnectedToSteam())
		{
			Notifications.ShowError("blueprints_workshop_cant_remove_from_library_not_connected");
			return;
		}
		LanguageChangeEventDataHolder bodyData = "blueprints_ui_confirm_deleting_from_library".Localize();
		Action onConfirmAction = RemoveSelectedBlueprintFromLibrary;
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
	}

	public void RemoveFromWorkshop()
	{
		if (!SteamHelper.IsConnectedToSteam())
		{
			Notifications.ShowError("blueprints_workshop_cant_remove_from_workshop_not_connected");
			return;
		}
		LanguageChangeEventDataHolder bodyData = "blueprints_ui_confirm_deleting_from_workshop".Localize();
		Action onConfirmAction = RemoveSelectedBlueprintFromWorkshop;
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, onConfirmAction);
	}

	private async void RemoveSelectedBlueprintFromLibrary()
	{
		LoadingSpinner.Show();
		if (_selectedBlueprint.metadata.blueprintType == BlueprintType.FeedbackSystem)
		{
			BlueprintFeedbackController.RemoveFromFeedbackSystem(_selectedBlueprint.name);
			reloadBlueprints();
			LoadingSpinner.Hide();
			return;
		}
		string blueprintFolder = BlueprintsFolderLoader.GetBlueprintFolder(_selectedBlueprint.name);
		if (BlueprintsListUI.Controller.workshopBlueprintsBySteamId.TryGetValue(_selectedBlueprint.metadata.itemId, out var value))
		{
			value.metadata.blueprintType = ((value.metadata.blueprintType == BlueprintType.UploadedToWorkshop) ? BlueprintType.SavedLocally : BlueprintType.Workshop);
			Item? item = await SteamUGC.QueryFileAsync(value.metadata.itemId);
			if (item.HasValue)
			{
				await item.Value.Unsubscribe();
			}
		}
		if (Directory.Exists(blueprintFolder))
		{
			Directory.Delete(blueprintFolder, recursive: true);
		}
		BlueprintsFolderLoader.UnloadPlayerBlueprints();
		await BlueprintsFolderLoader.GetBlueprints();
		if (!BlueprintsPanel.cancellationTokenSource.Token.IsCancellationRequested)
		{
			reloadBlueprints();
			LoadingSpinner.Hide();
		}
	}

	private async void RemoveSelectedBlueprintFromWorkshop()
	{
		LoadingSpinner.Show();
		await WorkshopBlueprints.RemoveBlueprintFromWorkshop(_selectedBlueprint);
		Close();
		reloadBlueprints();
	}

	public void Close()
	{
		_selectedBlueprint = null;
		base.gameObject.SetActive(value: false);
	}
}
