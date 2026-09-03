namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class FixFurnitureShopTutorialRolledBack : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (gameInstance.CompletedQuestEntries.Contains("44F21BFF-F6C9-438B-AB7C-675E3B548116") && !gameInstance.CompletedQuestEntries.Contains("BFC72C78-A771-4DAF-8930-5D235ADBE33B"))
		{
			gameInstance.CompletedQuestEntries.Add("BFC72C78-A771-4DAF-8930-5D235ADBE33B");
		}
	}
}
