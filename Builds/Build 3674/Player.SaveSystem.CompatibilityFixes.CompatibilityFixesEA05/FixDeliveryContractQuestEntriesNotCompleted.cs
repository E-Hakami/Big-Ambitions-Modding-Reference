namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class FixDeliveryContractQuestEntriesNotCompleted : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		string item = "90F91EA4-71D7-49E2-A1FB-50A8B48F43E5";
		if (gameInstance.CompletedQuestEntries.Contains(item))
		{
			string item2 = "84afSwzv0+m1yFMtNBsgw==";
			gameInstance.CompletedQuestEntries.Add(item2);
		}
	}
}
