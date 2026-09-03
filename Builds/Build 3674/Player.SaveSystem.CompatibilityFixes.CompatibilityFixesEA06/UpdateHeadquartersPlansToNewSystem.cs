using System.Collections.Generic;
using System.Linq;
using Buildings.Office.Headquarters;
using Entities;
using Extensions;
using Helpers;
using Streets;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class UpdateHeadquartersPlansToNewSystem : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.logisticsManagerPlans = new List<LogisticsManagerPlan>();
		gameInstance.headhunterPlans = new List<HeadhunterPlan>();
		gameInstance.hrManagerPlans = new List<HrManagerPlan>();
		EmployeeHelper.EnsureInit(gameInstance);
		foreach (LogisticsManager item3 in gameInstance.EmployeeInstances.Where((EmployeeInstance x) => x is LogisticsManager).Cast<LogisticsManager>())
		{
			if (!(item3.assignedAddress == null) && !(item3.assignedAddress == null) && !(item3.assignedWarehouseAddress == null) && !(item3.assignedWarehouseAddress == null))
			{
				LogisticsManagerPlan item = new LogisticsManagerPlan
				{
					targetAddress = item3.assignedWarehouseAddress,
					headquartersAddress = item3.assignedAddress,
					assignedEmployeeId = item3.id,
					destinations = item3.deliveryPlans
				};
				gameInstance.logisticsManagerPlans.Add(item);
				item3.assignedWarehouseAddress = null;
				item3.deliveryPlans = null;
			}
		}
		foreach (ImportPartnership importPartnership in gameInstance.importPartnerships)
		{
			EmployeeInstance employeeInstance = gameInstance.EmployeeInstances.FirstOrDefault((EmployeeInstance x) => x.id == importPartnership.employeeInstanceId);
			if (employeeInstance == null)
			{
				Debug.Log("Error converting import partnership to new system: no employee assigned");
				continue;
			}
			importPartnership.id = UuidHelper.GenerateBase64Uuid();
			importPartnership.headquartersAddress = employeeInstance.assignedAddress;
		}
		foreach (Headhunter item4 in gameInstance.EmployeeInstances.Where((EmployeeInstance x) => x is Headhunter).Cast<Headhunter>())
		{
			if (!item4.assignedAddress.IsUndefined() && !(item4.assignedAddress == null))
			{
				HeadhunterPlan item2 = new HeadhunterPlan
				{
					headquartersAddress = item4.assignedAddress,
					assignedEmployeeId = item4.id,
					assignedHrPlans = item4.assignedHRManagers,
					isRecruiting = item4.isRecruiting,
					skillRecruiting = item4.skillRecruiting,
					skillValueTarget = item4.skillValueTarget,
					dealBreakerTypes = item4.dealBreakerTypes,
					nextRecruit = item4.nextRecruit,
					automaticallyReplaceOnRetire = item4.automaticallyReplaceOnRetire,
					automaticallyReplaceOnResign = item4.automaticallyReplaceOnResign,
					remainingCandidatesToRecruit = item4.remainingCandidatesToRecruit,
					amountOfCandidatesToRecruitPreference = item4.amountOfCandidatesToRecruitPreference,
					headhunterReplacementDataList = (item4.headhunterReplacementDataList ?? new List<HeadhunterReplacementData>())
				};
				gameInstance.headhunterPlans.Add(item2);
				item4.assignedHRManagers = null;
				item4.isRecruiting = false;
				item4.skillRecruiting = "ba:skill_headhunter";
				item4.skillValueTarget = 0f;
				item4.dealBreakerTypes = null;
				item4.nextRecruit = null;
				item4.automaticallyReplaceOnRetire = false;
				item4.automaticallyReplaceOnResign = false;
				item4.remainingCandidatesToRecruit = 0;
				item4.amountOfCandidatesToRecruitPreference = 0;
				item4.headhunterReplacementDataList = null;
			}
		}
		foreach (HRManager hrManager in gameInstance.EmployeeInstances.Where((EmployeeInstance x) => x is HRManager).Cast<HRManager>())
		{
			if (hrManager.assignedAddress.IsUndefined() || hrManager.assignedAddress == null)
			{
				continue;
			}
			HrManagerPlan hrManagerPlan = new HrManagerPlan
			{
				headquartersAddress = hrManager.assignedAddress,
				assignedEmployeeId = hrManager.id,
				assignedEmployees = (hrManager.assignedEmployees ?? new List<string>()),
				replaceAbsentEmployees = hrManager.replaceAbsentEmployees,
				trainingTarget = hrManager.trainingTarget,
				healthInsurancePlan = hrManager.healthInsurancePlan
			};
			gameInstance.hrManagerPlans.Add(hrManagerPlan);
			foreach (EmployeeInstance employeeInstance2 in hrManagerPlan.EmployeeInstances)
			{
				if (employeeInstance2 != null)
				{
					employeeInstance2.assignedHRManager = null;
					employeeInstance2.assignedHrManagerPlanId = hrManagerPlan.id;
				}
			}
			foreach (HeadhunterPlan headhunterPlan in gameInstance.headhunterPlans)
			{
				if (!headhunterPlan.assignedHrPlans.Contains(hrManager.id))
				{
					continue;
				}
				for (int num = 0; num < headhunterPlan.assignedHrPlans.Length; num++)
				{
					if (!(headhunterPlan.assignedHrPlans[num] != hrManager.id))
					{
						headhunterPlan.assignedHrPlans[num] = hrManagerPlan.id;
						break;
					}
				}
			}
			foreach (HealthInsurancePlanOffer item5 in SaveGameManager.Current.healthInsurancePlanOffers.Where((HealthInsurancePlanOffer x) => x.hrManagerId == hrManager.id))
			{
				item5.hrManagerPlanId = hrManagerPlan.id;
				item5.hrManagerId = null;
			}
			hrManager.assignedEmployees = null;
			hrManager.replaceAbsentEmployees = false;
			hrManager.trainingTarget = 0;
			hrManager.healthInsurancePlan = null;
		}
	}
}
