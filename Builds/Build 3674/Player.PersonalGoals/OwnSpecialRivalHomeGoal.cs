using Buildings;
using Entities;
using Helpers;
using UnityEngine;

namespace Player.PersonalGoals;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/OwnSpecialRivalHomeGoal")]
public class OwnSpecialRivalHomeGoal : GenericPersonalGoal
{
	protected override bool CheckIfCompleted()
	{
		foreach (RealEstate item in SaveGameManager.Current.realEstate)
		{
			if (item.BuildingRegistration.BuildingOwnedByPlayer)
			{
				Building building = BuildingHelper.GetBuilding(item.address);
				if (building != null && BuildingHelper.IsHamptonsBuildingOwnedByRival(building))
				{
					return true;
				}
			}
		}
		return false;
	}
}
