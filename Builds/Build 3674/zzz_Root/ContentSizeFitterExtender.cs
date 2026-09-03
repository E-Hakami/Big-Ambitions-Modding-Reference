using UnityEngine;
using UnityEngine.UI;

public class ContentSizeFitterExtender : ContentSizeFitter
{
	public float maxWidth;

	public float maxHeight;

	public bool expandNoFurtherThanParentBounds;

	private RectTransform _parentRectTransform;

	private RectTransform _rectTransform;

	public override void SetLayoutHorizontal()
	{
		base.SetLayoutHorizontal();
		if (Init())
		{
			float num = _rectTransform.rect.width;
			if (maxWidth > 0f && num > maxWidth)
			{
				num = maxWidth;
			}
			if (expandNoFurtherThanParentBounds && TryGetParentSize(RectTransform.Axis.Horizontal, out var size) && num > size)
			{
				num = size;
			}
			if (!Mathf.Approximately(num, _rectTransform.rect.width))
			{
				_rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, num);
				SetDirty();
			}
		}
	}

	public override void SetLayoutVertical()
	{
		base.SetLayoutVertical();
		if (Init())
		{
			float num = _rectTransform.rect.height;
			if (maxHeight > 0f && num > maxHeight)
			{
				num = maxHeight;
			}
			if (expandNoFurtherThanParentBounds && TryGetParentSize(RectTransform.Axis.Vertical, out var size) && num > size)
			{
				num = size;
			}
			if (!Mathf.Approximately(num, _rectTransform.rect.height))
			{
				_rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, num);
				SetDirty();
			}
		}
	}

	private bool Init()
	{
		if ((bool)_rectTransform)
		{
			return true;
		}
		_rectTransform = GetComponent<RectTransform>();
		if ((bool)_rectTransform)
		{
			return true;
		}
		Debug.LogError("ContentSizeFitterExtender requires a RectTransform component.");
		return false;
	}

	private bool TryGetParentSize(RectTransform.Axis axis, out float size)
	{
		size = 0f;
		if (!_parentRectTransform)
		{
			_parentRectTransform = _rectTransform.parent as RectTransform;
		}
		if (!_parentRectTransform)
		{
			return false;
		}
		size = ((axis == RectTransform.Axis.Horizontal) ? _parentRectTransform.rect.width : _parentRectTransform.rect.height);
		return size > 0f;
	}
}
