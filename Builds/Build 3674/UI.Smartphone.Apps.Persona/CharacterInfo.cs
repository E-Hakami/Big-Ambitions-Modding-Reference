using System.Collections.Generic;
using BigAmbitions.Tags;
using Entities;
using Extensions;
using Helpers;
using TMPro;
using UnityEngine;

namespace UI.Smartphone.Apps.Persona;

public class CharacterInfo : MonoBehaviour
{
	[SerializeField]
	private TMP_Text characterNameLabel;

	[SerializeField]
	private TMP_Text characterAgeLabel;

	[SerializeField]
	private TMP_Text totalBusinessesLabel;

	[SerializeField]
	private TMP_Text totalEmployeesLabel;

	[SerializeField]
	private TMP_Text weeklyIncomeLabel;

	[SerializeField]
	private TMP_Text personalWealthLabel;

	[SerializeField]
	private TMP_Text cashLabel;

	[SerializeField]
	private TMP_Text investmentsLabel;

	[SerializeField]
	private TMP_Text loansLabel;

	[SerializeField]
	private TMP_Text assetsLabel;

	private void OnEnable()
	{
		CharacterData characterData = SaveGameManager.Current.charactersData[0];
		int yearsByDays = TimeHelper.GetYearsByDays(characterData.ageInDays);
		int num = SaveGameManager.Current.BuildingRegistrations.CountWhere((BuildingRegistration x) => x.RentedByPlayer && BusinessTypeHelper.GetData(x).HasTag(TagRef.Businesstag.generatesrevenue));
		int num2 = EmployeeHelper.EmployeeInstancesDictionary.CountWhere((KeyValuePair<string, EmployeeInstance> x) => !x.Value.IsCandidate);
		float num3 = FinancialSummaryHelper.GetLastFinancialSummaries(7).SumValues((FinancialSummary x) => x.totalProfit);
		characterNameLabel.text = characterData.name;
		characterAgeLabel.SetText(yearsByDays.ToString());
		totalBusinessesLabel.SetText(num.ToString());
		totalEmployeesLabel.SetText(num2.ToString());
		weeklyIncomeLabel.SetText(num3.ToShortCurrencyFormat(abbreviated: true));
		weeklyIncomeLabel.ColorValueLabel(num3);
		PersonalWealthData personalWealth = PlayerHelper.GetPersonalWealth();
		cashLabel.SetText(personalWealth.cash.ToShortCurrencyFormat(abbreviated: true));
		investmentsLabel.SetText(personalWealth.totalInvestments.ToShortCurrencyFormat(abbreviated: true));
		loansLabel.SetText((personalWealth.totalLoans > 0f) ? ("-" + personalWealth.totalLoans.ToShortCurrencyFormat(abbreviated: true)) : personalWealth.totalLoans.ToShortCurrencyFormat(abbreviated: true));
		assetsLabel.SetText(personalWealth.totalAssets.ToShortCurrencyFormat(abbreviated: true));
		personalWealthLabel.SetText(personalWealth.CurrentWealth.ToShortCurrencyFormat(abbreviated: true));
		cashLabel.ColorValueLabel(personalWealth.cash);
		investmentsLabel.ColorValueLabel(personalWealth.totalInvestments);
		loansLabel.ColorValueLabel(personalWealth.totalLoans, invert: true);
		assetsLabel.ColorValueLabel(personalWealth.totalAssets);
		personalWealthLabel.ColorValueLabel(personalWealth.CurrentWealth);
	}
}
