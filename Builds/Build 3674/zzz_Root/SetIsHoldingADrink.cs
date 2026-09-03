using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class SetIsHoldingADrink : Action
{
	[SharedRequired]
	public SharedCustomer sharedCustomer;

	[SharedRequired]
	public SharedBool isHoldingADrink;

	public override void OnStart()
	{
		isHoldingADrink.Value = sharedCustomer.Value.isHoldingADrink;
	}
}
