using BigAmbitions.ModsInternal;
using UnityEngine;
using UnityEngine.UI;

namespace BigAmbitions;

public abstract class SingleModUI : MonoBehaviour
{
	[SerializeField]
	protected Image thumbnailImage;

	protected ulong currentSteamItemId;

	public virtual void Setup(ModInfo modInfo)
	{
		currentSteamItemId = modInfo.steamItemId;
		thumbnailImage.sprite = modInfo.thumbnail;
		if (modInfo.thumbnail == null)
		{
			ModThumbnailLoader.LoadThumbnailAsync(modInfo, OnThumbnailLoaded);
		}
		else
		{
			OnThumbnailLoaded(modInfo, modInfo.thumbnail);
		}
	}

	private void OnThumbnailLoaded(ModInfo modInfo, Sprite sprite)
	{
		if (modInfo != null && !(sprite == null) && modInfo.steamItemId == currentSteamItemId)
		{
			modInfo.thumbnail = sprite;
			thumbnailImage.sprite = sprite;
		}
	}
}
