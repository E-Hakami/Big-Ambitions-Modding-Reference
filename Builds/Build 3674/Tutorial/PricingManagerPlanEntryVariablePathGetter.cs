using Buildings.Office.Headquarters;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/VariablePathGetters/PricingManagerPlanEntryVariablePathGetter")]
public class PricingManagerPlanEntryVariablePathGetter : TutorialPointerVariablePathGetter
{
	[SerializeField]
	private string missingPlanPath = "MissingPricingManagerPlan/Selected/ManagerDropdown";

	[SerializeField]
	private string managerDropdownPath = "/Selected/ManagerDropdown";

	public override string GetVariablePath()
	{
		PricingManagerPlan firstPricingManagerPlan = TutorialPointerHeadquartersPlanHelper.GetFirstPricingManagerPlan();
		if (firstPricingManagerPlan != null)
		{
			return firstPricingManagerPlan.id + managerDropdownPath;
		}
		return missingPlanPath;
	}
}
