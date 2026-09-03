using System.Linq;
using BehaviorDesigner.Runtime.Tasks;
using BigAmbitions.Items;
using BigAmbitions.Tags;
using Controllers;
using EmployeeStations;
using Entities;
using Extensions;

[TaskCategory("Big Ambitions/Nightclub")]
public class ProcessCoatCheckEntryInAction : Action
{
	[SharedRequired]
	public SharedNightclubCustomer sharedNightclubCustomer;

	private static string[] CoatCheckStationNames => ItemsGetter.GetAllItemNamesByTag(TagRef.Itemtag.iscoatcheck);

	public override void OnStart()
	{
		WaitingLine random = WaitingLinesHelper.GetAvailableWaitingLines(CoatCheckStationNames).GetRandom();
		if ((bool)random)
		{
			EmployeeStationController employeeStationController = random.EmployeeStationController;
			Order order = sharedNightclubCustomer.Value.order;
			OrderEntry orderEntry = order.entries.First((OrderEntry x) => x.itemName == "ba:itemname_coatcheckfee");
			orderEntry.available = true;
			PayOrderEntryIfAcceptable(orderEntry);
			EmployeeInstance employeeInstance = employeeStationController.employee.employeeInstance;
			order.customerServiceSkill = employeeInstance.GetSkillValue("ba:skill_customerservice") * (employeeInstance.satisfaction / 100f);
			sharedNightclubCustomer.Value.coatCheckController = (CoatCheckController)employeeStationController;
			sharedNightclubCustomer.Value.coatCheckController.IncreaseStoredCoats();
		}
	}

	private void PayOrderEntryIfAcceptable(OrderEntry orderEntry)
	{
		if (!IsPriceAcceptable(orderEntry))
		{
			orderEntry.priceAccceptable = false;
		}
		else
		{
			orderEntry.paid = true;
		}
	}

	private bool IsPriceAcceptable(OrderEntry orderEntry)
	{
		float price = ItemHelper.GetPrice(orderEntry.itemName, InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
		return sharedNightclubCustomer.Value.citizenData.IsPriceAcceptable(orderEntry.itemName, price);
	}
}
