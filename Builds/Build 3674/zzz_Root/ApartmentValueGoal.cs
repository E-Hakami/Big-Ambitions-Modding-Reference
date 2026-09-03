using System.Collections.Generic;
using BigAmbitions.Items;
using Extensions;
using Localizor.LanguageChangeEvent;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/ApartmentValueGoal")]
public class ApartmentValueGoal : FloatBaseGoal
{
	protected override float GetValue()
	{
		List<BuildingRegistration> buildingRegistrations = SaveGameManager.Current.BuildingRegistrations;
		float num = 0f;
		for (int i = 0; i < buildingRegistrations.Count; i++)
		{
			BuildingRegistration buildingRegistration = buildingRegistrations[i];
			if (!buildingRegistration.RentedByPlayer || buildingRegistration.GetBuildingType() != "ba:buildingtype_residential")
			{
				continue;
			}
			Dictionary<string, ItemInstance> itemInstances = buildingRegistration.itemInstances;
			if (itemInstances != null)
			{
				float num2 = itemInstances.Values.SumValues((ItemInstance x) => x.priceOnPurchase);
				if (num2 > num)
				{
					num = num2;
				}
			}
		}
		return num;
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
