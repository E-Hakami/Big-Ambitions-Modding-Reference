using BehaviorDesigner.Runtime.Tasks;
using Entities;

[TaskCategory("Big Ambitions/Order")]
public class UnpayOrderEntries : Action
{
	[SharedRequired]
	public SharedCustomer sharedCustomer;

	public override void OnStart()
	{
		sharedCustomer.Value.order.entries.ForEach(delegate(OrderEntry x)
		{
			x.paid = false;
		});
	}
}
