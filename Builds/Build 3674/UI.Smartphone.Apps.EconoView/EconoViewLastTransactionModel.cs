using Localizor.LanguageChangeEvent;

namespace UI.Smartphone.Apps.EconoView;

public class EconoViewLastTransactionModel
{
	public readonly float amount;

	public readonly int day;

	public readonly LanguageChangeEventDataHolder labelData;

	public EconoViewLastTransactionModel(LanguageChangeEventDataHolder labelData, int day, float amount)
	{
		this.labelData = labelData;
		this.day = day;
		this.amount = amount;
	}
}
