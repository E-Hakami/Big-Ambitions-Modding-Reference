using BigAmbitions.Items;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Factories/HasOneProductionLine")]
public class HasOneProductionLine : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!buildingRegistration.RentedByPlayer || buildingRegistration.businessTypeName != "ba:businesstype_factory")
			{
				continue;
			}
			foreach (ItemInstance value in buildingRegistration.itemInstances.Values)
			{
				if (value is FactoryWorkstationInstance factoryWorkstationInstance && factoryWorkstationInstance.IsWorkstationValid())
				{
					return true;
				}
			}
		}
		return false;
	}
}
