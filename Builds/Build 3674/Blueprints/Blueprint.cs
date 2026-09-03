using System;
using System.IO;
using System.Threading.Tasks;
using BigAmbitions.ModsInternal;
using Blueprints.Compatibility;
using BlueprintsUI;
using DG.Tweening;
using Localizor;
using Localizor.LanguageChangeEvent;
using Player.SaveSystem.CompatibilityFixes;
using UnityEngine;
using UnityEngine.UI;

namespace Blueprints;

public class Blueprint
{
	private Texture2D _cachedTexture;

	private Sprite _cachedThumbnail;

	public string author;

	public float downloads;

	public bool isHidden;

	public BlueprintMetadata metadata;

	public string name;

	public ulong ownerId;

	public float rating;

	public DateTime releaseDate;

	public string thumbnailURL;

	private byte[] _cachedBytes;

	private bool _fetchedSteamInfo;

	public async void ShowThumbnail(Image image)
	{
		BlueprintType blueprintType = metadata.blueprintType;
		if (blueprintType != BlueprintType.Workshop && blueprintType != BlueprintType.FeedbackSystem && _cachedBytes == null)
		{
			_cachedBytes = await Task.Run(() => BlueprintsFolderLoader.LoadBlueprintThumbnailBytes(this));
		}
		if (_cachedThumbnail == null)
		{
			if (_cachedBytes != null)
			{
				_cachedTexture = new Texture2D(2, 2)
				{
					hideFlags = HideFlags.HideAndDontSave
				};
				if (!_cachedTexture.LoadImage(_cachedBytes))
				{
					UnityEngine.Object.Destroy(_cachedTexture);
					return;
				}
				_cachedThumbnail = Sprite.Create(_cachedTexture, new Rect(0f, 0f, _cachedTexture.width, _cachedTexture.height), new Vector2(0.5f, 0.5f), 100f, 0u, SpriteMeshType.FullRect);
			}
			else
			{
				if (string.IsNullOrEmpty(thumbnailURL))
				{
					return;
				}
				(_cachedTexture, _cachedThumbnail) = await BlueprintsWorkshopHelper.DownloadBlueprintThumbnail(thumbnailURL);
				if (isHidden)
				{
					UnityEngine.Object.DestroyImmediate(_cachedTexture);
					UnityEngine.Object.DestroyImmediate(_cachedThumbnail);
				}
			}
		}
		if (!(_cachedThumbnail == null) && !(image == null) && !(image.gameObject == null))
		{
			image.sprite = _cachedThumbnail;
			image.DOFade(1f, 1f).SetLink(image.gameObject).SetUpdate(isIndependentUpdate: true);
		}
	}

	public void FetchSteamInfo()
	{
		if (metadata.blueprintType != BlueprintType.Workshop && !_fetchedSteamInfo && BlueprintsListUI.Controller.workshopBlueprintsBySteamId.TryGetValue(metadata.itemId, out var value))
		{
			downloads = value.downloads;
			rating = value.rating;
			releaseDate = value.releaseDate;
			ownerId = value.ownerId;
			_fetchedSteamInfo = true;
		}
	}

	public LanguageChangeEventDataHolder GetDownloadsLabel()
	{
		int num = Mathf.RoundToInt(downloads);
		if (num == 0)
		{
			return "blueprints_no_downloads".Localize();
		}
		if (num == 1)
		{
			return "blueprints_one_download".Localize();
		}
		if (num < 1000)
		{
			return "blueprints_downloads_amount".Localize(new
			{
				downloadsAmount = num
			});
		}
		if (num < 1000000)
		{
			int downloadsInThousands = Mathf.FloorToInt((float)num / 1000f);
			return "blueprints_downloads_amount_thousands".Localize(new { downloadsInThousands });
		}
		int downloadsInMillions = Mathf.FloorToInt((float)num / 1000000f);
		return "blueprints_downloads_amount_millions".Localize(new { downloadsInMillions });
	}

	public async Task<BusinessLayoutSet> GetLayout(bool validate = true)
	{
		string layoutPath = GetLayoutPath();
		await BlueprintsFolderLoader.ApplyCompatibilityFixes(this, CompatibilityFixScope.Layout, layoutPath);
		BusinessLayoutSet businessLayoutSet = await BlueprintsFolderLoader.LoadBlueprintLayout(layoutPath);
		if (businessLayoutSet == null)
		{
			Debug.LogError("Layout of blueprint '" + name + "' could not be loaded");
			return null;
		}
		return validate ? CompatibilityBlueprintValidator.ValidateLayout(businessLayoutSet) : businessLayoutSet;
	}

	public void CleanCachedThumbnail()
	{
		if (_cachedTexture != null)
		{
			UnityEngine.Object.Destroy(_cachedTexture);
			_cachedTexture = null;
		}
		if (_cachedThumbnail != null)
		{
			UnityEngine.Object.Destroy(_cachedThumbnail);
			_cachedThumbnail = null;
		}
	}

	public string GetLayoutPath()
	{
		if (!GameManager.IsDevMode || !metadata.IsDevBlueprint)
		{
			return Path.Combine(BlueprintsFolderLoader.GetBlueprintFolder(name), "Layout.json");
		}
		string dataElementValue = metadata.GetDataElementValue(DataElement.BusinessTypeName);
		if (metadata.blueprintType == BlueprintType.DevBusinessLayout)
		{
			return BlueprintBusinessLayoutsController.GetLayoutPath(metadata.buildingSizeInfo, dataElementValue, name);
		}
		if (metadata.blueprintType == BlueprintType.DevInteriorDesign)
		{
			return BlueprintInteriorDesignsController.GetLayoutPath(metadata.buildingType, metadata.buildingSizeInfo, dataElementValue, name);
		}
		if (metadata.blueprintType == BlueprintType.FeedbackSystem)
		{
			return BlueprintFeedbackController.GetLayoutPath(name);
		}
		return string.Empty;
	}

	public bool IsMissingMods()
	{
		if (metadata.requiredModIds == null || metadata.requiredModIds.Count == 0)
		{
			return false;
		}
		return !ModDiscoveryRegistry.IsDiscovered(metadata.requiredModIds);
	}
}
