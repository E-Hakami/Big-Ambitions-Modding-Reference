using System;
using System.Collections.Generic;
using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Components;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

public class LoanEntryUi : MonoBehaviour
{
	[SerializeField]
	private TMP_Text paidTotalLabel;

	[SerializeField]
	private TMP_Text bankLabel;

	[SerializeField]
	private TMP_Text dailyInterestRateLabel;

	[SerializeField]
	private TextLocalizationComponent timeLeftLabel;

	[SerializeField]
	private UI.Components.InputField dailyPaymentInput;

	[SerializeField]
	private UI.Components.InputField payOffInput;

	[SerializeField]
	private Button payOffButton;

	private string _bankName;

	private Loan _loan;

	private Action _onLoanChanged;

	private int _payOffAmount;

	public RectTransform PayOffButtonTarget => (RectTransform)payOffButton.transform;

	private void Awake()
	{
		payOffButton.onClick.AddListener(OnPayOffClick);
		dailyPaymentInput.tmpInputField.onEndEdit.AddListener(OnDailyPaymentEndEdit);
	}

	public void Setup(Loan loan, Action onLoanChanged)
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(loan.bankAddress);
		_loan = loan;
		_bankName = buildingRegistration?.BusinessName;
		_onLoanChanged = onLoanChanged;
		SetPaidTotalLabel(loan);
		bankLabel.text = _bankName;
		dailyInterestRateLabel.text = ((float)loan.dailyInterest).ToShortCurrencyFormat(false);
		int num = Mathf.FloorToInt(loan.remainingAmount);
		dailyPaymentInput.SetMaxNumeralAmount(num);
		ClampDailyPayment();
		if ((bool)payOffInput)
		{
			payOffInput.SetMaxNumeralAmount(num);
			payOffInput.SetText(num.ToShortCurrencyFormat());
		}
		SetDailyPaymentInput();
		SetLoanTimeLeft();
	}

	private void OnDailyPaymentEndEdit(string _)
	{
		string rawValue = dailyPaymentInput.GetRawValue();
		if (!string.IsNullOrEmpty(rawValue))
		{
			if (!int.TryParse(rawValue, out var result))
			{
				Notifications.ShowError("common_notification_invalid_amount");
				return;
			}
			_loan.dailyPayment = result;
			ClampDailyPayment();
			SetDailyPaymentInput();
			SetLoanTimeLeft();
		}
	}

	private void OnPayOffClick()
	{
		int num = (_payOffAmount = Mathf.FloorToInt(_loan.remainingAmount));
		if (!payOffInput || TryGetPayOffInputAmount(out _payOffAmount))
		{
			if (_payOffAmount <= 0)
			{
				Notifications.ShowError("common_notification_invalid_amount");
				return;
			}
			_payOffAmount = Mathf.Min(_payOffAmount, num);
			string key = ((_payOffAmount >= num) ? "econoview_loan_hud_confirm_payback_full_loan" : "econoview_loan_hud_confirm_payback_loan");
			HudConfirm.Show(default(LanguageChangeEventDataHolder), LanguageChangeEventDataHolder.Create(key, new
			{
				loan = _payOffAmount.ToShortCurrencyFormat()
			}), PayOffLoan);
		}
	}

	private bool TryGetPayOffInputAmount(out int payOffAmount)
	{
		payOffAmount = 0;
		string rawValue = payOffInput.GetRawValue();
		if (string.IsNullOrEmpty(rawValue))
		{
			return false;
		}
		return int.TryParse(rawValue, out payOffAmount);
	}

	private void PayOffLoan()
	{
		Dictionary<string, string> data = new Dictionary<string, string> { { "businessName", _bankName } };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_loanpayoff", data);
		if (GameManager.ChangeMoneySafe(-_payOffAmount, transactionInfo, null, null, force: false, showNotification: true))
		{
			_loan.remainingAmount -= _payOffAmount;
			if (_loan.remainingAmount <= 0f)
			{
				SaveGameManager.Current.Loans.Remove(_loan);
				GameEvent.Invoke("ba:gameevent_loanpaid");
			}
			_onLoanChanged?.Invoke();
		}
	}

	private void ClampDailyPayment()
	{
		int num = Mathf.FloorToInt(_loan.remainingAmount);
		int min = Mathf.Min(LoanHelper.CalculateMinimumDailyPayment(_loan), num);
		_loan.dailyPayment = Mathf.Clamp(_loan.dailyPayment, min, num);
	}

	private void SetDailyPaymentInput()
	{
		dailyPaymentInput.ClearText();
		dailyPaymentInput.SetText(_loan.dailyPayment.ToShortCurrencyFormat());
	}

	private void SetLoanTimeLeft()
	{
		if (_loan.dailyPayment > 0)
		{
			int num = Mathf.CeilToInt(_loan.remainingAmount / (float)_loan.dailyPayment);
			timeLeftLabel.SetData(LanguageChangeEventDataHolder.Create("econoview_loan_time_remaining", new
			{
				finalDay = TimeHelper.CurrentDay + num,
				daysLeft = ((num > 999) ? "999+" : num.ToString())
			}));
		}
		else
		{
			timeLeftLabel.Key = "";
			timeLeftLabel.TextContainer.text = "-";
		}
	}

	private void SetPaidTotalLabel(Loan loan)
	{
		string text = Colors.DarkGreen.ToHex();
		string text2 = Colors.Red.ToHex();
		string text3 = "<color=" + text + ">" + loan.PaidAmount.ToShortCurrencyFormat() + "</color>";
		string text4 = "<color=" + text2 + ">/" + loan.totalAmount.ToShortCurrencyFormat() + "</color>";
		paidTotalLabel.SetText(text3 + text4);
	}
}
