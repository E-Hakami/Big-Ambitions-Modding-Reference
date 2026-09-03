using System;
using BigAmbitions.DayNightCycle;
using Buildings.BuildingTypes.Special.FoodDelivery;
using Buildings.Office.Headquarters;
using Entities;
using Helpers;
using UI;
using UI.Guiders;
using UI.Smartphone.Apps.BizMan.Schedule;

namespace Buildings.BuildingTypes.Special.MovingCompany;

public static class BizManTransfer
{
	public static void Transfer(BuildingRegistration originBuildingRegistration, BuildingRegistration destinationBuildingRegistration)
	{
		if (string.IsNullOrEmpty(originBuildingRegistration.BusinessName))
		{
			return;
		}
		BizManSchedule.AbortAutoFillForBusiness(originBuildingRegistration);
		ChangeBusinessName(originBuildingRegistration, destinationBuildingRegistration);
		ChangeBusinessTypeIfNeeded(originBuildingRegistration, destinationBuildingRegistration);
		HandleLogoSettings(originBuildingRegistration, destinationBuildingRegistration);
		CopyUniforms(originBuildingRegistration, destinationBuildingRegistration);
		MoveEmployees(originBuildingRegistration.Address, destinationBuildingRegistration.Address);
		UpdateImportPartnershipsIfNeeded(originBuildingRegistration, destinationBuildingRegistration);
		UpdateDeliveryContracts(originBuildingRegistration, destinationBuildingRegistration);
		UpdateHeadquartersPlans(originBuildingRegistration, destinationBuildingRegistration);
		UpdateVehicleSlotsIfNeeded(originBuildingRegistration, destinationBuildingRegistration);
		destinationBuildingRegistration.retailPrices = originBuildingRegistration.retailPrices.Copy();
		destinationBuildingRegistration.storedRetailPrices = originBuildingRegistration.storedRetailPrices.Copy();
		destinationBuildingRegistration.scheduleDays = originBuildingRegistration.scheduleDays.Copy();
		if (destinationBuildingRegistration.businessTypeName == "ba:businesstype_headquarters")
		{
			destinationBuildingRegistration.TemporarilyClose(closed: false);
			foreach (ScheduleDay scheduleDay in destinationBuildingRegistration.scheduleDays)
			{
				scheduleDay.isOpen = scheduleDay.day != DayOfWeekOrdered.Saturday && scheduleDay.day != DayOfWeekOrdered.Sunday;
			}
		}
		destinationBuildingRegistration.marketingCampaigns = originBuildingRegistration.marketingCampaigns.Copy();
		destinationBuildingRegistration.deliveredItems = originBuildingRegistration.deliveredItems.Copy();
		BusinessHelper.UpdatePromotion(destinationBuildingRegistration);
		BusinessHelper.ShutdownBusiness(originBuildingRegistration);
		InstanceBehavior<CityManager>.Instance.FindCityBuildingController(originBuildingRegistration.Address)?.UpdatePoi();
		GuidersManager.UpdateGuidersWithAddress(originBuildingRegistration.Address);
		InstanceBehavior<UIs>.Instance.mapFilters.ApplyFilters();
	}

	private static void ChangeBusinessName(BuildingRegistration originBuildingRegistration, BuildingRegistration destinationBuildingRegistration)
	{
		destinationBuildingRegistration.BusinessName = originBuildingRegistration.BusinessName;
		GuidersManager.UpdateGuidersWithAddress(destinationBuildingRegistration.Address);
	}

	private static void ChangeBusinessTypeIfNeeded(BuildingRegistration originBuildingRegistration, BuildingRegistration destinationBuildingRegistration)
	{
		if (!(destinationBuildingRegistration.businessTypeName == originBuildingRegistration.businessTypeName))
		{
			LogisticsManagerHelper.CancelAllDeliveriesForAddress(destinationBuildingRegistration.Address);
			if (destinationBuildingRegistration.businessTypeName == "ba:businesstype_headquarters")
			{
				BusinessHelper.DeleteHQPlans(destinationBuildingRegistration);
			}
			if (originBuildingRegistration.businessTypeName == "ba:businesstype_headquarters")
			{
				destinationBuildingRegistration.TemporarilyClose(closed: false);
			}
			destinationBuildingRegistration.businessTypeName = originBuildingRegistration.businessTypeName;
			InstanceBehavior<CityManager>.Instance.FindCityBuildingController(destinationBuildingRegistration.Address)?.UpdatePoi();
			InstanceBehavior<UIs>.Instance.mapFilters.ApplyFilters();
		}
	}

	private static void HandleLogoSettings(BuildingRegistration originBuildingRegistration, BuildingRegistration destinationBuildingRegistration)
	{
		if (destinationBuildingRegistration.GetBuildingType() != "ba:buildingtype_warehouse")
		{
			InstanceBehavior<CityManager>.Instance.UpdateBillboardsFromBusiness(destinationBuildingRegistration.BusinessName);
		}
		destinationBuildingRegistration.logoSettings = originBuildingRegistration.logoSettings;
		destinationBuildingRegistration.signAppearanceSettings = originBuildingRegistration.signAppearanceSettings;
	}

	private static void CopyUniforms(BuildingRegistration originBuildingRegistration, BuildingRegistration destinationBuildingRegistration)
	{
		destinationBuildingRegistration.uniformsBySkill = originBuildingRegistration.uniformsBySkill.Copy();
	}

	private static void MoveEmployees(Address fromAddress, Address toAddress)
	{
		foreach (EmployeeInstance employeeInstance in EmployeeHelper.GetEmployeeInstances())
		{
			if (employeeInstance.assignedAddress == fromAddress)
			{
				employeeInstance.assignedAddress = toAddress;
			}
		}
	}

	private static void UpdateImportPartnershipsIfNeeded(BuildingRegistration originBuildingRegistration, BuildingRegistration destinationBuildingRegistration)
	{
		foreach (ImportPartnership importPartnership in SaveGameManager.Current.importPartnerships)
		{
			if (importPartnership.headquartersAddress == originBuildingRegistration.Address)
			{
				importPartnership.headquartersAddress = destinationBuildingRegistration.Address;
			}
			foreach (ImportProduct product in importPartnership.products)
			{
				if (product.assignedWarehouse == originBuildingRegistration.Address)
				{
					product.assignedWarehouse = destinationBuildingRegistration.Address;
				}
			}
		}
	}

	private static void UpdateDeliveryContracts(BuildingRegistration originBuildingRegistration, BuildingRegistration destinationBuildingRegistration)
	{
		foreach (DeliveryContract deliveryContract in SaveGameManager.Current.DeliveryContracts)
		{
			if (deliveryContract.businessAddress == originBuildingRegistration.Address)
			{
				deliveryContract.businessAddress = destinationBuildingRegistration.Address;
			}
		}
		foreach (FurnitureDeliveryContract furnitureDeliveryContract in SaveGameManager.Current.FurnitureDeliveryContracts)
		{
			if (furnitureDeliveryContract.toAddress == originBuildingRegistration.Address)
			{
				furnitureDeliveryContract.toAddress = destinationBuildingRegistration.Address;
			}
		}
		FoodDeliveryHelper.MoveContractsToAddress(originBuildingRegistration.Address, destinationBuildingRegistration.Address);
	}

	private static void UpdateHeadquartersPlans(BuildingRegistration originBuildingRegistration, BuildingRegistration destinationBuildingRegistration)
	{
		foreach (LogisticsManagerPlan logisticsManagerPlan in SaveGameManager.Current.logisticsManagerPlans)
		{
			if (logisticsManagerPlan.headquartersAddress == originBuildingRegistration.Address)
			{
				logisticsManagerPlan.headquartersAddress = destinationBuildingRegistration.Address;
			}
			if (logisticsManagerPlan.targetAddress == destinationBuildingRegistration.Address)
			{
				logisticsManagerPlan.targetAddress = null;
			}
			else if (logisticsManagerPlan.targetAddress == originBuildingRegistration.Address)
			{
				logisticsManagerPlan.targetAddress = destinationBuildingRegistration.Address;
			}
			foreach (LogisticsManagerPlanDestination destination in logisticsManagerPlan.destinations)
			{
				if (destination.deliveryTargetAddress == originBuildingRegistration.Address)
				{
					destination.deliveryTargetAddress = destinationBuildingRegistration.Address;
				}
			}
		}
		if (!(originBuildingRegistration.businessTypeName == "ba:businesstype_headquarters"))
		{
			return;
		}
		foreach (HeadhunterPlan headhunterPlan in SaveGameManager.Current.headhunterPlans)
		{
			if (headhunterPlan.headquartersAddress == originBuildingRegistration.Address)
			{
				headhunterPlan.headquartersAddress = destinationBuildingRegistration.Address;
			}
		}
		foreach (HrManagerPlan hrManagerPlan in SaveGameManager.Current.hrManagerPlans)
		{
			if (hrManagerPlan.headquartersAddress == originBuildingRegistration.Address)
			{
				hrManagerPlan.headquartersAddress = destinationBuildingRegistration.Address;
			}
		}
		foreach (PricingManagerPlan pricingManagerPlan in SaveGameManager.Current.pricingManagerPlans)
		{
			if (pricingManagerPlan.headquartersAddress == originBuildingRegistration.Address)
			{
				pricingManagerPlan.headquartersAddress = destinationBuildingRegistration.Address;
			}
		}
	}

	private static void UpdateVehicleSlotsIfNeeded(BuildingRegistration originBuildingRegistration, BuildingRegistration destinationBuildingRegistration)
	{
		if (destinationBuildingRegistration is Warehouse warehouse && originBuildingRegistration is Warehouse warehouse2)
		{
			for (int i = 0; i < warehouse.vehicleSlots.Count && warehouse2.vehicleSlots.Count > i; i++)
			{
				warehouse.vehicleSlots[i] = warehouse2.vehicleSlots[i].Copy();
			}
			warehouse2.ResetVehicleSlots();
		}
	}
}
