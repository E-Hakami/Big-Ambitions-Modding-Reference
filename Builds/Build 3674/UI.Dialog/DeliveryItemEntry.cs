using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Dialog;

public class DeliveryItemEntry : MonoBehaviour
{
	[SerializeField]
	private ItemsListEntry itemsListEntry;

	[SerializeField]
	private Image selectButtonImage;

	[SerializeField]
	private Button hoverOutlineButton;

	[SerializeField]
	private AmountSelector amountSelector;

	[SerializeField]
	private Sprite availableAddItemButtonSprite;

	[SerializeField]
	private Sprite unavailableAddItemButtonSprite;

	public ItemsListEntry ItemsListEntry => itemsListEntry;

	public AmountSelector AmountSelector => amountSelector;

	public void Init(LanguageChangeEventDataHolder itemName, Sprite icon, float price)
	{
		itemsListEntry.Init(itemName, icon, price);
	}

	public void SetAddButtonState(bool isSelected, bool canAddItem)
	{
		selectButtonImage.gameObject.SetActive(!isSelected);
		selectButtonImage.sprite = (canAddItem ? availableAddItemButtonSprite : unavailableAddItemButtonSprite);
		amountSelector.gameObject.SetActive(isSelected);
		hoverOutlineButton.interactable = isSelected | canAddItem;
	}

	public void AddClickListener(UnityAction action)
	{
		hoverOutlineButton.onClick.AddListener(action);
	}
}
