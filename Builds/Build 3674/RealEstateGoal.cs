using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/PersonalGoals/RealEstateGoal")]
public class RealEstateGoal : IntBaseGoal
{
	protected override int GetValue()
	{
		return SaveGameManager.Current.realEstate.Count;
	}
}
