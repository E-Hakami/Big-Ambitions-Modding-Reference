using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using BigAmbitions.DayNightCycle;
using BigAmbitions.SaveSystem.Legacy.CompatParsers;
using Entities;
using Helpers;
using Localizor.LanguageChangeEvent;

[Serializable]
public class Transaction : IDeserializationCallback
{
	public enum AmountOption
	{
		All,
		Positive,
		Negative
	}

	[Serializable]
	[Obsolete]
	public struct DataHolder
	{
		public string businessName;

		public Address address;

		public string itemName;

		public MarketingTypeName marketingTypeName;

		public string warehouseName;

		public float value;

		public string employee;

		public string vehicleName;

		public DiplomaName diplomaName;

		public string skillName;

		public string investmentFundName;

		public string element;

		public BoatTypeName boatTypeName;

		public float secondValue;

		public HealthInsurancePlanType healthInsurancePlanType;

		public string cargoName;

		public LanguageChangeEventDataHolder GetItemLabel(bool useQuantity = true)
		{
			return LocalizationHelper.GetItemLabel(itemName, (!useQuantity) ? 1 : ((int)value));
		}

		public bool Equals(DataHolder other)
		{
			if (businessName == other.businessName && object.Equals(address, other.address) && itemName == other.itemName && EqualityComparer<MarketingTypeName>.Default.Equals(marketingTypeName, other.marketingTypeName) && warehouseName == other.warehouseName && value.Equals(other.value) && employee == other.employee && vehicleName == other.vehicleName && EqualityComparer<DiplomaName>.Default.Equals(diplomaName, other.diplomaName) && skillName == other.skillName && string.Equals(investmentFundName, other.investmentFundName) && element == other.element && EqualityComparer<BoatTypeName>.Default.Equals(boatTypeName, other.boatTypeName) && secondValue.Equals(other.secondValue) && EqualityComparer<HealthInsurancePlanType>.Default.Equals(healthInsurancePlanType, other.healthInsurancePlanType))
			{
				return cargoName == other.cargoName;
			}
			return false;
		}
	}

	public string transactionType;

	public List<string> transactionCategories;

	public float amount;

	public Timestamp timestamp;

	public Address address;

	public float balance;

	public bool isTaxDeductible;

	[Obsolete("Since EA 0.11")]
	public DataHolder data;

	public Dictionary<string, string> transactionData;

	public Transaction(TransactionInfo info)
	{
		if (SaveGameManager.Current != null)
		{
			timestamp = TimeHelper.Now();
		}
		transactionType = info.Type;
		transactionData = info.Data;
		transactionCategories = info.Categories;
		isTaxDeductible = info.IsTaxDeductible;
	}

	public void OnDeserialization(object sender)
	{
		if (transactionData == null)
		{
			transactionData = new Dictionary<string, string>();
		}
		bool num = !data.Equals(default(DataHolder));
		if (num && transactionData.Count == 0)
		{
			transactionData = TransactionDataParser.ParseData(data);
		}
		if (num && transactionData.Count > 0)
		{
			data = default(DataHolder);
		}
		List<string> list = transactionCategories;
		if (list == null || list.Count <= 0)
		{
			transactionCategories = TransactionDataParser.GetTransactionCategories(transactionType);
			(bool, string) tuple = TransactionDataParser.IsTaxDeductible(transactionType, transactionData);
			bool item = tuple.Item1;
			string item2 = tuple.Item2;
			isTaxDeductible = item;
			if (isTaxDeductible && !string.IsNullOrEmpty(item2))
			{
				transactionData.TryAdd("taxDeductibleName", item2);
			}
		}
	}
}
