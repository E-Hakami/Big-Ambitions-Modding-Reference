using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions/Nightclub")]
public class RemoveDrink : Action
{
	[SharedRequired]
	public SharedCustomer sharedCustomer;

	public override void OnStart()
	{
		sharedCustomer.Value.RemoveDrink();
	}
}
