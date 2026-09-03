using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/UiTextEquals")]
public class TutorialPointerHideConditionIfTextEquals : TutorialPointerHideCondition
{
	[SerializeField]
	private string staticUiInputPath;

	[SerializeField]
	private string dynamicUiInputPath;

	[SerializeField]
	private bool useDynamicValueGetter;

	[SerializeField]
	[ShowIf("useDynamicValueGetter")]
	private TutorialPointerVariablePathGetter dynamicValueGetter;

	[SerializeField]
	[HideIf("useDynamicValueGetter")]
	private string value;

	private RectTransform _uiPermanentElementTarget;

	private TMP_Text _uiInputField;

	private TMP_Text GetUiTextTarget()
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
		_uiInputField = (string.IsNullOrEmpty(dynamicUiInputPath) ? _uiPermanentElementTarget.GetComponent<TMP_Text>() : _uiPermanentElementTarget.Find(dynamicUiInputPath)?.GetComponent<TMP_Text>());
		return _uiInputField;
	}

	protected override bool ConditionMetInternal()
	{
		string text = GetUiTextTarget()?.text ?? string.Empty;
		string text2 = (useDynamicValueGetter ? dynamicValueGetter.GetVariablePath() : value);
		if (!string.IsNullOrEmpty(text))
		{
			return text.Equals(text2, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}
}
