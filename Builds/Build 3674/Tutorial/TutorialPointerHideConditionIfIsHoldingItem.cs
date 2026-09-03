using Helpers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/IsHoldingItem")]
public class TutorialPointerHideConditionIfIsHoldingItem : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		return PlayerHelper.IsHoldingItem;
	}
}
