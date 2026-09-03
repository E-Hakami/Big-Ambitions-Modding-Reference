using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Employees/HasUniformAssignedToStore")]
public class HasUniformAssignedToStore : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && buildingRegistration.uniformsBySkill.Keys.Count > 0)
			{
				return true;
			}
		}
		return false;
	}
}
