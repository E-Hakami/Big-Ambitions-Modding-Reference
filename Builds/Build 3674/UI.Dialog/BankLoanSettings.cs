using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using SpecialServices.Bank;
using TMPro;
using UnityEngine;

namespace UI.Dialog;

public class BankLoanSettings : MonoBehaviour
{
	public TMP_InputField amountInput;

	[HideInInspector]
	public int dailyInterest;

	[HideInInspector]
	public int dailyPayment;

	[SerializeField]
	private TextLocalizationComponent interestLabel;

	[SerializeField]
	private TMP_Text dailyInterestPaymentAmountLabel;

	[SerializeField]
	private TMP_Text dailyPaymentAmountLabel;

	[SerializeField]
	private TMP_Text totalDailyPaymentAmountLabel;

	private BankSettings _bankSettings;

	private void Start()
	{
		_bankSettings = LoanHelper.GetBankSettings(DialogController.current.contact.Address);
		interestLabel.Arguments = new
		{
			rate = LoanHelper.CalculateEffectiveAnnualInterestRate(_bankSettings)
		};
		amountInput.onValueChanged.AddListener(delegate
		{
			SetDailyAmounts();
		});
		SetDailyAmounts();
	}

	private void SetDailyAmounts()
	{
		float result = 0f;
		if (string.IsNullOrEmpty(amountInput.text) || float.TryParse(amountInput.text, out result))
		{
			dailyInterest = LoanHelper.CalculateDailyInterestPayment(result, _bankSettings);
			dailyPayment = LoanHelper.CalculateDailyPayment(result, _bankSettings);
			int val = dailyInterest + dailyPayment;
			dailyInterestPaymentAmountLabel.text = dailyInterest.ToShortCurrencyFormat();
			dailyPaymentAmountLabel.text = dailyPayment.ToShortCurrencyFormat();
			totalDailyPaymentAmountLabel.text = val.ToShortCurrencyFormat();
		}
	}
}
