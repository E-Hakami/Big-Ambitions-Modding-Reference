using Buildings.Office.Headquarters;
using JimmysUnityUtilities;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Headquarters/HasPricingManagerPlan")]
public class HasPricingManagerPlan : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		foreach (PricingManagerPlan pricingManagerPlan in SaveGameManager.Current.pricingManagerPlans)
		{
			if (!pricingManagerPlan.assignedEmployeeId.IsNullOrEmpty() && !pricingManagerPlan.supervisedNeighborhood.IsNullOrEmpty())
			{
				return true;
			}
		}
		return false;
	}
}
