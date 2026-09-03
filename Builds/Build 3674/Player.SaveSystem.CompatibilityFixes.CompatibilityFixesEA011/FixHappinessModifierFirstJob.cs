using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA011;

public class FixHappinessModifierFirstJob : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (HappinessModifierData happinessModifier in gameInstance.happinessModifiers)
		{
			if (happinessModifier.type == "ba:happinessmodifier_first_job")
			{
				happinessModifier.type = "ba:happinessmodifier_firstjob";
			}
			else if (happinessModifier.type == "ba:happinessmodifier_watchingshow")
			{
				happinessModifier.type = "ba:happinessmodifier_watching_show";
			}
		}
	}
}
