using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/UIElement/EconoViewLoanPaymentButton")]
public class TutorialPointerDataEconoViewLoanPaymentButton : TutorialPointerDataUiElement
{
	private RectTransform _loansContainerTarget;

	protected override RectTransform GetUiElementTarget()
	{
		if (uiElementTarget != null && uiElementTarget.gameObject.activeInHierarchy)
		{
			return uiElementTarget;
		}
		uiElementTarget = null;
		if (_loansContainerTarget == null)
		{
			_loansContainerTarget = GetStaticUiElementTarget();
		}
		if (_loansContainerTarget == null)
		{
			Debug.LogError("No UI element found on static path '" + uiPath + "' (" + base.name + ")", this);
			return null;
		}
		if (!_loansContainerTarget.gameObject.activeInHierarchy)
		{
			return null;
		}
		LoanEntryUi[] componentsInChildren = _loansContainerTarget.GetComponentsInChildren<LoanEntryUi>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			if (componentsInChildren[i].gameObject.activeInHierarchy)
			{
				uiElementTarget = componentsInChildren[i].PayOffButtonTarget;
				return uiElementTarget;
			}
		}
		return null;
	}
}
