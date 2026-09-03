using System.Linq;
using BigAmbitions.Rivals;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class FixReappearingDefeatedRivals : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (SpecialRivalState item in gameInstance.specialRivalStates.Where((SpecialRivalState specialRivalState) => specialRivalState.isDefeated))
		{
			RivalsHelper.OnRivalDefeat(RivalsHelper.GetRivalData(item.rivalId));
		}
	}
}
