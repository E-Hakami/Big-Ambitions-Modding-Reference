using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

public class GetOrderEntriesCount : Action
{
	[SharedRequired]
	public SharedCustomer sharedCustomer;

	[SharedRequired]
	public SharedOrder order;

	[SharedRequired]
	public SharedInt orderEntriesCount;

	public override void OnStart()
	{
		orderEntriesCount.Value = ((sharedCustomer?.Value == null) ? order.Value.entries.Count : sharedCustomer.Value.order.entries.Count);
	}
}
