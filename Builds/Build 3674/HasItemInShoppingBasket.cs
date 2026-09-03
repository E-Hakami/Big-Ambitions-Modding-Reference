using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Items;
using Entities;

[TaskCategory("Big Ambitions")]
public class HasItemInShoppingBasket : Conditional
{
	public SharedOrder order;

	public override TaskStatus OnUpdate()
	{
		if (!order.Value.entries.Exists((OrderEntry x) => x.available && x.priceAccceptable && !IsServiceProduct(x.itemName)))
		{
			return TaskStatus.Failure;
		}
		return TaskStatus.Success;
	}

	private static bool IsServiceProduct(string itemName)
	{
		Item byName = ItemsGetter.GetByName(itemName);
		if (byName == null)
		{
			return false;
		}
		return (byName.type & ItemType.ServiceProduct) != 0;
	}
}
