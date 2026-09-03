using NaughtyAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/UIElement/UiElementWithVariablePath")]
public class TutorialPointerDataUiElementWithVariablePath : TutorialPointerDataUiElement
{
	[SerializeField]
	private bool useVariableGetter;

	[HideIf("useVariableGetter")]
	[SerializeField]
	protected string uiVariablePath;

	[ShowIf("useVariableGetter")]
	[SerializeField]
	protected TutorialPointerVariablePathGetter variablePathGetter;

	private RectTransform _uiPermanentElementTarget;

	protected override RectTransform GetUiElementTarget()
	{
		if (uiElementTarget != null)
		{
			return uiElementTarget;
		}
		if (_uiPermanentElementTarget == null)
		{
			_uiPermanentElementTarget = GetStaticUiElementTarget();
		}
		if (_uiPermanentElementTarget == null)
		{
			Debug.LogError("No UI element found on static path '" + uiPath + "' (" + base.name + ")", this);
			return null;
		}
		if (!_uiPermanentElementTarget.gameObject.activeInHierarchy)
		{
			return null;
		}
		string n = (useVariableGetter ? variablePathGetter.GetVariablePath() : uiVariablePath);
		Transform transform = _uiPermanentElementTarget.Find(n);
		if (transform != null)
		{
			uiElementTarget = transform.GetComponent<RectTransform>();
		}
		return uiElementTarget;
	}
}
