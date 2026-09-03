using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/ValuationGoal")]
public class ValuationGoal : FloatBaseGoal
{
	protected override float GetValue()
	{
		return PlayerHelper.GetPersonalWealth().CurrentWealth;
	}

	public override LanguageChangeEventDataHolder GetTitle()
	{
		LanguageChangeEventDataHolder result = base.GetTitle();
		result.Arguments = new
		{
			valuation = amount.ToShortCurrencyFormat()
		};
		return result;
	}

	protected override object FormatProgressValue(float value)
	{
		return value.ToShortCurrencyFormat();
	}
}
