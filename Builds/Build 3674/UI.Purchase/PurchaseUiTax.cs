using System;
using Extensions;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Purchase;

public class PurchaseUiTax : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent nameLabel;

	[SerializeField]
	private TextMeshProUGUI priceLabel;

	[SerializeField]
	private UI.Components.InputField amountInput;

	[SerializeField]
	private Button button;

	private float _maxAmount;

	private bool _isSubDollarPayment;

	private Action<float> _onPaymentSelected;

	public void Setup(string key, float owedAmount, Action<float> onPaymentSelected, bool focusInput)
	{
		float num = Mathf.Floor(owedAmount);
		float num2 = Mathf.Min(num, Mathf.Floor(SaveGameManager.Current.Money));
		if (num2 < 0f)
		{
			num2 = 0f;
		}
		_onPaymentSelected = onPaymentSelected;
		_maxAmount = num2;
		_isSubDollarPayment = owedAmount > 0f && num <= 0f;
		nameLabel.Key = key;
		priceLabel.text = num.ToShortCurrencyFormat();
		amountInput.SetMaxNumeralAmount(num2);
		amountInput.SetText(num2.ToShortCurrencyFormat(), notify: false);
		button.onClick.RemoveAllListeners();
		button.onClick.AddListener(SelectPayment);
		KeyboardInputHelper.Configure(amountInput.tmpInputField, SelectPayment, focusInput);
		base.gameObject.SetActive(value: true);
	}

	private void SelectPayment()
	{
		if (!amountInput.GetRawValue().FromShortCurrencyFormat(out var value))
		{
			return;
		}
		value = Mathf.Floor(value);
		if (value <= 0f)
		{
			if (!_isSubDollarPayment)
			{
				return;
			}
			value = 0f;
		}
		value = Mathf.Min(value, _maxAmount);
		_onPaymentSelected?.Invoke(value);
	}
}
