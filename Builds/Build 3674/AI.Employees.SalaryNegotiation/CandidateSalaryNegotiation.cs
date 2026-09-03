using System;
using System.Collections.Generic;
using System.Linq;
using Buildings.BuildingTypes.Shared;
using Entities;
using Extensions;
using Helpers;
using UI;
using UnityEngine;

namespace AI.Employees.SalaryNegotiation;

[Serializable]
public class CandidateSalaryNegotiation
{
	private const int ReenableNegotiationAfterDays = 30;

	public EmployeeInstance employeeInstance;

	public bool isRival;

	public bool isPoached;

	public float hourlyWage;

	public float signingBonus;

	public List<CandidateSalaryOffer> offers = new List<CandidateSalaryOffer>();

	public bool completed;

	public bool accepted;

	public int mood = 100;

	public float negotiateThreshold;

	public float firmThreshold;

	public readonly string id;

	private CandidateSalaryNegotiationParams NegotiationParams => InstanceBehavior<GlobalReferences>.Instance.candidateSalaryNegotiationParams;

	public bool IsDeclinedByMood => mood < NegotiationParams.declineIfMoodBelow;

	public CandidateSalaryNegotiation(EmployeeInstance employeeInstance, bool isRival, bool isPoached)
	{
		id = UuidHelper.GenerateBase64Uuid();
		this.employeeInstance = employeeInstance;
		this.isRival = isRival;
		this.isPoached = isPoached;
		InitializeThresholds();
	}

	public void InitializeThresholds()
	{
		if (!(negotiateThreshold > 0f) || !(firmThreshold > 0f))
		{
			UnityEngine.Random.State state = UnityEngine.Random.state;
			UnityEngine.Random.InitState(employeeInstance.id.GetHashCode() + TimeHelper.CurrentDay);
			negotiateThreshold = UnityEngine.Random.Range(NegotiationParams.compromiseThresholdMin, NegotiationParams.compromiseThresholdMax);
			firmThreshold = UnityEngine.Random.Range(NegotiationParams.holdFirmThresholdMin, NegotiationParams.holdFirmThresholdMax);
			UnityEngine.Random.state = state;
		}
	}

	public void SendOffer()
	{
		hourlyWage = GetInitialOffer();
		hourlyWage = Mathf.Ceil(hourlyWage * 20f) / 20f;
		signingBonus = 0f;
		offers.Add(new CandidateSalaryOffer(hourlyWage, signingBonus, fromCandidate: true));
		TextMessage.ContextAction contextAction = new TextMessage.ContextAction
		{
			salaryNegotiationId = id,
			employeeInstanceId = employeeInstance.id,
			type = TextMessage.ContextAction.ContextActionType.SalaryNegotiation
		};
		Contact contact = employeeInstance.GetContact();
		GameManager.SendTextMessage(contact, "ba:messagetype_contacts_salary_negotiation_start", null, contextAction, null, notify: false);
		InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.Contacts);
		InstanceBehavior<UIs>.Instance.fullMenu.contactsApp.OpenAppWithContact(contact);
	}

	private float GetInitialOffer()
	{
		if (isRival)
		{
			return employeeInstance.hourlyWage * 1.25f;
		}
		if (isPoached)
		{
			return employeeInstance.poachProposedHourlyWage * 1.25f;
		}
		return employeeInstance.hourlyWage;
	}

	public void AcceptOffer(float hourlyWageAmount, float bonusAmount)
	{
		EmployeeHelper.HireCandidate(employeeInstance);
		employeeInstance.hourlyWage = hourlyWageAmount;
		AddExtraSatisfactionForGoodOffer();
		Dictionary<string, string> data = new Dictionary<string, string> { 
		{
			"employee",
			employeeInstance.characterData.name
		} };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_employeebonus", data);
		transactionInfo.SetTaxDeductibleName("ba:transaction_employeebonus_label");
		GameManager.ChangeMoneySafe(0f - bonusAmount, transactionInfo, null, null, force: true);
		if (isRival)
		{
			BuildingHelper.GetBuildingRegistration(employeeInstance.assignedAddress)?.ReplaceAiBusinessEmployee(employeeInstance.id);
			employeeInstance.assignedAddress = null;
		}
		InstanceBehavior<UIs>.Instance.fullMenu.contactsApp.RefreshHeader();
	}

	private void AddExtraSatisfactionForGoodOffer()
	{
		employeeInstance.satisfaction = Mathf.Min(100f, 50f + ((float)mood - 100f) / 2f);
	}

	public void DeclineOffer()
	{
		EmployeeHelper.DiscardCandidate(employeeInstance);
		if (isRival)
		{
			AiBusinessEmployeeData aiBusinessEmployeeData = BuildingHelper.GetBuildingRegistration(employeeInstance.assignedAddress).aiEmployees.FirstOrDefault((AiBusinessEmployeeData x) => x.id == employeeInstance.id);
			if (aiBusinessEmployeeData != null)
			{
				aiBusinessEmployeeData.isNegotiationFinished = true;
				aiBusinessEmployeeData.reenableNegotiationAtDay = TimeHelper.CurrentDay + 30;
			}
		}
	}
}
