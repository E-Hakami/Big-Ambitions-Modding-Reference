using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/ExitCondition/OwnsAHamptonsHouse")]
public class OwnsAHamptonsHouseExitCondition : ExitCondition
{
	private const string BlockedNotificationKeyValue = "exitzonedespawner_notification_must_own_hamptons_house";

	public override string BlockedNotificationKey => "exitzonedespawner_notification_must_own_hamptons_house";

	public override bool CanExit()
	{
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (PlayerOwnsHamptonsHouse(buildingRegistration))
			{
				return true;
			}
		}
		return false;
	}

	private static bool PlayerOwnsHamptonsHouse(BuildingRegistration registration)
	{
		if (registration.BuildingCached.IsHamptonsHouse())
		{
			if (!registration.RentedByPlayer)
			{
				return registration.BuildingOwnedByPlayer;
			}
			return true;
		}
		return false;
	}
}
