using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Items;
using Entities;

[TaskCategory("Big Ambitions")]
public class AnyQuantityItemEntry : Conditional
{
	[SharedRequired]
	public SharedCustomer sharedCustomer;

	public override TaskStatus OnUpdate()
	{
		if (!sharedCustomer.Value.order.entries.Exists((OrderEntry x) => (ItemsGetter.GetByName(x.itemName).type & ItemType.RetailProduct) != 0))
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}
}
