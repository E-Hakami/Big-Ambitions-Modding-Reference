using Extensions;
using Localizor.LanguageChangeEvent;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/Taxes Paid Goal")]
public class TaxesPaidGoal : FloatBaseGoal
{
	protected override float GetValue()
	{
		return SaveGameManager.Current.achievementsData.taxesPaid;
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
