using System;

public class FullServiceCustomer : Customer
{
	public override void Init()
	{
		base.Init();
		if (CompareTag("Player"))
		{
			state = CustomerState.InWaitingLine;
			return;
		}
		behaviorTree.EnableBehavior();
		if (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(base.OnExitBuilding));
		}
	}

	public override void Leave()
	{
		ForceFinishOrder();
		base.Leave();
	}

	public override void SetCurrentTimeState()
	{
		if (customerEntry.spawnTime.GetTotalMinutes() + 20f <= TimeHelper.NowInMinutes())
		{
			customerTimeState = CustomerTimeState.AlreadyInAction;
		}
		else if (customerEntry.spawnTime.GetTotalMinutes() + 5f <= TimeHelper.NowInMinutes())
		{
			customerTimeState = CustomerTimeState.RecentlyArrived;
		}
		else
		{
			customerTimeState = CustomerTimeState.JustArrived;
		}
	}

	protected override void ReleaseGameObject()
	{
		InstanceBehavior<BuildingManager>.Instance.customerSpawner.ReleaseCustomer(this, CustomerType.FullService);
	}
}
