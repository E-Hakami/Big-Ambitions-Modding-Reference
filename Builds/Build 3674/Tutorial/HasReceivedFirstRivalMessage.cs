using System.Collections.Generic;
using BigAmbitions.Rivals;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Rivals/HasReceivedFirstRivalMessage")]
public class HasReceivedFirstRivalMessage : QuestRequirement
{
	public override List<string> ChangesToCheckOn => new List<string> { "ba:gameevent_rivalsentmessage" };

	public override bool CheckIfCompleted()
	{
		return RivalsHelper.GetFirstMessageRival() != null;
	}
}
