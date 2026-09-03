using BigAmbitions.Rivals;
using UI.Smartphone.Apps.Rivals;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Rivals/HasCheckedOutActiveRivalInApp")]
public class HasCheckedOutActiveRivalInApp : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		SpecialRival firstActiveRival = RivalsHelper.GetFirstActiveRival();
		if (firstActiveRival == null)
		{
			return true;
		}
		RivalLeaderboardData rivalLeaderboardData = InstanceBehavior<RivalsApp>.Instance?.selectedRival;
		if (rivalLeaderboardData == null)
		{
			return false;
		}
		return rivalLeaderboardData.rivalId == firstActiveRival.rivalData.id;
	}
}
