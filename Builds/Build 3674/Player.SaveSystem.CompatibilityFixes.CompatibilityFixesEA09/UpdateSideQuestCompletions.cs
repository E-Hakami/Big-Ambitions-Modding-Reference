using System.Collections.Generic;
using Tutorial.SideQuests;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class UpdateSideQuestCompletions : ICompatibilityFix
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
		foreach (SideQuest allQuest in SideQuestHelper.AllQuests)
		{
			allQuest.CheckCompatCompletion(gameInstance);
		}
	}
}
