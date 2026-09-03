using System.Collections.Generic;
using BigAmbitions.Rivals;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Rivals/IsAnyRivalActive")]
public class IsAnyRivalActive : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		List<SpecialRival> activeSpecialRivals = RivalsHelper.GetActiveSpecialRivals();
		if (activeSpecialRivals != null)
		{
			return activeSpecialRivals.Count > 0;
		}
		return false;
	}
}
