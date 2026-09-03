using UI.ItemPanel;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/IsItemPanelClosed")]
public class TutorialPointerHideConditionIfItemPanelIsClosed : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		return !ItemPanelUI.IsVisible;
	}
}
