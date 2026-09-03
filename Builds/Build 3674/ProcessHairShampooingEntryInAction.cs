using System.Linq;
using BehaviorDesigner.Runtime.Tasks;
using EmployeeStations;
using Entities;
using Extensions;

[TaskCategory("Big Ambitions/Hairdresser")]
public class ProcessHairShampooingEntryInAction : Action
{
	private static readonly string[] HairdresserHeadWashChairName = new string[1] { "ba:itemname_hairdresserheadwash" };

	[RequiredField]
	public SharedCustomer sharedCustomer;

	public override void OnStart()
	{
		if (!InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness)
		{
			return;
		}
		OrderEntry orderEntry = sharedCustomer.Value.order.entries.FirstOrDefault((OrderEntry x) => x.itemName == "ba:itemname_hairshampooingfee");
		if (orderEntry == null)
		{
			return;
		}
		orderEntry.processed = true;
		WaitingLine random = WaitingLinesHelper.GetAvailableWaitingLines(HairdresserHeadWashChairName).GetRandom();
		if ((bool)random)
		{
			EmployeeStationController employeeStationController = random.EmployeeStationController;
			ItemController itemController = InstanceBehavior<BuildingManager>.Instance.FindOptimalItemController("ba:itemname_haircareproduct");
			if (!(itemController == null))
			{
				ProcessOrderEntry(itemController, orderEntry, employeeStationController);
			}
		}
	}

	private void ProcessOrderEntry(ItemController hairCareProductProducer, OrderEntry orderEntry, EmployeeStationController employeeStation)
	{
		orderEntry.available = true;
		orderEntry.price = ItemHelper.GetPrice(orderEntry.itemName, InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
		if (!IsPriceAcceptable(orderEntry))
		{
			orderEntry.priceAccceptable = false;
			return;
		}
		hairCareProductProducer.ItemInstance.SubtractFromStock();
		orderEntry.paid = true;
		SetCustomerService(employeeStation);
	}

	private bool IsPriceAcceptable(OrderEntry orderEntry)
	{
		float price = ItemHelper.GetPrice(orderEntry.itemName, InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
		return sharedCustomer.Value.citizenData.IsPriceAcceptable(orderEntry.itemName, price);
	}

	private void SetCustomerService(EmployeeStationController employeeStation)
	{
		EmployeeInstance employeeInstance = employeeStation.employee.employeeInstance;
		sharedCustomer.Value.order.customerServiceSkill = employeeInstance.GetSkillValue("ba:skill_hairstylist") * (employeeInstance.satisfaction / 100f);
	}
}
