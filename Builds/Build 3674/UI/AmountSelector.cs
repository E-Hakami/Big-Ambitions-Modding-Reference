using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI;

public class AmountSelector : MonoBehaviour
{
	public int maxAmount;

	public UnityEvent<int> onAmountUpdate = new UnityEvent<int>();

	public UnityEvent onDelete;

	[SerializeField]
	private bool capAtMax;

	[SerializeField]
	private int stepAmount = 1;

	[SerializeField]
	private Button decreaseButton;

	[SerializeField]
	private Button increaseButton;

	[SerializeField]
	private TMP_InputField amountInput;

	[SerializeField]
	private Button deleteRowButton;

	private bool _interactable = true;

	public int Amount
	{
		get
		{
			if (!int.TryParse(amountInput.text, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
			{
				return 0;
			}
			return result;
		}
	}

	public bool Interactable
	{
		get
		{
			return _interactable;
		}
		set
		{
			_interactable = value;
			decreaseButton.interactable = _interactable;
			increaseButton.interactable = _interactable;
			amountInput.interactable = _interactable;
			if ((bool)deleteRowButton)
			{
				deleteRowButton.interactable = _interactable;
			}
		}
	}

	private void Start()
	{
		increaseButton.onClick.AddListener(Increase);
		decreaseButton.onClick.AddListener(Decrease);
		deleteRowButton?.onClick.AddListener(delegate
		{
			onDelete?.Invoke();
		});
		amountInput.onEndEdit.AddListener(delegate
		{
			Change();
		});
		amountInput.onValueChanged.AddListener(OnValueChange);
	}

	public void SetMaxAmount(int newMaxAmount)
	{
		maxAmount = newMaxAmount;
		int amount = Amount;
		increaseButton.interactable = amount + stepAmount <= maxAmount;
		decreaseButton.interactable = amount - stepAmount >= 0;
	}

	public void Increase()
	{
		int amount = Amount;
		if (amount < maxAmount)
		{
			SetAmount(amount + stepAmount);
		}
	}

	private void Decrease()
	{
		int amount = Amount;
		if (amount != 0)
		{
			SetAmount(amount - stepAmount);
		}
	}

	private void OnValueChange(string _)
	{
		if (int.TryParse(amountInput.text, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
		{
			if (capAtMax && result > maxAmount)
			{
				amountInput.SetTextWithoutNotify(maxAmount.ToString(CultureInfo.InvariantCulture));
				result = maxAmount;
			}
			UpdateButtonsInteractivity(result);
		}
	}

	private void Change()
	{
		if (int.TryParse(amountInput.text, NumberStyles.Number, CultureInfo.InvariantCulture, out var result))
		{
			if (result > maxAmount)
			{
				result = maxAmount;
			}
			else if (result < 0)
			{
				result = 0;
			}
			SetAmount(result);
		}
	}

	public void SetAmount(int newAmount)
	{
		amountInput.text = newAmount.ToString();
		onAmountUpdate.Invoke(newAmount);
		UpdateButtonsInteractivity(newAmount);
	}

	private void UpdateButtonsInteractivity(int newAmount)
	{
		increaseButton.interactable = _interactable && newAmount < maxAmount;
		decreaseButton.interactable = _interactable && newAmount > 0;
	}

	public void OnEndEdit()
	{
		if (string.IsNullOrEmpty(amountInput.text))
		{
			SetAmount(0);
		}
	}

	public void UpdateAmountText(int newAmount)
	{
		amountInput.text = newAmount.ToString();
	}
}
