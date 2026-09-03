using Character;
using Helpers;
using UI;

public class DanceFloorController : ItemController
{
	public override bool Interact()
	{
		if (!CanDance())
		{
			return base.Interact();
		}
		if (CanBeInteractedFromCurrentPosition())
		{
			InstanceBehavior<UIs>.Instance.topBar.playerDancesUI.OnDanceButtonClick();
			return true;
		}
		MoveTowardsEntity(delegate
		{
			InstanceBehavior<UIs>.Instance.topBar.playerDancesUI.OnDanceButtonClick();
		});
		return true;
	}

	public static bool CanDance()
	{
		if (BuildingManager.IsInsideBuilding && InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName == "ba:businesstype_nightclub" && !PlayerHelper.IsHoldingItem && !PlayerHelper.IsUsingVehicle)
		{
			return PlayerDances.IsEnabled;
		}
		return false;
	}
}
