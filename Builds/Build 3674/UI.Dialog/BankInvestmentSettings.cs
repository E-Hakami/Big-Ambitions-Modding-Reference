using System.Collections.Generic;
using Entities;
using Enums;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI.Components;
using UI.Elements;
using UnityEngine;

namespace UI.Dialog;

public class BankInvestmentSettings : MonoBehaviour
{
	public const float MinimumAmount = 1000f;

	public InputField amountField;

	public InputField autoInvestField;

	[SerializeField]
	private Dropdown investmentFundDropdown;

	[SerializeField]
	private TextLocalizationComponent riskLabel;

	[SerializeField]
	private GameObject riskTooltipGameObject;

	[SerializeField]
	private float maxInvestment = 1E+09f;

	[HideInInspector]
	public InvestmentFundData selectedFundData;

	private List<InvestmentFundData> _funds;

	private int _maxNumeral;

	private void Start()
	{
		selectedFundData = null;
		amountField.SetText("", notify: false);
		autoInvestField.SetText("", notify: false);
		riskLabel.TextContainer.text = "-";
		riskTooltipGameObject.SetActive(value: false);
		List<InvestmentFundData> investmentFundsByAddress = InvestmentFundHelper.GetInvestmentFundsByAddress(DialogController.current.contact.Address);
		_funds = investmentFundsByAddress;
		List<string> list = new List<string>(_funds.Count);
		for (int i = 0; i < _funds.Count; i++)
		{
			list.Add(_funds[i].fundName);
		}
		investmentFundDropdown.SetPlaceholder("dialog_select_investment_fund");
		investmentFundDropdown.SetOptions(list);
		investmentFundDropdown.onOptionSelected.AddListener(SelectFund);
		_maxNumeral = Mathf.RoundToInt(Mathf.Min(SaveGameManager.Current.Money, maxInvestment));
		amountField.SetMaxNumeralAmount(_maxNumeral);
		autoInvestField.SetMaxNumeralAmount(_maxNumeral);
		amountField.tmpInputField.onValueChanged.AddListener(OnAmountChanged);
		autoInvestField.tmpInputField.onValueChanged.AddListener(OnAutoInvestChanged);
	}

	private void OnAmountChanged(string _)
	{
		ClampFields(amountField, autoInvestField);
	}

	private void OnAutoInvestChanged(string _)
	{
		ClampFields(autoInvestField, amountField);
	}

	private void ClampFields(InputField changedField, InputField otherField)
	{
		float num = ClampFieldToMax(otherField, GetFieldValue(otherField), _maxNumeral);
		int num2 = Mathf.FloorToInt(Mathf.Max((float)_maxNumeral - num, 0f));
		changedField.SetMaxNumeralAmount(num2);
		float num3 = ClampFieldToMax(changedField, GetFieldValue(changedField), num2);
		int maxNumeralAmount = Mathf.FloorToInt(Mathf.Max((float)_maxNumeral - num3, 0f));
		otherField.SetMaxNumeralAmount(maxNumeralAmount);
	}

	private static float ClampFieldToMax(InputField field, float value, int max)
	{
		if (value <= (float)max)
		{
			return value;
		}
		field.SetText(max.ToShortCurrencyFormat(), notify: false);
		return max;
	}

	private static float GetFieldValue(InputField field)
	{
		if (!field.GetRawValue().FromShortCurrencyFormat(out var value))
		{
			return 0f;
		}
		return Mathf.Max(value, 0f);
	}

	private void SelectFund(int fundIndex)
	{
		selectedFundData = _funds[fundIndex];
		(Priority, int, int) tuple = InvestmentFundHelper.DetermineRisk(selectedFundData);
		riskLabel.Arguments = new
		{
			risk = tuple.Item1.GetLocalizeKey().GetLocalization(),
			min = tuple.Item2,
			max = tuple.Item3
		};
		riskTooltipGameObject.SetActive(value: true);
	}
}
