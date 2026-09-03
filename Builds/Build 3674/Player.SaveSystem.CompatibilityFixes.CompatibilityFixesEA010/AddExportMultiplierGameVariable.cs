namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class AddExportMultiplierGameVariable : ICompatibilityFix
{
	private float _money;

	public void Apply(GameInstance gameInstance)
	{
		GameVariables gameVariables = gameInstance.gameVariables;
		gameVariables.exportMultiplier = gameInstance.gameVariables.difficulty switch
		{
			Difficulty.Easy => DifficultySetting.GetDifficultySettings(Difficulty.Easy).exportMultiplier, 
			Difficulty.Hard => DifficultySetting.GetDifficultySettings(Difficulty.Hard).exportMultiplier, 
			_ => DifficultySetting.GetDifficultySettings(Difficulty.Normal).exportMultiplier, 
		};
	}
}
