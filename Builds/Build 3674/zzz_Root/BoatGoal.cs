using System.Linq;
using Boats;
using Entities;
using Extensions;
using Localizor.LanguageChangeEvent;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/BoatGoal")]
public class BoatGoal : IntBaseGoal
{
	public float minimumPrice;

	protected override int GetValue()
	{
		return SaveGameManager.Current.playerBoats.Count((BoatData x) => (float)x.type.GetBoatType().price >= minimumPrice);
	}

	public override LanguageChangeEventDataHolder GetTitle()
	{
		LanguageChangeEventDataHolder result = base.GetTitle();
		result.Arguments = new
		{
			price = minimumPrice.ToShortCurrencyFormat(),
			amount = amount
		};
		return result;
	}
}
