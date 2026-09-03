using System.Collections.Generic;
using BigAmbitions.Rivals;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA05;

public class GenerateRivals : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.specialRivalStates = new List<SpecialRivalState>();
		RivalsHelper.GenerateRivals(gameInstance);
		gameInstance.gameVariables.rivalsDifficultyMultiplier = ((gameInstance.gameVariables.difficulty == Difficulty.Custom) ? 1f : DifficultySetting.GetDifficultySettings(gameInstance.gameVariables.difficulty).rivalsDifficultyMultiplier);
	}
}
