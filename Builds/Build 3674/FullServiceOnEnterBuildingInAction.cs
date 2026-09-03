using System.Linq;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Entities;

[TaskCategory("Big Ambitions/FullServiceCustomer")]
public class FullServiceOnEnterBuildingInAction : Action
{
	[RequiredField]
	public SharedCustomer sharedCustomer;

	public override void OnStart()
	{
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			ProcessOrderForCompetitorBusiness();
			return;
		}
		if (AreThereCashRegisters())
		{
			ProcessOrderForPlayerBusiness();
		}
		if (AnyEntryPaid())
		{
			ItemController itemController = InstanceBehavior<BuildingManager>.Instance.FindOptimalItemController("ba:itemname_paperbag");
			if (!(itemController != null) || !itemController.ItemInstance.SubtractFromStock())
			{
				UnPayAllOrderEntries();
				sharedCustomer.Value.Leave();
			}
		}
		else
		{
			SetAllOrderEntriesAsProcessed();
			sharedCustomer.Value.Leave();
		}
	}

	private void UnPayAllOrderEntries()
	{
		foreach (OrderEntry entry in sharedCustomer.Value.order.entries)
		{
			entry.paid = false;
		}
	}

	private void SetAllOrderEntriesAsProcessed()
	{
		foreach (OrderEntry entry in sharedCustomer.Value.order.entries)
		{
			entry.processed = true;
		}
	}

	private bool AnyEntryPaid()
	{
		return sharedCustomer.Value.order.entries.Exists((OrderEntry x) => x.paid);
	}

	private static bool AreThereCashRegisters()
	{
		return WaitingLinesHelper.GetAvailableWaitingLines(ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.isfullservicecashregister)).Any();
	}

	private void ProcessOrderForPlayerBusiness()
	{
		foreach (OrderEntry entry in sharedCustomer.Value.order.entries)
		{
			entry.processed = true;
			ItemController itemController = InstanceBehavior<BuildingManager>.Instance.FindOptimalItemController(entry.itemName);
			if (!(itemController == null))
			{
				OrderHelper.Validate(sharedCustomer.Value.citizenData, entry, itemController, payIfAcceptable: true);
				if (entry.paid)
				{
					itemController.ItemInstance.SubtractFromStock();
				}
			}
		}
	}

	private void ProcessOrderForCompetitorBusiness()
	{
		foreach (OrderEntry entry in sharedCustomer.Value.order.entries)
		{
			entry.processed = true;
			entry.available = true;
			entry.priceAccceptable = true;
			entry.paid = true;
		}
	}
}
