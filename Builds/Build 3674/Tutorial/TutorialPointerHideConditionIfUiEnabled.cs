using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/UiEnabled")]
public class TutorialPointerHideConditionIfUiEnabled : TutorialPointerHideCondition
{
	[SerializeField]
	protected string uiPath;

	[SerializeField]
	protected string uiVariablePath;

	private RectTransform _uiElementTarget;

	private RectTransform _uiPermanentElementTarget;

	protected virtual RectTransform GetUiElementTarget()
	{
		if (_uiElementTarget != null)
		{
			return _uiElementTarget;
		}
		if (_uiPermanentElementTarget == null)
		{
			_uiPermanentElementTarget = TutorialPointersManager.FindUiRectByPath(uiPath);
		}
		if (_uiPermanentElementTarget == null || !_uiPermanentElementTarget.gameObject.activeInHierarchy)
		{
			return null;
		}
		_uiElementTarget = (string.IsNullOrEmpty(uiVariablePath) ? _uiPermanentElementTarget : _uiPermanentElementTarget.Find(uiVariablePath)?.GetComponent<RectTransform>());
		return _uiElementTarget;
	}

	protected override bool ConditionMetInternal()
	{
		if (GetUiElementTarget() != null)
		{
			return GetUiElementTarget().gameObject.activeInHierarchy;
		}
		return false;
	}
}
