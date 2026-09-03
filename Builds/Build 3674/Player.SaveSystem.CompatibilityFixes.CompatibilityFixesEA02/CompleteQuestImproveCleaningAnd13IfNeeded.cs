namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA02;

public class CompleteQuestImproveCleaningAnd13IfNeeded : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		string item = "CD6BBF02-A6B6-4E3E-A3C4-F7D02FED051A";
		if (gameInstance.CompletedQuestEntries.Contains(item))
		{
			string item2 = "7DEAFJuke0qTvUcmOkkMjw==";
			gameInstance.CompletedQuestEntries.Add(item2);
		}
		string item3 = "ea1803ec2e4a427193d87b36ade9ab04";
		if (gameInstance.CompletedQuestEntries.Contains(item3))
		{
			string item4 = "vReeqwwli06NaoHsukc2Hg==";
			gameInstance.CompletedQuestEntries.Add(item4);
		}
	}
}
