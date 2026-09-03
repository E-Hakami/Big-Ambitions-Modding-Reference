using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA04;

public class UpdateHappinessModifiers : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (HappinessModifierData happinessModifier in gameInstance.happinessModifiers)
		{
			happinessModifier.hideDuration = HappinessHelper.GetHappinessModifierFromType(happinessModifier.type)?.hideDuration ?? false;
		}
	}
}
