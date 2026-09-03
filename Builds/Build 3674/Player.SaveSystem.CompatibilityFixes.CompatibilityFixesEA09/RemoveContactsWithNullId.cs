using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class RemoveContactsWithNullId : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.Contacts.RemoveAll((Contact x) => x.id == null);
	}
}
