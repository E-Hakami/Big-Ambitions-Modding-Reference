using System;
using System.Collections.Generic;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using UnityEngine;

namespace UI.Smartphone.Apps.Contacts;

public class TaxesMessage : MonoBehaviour
{
	private const string MessageHeadingKey = "contacts_taxes_message_heading";

	private const string MessageEndingKey = "contacts_taxes_message_ending";

	private const string RepossessionMessageHeadingKey = "contacts_taxes_repossession_message_heading";

	private const string RepossessionMessageEndingKey = "contacts_taxes_repossession_message_ending";

	private const string IncomeKey = "contacts_taxes_income";

	private const string GamblingKey = "contacts_taxes_gambling";

	private const string TaxDeductionsKey = "contacts_taxes_tax_deductions";

	private const string RealEstateKey = "contacts_taxes_real_estate";

	private const string RepossessedValuablesKey = "contacts_taxes_repossessed_valuables";

	private const string GrandTotalKey = "contacts_taxes_grand_total";

	private const string TaxableIncomeKey = "contacts_taxes_taxable_income";

	private const string TaxRateKey = "contacts_taxes_tax_rate";

	private const string IncomeTaxKey = "contacts_taxes_income_tax";

	private const string RealEstateTaxKey = "contacts_taxes_real_estate_tax";

	private const string TotalTaxDueKey = "contacts_taxes_total_tax_due";

	private const string SubtotalKey = "common_subtotal";

	private const char NegativePrefix = '-';

	private static string IrsFormattedAddress;

	[SerializeField]
	private TextLocalizationComponent headerLabel;

	[SerializeField]
	private TextLocalizationComponent endingLabel;

	[SerializeField]
	private TaxesMessageLine taxLineTemplate;

	[SerializeField]
	private Transform splitterTemplate;

	[SerializeField]
	private Transform emptyLineTemplate;

	private void Awake()
	{
		IrsFormattedAddress = TaxHelper.GetIRSAddress().ToFormattedString();
	}

	public void SetData(Taxes taxes)
	{
		ClearTemplates();
		SetTaxesHeader(taxes);
		AddIncomeSection(taxes);
		AddDeductionSection(taxes);
		AddRealEstateSection(taxes);
		AddGrandTotalSection(taxes);
		base.gameObject.SetActive(value: true);
	}

	private void ClearTemplates()
	{
		taxLineTemplate.transform.ClearClones();
		taxLineTemplate.gameObject.SetActive(value: false);
		splitterTemplate.gameObject.SetActive(value: false);
		emptyLineTemplate.gameObject.SetActive(value: false);
	}

	private void SetTaxesHeader(Taxes taxes)
	{
		headerLabel.SetData("contacts_taxes_message_heading".Localize(new
		{
			startingDay = taxes.day - SaveGameManager.Current.gameVariables.daysPerYear + 1,
			endingDay = taxes.day
		}));
		endingLabel.SetData("contacts_taxes_message_ending".Localize(new
		{
			lastPayingDay = taxes.day + 20,
			address = IrsFormattedAddress
		}));
	}

	private void AddIncomeSection(Taxes taxes)
	{
		if (taxes.businessesIncome.Count <= 0 && taxes.subtotalGamblingWinnings <= 0f)
		{
			return;
		}
		AddSectionHeader("contacts_taxes_income");
		foreach (var item in taxes.businessesIncome)
		{
			if (item.Item2 != 0f)
			{
				AddPlainLine(item.Item1, item.Item2.ToCurrencyFormat());
			}
		}
		if (taxes.subtotalGamblingWinnings > 0f)
		{
			AddLocalizedLine("contacts_taxes_gambling", taxes.subtotalGamblingWinnings.ToCurrencyFormat());
		}
	}

	public void SetData(List<string> repossessedLabels)
	{
		SetData(repossessedLabels, 0f);
	}

	public void SetData(List<string> repossessedLabels, float backTaxesOwed)
	{
		ClearTemplates();
		headerLabel.SetData("contacts_taxes_repossession_message_heading".Localize());
		endingLabel.SetData("contacts_taxes_repossession_message_ending".Localize());
		bool flag = repossessedLabels != null && repossessedLabels.Count > 0;
		if (flag)
		{
			AddLocalizedLine("contacts_taxes_repossessed_valuables", string.Empty);
			foreach (string repossessedLabel in repossessedLabels)
			{
				AddPlainLine(repossessedLabel, string.Empty);
			}
		}
		if (backTaxesOwed > 0f)
		{
			if (flag)
			{
				UnityEngine.Object.Instantiate(emptyLineTemplate, emptyLineTemplate.parent).gameObject.SetActive(value: true);
			}
			AddPlainLine("contacts_taxes_back_taxes_still_owed".Localize(new
			{
				amount = backTaxesOwed.ToCurrencyFormat()
			}).ToString(), string.Empty);
		}
		base.gameObject.SetActive(value: true);
	}

	private void AddDeductionSection(Taxes taxes)
	{
		if (taxes.deductibleExpenses.Count <= 0)
		{
			return;
		}
		AddSectionHeader("contacts_taxes_tax_deductions");
		List<(string, float)> list = new List<(string, float)>(taxes.deductibleExpenses);
		list.Sort(CompareDeductibleExpenseLabels);
		foreach (var item in list)
		{
			AddPlainLine(item.Item1, item.Item2.ToCurrencyFormat());
		}
		AddEmptyLine();
		AddSubtotal(taxes.subtotalDeductibleExpenses.ToCurrencyFormat());
		AddSplitter();
	}

	private void AddRealEstateSection(Taxes taxes)
	{
		if (taxes.estateTaxes.Count <= 0)
		{
			return;
		}
		AddSectionHeader("contacts_taxes_real_estate");
		foreach (var (label, val) in taxes.estateTaxes)
		{
			AddPlainLine(label, val.ToCurrencyFormat());
		}
		AddEmptyLine();
		AddSubtotal(taxes.subtotalRealEstateTaxes.ToCurrencyFormat());
		AddSplitter();
	}

	private void AddGrandTotalSection(Taxes taxes)
	{
		AddSectionHeader("contacts_taxes_grand_total");
		float totalIncome = GetTotalIncome(taxes);
		float taxableIncome = GetTaxableIncome(taxes);
		float incomeTax = GetIncomeTax(taxableIncome, taxes.taxPercentage);
		float f = incomeTax + taxes.subtotalRealEstateTaxes;
		AddLocalizedLine("contacts_taxes_income", totalIncome.ToCurrencyFormat());
		AddLocalizedLine("contacts_taxes_tax_deductions", GetNegativeCurrencyFormat(taxes.subtotalDeductibleExpenses));
		AddSplitter();
		AddLocalizedLine("contacts_taxes_taxable_income", taxableIncome.ToCurrencyFormat());
		AddLocalizedLine("contacts_taxes_tax_rate", $"{taxes.taxPercentage}%");
		AddSplitter();
		AddLocalizedLine("contacts_taxes_income_tax", incomeTax.ToCurrencyFormat());
		AddLocalizedLine("contacts_taxes_real_estate_tax", taxes.subtotalRealEstateTaxes.ToCurrencyFormat());
		AddSplitter();
		AddLocalizedLine("contacts_taxes_total_tax_due", Mathf.Floor(f).ToCurrencyFormat());
	}

	private TaxesMessageLine AddLine()
	{
		TaxesMessageLine taxesMessageLine = UnityEngine.Object.Instantiate(taxLineTemplate, taxLineTemplate.transform.parent);
		taxesMessageLine.gameObject.SetActive(value: true);
		return taxesMessageLine;
	}

	private void AddPlainLine(string label, string value)
	{
		AddLine().SetPlain(label, value);
	}

	private void AddLocalizedLine(string labelKey, string value)
	{
		AddLine().SetLocalized(labelKey, value);
	}

	private void AddSectionHeader(string labelKey)
	{
		AddLine().SetBoldLocalized(labelKey);
	}

	private void AddSubtotal(string value)
	{
		AddLocalizedLine("common_subtotal", value);
	}

	private void AddSplitter()
	{
		UnityEngine.Object.Instantiate(splitterTemplate, splitterTemplate.parent).gameObject.SetActive(value: true);
	}

	private void AddEmptyLine()
	{
		UnityEngine.Object.Instantiate(emptyLineTemplate, emptyLineTemplate.parent).gameObject.SetActive(value: true);
	}

	private static float GetTotalIncome(Taxes taxes)
	{
		return taxes.subtotalRegisteredBusinesses + taxes.subtotalGamblingWinnings;
	}

	private static float GetTaxableIncome(Taxes taxes)
	{
		float num = GetTotalIncome(taxes) - taxes.subtotalDeductibleExpenses;
		if (!(num > 0f))
		{
			return 0f;
		}
		return num;
	}

	private static float GetIncomeTax(float taxableIncome, int taxPercentage)
	{
		return taxableIncome * (float)taxPercentage / 100f;
	}

	private static string GetNegativeCurrencyFormat(float value)
	{
		return "-" + value.ToCurrencyFormat();
	}

	private static int CompareDeductibleExpenseLabels((string, float) first, (string, float) second)
	{
		int num = string.Compare(first.Item1, second.Item1, StringComparison.CurrentCultureIgnoreCase);
		if (num == 0)
		{
			return string.Compare(first.Item1, second.Item1, StringComparison.OrdinalIgnoreCase);
		}
		return num;
	}
}
