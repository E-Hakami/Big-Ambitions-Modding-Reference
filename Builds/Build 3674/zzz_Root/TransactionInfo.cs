using System.Collections.Generic;

public class TransactionInfo
{
	public string Type { get; private set; }

	public List<string> Categories { get; private set; }

	public Dictionary<string, string> Data { get; private set; }

	public bool IsTaxDeductible { get; private set; }

	public TransactionInfo(string type, string category, bool isTaxDeductible = false)
	{
		Type = type;
		Categories = new List<string>(1) { category };
		IsTaxDeductible = isTaxDeductible;
	}

	public TransactionInfo(string type, string category, Dictionary<string, string> data, bool isTaxDeductible = false)
	{
		Type = type;
		Categories = new List<string>(1) { category };
		Data = data;
		IsTaxDeductible = isTaxDeductible;
	}

	public TransactionInfo(string type, Dictionary<string, string> data, bool isTaxDeductible = false)
	{
		Type = type;
		Data = data;
		IsTaxDeductible = isTaxDeductible;
	}

	public TransactionInfo(string type, bool isTaxDeductible = false)
	{
		Type = type;
		IsTaxDeductible = isTaxDeductible;
	}

	public void SetTaxDeductibleName(string taxDeductibleName)
	{
		if (!string.IsNullOrEmpty(taxDeductibleName))
		{
			if (Data == null)
			{
				Dictionary<string, string> dictionary = (Data = new Dictionary<string, string>());
			}
			Data["taxDeductibleName"] = taxDeductibleName;
			IsTaxDeductible = true;
		}
	}
}
