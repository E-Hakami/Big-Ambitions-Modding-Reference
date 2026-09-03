using Helpers;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA09;

public class RemoveAFreshStartHappinessModifier : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		if (gameInstance.gameVariables.tutorialEnabled && TutorialHelper.HasCompletedObjective("tutorial_quest_cleaning_objective_3"))
		{
			HappinessHelper.RemoveModifier("ba:happinessmodifier_a_fresh_start");
		}
	}
}
