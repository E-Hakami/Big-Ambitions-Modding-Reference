using System.Collections.Generic;
using System.Linq;
using Entities;
using Extensions;
using Localizor.LanguageChangeEvent;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/BusinessWeeklyIncomeGoal")]
public class BusinessWeeklyIncomeGoal : FloatBaseGoal
{
	protected override float GetValue()
	{
		if (SaveGameManager.Current.financialSummaries.Count == 0)
		{
			return 0f;
		}
		return (from x in SaveGameManager.Current.financialSummaries.Select((FinancialSummary x) => (x.dayNumber - TimeHelper.GetDayOfWeekIndex(TimeHelper.GetDayOfWeek(x.dayNumber)), businessIncomeStatements: x.businessIncomeStatements)).SelectMany(((int, List<FinancialSummary.BusinessIncomeStatement> businessIncomeStatements) x) => x.businessIncomeStatements.Select((FinancialSummary.BusinessIncomeStatement b) => (x.Item1, b: b)))
			group x by (x.Item1, Address: x.b.Address) into x
			select x.Sum(((int, FinancialSummary.BusinessIncomeStatement b) s) => s.b.TotalProfit)).DefaultIfEmpty(0f).Max();
	}

	public override LanguageChangeEventDataHolder GetTitle()
	{
		LanguageChangeEventDataHolder result = base.GetTitle();
		result.Arguments = new
		{
			amount = amount.ToShortCurrencyFormat()
		};
		return result;
	}

	protected override object FormatProgressValue(float value)
	{
		return value.ToShortCurrencyFormat();
	}
}
