using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Persona/HasHappinessModifier")]
public class HasHappinessModifier : QuestRequirement
{
	public string type;

	public override bool CheckIfCompleted()
	{
		foreach (HappinessModifierData happinessModifier in SaveGameManager.Current.happinessModifiers)
		{
			if (happinessModifier.type == type)
			{
				return true;
			}
		}
		return false;
	}
}
