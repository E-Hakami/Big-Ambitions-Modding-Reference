using Helpers;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/ExitCondition/BusinessIsOpen")]
public class BusinessIsOpenExitCondition : ExitCondition
{
	[SerializeField]
	private string blockedNotificationKeyValue;

	public override string BlockedNotificationKey => blockedNotificationKeyValue;

	public override bool CanExit()
	{
		return BusinessHelper.IsBusinessOpen(InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
	}
}
