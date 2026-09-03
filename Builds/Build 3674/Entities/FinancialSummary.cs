using System;
using System.Collections.Generic;

namespace Entities;

[Serializable]
public class FinancialSummary
{
	[Serializable]
	public class BusinessIncomeStatement
	{
		[Serializable]
		public class TransactionGroupEntry
		{
			public string ItemName;

			public float Amount;
		}

		public List<TransactionGroupEntry> Sales;

		public List<TransactionGroupEntry> Resources;

		public float SalaryExpenses;

		public float RentExpenses;

		public float MarketingExpenses;

		public float Theft;

		public float LicensingFees;

		public float TotalSales;

		public float TotalResources;

		public float TotalOngoing;

		public float TotalProfit;

		public Address Address;
	}

	[Serializable]
	public class ResidentialStatement
	{
		public Address Address;

		public float Amount;
	}

	[Serializable]
	public class RealEstateStatement
	{
		public Address Address;

		public float Amount;
	}

	public int dayNumber;

	public List<BusinessIncomeStatement> businessIncomeStatements;

	public float totalBusinessProfit;

	public float totalLoanExpenses;

	public float totalHealthInsuranceExpenses;

	public float totalHeadhunterReplacementFees;

	public float totalRealEstate;

	public float negativeInterestRates;

	public float parkingFees;

	public float salaryIncome;

	public List<ResidentialStatement> residentialStatements;

	public List<RealEstateStatement> realEstateStatements;

	public float totalResidentialExpenses;

	public float totalUnassignedStaffWages;

	public float totalProfit;
}
