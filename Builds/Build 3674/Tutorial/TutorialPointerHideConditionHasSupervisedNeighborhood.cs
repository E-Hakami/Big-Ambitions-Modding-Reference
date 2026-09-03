using Buildings.Office.Headquarters;
using JimmysUnityUtilities;
using UI;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/HasSupervisedNeighborhood")]
public class TutorialPointerHideConditionHasSupervisedNeighborhood : TutorialPointerHideCondition
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
			if (pricingManagerPlan.headquartersAddress == address && !pricingManagerPlan.supervisedNeighborhood.IsNullOrEmpty())
			{
				return true;
			}
		}
		return false;
	}
}
