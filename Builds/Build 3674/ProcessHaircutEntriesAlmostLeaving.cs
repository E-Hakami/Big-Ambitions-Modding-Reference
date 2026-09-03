using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using EmployeeStations;
using Entities;
using Extensions;

[TaskCategory("Big Ambitions/Hairdresser")]
public class ProcessHaircutEntriesAlmostLeaving : Action
{
	[RequiredField]
	public SharedHairdresserCustomer sharedHairdresserCustomer;

	private bool atLeastOneEntryPaid;

	public override void OnStart()
	{
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			return;
		}
		atLeastOneEntryPaid = false;
		WaitingLine random = WaitingLinesHelper.GetAvailableWaitingLines(ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.ishairdresserchair)).GetRandom();
		if (!random)
		{
			return;
		}
		EmployeeStationController employeeStationController = random.EmployeeStationController;
		foreach (OrderEntry entry in sharedHairdresserCustomer.Value.order.entries)
		{
			if (IsHaircutEntry(entry))
			{
				ItemController itemController = InstanceBehavior<BuildingManager>.Instance.FindOptimalItemController("ba:itemname_haircareproduct");
				if (itemController == null)
				{
					SetAllHaircutEntriesAsProcessed();
					break;
				}
				ProcessOrderEntry(itemController, entry);
			}
		}
		if (atLeastOneEntryPaid)
		{
			SetCustomerService(employeeStationController);
		}
	}

	private static bool IsHaircutEntry(OrderEntry orderEntry)
	{
		string itemName = orderEntry.itemName;
		return itemName == "ba:itemname_hairchemicalfee" || itemName == "ba:itemname_haircuttingfee" || itemName == "ba:itemname_hairstylingfee";
	}

	private void SetAllHaircutEntriesAsProcessed()
	{
		foreach (OrderEntry entry in sharedHairdresserCustomer.Value.order.entries)
		{
			if (IsHaircutEntry(entry))
			{
				entry.processed = true;
			}
		}
	}

	private void ProcessOrderEntry(ItemController hairCareProductProducer, OrderEntry orderEntry)
	{
		orderEntry.processed = true;
		orderEntry.available = true;
		orderEntry.price = ItemHelper.GetPrice(orderEntry.itemName, InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
		if (IsPriceAcceptable(orderEntry))
		{
			hairCareProductProducer.ItemInstance.SubtractFromStock();
			orderEntry.paid = true;
			atLeastOneEntryPaid = true;
		}
		else
		{
			orderEntry.priceAccceptable = false;
		}
	}

	private bool IsPriceAcceptable(OrderEntry orderEntry)
	{
		float price = ItemHelper.GetPrice(orderEntry.itemName, InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
		return sharedHairdresserCustomer.Value.citizenData.IsPriceAcceptable(orderEntry.itemName, price);
	}

	private void SetCustomerService(EmployeeStationController employeeStation)
	{
		EmployeeInstance employeeInstance = employeeStation.employee.employeeInstance;
		sharedHairdresserCustomer.Value.order.customerServiceSkill = employeeInstance.GetSkillValue("ba:skill_hairstylist") * (employeeInstance.satisfaction / 100f);
	}
}
