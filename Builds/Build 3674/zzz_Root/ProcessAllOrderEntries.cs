using BehaviorDesigner.Runtime.Tasks;
using Entities;

[TaskCategory("Big Ambitions/Order")]
public class ProcessAllOrderEntries : Action
{
	[SharedRequired]
	public SharedCustomer sharedCustomer;

	public bool process = true;

	public override void OnStart()
	{
		foreach (OrderEntry entry in sharedCustomer.Value.order.entries)
		{
			entry.processed = process;
		}
	}
}
