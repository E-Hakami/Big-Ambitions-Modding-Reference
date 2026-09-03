using BehaviorDesigner.Runtime.Tasks;
using Entities;

[TaskCategory("Big Ambitions/Nightclub")]
public class AreThereOrderEntriesRemaining : Conditional
{
	[SharedRequired]
	public SharedCustomer sharedCustomer;

	public override TaskStatus OnUpdate()
	{
		if (!sharedCustomer.Value.order.completed && !AreAllEntriesProcessed())
		{
			return TaskStatus.Success;
		}
		return TaskStatus.Failure;
	}

	private bool AreAllEntriesProcessed()
	{
		foreach (OrderEntry entry in sharedCustomer.Value.order.entries)
		{
			if (!entry.processed)
			{
				return false;
			}
		}
		return true;
	}
}
