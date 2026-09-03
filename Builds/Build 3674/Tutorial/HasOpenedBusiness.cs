using System.Linq;
using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Businesses/HasOpenedBusiness")]
public class HasOpenedBusiness : QuestRequirement
{
	[SerializeField]
	private CustomBuildingTarget playerStoreTarget;

	public override bool CheckIfCompleted()
	{
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(playerStoreTarget.GetAddress());
		if (buildingRegistration == null)
		{
			return false;
		}
		if (!buildingRegistration.temporarilyClosed)
		{
			return buildingRegistration.scheduleDays.Any((ScheduleDay scheduleDay) => scheduleDay.isOpen);
		}
		return false;
	}
}
