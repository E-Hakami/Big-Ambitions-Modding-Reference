using System.Collections.Generic;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;

public class MigrateCurrentTaxPeriodDeductibleExpenses : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (gameInstance.CurrentTaxPeriodDeductibleTransactions == null || gameInstance.CurrentTaxPeriodDeductibleTransactions.Count == 0)
		{
			return;
		}
		if (gameInstance.currentTaxPeriodDeductibleExpenses == null)
		{
			gameInstance.currentTaxPeriodDeductibleExpenses = new List<TaxDeductibleExpense>();
		}
		foreach (var currentTaxPeriodDeductibleTransaction in gameInstance.CurrentTaxPeriodDeductibleTransactions)
		{
			AddDeductibleExpense(gameInstance.currentTaxPeriodDeductibleExpenses, currentTaxPeriodDeductibleTransaction.Item1, currentTaxPeriodDeductibleTransaction.Item2);
		}
		gameInstance.CurrentTaxPeriodDeductibleTransactions.Clear();
	}

	private static void AddDeductibleExpense(List<TaxDeductibleExpense> expenses, string key, float amount)
	{
		if (string.IsNullOrEmpty(key))
		{
			return;
		}
		if (amount < 0f)
		{
			amount = 0f - amount;
		}
		if (amount <= 0f)
		{
			return;
		}
		for (int i = 0; i < expenses.Count; i++)
		{
			TaxDeductibleExpense taxDeductibleExpense = expenses[i];
			if (!(taxDeductibleExpense.key != key) && (taxDeductibleExpense.values == null || taxDeductibleExpense.values.Count <= 0))
			{
				double num = (double)taxDeductibleExpense.amount + (double)amount;
				if (!(num > 3.4028234663852886E+38))
				{
					taxDeductibleExpense.amount = (float)num;
					return;
				}
			}
		}
		expenses.Add(new TaxDeductibleExpense
		{
			key = key,
			amount = amount
		});
	}
}
