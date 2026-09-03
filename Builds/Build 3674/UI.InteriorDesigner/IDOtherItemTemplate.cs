using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.InteriorDesigner;

public class IDOtherItemTemplate : IDItemTemplateBase, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[SerializeField]
	private GameObject outline;

	[SerializeField]
	private Image itemIcon;

	[Header("InfoOverlay")]
	[SerializeField]
	private Image overlayBackground;

	[SerializeField]
	private Image overlayIcon;

	public override void OnPointerEnter(PointerEventData eventData)
	{
		if (!isSelected && (bool)outline)
		{
			outline.SetActive(value: true);
		}
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		if (!isSelected && (bool)outline)
		{
			outline.SetActive(value: false);
		}
	}

	public override void SetSelected(bool selected)
	{
		base.SetSelected(selected);
		if ((bool)outline)
		{
			outline.SetActive(isSelected);
		}
	}

	public override void SetUp(IDItemUiTemplateData data)
	{
		ItemController itemController = data.itemController;
		itemHash = itemController.GetHashCode();
		itemIcon.sprite = ItemHelper.GetIconWithFallback(itemController.itemName);
		focusButton.onClick.RemoveAllListeners();
		focusButton.onClick.AddListener(delegate
		{
			ActionPanelUI.focusOnItem?.Invoke(itemController);
		});
		focusButton.onClick.AddListener(delegate
		{
			data.onClickItemController?.Invoke(this, itemController);
		});
		if (overlayBackground != null)
		{
			if (data.overlayBackgroundSprite != null)
			{
				overlayBackground.sprite = data.overlayBackgroundSprite;
				overlayBackground.gameObject.SetActive(value: true);
			}
			else
			{
				overlayBackground.gameObject.SetActive(value: false);
			}
		}
		if (overlayIcon != null)
		{
			if (data.overlayIconSprite != null)
			{
				overlayIcon.sprite = data.overlayIconSprite;
				overlayIcon.gameObject.SetActive(value: true);
			}
			else
			{
				overlayIcon.gameObject.SetActive(value: false);
			}
		}
	}
}
