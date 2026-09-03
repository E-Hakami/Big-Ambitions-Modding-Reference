using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions/Casino")]
public class IsLastCasinoRandomAction : Conditional
{
	[SharedRequired]
	public SharedCasinoCustomer sharedCasinoCustomer;

	public CasinoRandomAction casinoRandomAction;

	public override TaskStatus OnUpdate()
	{
		if (sharedCasinoCustomer.Value.lastAction != casinoRandomAction)
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
