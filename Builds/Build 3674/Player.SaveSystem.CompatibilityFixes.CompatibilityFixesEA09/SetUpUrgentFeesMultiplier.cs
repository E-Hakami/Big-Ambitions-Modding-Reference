namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class SetUpUrgentFeesMultiplier : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		GameVariables gameVariables = gameInstance.gameVariables;
		gameVariables.wholesaleUrgentFeeMultiplier = gameInstance.gameVariables.difficulty switch
		{
			Difficulty.Easy => DifficultySetting.GetDifficultySettings(Difficulty.Easy).wholesaleUrgentFeeMultiplier, 
			Difficulty.Hard => DifficultySetting.GetDifficultySettings(Difficulty.Hard).wholesaleUrgentFeeMultiplier, 
			_ => DifficultySetting.GetDifficultySettings(Difficulty.Normal).wholesaleUrgentFeeMultiplier, 
		};
		gameVariables = gameInstance.gameVariables;
		gameVariables.importerUrgentFeeMultiplier = gameInstance.gameVariables.difficulty switch
		{
			Difficulty.Easy => DifficultySetting.GetDifficultySettings(Difficulty.Easy).importerUrgentFeeMultiplier, 
			Difficulty.Hard => DifficultySetting.GetDifficultySettings(Difficulty.Hard).importerUrgentFeeMultiplier, 
			_ => DifficultySetting.GetDifficultySettings(Difficulty.Normal).importerUrgentFeeMultiplier, 
		};
	}
}
