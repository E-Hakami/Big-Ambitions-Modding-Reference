using BigAmbitions.Rivals;
using UI.Smartphone.Apps.Rivals;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Rivals/HasCheckedOutAttackingRivalInApp")]
public class HasCheckedOutAttackingRivalInApp : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		SpecialRival firstAttackRival = RivalsHelper.GetFirstAttackRival();
		if (firstAttackRival == null)
		{
			return true;
		}
		SpecialRivalState specialRivalState = RivalsHelper.GetSpecialRivalState(firstAttackRival.rivalData.id);
		if (specialRivalState != null && specialRivalState.isDefeated)
		{
			return true;
		}
		RivalLeaderboardData rivalLeaderboardData = InstanceBehavior<RivalsApp>.Instance?.selectedRival;
		if (rivalLeaderboardData == null)
		{
			return false;
		}
		return rivalLeaderboardData.rivalId == firstAttackRival.rivalData.id;
	}
}
