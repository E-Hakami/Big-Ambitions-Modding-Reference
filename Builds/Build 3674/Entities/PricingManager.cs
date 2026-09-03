using System;
using Buildings.Office.Headquarters;

namespace Entities;

[Serializable]
public class PricingManager : EmployeeInstance
{
	public override void Reset()
	{
		UnAssignWork();
		base.Reset();
	}

	public override void UnAssignWork()
	{
		PricingManagerHelper.GetAssignedPlanForPricingManager(id)?.UnAssignEmployee();
	}
}
