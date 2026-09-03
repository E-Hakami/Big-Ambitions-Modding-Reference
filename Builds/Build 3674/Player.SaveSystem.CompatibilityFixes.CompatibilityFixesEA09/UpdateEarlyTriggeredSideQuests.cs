using System.Collections.Generic;
using Tutorial.SideQuests;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class UpdateEarlyTriggeredSideQuests : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		GameInstance gameInstance2 = gameInstance;
		if (gameInstance2.completedSideQuestEntries == null)
		{
			gameInstance2.completedSideQuestEntries = new List<string>();
		}
		gameInstance2 = gameInstance;
		if (gameInstance2.activeSideQuestEntries == null)
		{
			gameInstance2.activeSideQuestEntries = new List<string>();
		}
		foreach (SideQuest activeSideQuest in SideQuestHelper.GetActiveSideQuests())
		{
			if (!activeSideQuest.CheckIfInitiationCompleted())
			{
				activeSideQuest.Deactivate();
			}
		}
		GameEvent.Invoke("ba:gameevent_rivalsentmessage");
	}
}
