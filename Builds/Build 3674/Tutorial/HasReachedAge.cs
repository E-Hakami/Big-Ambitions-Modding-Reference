using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Persona/HasReachedAge")]
public class HasReachedAge : QuestRequirement
{
	public int age;

	public override bool CheckIfCompleted()
	{
		return TimeHelper.GetYearsByDays(PlayerHelper.CharacterData.ageInDays) >= age;
	}
}
