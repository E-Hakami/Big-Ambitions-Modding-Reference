using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Tags;
using Entities;
using Extensions;

public class SelfServiceCustomer : Customer
{
	public override void Init()
	{
		base.Init();
		order.entries = (InstanceBehavior<BuildingManager>.Instance.businessType.HasTag(TagRef.Businesstag.customersneedshoppingcontainer) ? order.entries.Shuffle().ToList() : new List<OrderEntry> { order.entries.GetRandom() });
		behaviorTree.EnableBehavior();
	}

	public override void SetCurrentTimeState()
	{
		if (customerEntry.spawnTime.GetTotalMinutes() + 40f <= TimeHelper.NowInMinutes())
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
		InstanceBehavior<BuildingManager>.Instance.customerSpawner.ReleaseCustomer(this, CustomerType.SelfService);
	}
}
