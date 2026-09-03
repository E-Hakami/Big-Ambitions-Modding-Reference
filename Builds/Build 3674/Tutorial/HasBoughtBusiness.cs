using System.Linq;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasBoughtBusiness")]
public class HasBoughtBusiness : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		return SaveGameManager.Current.BuildingRegistrations.Any((BuildingRegistration x) => x.RentedByPlayer && x.takenOver);
	}
}
