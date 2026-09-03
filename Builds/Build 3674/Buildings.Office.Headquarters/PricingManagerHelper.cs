using System.Collections.Generic;
using BigAmbitions.Tags;
using Helpers;
using JimmysUnityUtilities;
using UnityEngine;

namespace Buildings.Office.Headquarters;

public static class PricingManagerHelper
{
	public const float CentsPerUnit = 100f;

	private const int HoursPerDay = 24;

	public static PricingManagerSettings Settings => InstanceBehavior<GlobalReferences>.Instance.pricingManagerSettings;

	public static int UpdateHour => Settings.updateHour;

	public static int HoursBetweenUpdates => Settings.hoursBetweenUpdates;

	private static List<PricingManagerPlan> Plans => SaveGameManager.Current.pricingManagerPlans;

	public static int GetHighestAcceptableCents(string itemName, string neighborhood)
	{
		float num = Mathf.Min(ItemHelper.CalculateMaxAcceptablePriceByNeighborhood(itemName, neighborhood), 10000f);
		if (num <= 0f)
		{
			return 0;
		}
		return FloorToCents(num);
	}

	public static int FloorToCents(float price)
	{
		return (int)((double)price * 100.0);
	}

	public static (float min, float max) ComputeSuggestion(string itemName, string neighborhood, float normalizedSkill, float jitter01)
	{
		int highestAcceptableCents = GetHighestAcceptableCents(itemName, neighborhood);
		if (highestAcceptableCents <= 0)
		{
			return (min: 0f, max: 0f);
		}
		int num = Mathf.Clamp(FloorToCents(ItemHelper.GetMarketReferencePrice(itemName, neighborhood)), 0, highestAcceptableCents);
		PricingManagerSettings settings = Settings;
		float num2 = Mathf.Clamp01(normalizedSkill);
		float num3 = Mathf.Clamp01(Mathf.Lerp(settings.minSkillCapture, 1f, num2) * (1f - settings.captureJitter * (1f - num2) * Mathf.Clamp01(jitter01)));
		int num4 = highestAcceptableCents - num;
		int num5 = Mathf.Clamp(num + Mathf.RoundToInt((float)num4 * num3), num, highestAcceptableCents);
		return (min: (float)Mathf.Max(num5 - Mathf.RoundToInt((float)num4 * settings.visibleSpread * (1f - num2)), 0) / 100f, max: (float)num5 / 100f);
	}

	public static bool IsManageableBusiness(BuildingRegistration registration)
	{
		if (registration.RentedByPlayer && registration.retailPrices != null)
		{
			return BusinessTypeHelper.GetData(registration).HasTag(TagRef.Businesstag.generatesrevenue);
		}
		return false;
	}

	public static void RunHourly()
	{
		foreach (PricingManagerPlan plan in Plans)
		{
			if (!plan.assignedEmployeeId.IsNullOrEmpty() && plan.IsUpdateDue())
			{
				plan.RunUpdate();
			}
		}
	}

	public static (int day, int hour) GetNextUpdateTime()
	{
		if (HoursBetweenUpdates < 24)
		{
			int num = TimeHelper.CurrentHour + HoursBetweenUpdates;
			return (day: TimeHelper.CurrentDay + num / 24, hour: num % 24);
		}
		int num2 = ((TimeHelper.CurrentHour >= UpdateHour) ? 1 : 0);
		return (day: TimeHelper.CurrentDay + num2, hour: UpdateHour);
	}

	public static void AddPlan(PricingManagerPlan plan)
	{
		Plans.Add(plan);
	}

	public static bool IsEmployeeAssignedToOtherPlan(string employeeId, string exceptPlanId)
	{
		foreach (PricingManagerPlan plan in Plans)
		{
			if (!(plan.id == exceptPlanId) && plan.assignedEmployeeId == employeeId)
			{
				return true;
			}
		}
		return false;
	}

	public static PricingManagerPlan GetAssignedPlanForPricingManager(string employeeId)
	{
		foreach (PricingManagerPlan plan in Plans)
		{
			if (plan.assignedEmployeeId == employeeId)
			{
				return plan;
			}
		}
		return null;
	}

	public static PricingManagerPlan GetPlanFromId(string planId)
	{
		foreach (PricingManagerPlan plan in Plans)
		{
			if (plan.id == planId)
			{
				return plan;
			}
		}
		return null;
	}

	public static List<PricingManagerPlan> GetPlansForHeadquarters(Address headquartersAddress)
	{
		List<PricingManagerPlan> list = new List<PricingManagerPlan>();
		foreach (PricingManagerPlan plan in Plans)
		{
			if (plan.headquartersAddress == headquartersAddress)
			{
				list.Add(plan);
			}
		}
		return list;
	}

	public static void DeletePlan(string planId)
	{
		GetPlanFromId(planId)?.Delete();
	}

	public static bool IsNeighborhoodSupervised(string neighborhood, string exceptPlanId)
	{
		foreach (PricingManagerPlan plan in Plans)
		{
			if (!(plan.id == exceptPlanId) && plan.supervisedNeighborhood == neighborhood)
			{
				return true;
			}
		}
		return false;
	}

	public static PricingManagerPlan GetPlanCoveringNeighborhood(string neighborhood)
	{
		foreach (PricingManagerPlan plan in Plans)
		{
			if (plan.supervisedNeighborhood == neighborhood && !plan.assignedEmployeeId.IsNullOrEmpty())
			{
				return plan;
			}
		}
		return null;
	}
}
