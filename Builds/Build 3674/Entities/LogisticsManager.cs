using System;
using System.Collections.Generic;
using Buildings.Office.Headquarters;

namespace Entities;

[Serializable]
public class LogisticsManager : EmployeeInstance
{
	[Obsolete]
	public Address assignedWarehouseAddress;

	[Obsolete]
	public List<LogisticsManagerPlanDestination> deliveryPlans;

	public override void Reset()
	{
		UnAssignWork();
		base.Reset();
	}

	public override void UnAssignWork()
	{
		LogisticsManagerHelper.GetAssignedPlanForEmployee(id)?.UnAssignEmployee();
	}
}
