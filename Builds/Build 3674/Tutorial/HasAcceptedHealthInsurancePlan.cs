using System.Linq;
using Buildings.Office.Headquarters;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Hospital/HasAcceptedHealthInsurancePlan")]
public class HasAcceptedHealthInsurancePlan : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		return SaveGameManager.Current.hrManagerPlans.Any((HrManagerPlan x) => x.healthInsurancePlan != null);
	}
}
