using System.Collections.Generic;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using Entities;

[TaskCategory("Big Ambitions/Order")]
public class SelectOrderEntry : Action
{
	[SharedRequired]
	public SharedOrderEntry orderEntry;

	[SharedRequired]
	public SharedCustomer sharedCustomer;

	[SharedRequired]
	public SharedOrder order;

	[SharedRequired]
	public SharedInt orderIndex;

	public override void OnStart()
	{
		int value = orderIndex.Value;
		List<OrderEntry> list = ((sharedCustomer?.Value == null) ? order.Value.entries : sharedCustomer.Value.order.entries);
		if (value < list.Count)
		{
			orderEntry.Value = list[value];
		}
	}
}
