using System.Collections.Generic;
using System.Globalization;
using Entities;
using Helpers;
using Localizor;
using Streets;

namespace BigAmbitions.SaveSystem.Legacy.CompatParsers;

public static class TransactionDataParser
{
	public static Dictionary<string, string> ParseData(Transaction.DataHolder data)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (!string.IsNullOrEmpty(data.businessName))
		{
			dictionary["businessName"] = data.businessName;
		}
		if (data.address != null && !data.address.IsUndefined())
		{
			dictionary["address"] = data.address.ToFormattedString();
		}
		if (!string.IsNullOrEmpty(data.itemName))
		{
			dictionary["itemName"] = data.itemName;
		}
		if (!string.IsNullOrEmpty(data.warehouseName))
		{
			dictionary["warehouseName"] = data.warehouseName;
		}
		if (data.value != 0f)
		{
			string value = (dictionary["value"] = data.value.ToString(CultureInfo.InvariantCulture));
			dictionary["numberOfEmployees"] = value;
		}
		if (!string.IsNullOrEmpty(data.employee))
		{
			dictionary["employee"] = data.employee;
		}
		if (!string.IsNullOrEmpty(data.vehicleName))
		{
			dictionary["vehicleName"] = data.vehicleName;
		}
		if (!EqualityComparer<DiplomaName>.Default.Equals(data.diplomaName, DiplomaName.Undefined))
		{
			dictionary["diplomaName"] = data.diplomaName.GetLocalization();
		}
		if (!string.IsNullOrEmpty(data.skillName))
		{
			dictionary["skillName"] = data.skillName;
		}
		if (!string.IsNullOrEmpty(data.element))
		{
			dictionary["element"] = data.element;
			dictionary["text"] = data.element;
		}
		if (!EqualityComparer<BoatTypeName>.Default.Equals(data.boatTypeName, BoatTypeName.Speedboat))
		{
			dictionary["boatTypeName"] = data.boatTypeName.GetLocalization();
		}
		if (data.secondValue != 0f)
		{
			dictionary["secondValue"] = data.secondValue.ToString(CultureInfo.InvariantCulture);
		}
		if (!string.IsNullOrEmpty(data.cargoName))
		{
			dictionary["cargoName"] = data.cargoName;
		}
		dictionary["healthInsurancePlanType"] = data.healthInsurancePlanType.GetLocalization();
		if (!string.IsNullOrEmpty(data.investmentFundName))
		{
			dictionary["investmentFundName"] = data.investmentFundName.GetLocalization();
		}
		dictionary["marketingTypeName"] = data.marketingTypeName.GetLocalization();
		return dictionary;
	}

	public static List<string> GetTransactionCategories(string transactionType)
	{
		List<string> list = new List<string>();
		switch (transactionType)
		{
		case "ba:transaction_wage":
		case "ba:transaction_replacementwage":
		case "ba:transaction_unassignedwage":
			list.Add("ba:transactioncategory_salaryexpenses");
			break;
		}
		if (transactionType == "ba:transaction_rent")
		{
			list.Add("ba:transactioncategory_rent");
		}
		if (transactionType == "ba:transaction_marketing")
		{
			list.Add("ba:transactioncategory_marketing");
		}
		if (transactionType == "ba:transaction_licensingfee")
		{
			list.Add("ba:transactioncategory_licensingfees");
		}
		if (transactionType == "ba:transaction_loanpayment")
		{
			list.Add("ba:transactioncategory_loanexpenses");
		}
		if (transactionType == "ba:transaction_healthinsurance")
		{
			list.Add("ba:transactioncategory_healthinsuranceexpenses");
		}
		if (transactionType == "ba:transaction_replacementfee")
		{
			list.Add("ba:transactioncategory_headhunterreplacementfees");
		}
		if (transactionType == "ba:transaction_banknegativeinterestrate")
		{
			list.Add("ba:transactioncategory_negativeinterestrates");
		}
		if (transactionType == "ba:transaction_publicparking" || transactionType == "ba:transaction_parkingticket")
		{
			list.Add("ba:transactioncategory_parkingfees");
		}
		if (transactionType == "ba:transaction_playerjobsalary" || transactionType == "ba:transaction_deliveryjobwage")
		{
			list.Add("ba:transactioncategory_salaryincome");
		}
		if (transactionType == "ba:transaction_casino")
		{
			list.Add("ba:transactioncategory_casino");
		}
		return list;
	}

	public static (bool, string) IsTaxDeductible(string transactionType, Dictionary<string, string> data)
	{
		if (transactionType == "ba:transaction_vehiclebought")
		{
			return (true, "tax_vehicle");
		}
		if (transactionType == "ba:transaction_tuitionfee")
		{
			return (true, "ba:businesstype_school");
		}
		return (false, string.Empty);
	}
}
