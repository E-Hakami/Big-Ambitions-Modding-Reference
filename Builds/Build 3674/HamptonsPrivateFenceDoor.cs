using UI.Notification;

public class HamptonsPrivateFenceDoor : FenceDoor
{
	private const string HamptonsPrivateZoneLockedMessageKey = "hamptons_private_zone_locked_message";

	public int privateFenceIndex;

	protected override bool IsLocked()
	{
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (PlayerOwnsABuildingInsideGates(buildingRegistration))
			{
				return false;
			}
		}
		return true;
	}

	private bool PlayerOwnsABuildingInsideGates(BuildingRegistration x)
	{
		if ((x.RentedByPlayer || x.BuildingOwnedByPlayer) && x.BuildingCached.unlocksHamptonsPrivateFence)
		{
			return x.BuildingCached.privateFenceIndex == privateFenceIndex;
		}
		return false;
	}

	protected override void ShowLockedMessage()
	{
		Notifications.ShowError("hamptons_private_zone_locked_message", "hamptons_private_zone_locked_message");
	}

	protected override void SetItemControllerReference()
	{
	}

	protected override void SubscribeToEvents()
	{
	}

	protected override void UnsubscribeFromEvents()
	{
	}
}
