using System.Collections.Generic;
using System.Linq;
using Entities;

namespace Buildings.Office.Headquarters;

public static class PurchasingAgentHelper
{
	public static ImportPartnership GetAssignedPlanForPurchasingAgent(string purchasingAgentId)
	{
		return SaveGameManager.Current.importPartnerships.FirstOrDefault((ImportPartnership x) => x.employeeInstanceId == purchasingAgentId);
	}

	public static List<ImportPartnership> GetAssignedPlansForHeadquarters(Address headquartersAddress)
	{
		return SaveGameManager.Current.importPartnerships.Where((ImportPartnership x) => x.headquartersAddress == headquartersAddress).ToList();
	}

	public static void DeletePlan(string planId)
	{
		SaveGameManager.Current.importPartnerships.RemoveAll((ImportPartnership x) => x.id == planId);
	}

	public static void CancelPlansThatDeliverToAddress(Address address)
	{
		foreach (ImportPartnership importPartnership in SaveGameManager.Current.importPartnerships)
		{
			bool flag = false;
			foreach (ImportProduct product in importPartnership.products)
			{
				if (!(product.assignedWarehouse != address))
				{
					flag = true;
					product.assignedWarehouse = null;
				}
			}
			if (flag)
			{
				importPartnership.isActive = false;
				importPartnership.isUrgentOrder = false;
			}
		}
	}
}
