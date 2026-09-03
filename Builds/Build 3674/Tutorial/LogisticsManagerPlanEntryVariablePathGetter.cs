using Buildings.Office.Headquarters;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/VariablePathGetters/LogisticsManagerPlanEntryVariablePathGetter")]
public class LogisticsManagerPlanEntryVariablePathGetter : TutorialPointerVariablePathGetter
{
	[SerializeField]
	private string missingPlanPath = "MissingLogisticsManagerPlan/Selected/ManagerDropdown";

	[SerializeField]
	private string managerDropdownPath = "/Selected/ManagerDropdown";

	public override string GetVariablePath()
	{
		LogisticsManagerPlan firstLogisticsManagerPlan = TutorialPointerHeadquartersPlanHelper.GetFirstLogisticsManagerPlan();
		if (firstLogisticsManagerPlan != null)
		{
			return firstLogisticsManagerPlan.id + managerDropdownPath;
		}
		return missingPlanPath;
	}
}
