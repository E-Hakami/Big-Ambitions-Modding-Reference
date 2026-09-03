namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA02;

public class CompleteDeliveryContractQuestIfNeeded : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		string item = "A00BB514-4ABA-48BD-B6BA-0DA088987AB9";
		if (gameInstance.CompletedQuestEntries.Contains(item))
		{
			string item2 = "748DD487-C682-43F1-B117-FF69351B20D0";
			string item3 = "e3fScZMGHUiOX5+bph1zAQ==";
			string item4 = "84afSwzv0+m1yFMtNBsgw==";
			gameInstance.CompletedQuestEntries.Add(item2);
			gameInstance.CompletedQuestEntries.Add(item3);
			gameInstance.CompletedQuestEntries.Add(item4);
		}
	}
}
