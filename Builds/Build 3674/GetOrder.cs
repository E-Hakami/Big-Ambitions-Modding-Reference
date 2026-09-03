using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions/Order")]
public class GetOrder : Action
{
	[SharedRequired]
	public SharedOrder order;

	public override void OnStart()
	{
		order.Value = GetComponent<Customer>().order;
	}
}
