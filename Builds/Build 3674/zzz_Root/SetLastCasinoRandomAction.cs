using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions/Casino")]
public class SetLastCasinoRandomAction : Action
{
	[SharedRequired]
	public SharedCasinoCustomer sharedCasinoCustomer;

	public CasinoRandomAction casinoRandomAction;

	public override void OnStart()
	{
		sharedCasinoCustomer.Value.lastAction = casinoRandomAction;
	}
}
