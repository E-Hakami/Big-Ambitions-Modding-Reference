using System.Collections.Generic;
using Entities;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Money/HasGeneratedRevenueOnEachBusiness")]
public class HasGeneratedRevenueOnEachBusiness : QuestRequirement
{
	[SerializeField]
	private int amountOfBusinesses;

	[SerializeField]
	private int minimumDailyProfitPerBusiness;

	[SerializeField]
	private int amountOfDays;

	public override bool CheckIfCompleted()
	{
		List<FinancialSummary> lastFinancialSummaries = FinancialSummaryHelper.GetLastFinancialSummaries(amountOfDays);
		if (lastFinancialSummaries.Count < amountOfDays)
		{
			return false;
		}
		for (int i = 0; i < amountOfDays; i++)
		{
			if (lastFinancialSummaries[i].businessIncomeStatements == null)
			{
				return false;
			}
			int num = 0;
			foreach (FinancialSummary.BusinessIncomeStatement businessIncomeStatement in lastFinancialSummaries[i].businessIncomeStatements)
			{
				if (businessIncomeStatement.TotalProfit >= (float)minimumDailyProfitPerBusiness)
				{
					num++;
				}
			}
			if (num < amountOfBusinesses)
			{
				return false;
			}
		}
		return true;
	}
}
