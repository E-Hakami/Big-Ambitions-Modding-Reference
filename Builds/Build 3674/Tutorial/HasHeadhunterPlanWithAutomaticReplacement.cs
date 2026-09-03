using System.Linq;
using Buildings.Office.Headquarters;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Headquarters/HasHeadhunterPlanWithAutomaticReplacement")]
public class HasHeadhunterPlanWithAutomaticReplacement : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		return SaveGameManager.Current.headhunterPlans.Any((HeadhunterPlan x) => x.automaticallyReplaceOnResign || x.automaticallyReplaceOnRetire);
	}
}
