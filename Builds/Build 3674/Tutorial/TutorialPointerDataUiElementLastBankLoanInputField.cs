using System;
using UI;
using UI.Dialog;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/UIElement/LastBankLoanInputField")]
public class TutorialPointerDataUiElementLastBankLoanInputField : TutorialPointerDataUiElement
{
	private Transform _content;

	protected override RectTransform GetUiElementTarget()
	{
		if (_content == null)
		{
			DialogUI dialogUI = InstanceBehavior<UIs>.Instance.playerHUD.dialogUI;
			_content = dialogUI.transform.Find(uiPath);
		}
		if (!_content.gameObject.activeInHierarchy)
		{
			return null;
		}
		BankLoanSettings[] componentsInChildren = _content.GetComponentsInChildren<BankLoanSettings>();
		if (componentsInChildren.Length == 0)
		{
			return null;
		}
		Array.Sort(componentsInChildren, (BankLoanSettings a, BankLoanSettings b) => b.transform.parent.parent.GetSiblingIndex().CompareTo(a.transform.parent.parent.GetSiblingIndex()));
		uiElementTarget = componentsInChildren[0].amountInput.GetComponent<RectTransform>();
		return uiElementTarget;
	}
}
