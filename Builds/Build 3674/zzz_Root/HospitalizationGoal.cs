using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/Hospitalization Goal")]
public class HospitalizationGoal : IntBaseGoal
{
	protected override int GetValue()
	{
		return SaveGameManager.Current.achievementsData.hospitalization;
	}
}
