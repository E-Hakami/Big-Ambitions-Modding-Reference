using BehaviorDesigner.Runtime.Tasks;
using Entities;

[TaskCategory("Big Ambitions")]
public class AnyEntryPaid : Conditional
{
	[SharedRequired]
	public SharedCustomer sharedCustomer;

	public override TaskStatus OnUpdate()
	{
		if (!sharedCustomer.Value.order.entries.Exists((OrderEntry x) => x.paid))
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
