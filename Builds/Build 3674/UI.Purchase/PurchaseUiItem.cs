using System;
using Extensions;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Purchase;

public class PurchaseUiItem : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent nameLabel;

	[SerializeField]
	private TextMeshProUGUI priceLabel;

	[SerializeField]
	private AmountSelector amountSelector;

	[SerializeField]
	private Button button;

	private Action<int> _onAmountUpdate;

	public int Amount => amountSelector.Amount;

	public void Setup(string itemNameKey, float price)
	{
		Setup(LanguageChangeEventDataHolder.Create(itemNameKey), price);
	}

	public void Setup(LanguageChangeEventDataHolder itemNameData, float price)
	{
		nameLabel.SetData(itemNameData);
		SetPrice(price);
		SetButtonAction(null);
		SetAmountSelector(visible: false, 0, null);
		base.gameObject.SetActive(value: true);
	}

	public void SetPrice(float price)
	{
		priceLabel.text = price.ToCurrencyFormat();
	}

	public void SetAmountSelector(bool visible, int maxAmount, Action<int> onAmountUpdate)
	{
		amountSelector.gameObject.SetActive(visible);
		amountSelector.onAmountUpdate.RemoveAllListeners();
		_onAmountUpdate = null;
		if (visible)
		{
			amountSelector.SetMaxAmount(maxAmount);
			amountSelector.SetAmount(0);
			_onAmountUpdate = onAmountUpdate;
			amountSelector.onAmountUpdate.AddListener(OnAmountUpdate);
		}
	}

	public void SetMaxAmount(int maxAmount)
	{
		amountSelector.SetMaxAmount(maxAmount);
	}

	public void SetButtonAction(Action onClick)
	{
		if ((bool)button)
		{
			button.onClick.RemoveAllListeners();
			if (onClick != null)
			{
				button.onClick.AddListener(onClick.Invoke);
			}
		}
	}

	private void OnAmountUpdate(int amount)
	{
		_onAmountUpdate?.Invoke(amount);
	}
}
