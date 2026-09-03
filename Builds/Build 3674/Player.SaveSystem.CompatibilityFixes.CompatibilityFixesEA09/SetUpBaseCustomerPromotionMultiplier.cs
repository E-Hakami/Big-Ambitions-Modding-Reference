namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class SetUpBaseCustomerPromotionMultiplier : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (!(gameInstance.gameVariables.baseCustomerPromotionMultiplier > 0f))
		{
			GameVariables gameVariables = gameInstance.gameVariables;
			gameVariables.baseCustomerPromotionMultiplier = gameInstance.gameVariables.difficulty switch
			{
				Difficulty.Easy => DifficultySetting.GetDifficultySettings(Difficulty.Easy).baseCustomerPromotionMultiplier, 
				Difficulty.Hard => DifficultySetting.GetDifficultySettings(Difficulty.Hard).baseCustomerPromotionMultiplier, 
				_ => DifficultySetting.GetDifficultySettings(Difficulty.Normal).baseCustomerPromotionMultiplier, 
			};
		}
	}
}
