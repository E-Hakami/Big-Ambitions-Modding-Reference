using System.Collections.Generic;
using System.Linq;
using Entities;
using Extensions;
using Helpers;
using UnityEngine;

namespace Buildings.Office.Headquarters;

public class HrManagerPlan
{
	public readonly string id = UuidHelper.GenerateBase64Uuid();

	public string assignedEmployeeId;

	public Address headquartersAddress;

	public List<string> assignedEmployees = new List<string>();

	public bool replaceAbsentEmployees = true;

	public int trainingTarget = 50;

	public HealthInsurancePlan healthInsurancePlan;

	private EmployeeInstance _hrManagerInstance;

	public EmployeeInstance HrManagerInstance => GetHrManagerInstance();

	public float HrManagerSkillValue => HrManagerInstance?.GetSkillValue("ba:skill_hrmanager") ?? 0f;

	public int NumberOfAssignedEmployees => assignedEmployees.Count;

	public int MaxEmployees => HrManagerSkillValue.CalculateMaxAssignableEmployees();

	public float TrainingPriceMultiplier => 0.9f - 0.3f * HrManagerSkillValue / 100f;

	public List<EmployeeInstance> EmployeeInstances => assignedEmployees.Select((string x) => EmployeeHelper.GetEmployeeById(x)).ToList();

	public bool CanHaveHealthInsurancePlan
	{
		get
		{
			if (healthInsurancePlan == null && HrManagerInstance != null)
			{
				return !SaveGameManager.Current.healthInsurancePlanOffers.Exists((HealthInsurancePlanOffer offer) => offer.hrManagerPlanId == id && !offer.negotiationFinished);
			}
			return false;
		}
	}

	private EmployeeInstance GetHrManagerInstance()
	{
		if (string.IsNullOrEmpty(assignedEmployeeId))
		{
			return null;
		}
		if (_hrManagerInstance != null)
		{
			return _hrManagerInstance;
		}
		_hrManagerInstance = EmployeeHelper.GetEmployeeById(assignedEmployeeId);
		return _hrManagerInstance;
	}

	public void TrainEmployees()
	{
		if (string.IsNullOrEmpty(assignedEmployeeId))
		{
			return;
		}
		float num = 0f;
		float trainingPriceMultiplier = TrainingPriceMultiplier;
		List<EmployeeInstance> employeeInstances = EmployeeInstances;
		for (int num2 = employeeInstances.Count - 1; num2 >= 0; num2--)
		{
			EmployeeInstance employeeInstance = employeeInstances[num2];
			if (employeeInstance == null)
			{
				assignedEmployees.RemoveAt(num2);
			}
			else
			{
				int num3 = Mathf.RoundToInt(Mathf.Min((float)trainingTarget - employeeInstance.characterData.skills[0].value, 2f));
				if (num3 > 0)
				{
					float value = employeeInstance.characterData.skills[0].value;
					employeeInstance.characterData.skills[0].value += num3;
					employeeInstance.IncreaseWageFromTraining(employeeInstance.characterData.skills[0], value);
					num += EmployeeHelper.GetTrainingCost(employeeInstance, employeeInstance.characterData.skills[0].name, num3);
				}
				if (employeeInstance.characterData.skills.Count > 1)
				{
					for (int i = 1; i < employeeInstance.characterData.skills.Count; i++)
					{
						num3 = Mathf.RoundToInt(Mathf.Min((float)trainingTarget - employeeInstance.characterData.skills[i].value, num3));
						if (num3 > 0)
						{
							float value2 = employeeInstance.characterData.skills[i].value;
							employeeInstance.characterData.skills[i].value += num3;
							employeeInstance.IncreaseWageFromTraining(employeeInstance.characterData.skills[i], value2);
							num += EmployeeHelper.GetTrainingCost(employeeInstance, employeeInstance.characterData.skills[i].name, num3);
						}
					}
				}
			}
		}
		num *= trainingPriceMultiplier;
		if (!(num <= 0f))
		{
			string value3 = HrManagerInstance?.characterData.name ?? "ba:transaction_hrtraining";
			Dictionary<string, string> data = new Dictionary<string, string> { { "employee", value3 } };
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_hrtraining", data);
			transactionInfo.SetTaxDeductibleName("ba:transaction_employeetraining_label");
			GameManager.ChangeMoneySafe(0f - num, transactionInfo, SaveGameManager.Current.Day - 1, null, force: true);
		}
	}

	public void CancelHealthInsurancePlan()
	{
		healthInsurancePlan = null;
	}

	public void PayHealthInsurance()
	{
		if (HasActiveHealthInsurance())
		{
			int num = NumberOfAssignedEmployees;
			if (num < HealthInsuranceHelper.MinimumEmployeesInCharge)
			{
				num = HealthInsuranceHelper.MinimumEmployeesInCharge;
			}
			float num2 = (float)num * healthInsurancePlan.pricePerDayAndEmployee;
			Dictionary<string, string> data = new Dictionary<string, string>
			{
				{
					"healthInsurancePlanType",
					healthInsurancePlan.planType.GetLocalization()
				},
				{
					"employee",
					HrManagerInstance?.characterData.name
				},
				{
					"numberOfEmployees",
					num.ToString()
				}
			};
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_healthinsurance", "ba:transactioncategory_healthinsuranceexpenses", data);
			transactionInfo.SetTaxDeductibleName("ba:transaction_healthinsurance_label");
			GameManager.ChangeMoneySafe(0f - num2, transactionInfo, SaveGameManager.Current.Day - 1, headquartersAddress, force: true);
		}
	}

	public bool CanUpgradeHealthInsurancePlan()
	{
		if (healthInsurancePlan.planType == HealthInsurancePlanType.Gold)
		{
			return false;
		}
		return HrManagerSkillValue >= healthInsurancePlan.planType.Next().GetMinSkillForPlan();
	}

	public float GetUpgradeHealthInsurancePlanPrice()
	{
		HealthInsurancePlanType planType = healthInsurancePlan.planType;
		HealthInsurancePlanType planType2 = healthInsurancePlan.planType.Next();
		float defaultPrice = HealthInsuranceHelper.GetDefaultPrice(planType);
		float num = HealthInsuranceHelper.GetDefaultPrice(planType2) - defaultPrice;
		return healthInsurancePlan.pricePerDayAndEmployee + (num + 1f);
	}

	public void UpgradeHealthInsurancePlan()
	{
		float upgradeHealthInsurancePlanPrice = GetUpgradeHealthInsurancePlanPrice();
		HealthInsurancePlan healthInsurancePlan = new HealthInsurancePlan
		{
			planType = this.healthInsurancePlan.planType.Next(),
			pricePerDayAndEmployee = upgradeHealthInsurancePlanPrice
		};
		Dictionary<string, string> data = new Dictionary<string, string>
		{
			{
				"healthInsurancePlanType",
				this.healthInsurancePlan.planType.GetLocalization()
			},
			{ "numberOfEmployees", "1" }
		};
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_healthinsurance", "ba:transactioncategory_healthinsuranceexpenses", data);
		transactionInfo.SetTaxDeductibleName("ba:transaction_healthinsurance_label");
		if (GameManager.ChangeMoneySafe(0f - upgradeHealthInsurancePlanPrice, transactionInfo, null, null, force: false, showNotification: true))
		{
			this.healthInsurancePlan = healthInsurancePlan;
		}
	}

	public bool HasActiveHealthInsurance()
	{
		if (healthInsurancePlan != null)
		{
			EmployeeInstance hrManagerInstance = HrManagerInstance;
			if (hrManagerInstance != null)
			{
				return !hrManagerInstance.isBeingReplaced;
			}
			return false;
		}
		return false;
	}

	public void AssignEmployee(string employeeId)
	{
		assignedEmployeeId = employeeId;
		_hrManagerInstance = null;
	}

	public void UnAssignEmployee()
	{
		assignedEmployeeId = null;
		_hrManagerInstance = null;
	}

	public void Delete()
	{
		foreach (EmployeeInstance employeeInstance in EmployeeInstances)
		{
			employeeInstance.assignedHrManagerPlanId = null;
		}
		foreach (HealthInsurancePlanOffer healthInsurancePlanOffer in SaveGameManager.Current.healthInsurancePlanOffers)
		{
			if (healthInsurancePlanOffer.hrManagerPlanId == id)
			{
				healthInsurancePlanOffer.negotiationFinished = true;
			}
		}
		SaveGameManager.Current.hrManagerPlans.RemoveAll((HrManagerPlan x) => x.id == id);
	}
}
