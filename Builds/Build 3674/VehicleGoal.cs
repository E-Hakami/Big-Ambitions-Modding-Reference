using Extensions;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using Vehicles.VehicleTypes;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/VehicleGoal")]
public class VehicleGoal : IntBaseGoal
{
	[SerializeField]
	private float minimumPrice;

	[SerializeField]
	private bool sortByPrice;

	public override float GetSortValue()
	{
		if (!sortByPrice)
		{
			return amount;
		}
		return minimumPrice;
	}

	protected override int GetValue()
	{
		return SaveGameManager.Current.VehicleInstances.CountWhere(delegate(VehicleInstance x)
		{
			VehicleType vehicleType = x.VehicleType;
			if (vehicleType == null)
			{
				return false;
			}
			return vehicleType.countsForPersonalGoals && vehicleType.price >= minimumPrice;
		});
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
