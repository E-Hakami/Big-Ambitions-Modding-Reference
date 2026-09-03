using BigAmbitions.Items;
using Extensions;
using Helpers;
using TMPro;
using Tooltip;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI.InteriorDesigner;

public class IDFurnitureItemTemplate : IDItemTemplateBase, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	[SerializeField]
	private TMP_Text priceField;

	[SerializeField]
	private Color selectedColor;

	[SerializeField]
	private Image background;

	[SerializeField]
	private Image itemIcon;

	[SerializeField]
	private ItemInfoTooltip itemTooltip;

	[Header("Favorites")]
	[SerializeField]
	private Toggle favoriteToggle;

	[SerializeField]
	private Image favoriteIcon;

	[SerializeField]
	private Color selectedFavoriteColor;

	[SerializeField]
	private Color deselectedFavoriteColor;

	private string _itemName;

	private void Awake()
	{
		favoriteToggle.onValueChanged.AddListener(ToggleFavorite);
	}

	private void OnEnable()
	{
		OnPointerExit(null);
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		if (!isSelected)
		{
			if ((bool)background)
			{
				background.color = selectedColor;
			}
			if ((bool)priceField)
			{
				priceField.color = InstanceBehavior<GlobalReferences>.Instance.colors.white;
			}
		}
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		if (!isSelected)
		{
			if ((bool)background)
			{
				background.color = InstanceBehavior<GlobalReferences>.Instance.colors.white;
			}
			if ((bool)priceField)
			{
				priceField.color = InstanceBehavior<GlobalReferences>.Instance.colors.black;
			}
		}
	}

	public override void SetSelected(bool selected)
	{
		base.SetSelected(selected);
		if ((bool)background)
		{
			background.color = (isSelected ? selectedColor : ((Color)InstanceBehavior<GlobalReferences>.Instance.colors.white));
		}
		if ((bool)priceField)
		{
			priceField.color = (isSelected ? InstanceBehavior<GlobalReferences>.Instance.colors.white : InstanceBehavior<GlobalReferences>.Instance.colors.black);
		}
	}

	public override void SetUp(IDItemUiTemplateData data)
	{
		_itemName = data.itemName;
		itemHash = data.itemName.GetHashCode();
		itemIcon.sprite = ItemHelper.GetIconWithFallback(data.itemName);
		focusButton.onClick.RemoveAllListeners();
		focusButton.onClick.AddListener(delegate
		{
			data.onClickItemName?.Invoke(this, data.itemName);
		});
		priceField.gameObject.SetActive(value: true);
		priceField.SetText(data.price.ToShortCurrencyFormat());
		bool flag = PlayerSettingsHelper.IsFurnitureFavorite(data.itemName);
		favoriteToggle.SetIsOnWithoutNotify(flag);
		favoriteIcon.color = (flag ? selectedFavoriteColor : deselectedFavoriteColor);
		if ((bool)itemTooltip)
		{
			itemTooltip.targetItem = ItemsGetter.GetByName(data.itemName);
		}
	}

	private void ToggleFavorite(bool isOn)
	{
		favoriteIcon.color = (isOn ? selectedFavoriteColor : deselectedFavoriteColor);
		PlayerSettingsHelper.ToggleFurnitureFavorite(_itemName);
	}
}
