using BigAmbitions.Rivals;
using UI.Smartphone.Apps.Rivals;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Rivals/HasCheckedOutFirstMessageRivalInApp")]
public class HasCheckedOutFirstMessageRivalInApp : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		SpecialRival firstMessageRival = RivalsHelper.GetFirstMessageRival();
		if (firstMessageRival == null)
		{
			return false;
		}
		SpecialRivalState specialRivalState = RivalsHelper.GetSpecialRivalState(firstMessageRival.rivalData.id);
		if (specialRivalState != null && specialRivalState.isDefeated)
		{
			return true;
		}
		RivalLeaderboardData rivalLeaderboardData = InstanceBehavior<RivalsApp>.Instance?.selectedRival;
		if (rivalLeaderboardData == null)
		{
			return false;
		}
		return rivalLeaderboardData.rivalId == firstMessageRival.rivalData.id;
	}
}
