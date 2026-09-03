using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions")]
public class SetCustomerTimeState : Action
{
	[SharedRequired]
	public SharedCustomer sharedCustomer;

	public CustomerTimeState timeState;

	public override void OnStart()
	{
		sharedCustomer.Value.customerTimeState = timeState;
	}
}
