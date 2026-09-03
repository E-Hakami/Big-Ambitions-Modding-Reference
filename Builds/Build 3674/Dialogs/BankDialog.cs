using System;
using System.Collections.Generic;
using Buildings;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using SpecialServices.Bank;
using Tutorial.SideQuests;
using UI.Dialog;
using UI.Notification;
using UnityEngine;

namespace Dialogs;

public class BankDialog : Dialog
{
	private const float SideQuestEmergencyLoanMaxAmount = 15000f;

	private const float MinimumLoanAmount = 500f;

	private const float DebtToIncomeRatio = 0.25f;

	public BankDialog()
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(DialogController.current.contact.Address);
		npcNameKey = ((buildingRegistration.BusinessName == "Jensen Capital") ? "dialog_bank_larry" : "dialog_bank_npc_name");
		DialogController.current.ShowEntry(Start());
	}

	private DialogEntry Start()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_bank_selector", null, read: true, isNewInteraction: true));
		return new DialogEntry
		{
			headerKey = npcNameKey,
			messageData = "dialog_bank_selector".Localize(),
			Template = DialogEntry.TemplateType.Text,
			ConfirmTextOverride = "dialog_bank_new_loan".Localize(),
			SecondOptionTextOverride = "dialog_bank_new_investment",
			OnCancel = DialogController.current.CancelDialog,
			OnConfirm = LoanAmountInput,
			OnSecondOption = InvestmentInput
		};
	}

	private DialogEntry LoanAmountInput()
	{
		return new DialogEntry
		{
			headerKey = "dialog_bank_loan_amount_header",
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.BankLoan,
			OnConfirm = LoanRequest,
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private DialogEntry InvestmentInput()
	{
		return new DialogEntry
		{
			headerKey = "dialog_bank_investment_header",
			Template = DialogEntry.TemplateType.Input,
			InputTemplate = DialogEntry.InputTemplateName.BankInvestment,
			OnConfirm = InvestmentRequest,
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private DialogEntry InvestmentRequest()
	{
		BankInvestmentSettings inputComponent = DialogController.current.GetInputComponent<BankInvestmentSettings>();
		if (inputComponent == null || inputComponent.selectedFundData == null)
		{
			Notifications.ShowError("bankdialog_notification_select_investment_fund");
			return null;
		}
		if (!float.TryParse(inputComponent.amountField.GetRawValue(), out var result) || result < 1000f)
		{
			return InvestmentTooLow(1000f);
		}
		float num = 0f;
		if (float.TryParse(inputComponent.autoInvestField.GetRawValue(), out var result2) && result2 > 0f)
		{
			num = result2;
		}
		InvestmentFund investmentByName = InvestmentFundHelper.GetInvestmentByName(inputComponent.selectedFundData.fundName, createIfNotExists: true);
		if (!investmentByName.Invest(result))
		{
			return null;
		}
		if (num > 0f)
		{
			investmentByName.isAutoInvesting = true;
			investmentByName.autoInvestment = num;
		}
		Dictionary<string, string> messageData = new Dictionary<string, string>
		{
			{
				"amount",
				result.ToShortCurrencyFormat()
			},
			{
				"autoInvest",
				num.ToShortCurrencyFormat()
			},
			{
				"investmentFund",
				inputComponent.selectedFundData.fundName.GetLocalization()
			}
		};
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_bank_investment_request_player", messageData));
		return InvestmentAccepted();
	}

	private DialogEntry InvestmentTooLow(float minimumAmount)
	{
		string text = minimumAmount.ToShortCurrencyFormat();
		Dictionary<string, string> messageData = new Dictionary<string, string> { { "minAmount", text } };
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_bank_investment_too_low", messageData, read: true));
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Text,
			headerKey = npcNameKey,
			messageData = "ba:messagetype_dialog_bank_investment_too_low".Localize(new
			{
				minAmount = text
			}),
			OnConfirm = InvestmentInput,
			ConfirmTextOverride = "dialog_try_again_button".Localize(),
			OnCancel = DialogController.current.CancelDialog
		};
	}

	private DialogEntry InvestmentAccepted()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_bank_investment_accepted", null, read: true));
		return new DialogEntry
		{
			messageData = "dialog_bank_investment_accepted".Localize(),
			OnConfirm = InvestmentInput,
			ConfirmTextOverride = "dialog_bank_make_another_investment".Localize(),
			OnCancel = DialogController.current.CancelDialog,
			onCancelMessage = new TextMessage("ba:messagetype_contacts_message_player_cancel_call")
		};
	}

	private DialogEntry LoanRequest()
	{
		BankLoanSettings inputComponent = DialogController.current.GetInputComponent<BankLoanSettings>();
		string text = inputComponent.amountInput.text;
		if (string.IsNullOrEmpty(text))
		{
			text = "0";
		}
		Dictionary<string, string> messageData = new Dictionary<string, string> { { "amount", text } };
		DialogController.current.contact.ReceivePlayerMessage(new TextMessage("ba:messagetype_dialog_bank_loan_request_player", messageData));
		float num = float.Parse(text);
		if (num < 0f)
		{
			return Fraud();
		}
		if (num == 0f)
		{
			return ZeroInput();
		}
		if (num < 500f)
		{
			return TooLow();
		}
		Building building = BuildingHelper.GetBuilding(DialogController.current.contact.Address);
		BankSettings bankSettings = (BankSettings)building.SpecialService.settings;
		bool flag = bankSettings.allowSideQuestEmergencyLoan && SideQuestHelper.IsSideQuestActive("SQBankruptcy");
		float num2 = float.PositiveInfinity;
		if (flag)
		{
			float money = SaveGameManager.Current.Money;
			num2 = ((money < 0f) ? (Mathf.Abs(money) + 15000f) : (15000f - money));
		}
		else if (building.SpecialService.settings != null)
		{
			num2 = bankSettings.maxTotalLoanAmount;
		}
		float num3 = Mathf.Max(0f, flag ? num2 : (num2 - GetRemainingLoanAmount(DialogController.current.contact.Address)));
		if (!flag)
		{
			float economyMaxLoanAmount = GetEconomyMaxLoanAmount(bankSettings);
			if (num > economyMaxLoanAmount && economyMaxLoanAmount < num3)
			{
				return LoanDenied(economyMaxLoanAmount);
			}
		}
		if (num > num3)
		{
			return ExceedingBankMaxTotal(num3);
		}
		Loan item = new Loan(num, inputComponent.dailyInterest, inputComponent.dailyPayment, DialogController.current.contact.Address);
		SaveGameManager.Current.Loans.Add(item);
		Dictionary<string, string> data = new Dictionary<string, string> { 
		{
			"businessName",
			BuildingHelper.GetBuildingRegistration(DialogController.current.contact.Address).BusinessName
		} };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_loanpayout", data);
		GameManager.ChangeMoneySafe(num, transactionInfo, null, null, force: true);
		GameEvent.Invoke("ba:gameevent_newloan");
		return LoanAccepted();
	}

	private DialogEntry LoanAccepted()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_bank_loan_accepted", null, read: true));
		return new DialogEntry
		{
			messageData = "dialog_bank_loan_accepted".Localize(),
			OnVisible = ((DialogController.current.dialogType == DialogType.PhoneCall) ? new Action(DialogController.current.FinishDialog) : null),
			OnCancel = ((DialogController.current.dialogType == DialogType.Physical) ? new Action(DialogController.current.FinishDialog) : null)
		};
	}

	private DialogEntry LoanDenied(float maxAmount)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		string text;
		if (maxAmount >= 500f)
		{
			text = "ba:messagetype_dialog_bank_loan_denied_1";
			dictionary.Add("amount", maxAmount.ToShortCurrencyFormat());
		}
		else
		{
			text = "ba:messagetype_dialog_bank_loan_denied_2";
		}
		DialogController.current.contact.SendMessage(new TextMessage(text, dictionary, read: true));
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Text,
			headerKey = npcNameKey,
			messageData = text.Localize(dictionary),
			OnConfirm = ((maxAmount > 0f) ? new Func<DialogEntry>(LoanAmountInput) : null),
			ConfirmTextOverride = "dialog_try_again_button".Localize(),
			OnCancel = DialogController.current.FinishDialog
		};
	}

	private DialogEntry ExceedingBankMaxTotal(float requestableAmount)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string> { 
		{
			"amount",
			requestableAmount.ToShortCurrencyFormat()
		} };
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_bank_loan_maximum_exceeded", dictionary, read: true));
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Text,
			headerKey = npcNameKey,
			messageData = "dialog_bank_loan_maximum_exceeded".Localize(dictionary),
			OnConfirm = LoanAmountInput,
			ConfirmTextOverride = "dialog_try_again_button".Localize(),
			OnCancel = DialogController.current.FinishDialog
		};
	}

	private DialogEntry Fraud()
	{
		string text = ((DialogController.current.dialogType == DialogType.Physical) ? "ba:messagetype_dialog_bank_loan_fraud_physical" : "ba:messagetype_dialog_bank_loan_fraud_phone");
		DialogController.current.contact.SendMessage(new TextMessage(text, null, read: true));
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Text,
			headerKey = npcNameKey,
			messageData = text.Localize(),
			OnVisible = ((DialogController.current.dialogType == DialogType.PhoneCall) ? new Action(DialogController.current.FinishDialog) : null),
			OnCancel = ((DialogController.current.dialogType == DialogType.Physical) ? new Action(DialogController.current.FinishDialog) : null)
		};
	}

	private DialogEntry ZeroInput()
	{
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_bank_loan_zero_input", null, read: true));
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Text,
			headerKey = npcNameKey,
			messageData = "dialog_bank_loan_zero_input".Localize(),
			OnConfirm = LoanAmountInput,
			ConfirmTextOverride = "dialog_try_again_button".Localize(),
			OnCancel = DialogController.current.CancelDialog
		};
	}

	private DialogEntry TooLow()
	{
		string text = 500f.ToShortCurrencyFormat();
		Dictionary<string, string> messageData = new Dictionary<string, string> { { "amount", text } };
		DialogController.current.contact.SendMessage(new TextMessage("ba:messagetype_dialog_bank_loan_too_low", messageData, read: true));
		return new DialogEntry
		{
			Template = DialogEntry.TemplateType.Text,
			headerKey = npcNameKey,
			messageData = "dialog_bank_loan_too_low".Localize(new
			{
				amount = text
			}),
			OnConfirm = LoanAmountInput,
			ConfirmTextOverride = "dialog_try_again_button".Localize(),
			OnCancel = DialogController.current.CancelDialog
		};
	}

	private static float GetEconomyMaxLoanAmount(BankSettings bankSettings)
	{
		float a = 0f;
		if (TutorialHelper.IsTutorialEnabled() && !SaveGameManager.Current.CompletedQuestEntries.Contains("tutorial_quest_establish_first_business_objective_1"))
		{
			a = 15500f;
		}
		float remainingLoanAmount = GetRemainingLoanAmount();
		float wealthBeforeLoans = PlayerHelper.GetPersonalWealth().WealthBeforeLoans;
		a = Mathf.Max(a, wealthBeforeLoans);
		float b = PlayerHelper.CalculateDailyIncome() * (float)LoanHelper.CalculatePayBackDays(bankSettings) * 0.25f;
		a = Mathf.Max(a, b);
		return Mathf.FloorToInt(Mathf.Max(0f, a - remainingLoanAmount));
	}

	private static float GetRemainingLoanAmount()
	{
		float num = 0f;
		foreach (Loan loan in SaveGameManager.Current.Loans)
		{
			num += loan.remainingAmount;
		}
		return num;
	}

	private static float GetRemainingLoanAmount(Address bankAddress)
	{
		float num = 0f;
		foreach (Loan loan in SaveGameManager.Current.Loans)
		{
			if (!(loan.bankAddress != bankAddress))
			{
				num += loan.remainingAmount;
			}
		}
		return num;
	}
}
