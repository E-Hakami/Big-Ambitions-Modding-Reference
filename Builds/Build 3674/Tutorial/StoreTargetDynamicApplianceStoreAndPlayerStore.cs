using System;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Targets/DynamicApplianceStoreAndPlayerStore")]
public class StoreTargetDynamicApplianceStoreAndPlayerStore : QuestEntryTarget
{
	[SerializeField]
	private QuestRequirement requirementToSwitchToPlayerStore;

	[SerializeField]
	private CustomBuildingTarget playerStoreTarget;

	[SerializeField]
	private QuestEntryTarget applianceStoreTarget;

	protected override void OnInit()
	{
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnTargetUpdated));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(OnTargetUpdated));
	}

	protected override void OnDispose()
	{
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnTargetUpdated));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Remove(GlobalEvents.onExitBuilding, new Action<Address>(OnTargetUpdated));
	}

	private void OnTargetUpdated(Address _)
	{
		SetTarget();
	}

	public override Address GetAddress()
	{
		if (requirementToSwitchToPlayerStore.CheckIfCompleted())
		{
			return playerStoreTarget.GetAddress();
		}
		return applianceStoreTarget.GetAddress();
	}
}
