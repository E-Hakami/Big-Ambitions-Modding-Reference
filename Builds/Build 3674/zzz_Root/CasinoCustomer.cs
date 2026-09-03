public class CasinoCustomer : Customer
{
	public CasinoRandomAction lastAction;

	public override void Init()
	{
		base.Init();
		behaviorTree.EnableBehavior();
	}

	protected override void ReleaseGameObject()
	{
		InstanceBehavior<BuildingManager>.Instance.customerSpawner.ReleaseCustomer(this, CustomerType.Casino);
	}
}
