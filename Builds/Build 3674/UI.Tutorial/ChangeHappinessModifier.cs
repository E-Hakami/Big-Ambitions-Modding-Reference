using Helpers;
using Tutorial;
using UnityEngine;

namespace UI.Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Quest Actions/ChangeHappinessModifier")]
public class ChangeHappinessModifier : QuestAction
{
	public string happinessModifierType;

	public bool remove;

	public override void Do()
	{
		if (remove)
		{
			HappinessHelper.RemoveModifier(happinessModifierType);
		}
		else
		{
			HappinessHelper.AddModifier(happinessModifierType);
		}
	}
}
