using JimmysUnityUtilities;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Extenders;

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(LayoutElement))]
public class LayoutElementExtender : MonoBehaviour
{
	[SerializeField]
	private bool setPreferredHeightToHighestChildHeight = true;

	[SerializeField]
	private bool limitWidth;

	[ShowIf("limitWidth")]
	[SerializeField]
	private float widthLimit;

	private LayoutElement _layoutElement;

	private RectTransform _rectTransform;

	private bool _updateQueued;

	private void Awake()
	{
		Init();
	}

	private void OnEnable()
	{
		ScheduleUpdateLayout();
	}

	private void OnDisable()
	{
		_updateQueued = false;
	}

	private void OnRectTransformDimensionsChange()
	{
		ScheduleUpdateLayout();
	}

	private void OnTransformChildrenChanged()
	{
		ScheduleUpdateLayout();
	}

	private void ScheduleUpdateLayout()
	{
		if (!_updateQueued)
		{
			_updateQueued = true;
			CoroutineUtility.RunAfterOneFrame(UpdateLayoutQueued);
		}
	}

	private void UpdateLayoutQueued()
	{
		if ((bool)this)
		{
			_updateQueued = false;
			if (base.isActiveAndEnabled)
			{
				UpdateLayout();
			}
		}
	}

	private void UpdateLayout()
	{
		if (setPreferredHeightToHighestChildHeight)
		{
			SetPreferredHeightToHighestChildHeight();
		}
		if (limitWidth)
		{
			LimitWidth();
		}
	}

	private void SetPreferredHeightToHighestChildHeight()
	{
		if (!Init())
		{
			return;
		}
		float num = 0f;
		for (int i = 0; i < base.transform.childCount; i++)
		{
			if (base.transform.GetChild(i).TryGetComponent<RectTransform>(out var component) && component.rect.height > num)
			{
				num = component.rect.height;
			}
		}
		if (!Mathf.Approximately(_layoutElement.preferredHeight, num))
		{
			_layoutElement.preferredHeight = num;
			SetDirty();
		}
	}

	private void LimitWidth()
	{
		if (Init() && !(_rectTransform.rect.width <= widthLimit))
		{
			_rectTransform.sizeDelta = new Vector2(widthLimit, _rectTransform.sizeDelta.y);
			SetDirty();
		}
	}

	private bool Init()
	{
		if (!_layoutElement)
		{
			_layoutElement = GetComponent<LayoutElement>();
		}
		if (!_rectTransform)
		{
			_rectTransform = GetComponent<RectTransform>();
		}
		if ((bool)_layoutElement)
		{
			return _rectTransform;
		}
		return false;
	}

	private void SetDirty()
	{
		LayoutRebuilder.MarkLayoutForRebuild(_rectTransform);
	}
}
