using PlayerActivity;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/IsPlayerActivityUIOpen")]
public class TutorialPointerHideConditionIfPlayerActivityUIOpen : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		return PlayerActivityUI.IsPanelOpen;
	}
}
