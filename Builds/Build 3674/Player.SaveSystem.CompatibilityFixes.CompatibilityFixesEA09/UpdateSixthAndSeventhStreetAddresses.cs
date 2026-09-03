using System.Collections.Generic;
using System.Linq;
using AI.Employees.SalaryNegotiation;
using BigAmbitions.Items;
using Buildings.BuildingTypes.Shared;
using Buildings.Office.Headquarters;
using Entities;
using JimmysUnityUtilities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class UpdateSixthAndSeventhStreetAddresses : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		(Address, Address)[] array = new(Address, Address)[16]
		{
			(new Address("ba:street_sixthstreet", 17), new Address("ba:street_sixthstreet", 21)),
			(new Address("ba:street_sixthstreet", 15), new Address("ba:street_sixthstreet", 19)),
			(new Address("ba:street_sixthstreet", 13), new Address("ba:street_sixthstreet", 17)),
			(new Address("ba:street_sixthstreet", 11), new Address("ba:street_sixthstreet", 15)),
			(new Address("ba:street_sixthstreet", 9), new Address("ba:street_sixthstreet", 13)),
			(new Address("ba:street_sixthstreet", 7), new Address("ba:street_sixthstreet", 11)),
			(new Address("ba:street_sixthstreet", 5), new Address("ba:street_sixthstreet", 9)),
			(new Address("ba:street_sixthstreet", 3), new Address("ba:street_sixthstreet", 7)),
			(new Address("ba:street_sixthavenue", 3), new Address("ba:street_sixthstreet", 5)),
			(new Address("ba:street_sixthstreet", 1), new Address("ba:street_sixthstreet", 3)),
			(new Address("ba:street_seventhstreet", 10), new Address("ba:street_seventhstreet", 12)),
			(new Address("ba:street_seventhstreet", 8), new Address("ba:street_seventhstreet", 10)),
			(new Address("ba:street_seventhstreet", 6), new Address("ba:street_seventhstreet", 8)),
			(new Address("ba:street_seventhstreet", 4), new Address("ba:street_seventhstreet", 6)),
			(new Address("ba:street_seventhstreet", 2), new Address("ba:street_seventhstreet", 4)),
			(new Address("ba:street_seventhstreet", 0), new Address("ba:street_seventhstreet", 2))
		};
		for (int i = 0; i < array.Length; i++)
		{
			(Address, Address) tuple = array[i];
			Address fromAddress = tuple.Item1;
			Address toAddress = tuple.Item2;
			BuildingRegistration buildingRegistration = gameInstance.BuildingRegistrations.FirstOrDefault((BuildingRegistration x) => x.StreetName == fromAddress.streetName && x.StreetNumber == fromAddress.streetNumber);
			if (buildingRegistration == null)
			{
				continue;
			}
			buildingRegistration.StreetName = toAddress.streetName;
			buildingRegistration.StreetNumber = toAddress.streetNumber;
			if (gameInstance.CurrentStreetName == fromAddress.streetName && gameInstance.CurrentStreetNumber == fromAddress.streetNumber)
			{
				gameInstance.CurrentStreetName = toAddress.streetName;
				gameInstance.CurrentStreetNumber = toAddress.streetNumber;
			}
			buildingRegistration.itemInstances.ForEach(delegate(KeyValuePair<string, ItemInstance> x)
			{
				x.Value.streetName = toAddress.streetName;
				x.Value.streetNumber = toAddress.streetNumber;
			});
			buildingRegistration.aiEmployees?.ForEach(delegate(AiBusinessEmployeeData x)
			{
				x.aiAddress = toAddress;
			});
			buildingRegistration.poachedEmployees?.ForEach(delegate(EmployeeInstance x)
			{
				x.assignedAddress = toAddress;
			});
			gameInstance.VehicleInstances.Where((VehicleInstance x) => x.Address == fromAddress).ForEach(delegate(VehicleInstance x)
			{
				x.streetName = toAddress.streetName;
				x.streetNumber = toAddress.streetNumber;
			});
			gameInstance.EmployeeInstances.Where((EmployeeInstance x) => x.assignedAddress == fromAddress).ForEach(delegate(EmployeeInstance x)
			{
				x.assignedAddress = toAddress;
			});
			gameInstance.CandidateEmployeeInstances.Where((EmployeeInstance x) => x.assignedAddress == fromAddress).ForEach(delegate(EmployeeInstance x)
			{
				x.assignedAddress = toAddress;
			});
			gameInstance.Transactions.Where((Transaction x) => x.address == fromAddress).ForEach(delegate(Transaction x)
			{
				x.address = toAddress;
			});
			if (gameInstance.customDestination == fromAddress)
			{
				gameInstance.customDestination = toAddress;
			}
			gameInstance.RecruitmentCampaigns.Where((RecruitmentCampaign x) => x.businessAddress == fromAddress).ForEach(delegate(RecruitmentCampaign x)
			{
				x.businessAddress = toAddress;
			});
			gameInstance.DeliveryContracts.Where((DeliveryContract x) => x.businessAddress == fromAddress).ForEach(delegate(DeliveryContract x)
			{
				x.businessAddress = toAddress;
			});
			gameInstance.FurnitureDeliveryContracts.Where((FurnitureDeliveryContract x) => x.toAddress == fromAddress).ForEach(delegate(FurnitureDeliveryContract x)
			{
				x.toAddress = toAddress;
			});
			gameInstance.TodoTasks.Where((TodoTask x) => x.address == fromAddress).ForEach(delegate(TodoTask x)
			{
				x.address = toAddress;
			});
			gameInstance.marketEvents.Where((MarketEvent x) => x.address == fromAddress).ForEach(delegate(MarketEvent x)
			{
				x.address = toAddress;
			});
			foreach (FinancialSummary financialSummary in gameInstance.financialSummaries)
			{
				financialSummary.businessIncomeStatements.Where((FinancialSummary.BusinessIncomeStatement x) => x.Address == fromAddress).ForEach(delegate(FinancialSummary.BusinessIncomeStatement x)
				{
					x.Address = toAddress;
				});
				financialSummary.residentialStatements.Where((FinancialSummary.ResidentialStatement x) => x.Address == fromAddress).ForEach(delegate(FinancialSummary.ResidentialStatement x)
				{
					x.Address = toAddress;
				});
				financialSummary.realEstateStatements.Where((FinancialSummary.RealEstateStatement x) => x.Address == fromAddress).ForEach(delegate(FinancialSummary.RealEstateStatement x)
				{
					x.Address = toAddress;
				});
			}
			gameInstance.buildingsForSale.Where((BuildingForSale x) => x.address == fromAddress).ForEach(delegate(BuildingForSale x)
			{
				x.address = toAddress;
			});
			gameInstance.realEstate.Where((RealEstate x) => x.address == fromAddress).ForEach(delegate(RealEstate x)
			{
				x.address = toAddress;
			});
			gameInstance.candidateSalaryNegotiations.Where((CandidateSalaryNegotiation x) => x.employeeInstance.assignedAddress == fromAddress).ForEach(delegate(CandidateSalaryNegotiation x)
			{
				x.employeeInstance.assignedAddress = toAddress;
			});
			gameInstance.interiorInstallationFirmContracts.Where((InteriorInstallationFirmContract x) => x.addressToDoTheInstallation == fromAddress).ForEach(delegate(InteriorInstallationFirmContract x)
			{
				x.addressToDoTheInstallation = toAddress;
			});
			gameInstance.movingServiceContracts.Where((MovingServiceContract x) => x.originMovingAddress == fromAddress).ForEach(delegate(MovingServiceContract x)
			{
				x.originMovingAddress = toAddress;
			});
			gameInstance.movingServiceContracts.Where((MovingServiceContract x) => x.destinationMovingAddress == fromAddress).ForEach(delegate(MovingServiceContract x)
			{
				x.destinationMovingAddress = toAddress;
			});
			gameInstance.importPartnerships.Where((ImportPartnership x) => x.headquartersAddress == fromAddress).ForEach(delegate(ImportPartnership x)
			{
				x.headquartersAddress = toAddress;
			});
			gameInstance.logisticsManagerPlans.Where((LogisticsManagerPlan x) => x.headquartersAddress == fromAddress).ForEach(delegate(LogisticsManagerPlan x)
			{
				x.headquartersAddress = toAddress;
			});
			foreach (LogisticsManagerPlan logisticsManagerPlan in gameInstance.logisticsManagerPlans)
			{
				if (logisticsManagerPlan.headquartersAddress == fromAddress)
				{
					logisticsManagerPlan.headquartersAddress = toAddress;
				}
				logisticsManagerPlan.destinations.Where((LogisticsManagerPlanDestination x) => x.deliveryTargetAddress == fromAddress).ForEach(delegate(LogisticsManagerPlanDestination x)
				{
					x.deliveryTargetAddress = toAddress;
				});
			}
			gameInstance.headhunterPlans.Where((HeadhunterPlan x) => x.headquartersAddress == fromAddress).ForEach(delegate(HeadhunterPlan x)
			{
				x.headquartersAddress = toAddress;
			});
			gameInstance.hrManagerPlans.Where((HrManagerPlan x) => x.headquartersAddress == fromAddress).ForEach(delegate(HrManagerPlan x)
			{
				x.headquartersAddress = toAddress;
			});
			buildingRegistration.BuildingCached = null;
		}
	}
}
