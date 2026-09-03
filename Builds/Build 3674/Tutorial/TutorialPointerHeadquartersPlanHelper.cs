using System.Collections.Generic;
using Buildings.Office.Headquarters;
using Streets;
using UI;

namespace Tutorial;

public static class TutorialPointerHeadquartersPlanHelper
{
	public static LogisticsManagerPlan GetFirstLogisticsManagerPlan()
	{
		Address currentHeadquartersAddress = GetCurrentHeadquartersAddress();
		if (currentHeadquartersAddress.IsUndefined())
		{
			return null;
		}
		List<LogisticsManagerPlan> logisticsManagerPlans = SaveGameManager.Current.logisticsManagerPlans;
		for (int i = 0; i < logisticsManagerPlans.Count; i++)
		{
			LogisticsManagerPlan logisticsManagerPlan = logisticsManagerPlans[i];
			if (logisticsManagerPlan.headquartersAddress == currentHeadquartersAddress)
			{
				return logisticsManagerPlan;
			}
		}
		return null;
	}

	public static PricingManagerPlan GetFirstPricingManagerPlan()
	{
		Address currentHeadquartersAddress = GetCurrentHeadquartersAddress();
		if (currentHeadquartersAddress.IsUndefined())
		{
			return null;
		}
		List<PricingManagerPlan> pricingManagerPlans = SaveGameManager.Current.pricingManagerPlans;
		for (int i = 0; i < pricingManagerPlans.Count; i++)
		{
			PricingManagerPlan pricingManagerPlan = pricingManagerPlans[i];
			if (pricingManagerPlan.headquartersAddress == currentHeadquartersAddress)
			{
				return pricingManagerPlan;
			}
		}
		return null;
	}

	private static Address GetCurrentHeadquartersAddress()
	{
		if (InstanceBehavior<UIs>.Instance == null || InstanceBehavior<UIs>.Instance.fullMenu == null || InstanceBehavior<UIs>.Instance.fullMenu.bizMan == null || InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business == null)
		{
			return null;
		}
		return InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address;
	}
}
