using System;
using UI;
using UnityEngine;
using UnityEngine.UI;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/UIElement/UiElementCityMapFilter")]
public class TutorialPointerDataUiElementCityMapFilter : TutorialPointerDataUiElement
{
	[SerializeField]
	private float yScrollOffset;

	[SerializeField]
	private string[] categoriesToUncollapse;

	public override void Init()
	{
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, new Action<bool>(OnCityMapToggle));
	}

	public override void Dispose()
	{
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Remove(GlobalEvents.onCityMapToggle, new Action<bool>(OnCityMapToggle));
	}

	private void OnCityMapToggle(bool isOpen)
	{
		if (!isOpen)
		{
			return;
		}
		CityMapFilters mapFilters = InstanceBehavior<UIs>.Instance.mapFilters;
		mapFilters.ResetSearchText();
		string[] array = categoriesToUncollapse;
		foreach (string text in array)
		{
			if (SaveGameManager.Current.CollapsedCitymapFilterCategories.Contains(text))
			{
				mapFilters.GetCategory(text).OnCollapseClick();
			}
		}
		RectTransform rectTransform = GetUiElementTarget();
		ScrollRect componentInParent = rectTransform.GetComponentInParent<ScrollRect>();
		if (componentInParent != null)
		{
			ScrollToShow(componentInParent, rectTransform);
		}
	}

	private void ScrollToShow(ScrollRect scrollRect, RectTransform target)
	{
		Canvas.ForceUpdateCanvases();
		RectTransform rectTransform = ((scrollRect.viewport != null) ? scrollRect.viewport : ((RectTransform)scrollRect.transform));
		float num = scrollRect.content.rect.height - rectTransform.rect.height;
		if (!(num <= 0f))
		{
			float num2 = RectTransformUtility.CalculateRelativeRectTransformBounds(rectTransform, target).center.y - rectTransform.rect.center.y + yScrollOffset;
			scrollRect.velocity = Vector2.zero;
			scrollRect.verticalNormalizedPosition = Mathf.Clamp01(scrollRect.verticalNormalizedPosition + num2 / num);
		}
	}
}
