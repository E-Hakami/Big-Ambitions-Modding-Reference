using System;
using System.Collections.Generic;
using System.Globalization;
using AwesomeCharts;
using Entities;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using Tooltip;
using UI;
using UI.Components;
using UI.Notification;
using UI.Smartphone.Apps.EconoView.Investments;
using UnityEngine;
using UnityEngine.UI;

public class EconoViewInvestments : MonoBehaviour
{
	private const int InvestmentHistoryMaxDays = 15;

	[SerializeField]
	private Transform fundButtonTemplate;

	[SerializeField]
	private GameObject noInvestmentsLabel;

	[SerializeField]
	private GameObject detailsArea;

	[SerializeField]
	private LineChart lineChart;

	[SerializeField]
	private GameObject notEnoughDataLabel;

	[SerializeField]
	private GameObject fundsListArea;

	[SerializeField]
	private TextLocalizationComponent fundNameLabel;

	[SerializeField]
	private TMP_Text currentValueLabel;

	[SerializeField]
	private ListTooltip currentValueTooltip;

	[SerializeField]
	private UI.Components.InputField withdrawField;

	[SerializeField]
	private UI.Components.InputField autoInvestField;

	[SerializeField]
	private Toggle autoInvestToggle;

	private InvestmentFund _currentInvestment;

	private string _currentInvestmentName;

	private readonly List<InvestmentFundButton> _fundButtons = new List<InvestmentFundButton>();

	private void Start()
	{
		autoInvestField.tmpInputField.onValueChanged.AddListener(OnAutoInvestValueChanged);
		autoInvestToggle.onValueChanged.AddListener(OnAutoInvestToggleValueChanged);
	}

	public void Init()
	{
		bool flag = InvestmentFundHelper.HasAnyInvestments();
		noInvestmentsLabel.gameObject.SetActive(!flag);
		detailsArea.SetActive(flag);
		fundsListArea.SetActive(flag);
		if (!flag)
		{
			_currentInvestment = null;
			_currentInvestmentName = string.Empty;
			return;
		}
		fundButtonTemplate.ResetTemplate();
		_fundButtons.Clear();
		string empty = string.Empty;
		string empty2 = string.Empty;
		foreach (InvestmentFund investmentFund in SaveGameManager.Current.investmentFunds)
		{
			InvestmentFundButton component = UnityEngine.Object.Instantiate(fundButtonTemplate, fundButtonTemplate.parent).GetComponent<InvestmentFundButton>();
			component.Setup(investmentFund, OnClickInvestmentFund);
			component.gameObject.SetActive(value: true);
			_fundButtons.Add(component);
			if (string.IsNullOrEmpty(empty))
			{
				empty = investmentFund.name;
			}
			if (investmentFund.name == _currentInvestmentName)
			{
				empty2 = investmentFund.name;
			}
		}
		string text = (string.IsNullOrEmpty(empty2) ? empty : empty2);
		if (!string.IsNullOrEmpty(text))
		{
			OnClickInvestmentFund(text);
		}
	}

	private void OnClickInvestmentFund(string fundName)
	{
		_currentInvestment = InvestmentFundHelper.GetInvestmentByName(fundName);
		_currentInvestmentName = fundName;
		RefreshFundButtons();
		fundNameLabel.Key = fundName;
		SetupCurrentValueTooltip(_currentInvestment);
		currentValueLabel.SetText(_currentInvestment.CurrentValue.ToShortCurrencyFormat());
		currentValueLabel.color = ((_currentInvestment.interestPayment >= 0f) ? InstanceBehavior<GlobalReferences>.Instance.colors.darkGreen : InstanceBehavior<GlobalReferences>.Instance.colors.red);
		long maxNumeralAmount = (long)Math.Floor(_currentInvestment.CurrentValue);
		withdrawField.SetMaxNumeralAmount(maxNumeralAmount);
		withdrawField.SetText(0.ToShortCurrencyFormat());
		autoInvestField.SetText(_currentInvestment.autoInvestment.ToShortCurrencyFormat());
		autoInvestToggle.SetIsOnWithoutNotify(_currentInvestment.isAutoInvesting);
		lineChart.valueFormatter = (float value) => value.ToShortCurrencyFormat();
		notEnoughDataLabel.gameObject.SetActive(_currentInvestment.developmentHistory.Count <= 1);
		lineChart.gameObject.SetActive(_currentInvestment.developmentHistory.Count > 1);
		if (_currentInvestment.developmentHistory.Count > 1)
		{
			LoadChart(lineChart, _currentInvestment.developmentHistory);
		}
	}

	private void RefreshFundButtons()
	{
		for (int i = 0; i < _fundButtons.Count; i++)
		{
			_fundButtons[i].SetSelected(_fundButtons[i].FundName == _currentInvestmentName);
		}
	}

	private void SetupCurrentValueTooltip(InvestmentFund investment)
	{
		currentValueTooltip.descriptionKey = "econoview_investments_current_value_tooltip";
		currentValueTooltip.localizationArguments = new
		{
			value = investment.CurrentValue.ToShortCurrencyFormat()
		};
		currentValueTooltip.list = new List<string>
		{
			"econoview_investments_initial_deposit_tooltip".Localize(new
			{
				value = investment.initialDeposit.ToShortCurrencyFormat()
			}).ToString(),
			"econoview_investments_additional_investments_tooltip".Localize(new
			{
				value = investment.additionalInvestment.ToShortCurrencyFormat()
			}).ToString(),
			"econoview_investments_withdrawals_tooltip".Localize(new
			{
				value = investment.withdrawal.ToShortCurrencyFormat()
			}).ToString(),
			"econoview_investments_interest_payments_tooltip".Localize(new
			{
				value = investment.interestPayment.RoundToInt().ToShortCurrencyFormat()
			}).ToString()
		};
	}

	private void OnAutoInvestValueChanged(string _)
	{
		if (int.TryParse(autoInvestField.GetRawValue(), out var result))
		{
			_currentInvestment.autoInvestment = result;
		}
	}

	private void OnAutoInvestToggleValueChanged(bool isOn)
	{
		_currentInvestment.isAutoInvesting = isOn;
	}

	public void OnWithdrawClick()
	{
		string rawValue = withdrawField.GetRawValue();
		if (!decimal.TryParse(rawValue, NumberStyles.Currency, CultureHelper.CultureInfo, out var result))
		{
			if (!rawValue.FromShortCurrencyFormat(out var value))
			{
				return;
			}
			result = (decimal)value;
		}
		long num = (long)Math.Floor(_currentInvestment.CurrentValue);
		long withdrawal = (long)Math.Floor(result);
		if (withdrawal <= 0 || withdrawal > num)
		{
			Notifications.ShowError("econoview_investments_withdrawal_invalid");
			return;
		}
		bool isFullWithdrawal = withdrawal >= num;
		string bodyKey = (isFullWithdrawal ? "econoview_investments_confirmpayout_full" : "econoview_investments_confirmpayout");
		HudConfirm.Show(null, bodyKey, delegate
		{
			Dictionary<string, string> data = new Dictionary<string, string> { 
			{
				"investmentFundName",
				_currentInvestment.name.GetLocalization()
			} };
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_investmentpayout", data);
			if (GameManager.ChangeMoneySafe(withdrawal, transactionInfo, null, null, force: false, showNotification: true))
			{
				if (!isFullWithdrawal)
				{
					_currentInvestment.withdrawal += withdrawal;
					_currentInvestment.UpdateDevelopmentHistory();
					isFullWithdrawal = Math.Floor(_currentInvestment.CurrentValue) <= 0.0;
				}
				if (isFullWithdrawal)
				{
					SaveGameManager.Current.investmentFunds.Remove(_currentInvestment);
					_currentInvestment = null;
					_currentInvestmentName = string.Empty;
					Init();
					InstanceBehavior<UIs>.Instance.fullMenu.econoView.overview.RefreshTransactions();
				}
				else
				{
					Init();
					InstanceBehavior<UIs>.Instance.fullMenu.econoView.overview.RefreshTransactions();
				}
			}
		});
	}

	private static void LoadChart(LineChart chart, List<InvestmentProgressEntry> developmentHistory)
	{
		int num = developmentHistory.Count - 15;
		if (num < 0)
		{
			num = 0;
		}
		List<InvestmentProgressEntry> list = new List<InvestmentProgressEntry>(developmentHistory.Count - num - 1);
		for (int i = num; i < developmentHistory.Count - 1; i++)
		{
			list.Add(developmentHistory[i]);
		}
		List<LineEntry> list2 = new List<LineEntry>(list.Count);
		List<string> list3 = new List<string>(list.Count);
		float newBalance = list[0].newBalance;
		float num2 = newBalance;
		for (int j = 0; j < list.Count; j++)
		{
			InvestmentProgressEntry investmentProgressEntry = list[j];
			list2.Add(new LineEntry(j, investmentProgressEntry.newBalance));
			list3.Add(investmentProgressEntry.day.ToString());
			if (investmentProgressEntry.newBalance < newBalance)
			{
				newBalance = investmentProgressEntry.newBalance;
			}
			if (investmentProgressEntry.newBalance > num2)
			{
				num2 = investmentProgressEntry.newBalance;
			}
		}
		chart.GetChartData().DataSets[0].Entries = list2;
		chart.AxisConfig.HorizontalAxisConfig.LabelsCount = list.Count;
		chart.AxisConfig.HorizontalAxisConfig.ValueFormatterConfig.CustomValues = list3;
		float num3 = Mathf.Floor(newBalance / 4f) * 4f;
		chart.AxisConfig.VerticalAxisConfig.Bounds.Min = num3;
		float num4 = Mathf.Ceil(num2 / 4f) * 4f;
		if (Mathf.Abs(num4 - num3) < 1f)
		{
			num4 = num3 + 4f;
		}
		chart.AxisConfig.VerticalAxisConfig.Bounds.Max = num4;
		chart.CustomVerticalAxisValueFormatter = new CurrencyAxisFormatter();
		chart.SetDirty();
	}
}
