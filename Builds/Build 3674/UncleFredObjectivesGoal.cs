using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/UncleFredObjectivesGoal")]
public class UncleFredObjectivesGoal : IntBaseGoal
{
	protected override int GetValue()
	{
		return SaveGameManager.Current.CompletedQuestEntries.Count;
	}
}
