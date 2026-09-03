using Extensions;
using Localizor.LanguageChangeEvent;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/InteriorDesigner Paid Goal")]
public class InteriorDesignerPaidGoal : FloatBaseGoal
{
	protected override float GetValue()
	{
		return Mathf.Max(0f, 0f - SaveGameManager.Current.achievementsData.totalInteriorDesignerCost);
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
