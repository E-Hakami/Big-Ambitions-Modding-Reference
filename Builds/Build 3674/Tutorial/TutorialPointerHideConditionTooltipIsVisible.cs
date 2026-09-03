using Tooltip;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/TooltipIsVisible")]
public class TutorialPointerHideConditionTooltipIsVisible : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		return TooltipSystem.IsVisible;
	}
}
