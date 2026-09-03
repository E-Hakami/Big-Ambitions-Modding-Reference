using Localizor.LanguageChangeEvent;

namespace UI.Smartphone.Apps.EconoView;

public class TransactionModel
{
	public string Id;

	public LanguageChangeEventDataHolder LabelData;

	public int Day;

	public string Type;

	public float Amount;

	public float Balance;

	public string Label => LabelData.ToString();

	public TransactionModel(LanguageChangeEventDataHolder labelData, int day, string type, float amount, float balance)
	{
		LabelData = labelData;
		Day = day;
		Type = type;
		Amount = amount;
		Balance = balance;
	}
}
