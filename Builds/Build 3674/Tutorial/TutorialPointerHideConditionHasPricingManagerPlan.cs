using Buildings.Office.Headquarters;
using UI;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/HasPricingManagerPlan")]
public class TutorialPointerHideConditionHasPricingManagerPlan : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		if (InstanceBehavior<UIs>.Instance?.fullMenu?.bizMan?.business == null)
		{
			return false;
		}
		Address address = InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.address;
		foreach (PricingManagerPlan pricingManagerPlan in SaveGameManager.Current.pricingManagerPlans)
		{
			if (pricingManagerPlan.headquartersAddress == address)
			{
				return true;
			}
		}
		return false;
	}
}
