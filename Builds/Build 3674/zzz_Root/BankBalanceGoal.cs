using Extensions;
using Localizor.LanguageChangeEvent;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/BankBalanceGoal")]
public class BankBalanceGoal : FloatBaseGoal
{
	protected override float GetValue()
	{
		return SaveGameManager.Current.Money;
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
