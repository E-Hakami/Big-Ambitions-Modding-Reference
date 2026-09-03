using System;
using Extensions;
using UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Dialog;

public class PlayerOfferSettings : MonoBehaviour
{
	[SerializeField]
	private UI.Components.InputField amountField;

	[SerializeField]
	private Button decreaseButton;

	[SerializeField]
	private Button increaseButton;

	[SerializeField]
	private float buttonIncreaseAmount = 0.01f;

	private int _maxNumeralAmount;

	private bool _allowNegative;

	private void OnEnable()
	{
		_allowNegative = true;
	}

	private void Start()
	{
		amountField.tmpInputField.onValueChanged.AddListener(delegate
		{
			if (!_allowNegative && float.TryParse(amountField.GetRawValue(), out var result))
			{
				decreaseButton.interactable = result - buttonIncreaseAmount >= 0f;
			}
		});
	}

	public void SetInitialAmount(float amount)
	{
		amountField.SetText("");
		ChangeAmount(amount);
		decreaseButton.interactable = true;
		amountField.tmpInputField.interactable = true;
		increaseButton.interactable = true;
	}

	public void SetMaxNumeralAmount(int maxNumeralAmount)
	{
		_maxNumeralAmount = maxNumeralAmount;
		amountField.SetMaxNumeralAmount(maxNumeralAmount);
	}

	public void SetAllowNegative(bool allowNegative)
	{
		_allowNegative = allowNegative;
		amountField.allowNegativeValues = allowNegative;
	}

	public void ConfigureKeyboardInput(Action onSubmit, bool autoFocus)
	{
		KeyboardInputHelper.Configure(amountField.tmpInputField, onSubmit, autoFocus);
	}

	public void FocusInput()
	{
		KeyboardInputHelper.FocusNextFrame(amountField.tmpInputField);
	}

	public bool GetAmount(ref float amount)
	{
		if (!string.IsNullOrEmpty(amountField.tmpInputField.text))
		{
			return float.TryParse(amountField.tmpInputField.text, out amount);
		}
		return false;
	}

	public void Increase()
	{
		ChangeAmount(buttonIncreaseAmount);
	}

	public void Decrease()
	{
		ChangeAmount(0f - buttonIncreaseAmount);
	}

	private void ChangeAmount(float change)
	{
		float amount = 0f;
		if (!GetAmount(ref amount))
		{
			amount = 0f;
		}
		amount += change;
		amount = (float)Mathf.RoundToInt(amount * 100f) / 100f;
		if (_maxNumeralAmount > 0)
		{
			amount = Mathf.Min(amount, _maxNumeralAmount);
		}
		amountField.SetText(amount.ToString("N2", CultureHelper.CultureInfo), notify: false);
		if (!_allowNegative)
		{
			decreaseButton.interactable = amount - buttonIncreaseAmount >= 0f;
		}
	}
}
