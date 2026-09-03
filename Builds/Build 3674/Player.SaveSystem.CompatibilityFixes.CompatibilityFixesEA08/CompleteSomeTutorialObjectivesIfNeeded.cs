namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class CompleteSomeTutorialObjectivesIfNeeded : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (gameInstance.CompletedQuestEntries.Contains("tutorial_quest_first_hq_objective_3") && !gameInstance.CompletedQuestEntries.Contains("tutorial_quest_first_hq_objective_2"))
		{
			gameInstance.CompletedQuestEntries.Add("tutorial_quest_first_hq_objective_2");
		}
		if (gameInstance.CompletedQuestEntries.Contains("tutorial_quest_purchasing_agent_objective_4") && !gameInstance.CompletedQuestEntries.Contains("tutorial_quest_purchasing_agent_objective_1"))
		{
			gameInstance.CompletedQuestEntries.Add("tutorial_quest_purchasing_agent_objective_1");
		}
		if (gameInstance.CompletedQuestEntries.Contains("tutorial_quest_rent_warehouse_objective_2") && !gameInstance.CompletedQuestEntries.Contains("tutorial_quest_rent_warehouse_objective_1"))
		{
			gameInstance.CompletedQuestEntries.Add("tutorial_quest_rent_warehouse_objective_1");
		}
	}
}
