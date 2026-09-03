using Entities;
using UnityEngine;

namespace Helpers;

public static class HealthInsuranceHelper
{
	public static int HourToSendOffer => NegotiationParams.hourToSendOffer;

	public static int MinimumEmployeesInCharge => NegotiationParams.minimumEmployeesInCharge;

	private static HealthInsuranceNegotiationParams NegotiationParams => InstanceBehavior<GlobalReferences>.Instance.healthInsuranceNegotiationParams;

	private static float MinSkillForSilverPlan => NegotiationParams.minSkillForSilverPlan;

	private static float MinSkillForGoldPlan => NegotiationParams.minSkillForGoldPlan;

	private static float BronzePlanDefaultPrice => NegotiationParams.bronzePlanDefaultPrice;

	private static float SilverPlanDefaultPrice => NegotiationParams.silverPlanDefaultPrice;

	private static float GoldPlanDefaultPrice => NegotiationParams.goldPlanDefaultPrice;

	private static float PlanPriceVariation => NegotiationParams.planPriceVariation;

	private static int MaxValuableYearsWithInsuranceExperience => NegotiationParams.maxValuableYearsWithInsuranceExperience;

	private static int MaxEmployeesInChargeToDiscount => NegotiationParams.maxEmployeesInChargeToDiscount;

	private static int MinEmployeesInChargeToDiscount => NegotiationParams.minEmployeesInChargeToDiscount;

	private static int EmployeesInChargeVariation => MaxEmployeesInChargeToDiscount - MinEmployeesInChargeToDiscount;

	public static float GetMinSkillForPlan(this HealthInsurancePlanType healthInsurancePlan)
	{
		return healthInsurancePlan switch
		{
			HealthInsurancePlanType.Silver => MinSkillForSilverPlan, 
			HealthInsurancePlanType.Gold => MinSkillForGoldPlan, 
			_ => 0f, 
		};
	}

	public static int GetEmployeesInChargePower(int numberOfAssignedEmployees)
	{
		return (numberOfAssignedEmployees - MinEmployeesInChargeToDiscount) / EmployeesInChargeVariation;
	}

	public static float GetExperiencePower(float experienceWithInsurancesInYears)
	{
		return Mathf.Min(experienceWithInsurancesInYears / (float)MaxValuableYearsWithInsuranceExperience, 1f);
	}

	public static (float, float) GetOfferPriceRange(float offerPower, HealthInsurancePlanType planType)
	{
		float defaultPrice = GetDefaultPrice(planType);
		float num = defaultPrice - PlanPriceVariation / 2f;
		float num2 = defaultPrice + PlanPriceVariation / 2f;
		float item = num - offerPower * PlanPriceVariation;
		float item2 = num2 - offerPower * PlanPriceVariation;
		return (item, item2);
	}

	public static float GetDefaultPrice(HealthInsurancePlanType planType)
	{
		return planType switch
		{
			HealthInsurancePlanType.Bronze => BronzePlanDefaultPrice, 
			HealthInsurancePlanType.Silver => SilverPlanDefaultPrice, 
			_ => GoldPlanDefaultPrice, 
		};
	}
}
