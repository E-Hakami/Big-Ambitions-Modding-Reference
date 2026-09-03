using UI.Components;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/UiTextIsValue")]
public class TutorialPointerHideConditionIfUiInputFieldIsValue : TutorialPointerHideCondition
{
	[SerializeField]
	private string staticUiInputPath;

	[SerializeField]
	private string dynamicUiInputPath;

	[SerializeField]
	private string value;

	[SerializeField]
	private bool valueHasToMatch = true;

	private RectTransform _uiPermanentElementTarget;

	private InputField _uiInputField;

	private InputField GetUiInputFieldTarget()
	{
		if (_uiInputField != null)
		{
			return _uiInputField;
		}
		if (_uiPermanentElementTarget == null)
		{
			_uiPermanentElementTarget = TutorialPointersManager.FindUiRectByPath(staticUiInputPath);
		}
		if (_uiPermanentElementTarget == null || !_uiPermanentElementTarget.gameObject.activeInHierarchy)
		{
			return null;
		}
		_uiInputField = (string.IsNullOrEmpty(dynamicUiInputPath) ? _uiPermanentElementTarget.GetComponent<InputField>() : _uiPermanentElementTarget.Find(dynamicUiInputPath)?.GetComponent<InputField>());
		return _uiInputField;
	}

	protected override bool ConditionMetInternal()
	{
		if (GetUiInputFieldTarget() != null)
		{
			return valueHasToMatch == (GetUiInputFieldTarget().GetRawValue() == value);
		}
		return false;
	}
}
