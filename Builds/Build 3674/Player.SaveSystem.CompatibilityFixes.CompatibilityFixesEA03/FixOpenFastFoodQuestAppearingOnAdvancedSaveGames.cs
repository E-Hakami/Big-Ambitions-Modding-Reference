namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA03;

public class FixOpenFastFoodQuestAppearingOnAdvancedSaveGames : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		string item = "ea1803ec2e4a427193d87b36ade9ab04";
		if (gameInstance.CompletedQuestEntries.Contains(item))
		{
			string item2 = "hulx8enTj0+IOtqGlZCF6w==";
			gameInstance.CompletedQuestEntries.Add(item2);
		}
	}
}
