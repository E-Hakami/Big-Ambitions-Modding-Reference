using System.Collections.Generic;
using Entities;
using Helpers;

namespace Buildings.Office.Headquarters;

public static class LogisticsManagerHelper
{
	public struct PlanDestinationPair(LogisticsManagerPlan plan, LogisticsManagerPlanDestination destination)
	{
		public readonly LogisticsManagerPlan plan = plan;

		public readonly LogisticsManagerPlanDestination destination = destination;
	}

	public static readonly DistributedWork<PlanDestinationPair> WarehouseDeliveriesWork = new DistributedWork<PlanDestinationPair>(ProgressDelivery);

	public static readonly DistributedWork<PlanDestinationPair> FactoryDeliveriesWork = new DistributedWork<PlanDestinationPair>(ProgressDelivery);

	public static void DoAllWarehouseDeliveries()
	{
		foreach (LogisticsManagerPlan item in SaveGameManager.Current.logisticsManagerPlans.FindAll((LogisticsManagerPlan x) => !x.isFactory))
		{
			foreach (LogisticsManagerPlanDestination plannedDelivery in item.GetPlannedDeliveries())
			{
				WarehouseDeliveriesWork.Enqueue(new PlanDestinationPair(item, plannedDelivery));
			}
		}
	}

	public static void DoAllFactoryDeliveries()
	{
		foreach (LogisticsManagerPlan item in SaveGameManager.Current.logisticsManagerPlans.FindAll((LogisticsManagerPlan x) => x.isFactory))
		{
			foreach (LogisticsManagerPlanDestination plannedDelivery in item.GetPlannedDeliveries())
			{
				FactoryDeliveriesWork.Enqueue(new PlanDestinationPair(item, plannedDelivery));
			}
		}
	}

	private static void ProgressDelivery(PlanDestinationPair pair)
	{
		pair.plan.DeliverDestination(pair.destination);
		if (!FactoryDeliveriesWork.HasPendingWork && !WarehouseDeliveriesWork.HasPendingWork)
		{
			LogisticsManagerPlan.OnFinishedDeliveries();
		}
	}

	public static LogisticsManagerPlan GetAssignedPlanForEmployee(string employeeId)
	{
		return SaveGameManager.Current.logisticsManagerPlans.Find((LogisticsManagerPlan x) => x.assignedEmployeeId == employeeId);
	}

	public static List<LogisticsManagerPlan> GetAssignedPlansForHeadquarters(Address headquartersAddress)
	{
		return SaveGameManager.Current.logisticsManagerPlans.FindAll((LogisticsManagerPlan x) => x.headquartersAddress == headquartersAddress);
	}

	public static void DeletePlan(string planId)
	{
		FactoryDeliveriesWork.ForceCompleteAllWork();
		WarehouseDeliveriesWork.ForceCompleteAllWork();
		SaveGameManager.Current.logisticsManagerPlans.RemoveAll((LogisticsManagerPlan x) => x.id == planId);
	}

	public static void CancelAllDeliveriesForAddress(Address address)
	{
		foreach (LogisticsManagerPlan logisticsManagerPlan in SaveGameManager.Current.logisticsManagerPlans)
		{
			logisticsManagerPlan.CancelDeliveriesForAddress(address);
		}
	}

	public static void ShutdownBusiness(Address address)
	{
		foreach (LogisticsManagerPlan logisticsManagerPlan in SaveGameManager.Current.logisticsManagerPlans)
		{
			if (logisticsManagerPlan.targetAddress == address)
			{
				logisticsManagerPlan.UnAssignEmployee();
				logisticsManagerPlan.UnAssignAddress();
			}
		}
	}
}
