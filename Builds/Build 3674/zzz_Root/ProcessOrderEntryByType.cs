using System.Linq;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Items;
using Entities;
using JimmysUnityUtilities;

[TaskCategory("Big Ambitions/Order")]
public class ProcessOrderEntryByType : Action
{
	[SharedRequired]
	public SharedCustomer sharedCustomer;

	public ItemType itemTypes;

	public override void OnStart()
	{
		sharedCustomer.Value.order.entries.Where((OrderEntry x) => (ItemsGetter.GetByName(x.itemName).type & itemTypes) != 0).ForEach(delegate(OrderEntry x)
		{
			x.processed = true;
		});
	}
}
