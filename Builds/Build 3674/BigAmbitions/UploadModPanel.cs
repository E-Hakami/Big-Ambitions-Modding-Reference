using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using BigAmbitions.ModsInternal;
using DG.Tweening;
using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using Steamworks;
using Steamworks.Ugc;
using TMPro;
using UI.Components;
using UI.Elements;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace BigAmbitions;

public class UploadModPanel : MonoBehaviour
{
	[SerializeField]
	private long thumbnailFileSizeLimit;

	[SerializeField]
	private ModsView modsView;

	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private RectTransform uploadSection;

	[SerializeField]
	private RectTransform updateOnlySection;

	[SerializeField]
	private UI.Components.InputField titleInput;

	[SerializeField]
	private UI.Components.InputField descriptionInput;

	[SerializeField]
	private UI.Elements.Dropdown modFolderDropdown;

	[SerializeField]
	private Image thumbnailPreviewImage;

	[SerializeField]
	private TMP_InputField targetBuildInput;

	[SerializeField]
	private UI.Components.InputField changeLogInput;

	[SerializeField]
	private Button uploadButton;

	[SerializeField]
	private TextLocalizationComponent uploadButtonText;

	[SerializeField]
	private ProgressBar progressBar;

	private bool _isUpdate;

	private bool _isActive;

	private ModInfo _updateModInfo;

	private Sprite _thumbnailPreviewSprite;

	private Texture2D _thumbnailPreviewTexture;

	private readonly List<string> _modFolders = new List<string>();

	private string _selectedModFolder;

	private string _selectedThumbnailPath;

	private ulong _thumbnailPreviewSteamItemId;

	private void Awake()
	{
		progressBar?.gameObject.SetActive(value: false);
		modFolderDropdown.onOptionSelected.AddListener(OnModFolderSelected);
	}

	private void OnEnable()
	{
		FolderWatcherHelper.StartWatching(ModDiscoveryRegistry.ModsLocalPath, RefreshModFolderDropdown);
	}

	private void OnDisable()
	{
		FolderWatcherHelper.StopWatching(ModDiscoveryRegistry.ModsLocalPath);
		DisposeLocalThumbnailPreview();
	}

	public void Show(ModInfo modInfo)
	{
		base.gameObject.SetActive(value: true);
		if (_isActive)
		{
			Hide(animate: true, FadeIn);
		}
		else
		{
			FadeIn();
		}
		void ApplyUiState()
		{
			RefreshModFolderDropdown();
			_selectedModFolder = null;
			_selectedThumbnailPath = null;
			if (modInfo != null)
			{
				_isUpdate = true;
				_updateModInfo = modInfo;
				titleInput.SetText(modInfo.title);
				descriptionInput.SetText(modInfo.description);
				targetBuildInput.text = modInfo.targetBuildNumber.ToString();
				uploadButtonText.Key = "main_menu_mods_update_mod";
				changeLogInput.SetText(modInfo.changeLog);
				updateOnlySection.gameObject.SetActive(value: true);
				ShowCurrentThumbnailPreview(modInfo);
			}
			else
			{
				_isUpdate = false;
				_updateModInfo = null;
				titleInput.SetText("");
				descriptionInput.SetText("");
				changeLogInput.SetText("");
				targetBuildInput.text = GameVersion.GetCurrent().buildNumber.ToString();
				uploadButtonText.Key = "main_menu_mods_upload_mod";
				updateOnlySection.gameObject.SetActive(value: false);
				SetThumbnailPreviewSprite(null);
			}
		}
		void FadeIn()
		{
			ApplyUiState();
			base.gameObject.SetActive(value: true);
			_isActive = true;
			uploadSection.DOKill(complete: true);
			canvasGroup.DOKill(complete: true);
			canvasGroup.alpha = 0f;
			canvasGroup.DOFade(1f, 0.2f).SetUpdate(isIndependentUpdate: true);
		}
	}

	public void Hide(bool animate, Action onComplete = null)
	{
		_isActive = false;
		uploadSection.DOKill(complete: true);
		canvasGroup.DOKill(complete: true);
		if (animate)
		{
			canvasGroup.DOFade(0f, 0.2f).SetUpdate(isIndependentUpdate: true).OnComplete(delegate
			{
				base.gameObject.SetActive(value: false);
				onComplete?.Invoke();
			});
		}
		else
		{
			canvasGroup.alpha = 0f;
			base.gameObject.SetActive(value: false);
			onComplete?.Invoke();
		}
	}

	public void SubmitUpload()
	{
		uploadButton.gameObject.SetActive(value: false);
		progressBar?.gameObject.SetActive(value: true);
		progressBar?.SetValue(0f);
		if ((_isUpdate && _updateModInfo == null) || !TryCreateModInfo(out var modInfo))
		{
			uploadButton.gameObject.SetActive(value: true);
			progressBar?.gameObject.SetActive(value: false);
			return;
		}
		if (_isUpdate)
		{
			modInfo.steamItemId = _updateModInfo.steamItemId;
		}
		modInfo.changeLog = changeLogInput.GetRawValue();
		StartCoroutine(UploadCoroutine(modInfo));
	}

	public void OpenModsLocal()
	{
		FolderHelper.OpenFolder(ModDiscoveryRegistry.ModsLocalPath);
	}

	private void RefreshModFolderDropdown()
	{
		string selectedFolderName = GetSelectedFolderName();
		_modFolders.Clear();
		string[] directories = Directory.GetDirectories(ModDiscoveryRegistry.ModsLocalPath, "*", SearchOption.TopDirectoryOnly);
		for (int i = 0; i < directories.Length; i++)
		{
			string fileName = Path.GetFileName(directories[i]);
			_modFolders.Add(fileName);
		}
		int num = -1;
		if (!string.IsNullOrEmpty(selectedFolderName))
		{
			for (int j = 0; j < _modFolders.Count; j++)
			{
				if (_modFolders[j].Equals(selectedFolderName, StringComparison.Ordinal))
				{
					num = j;
					break;
				}
			}
		}
		modFolderDropdown.SetOptions(_modFolders, localize: false, num);
		if (num == -1)
		{
			OnModFolderSelected(-1);
		}
		else
		{
			OnModFolderSelected(num);
		}
	}

	private void OnWorkshopUploadOrUpdateCompleted(ulong itemId)
	{
		modsView.ShowUploadPanel(isOn: false);
		SteamHelper.OpenSteamWithWorkshopItem(itemId);
	}

	private void OnProgressBarValueChanged(float value)
	{
		progressBar?.SetValue01(value);
	}

	private void OnModFolderSelected(int index)
	{
		if (index < 0 || index >= _modFolders.Count)
		{
			_selectedModFolder = null;
			_selectedThumbnailPath = null;
			_thumbnailPreviewSteamItemId = 0uL;
			DisposeLocalThumbnailPreview();
			SetThumbnailPreviewSprite(null);
			return;
		}
		_selectedModFolder = Path.Combine(ModDiscoveryRegistry.ModsLocalPath, _modFolders[index]);
		_thumbnailPreviewSteamItemId = 0uL;
		string[] files = Directory.GetFiles(_selectedModFolder, "*", SearchOption.TopDirectoryOnly);
		foreach (string text in files)
		{
			if (IsSupportedThumbnailFile(text) && new FileInfo(text).Length <= thumbnailFileSizeLimit)
			{
				byte[] data = File.ReadAllBytes(text);
				Texture2D texture2D = new Texture2D(2, 2, TextureFormat.RGBA32, mipChain: false);
				if (texture2D.LoadImage(data))
				{
					DisposeLocalThumbnailPreview();
					_thumbnailPreviewTexture = texture2D;
					_thumbnailPreviewSprite = Sprite.Create(_thumbnailPreviewTexture, new Rect(0f, 0f, _thumbnailPreviewTexture.width, _thumbnailPreviewTexture.height), new Vector2(0.5f, 0.5f));
					thumbnailPreviewImage.sprite = _thumbnailPreviewSprite;
					thumbnailPreviewImage.enabled = true;
					_selectedThumbnailPath = text;
					return;
				}
				UnityEngine.Object.Destroy(texture2D);
			}
		}
		_selectedThumbnailPath = null;
		DisposeLocalThumbnailPreview();
		SetThumbnailPreviewSprite(null);
	}

	private void ShowCurrentThumbnailPreview(ModInfo modInfo)
	{
		_thumbnailPreviewSteamItemId = 0uL;
		if (modInfo == null)
		{
			DisposeLocalThumbnailPreview();
			SetThumbnailPreviewSprite(null);
		}
		else if (modInfo.thumbnail != null)
		{
			DisposeLocalThumbnailPreview();
			SetThumbnailPreviewSprite(modInfo.thumbnail);
		}
		else if (string.IsNullOrWhiteSpace(modInfo.thumbnailUrl))
		{
			DisposeLocalThumbnailPreview();
			SetThumbnailPreviewSprite(null);
		}
		else
		{
			_thumbnailPreviewSteamItemId = modInfo.steamItemId;
			ModThumbnailLoader.LoadThumbnailAsync(modInfo, OnCurrentThumbnailLoaded);
		}
	}

	private void OnCurrentThumbnailLoaded(ModInfo modInfo, Sprite sprite)
	{
		if (modInfo != null && !(sprite == null) && _isUpdate && _updateModInfo != null && _updateModInfo.steamItemId == modInfo.steamItemId && _thumbnailPreviewSteamItemId == modInfo.steamItemId && modFolderDropdown.SelectedOptionIndex == -1)
		{
			SetThumbnailPreviewSprite(sprite);
		}
	}

	private void SetThumbnailPreviewSprite(Sprite sprite)
	{
		thumbnailPreviewImage.sprite = sprite;
		thumbnailPreviewImage.enabled = sprite != null;
	}

	private void DisposeLocalThumbnailPreview()
	{
		if (_thumbnailPreviewSprite != null)
		{
			UnityEngine.Object.Destroy(_thumbnailPreviewSprite);
			_thumbnailPreviewSprite = null;
		}
		if (_thumbnailPreviewTexture != null)
		{
			UnityEngine.Object.Destroy(_thumbnailPreviewTexture);
			_thumbnailPreviewTexture = null;
		}
	}

	private static bool IsSupportedThumbnailFile(string fullFileName)
	{
		string extension = Path.GetExtension(fullFileName);
		if (!extension.Equals(".png", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase))
		{
			return extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private bool HasFileWithExtension(string extension, bool recursive)
	{
		if (string.IsNullOrWhiteSpace(_selectedModFolder) || !Directory.Exists(_selectedModFolder))
		{
			return false;
		}
		string text = extension;
		if (!text.StartsWith("."))
		{
			text = "." + text;
		}
		SearchOption searchOption = (recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
		foreach (string item in Directory.EnumerateFiles(_selectedModFolder, "*", searchOption))
		{
			if (Path.GetExtension(item).Equals(text, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private string GetSelectedFolderName()
	{
		if (!string.IsNullOrWhiteSpace(_selectedModFolder))
		{
			return Path.GetFileName(_selectedModFolder);
		}
		int selectedOptionIndex = modFolderDropdown.SelectedOptionIndex;
		if (selectedOptionIndex < 0 || selectedOptionIndex >= _modFolders.Count)
		{
			return null;
		}
		return _modFolders[selectedOptionIndex];
	}

	private bool TryCreateModInfo(out ModInfo modInfo)
	{
		modInfo = new ModInfo
		{
			title = titleInput.GetRawValue()
		};
		if (string.IsNullOrWhiteSpace(modInfo.title))
		{
			Notifications.ShowError("main_menu_mods_upload_no_title");
			return false;
		}
		modInfo.description = descriptionInput.GetRawValue();
		if (string.IsNullOrWhiteSpace(modInfo.description))
		{
			Notifications.ShowError("main_menu_mods_upload_no_description");
			return false;
		}
		modInfo.modFolder = _selectedModFolder;
		if (string.IsNullOrWhiteSpace(modInfo.modFolder) || !Directory.Exists(modInfo.modFolder))
		{
			Dictionary<string, string> notificationData = new Dictionary<string, string> { { "fromname", modInfo.modFolder } };
			Notifications.Show(NotificationType.Error, "main_menu_mods_upload_mod_folder_not_found", notificationData);
			return false;
		}
		if (!HasFileWithExtension(".dll", recursive: false))
		{
			Notifications.Show(NotificationType.Error, "main_menu_mods_upload_no_dll_in_mod_folder");
			return false;
		}
		modInfo.thumbnailUrl = _selectedThumbnailPath;
		if (!string.IsNullOrEmpty(modInfo.thumbnailUrl) && !File.Exists(modInfo.thumbnailUrl))
		{
			Dictionary<string, string> notificationData2 = new Dictionary<string, string> { { "fromname", modInfo.thumbnailUrl } };
			Notifications.Show(NotificationType.Error, "main_menu_mods_upload_thumbnail_not_found", notificationData2);
			return false;
		}
		if (!int.TryParse(targetBuildInput.text, out modInfo.targetBuildNumber) || modInfo.targetBuildNumber < 0)
		{
			Dictionary<string, string> notificationData3 = new Dictionary<string, string> { { "fromname", targetBuildInput.text } };
			Notifications.Show(NotificationType.Error, "main_menu_mods_upload_target_build_invalid", notificationData3);
			return false;
		}
		if (modInfo.targetBuildNumber > GameVersion.GetCurrent().buildNumber)
		{
			Dictionary<string, string> notificationData4 = new Dictionary<string, string> { 
			{
				"fromname",
				modInfo.targetBuildNumber.ToString()
			} };
			Notifications.Show(NotificationType.Error, "main_menu_mods_upload_target_build_invalid", notificationData4);
			return false;
		}
		if (!_isUpdate)
		{
			return true;
		}
		modInfo.changeLog = changeLogInput.GetRawValue();
		if (string.IsNullOrWhiteSpace(modInfo.changeLog))
		{
			Notifications.ShowError("main_menu_mods_upload_no_changelog");
			return false;
		}
		return true;
	}

	private IEnumerator UploadCoroutine(ModInfo modInfo)
	{
		try
		{
			Task<PublishResult> uploadTask = (_isUpdate ? SteamModLoadingService.UpdateExistingModOnWorkshop(modInfo, OnProgressBarValueChanged) : SteamModLoadingService.UploadNewModToWorkshop(modInfo, OnProgressBarValueChanged));
			while (!uploadTask.IsCompleted)
			{
				yield return null;
			}
			if (uploadTask.IsFaulted)
			{
				Debug.LogError(uploadTask.Exception);
				Notifications.ShowError("main_menu_mods_upload_failed");
				yield break;
			}
			if (uploadTask.IsCanceled)
			{
				Debug.LogError("Workshop upload was canceled.");
				Notifications.ShowError("main_menu_mods_upload_canceled");
				yield break;
			}
			PublishResult result = uploadTask.Result;
			if (result.Result != Result.OK)
			{
				if (_isUpdate)
				{
					HandleUpdateResult(result);
				}
				else
				{
					HandleUploadResult(result);
				}
			}
			else
			{
				OnWorkshopUploadOrUpdateCompleted(result.FileId);
			}
		}
		finally
		{
			UploadModPanel uploadModPanel = this;
			uploadModPanel.uploadButton.gameObject.SetActive(value: true);
			uploadModPanel.progressBar?.gameObject.SetActive(value: false);
		}
	}

	private static void HandleUploadResult(PublishResult publishResult)
	{
		if (publishResult.Result != Result.OK)
		{
			switch (publishResult.Result)
			{
			case Result.Fail:
				Debug.LogError("Workshop upload failed.");
				Notifications.ShowError("main_menu_mods_upload_failed");
				break;
			case Result.ConnectFailed:
				Debug.LogError("Workshop upload failed: not connected to Steam.");
				Notifications.ShowError("main_menu_mods_upload_not_connected");
				break;
			case Result.FileNotFound:
				Debug.LogError("Workshop upload failed: mod folder not found.");
				Notifications.ShowError("main_menu_mods_upload_mod_folder_not_found");
				break;
			case Result.DuplicateRequest:
				Debug.LogError("Workshop upload failed: duplicate upload request detected.");
				Notifications.ShowError("main_menu_mods_upload_duplicate_request");
				break;
			default:
				Debug.LogError($"Workshop upload failed with result: {publishResult.Result}");
				Notifications.Show(NotificationType.Error, "main_menu_mods_upload_result_failed", new Dictionary<string, string> { 
				{
					"fromname",
					publishResult.Result.ToString()
				} });
				break;
			}
		}
	}

	private static void HandleUpdateResult(PublishResult publishResult)
	{
		if (publishResult.Result != Result.OK)
		{
			switch (publishResult.Result)
			{
			case Result.Fail:
				Debug.LogError("Workshop update failed.");
				Notifications.ShowError("main_menu_mods_update_failed");
				break;
			case Result.ConnectFailed:
				Debug.LogError("Workshop update failed: not connected to Steam.");
				Notifications.ShowError("main_menu_mods_update_not_connected");
				break;
			case Result.FileNotFound:
				Debug.LogError("Workshop update failed: mod folder not found.");
				Notifications.ShowError("main_menu_mods_upload_mod_folder_not_found");
				break;
			case Result.InvalidParam:
				Debug.LogError("Workshop update failed: invalid Steam item id.");
				break;
			case Result.AccessDenied:
				Debug.LogError("Workshop update failed: you don't own this Workshop item (access denied).");
				Notifications.ShowError("main_menu_mods_update_access_denied");
				break;
			default:
				Debug.LogError($"Workshop update failed with result: {publishResult.Result}");
				Notifications.Show(NotificationType.Error, "main_menu_mods_update_result_failed", new Dictionary<string, string> { 
				{
					"fromname",
					publishResult.Result.ToString()
				} });
				break;
			}
		}
	}
}
