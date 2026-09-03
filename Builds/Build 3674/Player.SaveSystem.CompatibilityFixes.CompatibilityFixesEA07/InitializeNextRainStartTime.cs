using BigAmbitions.DayNightCycle;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class InitializeNextRainStartTime : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.nextRainStartTime = new Timestamp();
	}
}
