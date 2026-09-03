using Buildings.Office.Headquarters;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA011;

public class NormalizeHeadhunterPlans : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		HeadhunterPlan[] array = gameInstance.headhunterPlans.ToArray();
		foreach (HeadhunterPlan headhunterPlan in array)
		{
			string[] assignedHrPlans = headhunterPlan.assignedHrPlans;
			if (assignedHrPlans != null && assignedHrPlans.Length == 2)
			{
				continue;
			}
			string[] array2 = new string[2];
			if (headhunterPlan.assignedHrPlans != null)
			{
				int num = Mathf.Min(headhunterPlan.assignedHrPlans.Length, 2);
				for (int j = 0; j < num; j++)
				{
					array2[j] = headhunterPlan.assignedHrPlans[j];
				}
			}
			headhunterPlan.assignedHrPlans = array2;
		}
	}
}
