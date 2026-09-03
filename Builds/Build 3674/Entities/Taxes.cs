using System;
using System.Collections.Generic;

namespace Entities;

[Serializable]
public class Taxes
{
	public int day;

	public int taxPercentage;

	public int dueDay;

	public bool lateFeeApplied;

	public List<(string, float)> businessesIncome;

	public List<(string, float)> estateTaxes;

	public List<(string, float)> deductibleExpenses;

	public float subtotalGamblingWinnings;

	public float subtotalRegisteredBusinesses;

	public float subtotalRealEstateTaxes;

	public float subtotalDeductibleExpenses;

	public float totalToPay;
}
