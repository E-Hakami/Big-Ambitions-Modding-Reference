using System;
using System.Collections.Generic;
using Buildings.Office.Headquarters;
using Extensions;
using Helpers;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;

namespace Entities;

public class HealthInsurancePlanOffer : NegotiationOffer
{
	public string hrManagerPlanId;

	public readonly HealthInsurancePlanType planType;

	[Obsolete]
	public string hrManagerId;

	public HrManagerPlan HrManagerPlan => HrManagerHelper.GetPlanFromId(hrManagerPlanId);

	private static HealthInsuranceNegotiationParams NegotiationParams => InstanceBehavior<GlobalReferences>.Instance.healthInsuranceNegotiationParams;

	public HealthInsurancePlanOffer(string hrManagerPlanId, HealthInsurancePlanType planType)
		: base(UuidHelper.GenerateBase64Uuid(), SaveGameManager.Current.Day + 1)
	{
		this.hrManagerPlanId = hrManagerPlanId;
		this.planType = planType;
	}

	public void SendOffer()
	{
		HrManagerPlan hrManagerPlan = HrManagerPlan;
		Contact contact = Contact.AddContact("hospital_health_insurance_manager", ContactCategoryName.Business, "hospital_health_insurance_manager_description", BuildingHelper.GetBuildingRegistration(GameManager.hospitalAddress));
		if (hrManagerPlan == null || hrManagerPlan.HrManagerInstance == null)
		{
			contact.SendMessage(new TextMessage("ba:messagetype_dialog_health_insurance_manager_hr_manager_not_found"));
			DeclineOffer();
		}
		else if (hrManagerPlan.HrManagerSkillValue < planType.GetMinSkillForPlan())
		{
			Dictionary<string, string> messageData = new Dictionary<string, string> { 
			{
				"healthPlanType",
				planType.GetLocalization()
			} };
			contact.SendMessage(new TextMessage("ba:messagetype_dialog_health_insurance_manager_hr_manager_skill_too_low", messageData));
			DeclineOffer();
		}
		else
		{
			initialOfferPrice = GetInitialOfferPrice();
			TextMessage.ContextAction contextAction = new TextMessage.ContextAction
			{
				healthPlanOfferId = id,
				type = TextMessage.ContextAction.ContextActionType.HealthInsurancePlanOffer
			};
			GameManager.SendTextMessage(contact, "ba:messagetype_dialog_health_insurance_manager_initial_offer", null, contextAction);
		}
	}

	private float GetInitialOfferPrice()
	{
		HrManagerPlan hrManagerPlan = HrManagerPlan;
		float num = 0f;
		float num2 = hrManagerPlan.HrManagerSkillValue / 100f;
		num += num2;
		int employeesInChargePower = HealthInsuranceHelper.GetEmployeesInChargePower(hrManagerPlan.NumberOfAssignedEmployees);
		if (employeesInChargePower > 0)
		{
			num += (float)employeesInChargePower;
		}
		float experiencePower = HealthInsuranceHelper.GetExperiencePower((hrManagerPlan.HrManagerInstance as HRManager)?.experienceWithInsurancesInYears ?? 0f);
		num += experiencePower;
		float num3 = PlayerHelper.GetPlayerEmployeeInstance().GetSkillValue("ba:skill_negotiation") / 100f;
		num += num3;
		num /= 4f;
		(float, float) offerPriceRange = HealthInsuranceHelper.GetOfferPriceRange(num, planType);
		float item = offerPriceRange.Item1;
		float item2 = offerPriceRange.Item2;
		float num4 = (float)Mathf.RoundToInt(UnityEngine.Random.Range(item, item2) * 100f) / 100f;
		minOfferPrice = UnityEngine.Random.Range(NegotiationParams.initialOfferMinFactor, NegotiationParams.initialOfferMaxFactor) * num4;
		return num4;
	}

	public void AcceptOffer(float pricePerDayAndEmployee)
	{
		HrManagerPlan.healthInsurancePlan = new HealthInsurancePlan
		{
			planType = planType,
			pricePerDayAndEmployee = pricePerDayAndEmployee
		};
		negotiationFinished = true;
		accepted = true;
		GameEvent.Invoke("ba:gameevent_healthinsuranceplanaccepted");
		IncreasePlayerNegotiationSkill();
	}

	public void DeclineOffer()
	{
		negotiationFinished = true;
		accepted = false;
		IncreasePlayerNegotiationSkill();
	}

	private void IncreasePlayerNegotiationSkill()
	{
		PlayerHelper.GetPlayerEmployeeInstance().IncreaseSkill("ba:skill_negotiation", UnityEngine.Random.Range(1, 3));
	}
}
