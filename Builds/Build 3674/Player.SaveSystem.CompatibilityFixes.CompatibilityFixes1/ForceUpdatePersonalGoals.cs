using System.Collections.Generic;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixes1;

public class ForceUpdatePersonalGoals : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		List<GenericPersonalGoal> personalGoals = InstanceBehavior<GameManager>.Instance.personalGoals;
		for (int i = 0; i < personalGoals.Count; i++)
		{
			GenericPersonalGoal genericPersonalGoal = personalGoals[i];
			if (!gameInstance.completedPersonalGoals.Contains(genericPersonalGoal.identifier))
			{
				genericPersonalGoal.CheckForCompletion();
			}
		}
	}
}
