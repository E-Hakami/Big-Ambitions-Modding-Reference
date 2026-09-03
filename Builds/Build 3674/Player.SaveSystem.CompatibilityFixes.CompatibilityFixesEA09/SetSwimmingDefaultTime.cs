namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class SetSwimmingDefaultTime : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.PlayerDefaults.swimmingMinutes = 30;
	}
}
