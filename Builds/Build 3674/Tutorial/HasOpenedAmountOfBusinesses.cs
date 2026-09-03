using HGAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasOpenedAmountOfBusinesses")]
public class HasOpenedAmountOfBusinesses : QuestRequirement
{
	[SerializeField]
	[AutocompleteDropdown("BuildingTypes")]
	private string buildingType;

	[SerializeField]
	private int amountRequired;

	public override bool CheckIfCompleted()
	{
		int num = 0;
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && !(buildingRegistration.BuildingCached.BuildingType != buildingType) && !buildingRegistration.temporarilyClosed)
			{
				num++;
				if (num >= amountRequired)
				{
					return true;
				}
			}
		}
		return false;
	}
}
