using System.Linq;
using BigAmbitions.Rivals;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class ShutdownBusinessesFromRivalsAlreadyDefeated : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		RivalsHelper.FillData(gameInstance.rivalStates.Select((RivalState x) => x.rivalId).ToList());
		foreach (SpecialRivalState specialRivalState in gameInstance.specialRivalStates)
		{
			if (specialRivalState.isDefeated)
			{
				RivalsHelper.OnRivalDefeat(RivalsHelper.GetSpecialRivals().First((SpecialRival x) => x.rivalData.id == specialRivalState.rivalId).rivalData);
			}
		}
	}
}
