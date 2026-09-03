using System.Linq;
using Buildings.Office.Headquarters;
using Entities;
using Helpers;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class RemoveInvalidPlans : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		LogisticsManagerPlan[] array = gameInstance.logisticsManagerPlans.ToArray();
		foreach (LogisticsManagerPlan logisticsManagerPlan in array)
		{
			BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(logisticsManagerPlan.headquartersAddress);
			if (buildingRegistration != null && !buildingRegistration.RentedByPlayer)
			{
				CompatibilityHelper.RemoveLogisticsManagerPlan(logisticsManagerPlan.headquartersAddress);
				continue;
			}
			BuildingRegistration buildingRegistration2 = BuildingHelper.GetBuildingRegistration(logisticsManagerPlan.targetAddress);
			if (buildingRegistration2 != null && !buildingRegistration2.RentedByPlayer)
			{
				CompatibilityHelper.RemoveLogisticsManagerPlan(logisticsManagerPlan.targetAddress);
			}
		}
		HeadhunterPlan[] array2 = gameInstance.headhunterPlans.ToArray();
		foreach (HeadhunterPlan headhunterPlan in array2)
		{
			BuildingRegistration buildingRegistration3 = BuildingHelper.GetBuildingRegistration(headhunterPlan.headquartersAddress);
			if (buildingRegistration3 != null && !buildingRegistration3.RentedByPlayer)
			{
				CompatibilityHelper.RemoveHeadhunterPlan(headhunterPlan.headquartersAddress);
			}
			string[] assignedHrPlans = headhunterPlan.assignedHrPlans;
			if (assignedHrPlans != null && assignedHrPlans.Length == 2)
			{
				continue;
			}
			string[] array3 = new string[2];
			if (headhunterPlan.assignedHrPlans != null)
			{
				int num = Mathf.Min(headhunterPlan.assignedHrPlans.Length, 2);
				for (int j = 0; j < num; j++)
				{
					array3[j] = headhunterPlan.assignedHrPlans[j];
				}
			}
			headhunterPlan.assignedHrPlans = array3;
		}
		HrManagerPlan[] array4 = gameInstance.hrManagerPlans.ToArray();
		foreach (HrManagerPlan hrManagerPlan in array4)
		{
			BuildingRegistration buildingRegistration4 = BuildingHelper.GetBuildingRegistration(hrManagerPlan.headquartersAddress);
			if (buildingRegistration4 != null && !buildingRegistration4.RentedByPlayer)
			{
				CompatibilityHelper.RemoveHrPlan(hrManagerPlan.headquartersAddress);
			}
		}
		ImportPartnership[] array5 = gameInstance.importPartnerships.ToArray();
		foreach (ImportPartnership importPartnership in array5)
		{
			BuildingRegistration buildingRegistration5 = BuildingHelper.GetBuildingRegistration(importPartnership.headquartersAddress);
			if (buildingRegistration5 != null && !buildingRegistration5.RentedByPlayer)
			{
				CompatibilityHelper.RemoveImportPartnership(importPartnership.headquartersAddress);
			}
		}
		DeliveryContract[] array6 = gameInstance.DeliveryContracts.ToArray();
		foreach (DeliveryContract deliveryContract in array6)
		{
			BuildingRegistration buildingRegistration6 = BuildingHelper.GetBuildingRegistration(deliveryContract.businessAddress);
			if (buildingRegistration6 != null && !buildingRegistration6.RentedByPlayer)
			{
				CompatibilityHelper.RemoveDeliveryContract(deliveryContract.businessAddress);
			}
		}
		foreach (EmployeeInstance employee in gameInstance.EmployeeInstances)
		{
			if (!string.IsNullOrEmpty(employee.assignedHrManagerPlanId) && gameInstance.hrManagerPlans.All((HrManagerPlan x) => x.id != employee.assignedHrManagerPlanId))
			{
				employee.assignedHrManagerPlanId = null;
			}
		}
	}
}
