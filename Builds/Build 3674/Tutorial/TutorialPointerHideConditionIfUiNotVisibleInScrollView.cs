using NaughtyAttributes;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/UiNotVisibleInScrollView")]
public class TutorialPointerHideConditionIfUiNotVisibleInScrollView : TutorialPointerHideCondition
{
	[SerializeField]
	private string pathScrollView;

	[SerializeField]
	private bool useVariableGetter;

	[SerializeField]
	[ShowIf("useVariableGetter")]
	private TutorialPointerVariablePathGetter variablePathGetter;

	[SerializeField]
	[HideIf("useVariableGetter")]
	private string pathScrollViewElement;

	private RectTransform _scrollView;

	private RectTransform _scrollViewElement;

	protected override bool ConditionMetInternal()
	{
		if (_scrollViewElement == null)
		{
			if (_scrollView == null)
			{
				_scrollView = TutorialPointersManager.FindUiRectByPath(pathScrollView);
			}
			string n = (useVariableGetter ? variablePathGetter.GetVariablePath() : pathScrollViewElement);
			_scrollViewElement = _scrollView?.Find(n)?.GetComponent<RectTransform>();
		}
		if (_scrollViewElement == null)
		{
			return false;
		}
		Bounds bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(_scrollView, _scrollViewElement);
		if (_scrollView.rect.Contains(bounds.min))
		{
			return !_scrollView.rect.Contains(bounds.max);
		}
		return true;
	}
}
