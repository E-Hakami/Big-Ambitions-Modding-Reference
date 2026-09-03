using System.Collections.Generic;
using BigAmbitions.Rivals;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Rivals/HasFirstRivalAttack")]
public class HasFirstRivalAttack : QuestRequirement
{
	private bool _hasFirstAttackRivalFlag;

	public override List<string> ChangesToCheckOn => new List<string> { "ba:gameevent_rivalsentmessage" };

	public override bool CheckIfCompleted()
	{
		if (_hasFirstAttackRivalFlag)
		{
			return true;
		}
		SpecialRival firstAttackRival = RivalsHelper.GetFirstAttackRival();
		_hasFirstAttackRivalFlag = firstAttackRival != null;
		return _hasFirstAttackRivalFlag;
	}
}
