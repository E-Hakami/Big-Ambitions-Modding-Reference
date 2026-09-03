namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;

public class CompleteResearchStoreObjectiveIfQuestAlreadyFinished : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (gameInstance.CompletedQuestEntries.Contains("tutorial_quest_setup_store_objective_5") && !gameInstance.CompletedQuestEntries.Contains("tutorial_quest_setup_store_objective_research"))
		{
			gameInstance.CompletedQuestEntries.Add("tutorial_quest_setup_store_objective_research");
		}
	}
}
