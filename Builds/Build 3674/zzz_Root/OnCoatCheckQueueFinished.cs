using System.Linq;
using BehaviorDesigner.Runtime.Tasks;
using Entities;

[TaskCategory("Big Ambitions/Nightclub")]
public class OnCoatCheckQueueFinished : Action
{
	[RequiredField]
	public SharedNightclubCustomer sharedNightclubCustomer;

	public override void OnStart()
	{
		NightclubCustomer value = sharedNightclubCustomer.Value;
		value.order.entries.First((OrderEntry x) => x.itemName == "ba:itemname_coatcheckfee").processed = true;
		if (value.isHoldingACoat)
		{
			value.RemoveCoat();
		}
	}
}
