namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA02;

public class UpdateQuests1213And17 : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		string item = "75A5C995-8C6D-4DEE-9C71-6B85C9FFB933";
		if (gameInstance.CompletedQuestEntries.Contains(item))
		{
			string item2 = "55E01B5E-3774-4344-8FEA-F2FD3A364FD0";
			string item3 = "6171FD4B-0AC5-40A2-882A-13640A287AA9";
			string item4 = "CD6BBF02-A6B6-4E3E-A3C4-F7D02FED051A";
			gameInstance.CompletedQuestEntries.Add(item2);
			gameInstance.CompletedQuestEntries.Add(item3);
			gameInstance.CompletedQuestEntries.Add(item4);
		}
		string item5 = "SdcEteUAK02vE+aqik+uvA==";
		if (gameInstance.CompletedQuestEntries.Contains(item5))
		{
			string item6 = "4531C6CD-7B88-4CB4-885C-EFE7FCB036CE";
			gameInstance.CompletedQuestEntries.Add(item6);
		}
		string item7 = "90F91EA4-71D7-49E2-A1FB-50A8B48F43E5";
		if (gameInstance.CompletedQuestEntries.Contains(item7))
		{
			string item8 = "A00BB514-4ABA-48BD-B6BA-0DA088987AB9";
			gameInstance.CompletedQuestEntries.Add(item8);
		}
	}
}
