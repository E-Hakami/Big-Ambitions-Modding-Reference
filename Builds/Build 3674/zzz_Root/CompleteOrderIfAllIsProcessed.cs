using BehaviorDesigner.Runtime.Tasks;
using Entities;

[TaskCategory("Big Ambitions/Order")]
public class CompleteOrderIfAllIsProcessed : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	public override void OnStart()
	{
		foreach (OrderEntry entry in sharedCustomer.Value.order.entries)
		{
			if (!entry.processed)
			{
				return;
			}
		}
		sharedCustomer.Value.CompleteOrder();
	}
}
