using System.Linq;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Targets/TakenOverBusinessTarget")]
public class TakenOverBusinessTarget : QuestEntryTarget
{
	public override Address GetAddress()
	{
		return GetBuildingRegistration()?.Address;
	}

	public BuildingRegistration GetBuildingRegistration()
	{
		return SaveGameManager.Current.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.RentedByPlayer && x.takenOver);
	}
}
