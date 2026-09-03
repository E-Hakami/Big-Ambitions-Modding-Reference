using DG.Tweening;
using UnityEngine;

namespace BigAmbitions;

public class ModCreatorUI : MonoBehaviour
{
	[SerializeField]
	private MyCreatedModsList myCreatedModsList;

	[SerializeField]
	private UploadModPanel uploadModPanel;

	[Header("Expansion")]
	[SerializeField]
	private RectTransform expansionRect;

	[SerializeField]
	private float initialWidth;

	[SerializeField]
	private float initialHorizontalCenterOffset;

	[SerializeField]
	private float expandedLeftInset;

	[SerializeField]
	private float expandedRightInset;

	[SerializeField]
	private float expansionDuration;

	private bool _isExpanded;

	private Tween _expansionTween;

	private bool _isApplyingLayout;

	private void OnEnable()
	{
		Close();
	}

	public void Expand(ModInfo modInfo)
	{
		if (_isExpanded)
		{
			uploadModPanel.Show(modInfo);
			return;
		}
		_expansionTween?.Kill();
		float currentOffsetMinY = expansionRect.offsetMin.y;
		float currentOffsetMaxY = expansionRect.offsetMax.y;
		GetInitialOffsets(out var initialLeft, out var initialRight);
		GetExpandedOffsets(out var expandedLeft, out var expandedRight);
		ApplyOffsets(initialLeft, initialRight);
		_expansionTween = DOTween.To(() => 0f, delegate(float animationProgress)
		{
			float x = Mathf.Lerp(initialLeft, expandedLeft, animationProgress);
			float x2 = Mathf.Lerp(initialRight, expandedRight, animationProgress);
			_isApplyingLayout = true;
			expansionRect.offsetMin = new Vector2(x, currentOffsetMinY);
			expansionRect.offsetMax = new Vector2(x2, currentOffsetMaxY);
			_isApplyingLayout = false;
		}, 1f, expansionDuration).SetUpdate(isIndependentUpdate: true).OnComplete(delegate
		{
			_isExpanded = true;
			_expansionTween = null;
			uploadModPanel.Show(modInfo);
		});
	}

	public void Close()
	{
		_expansionTween?.Kill();
		_expansionTween = null;
		GetInitialOffsets(out var left, out var right);
		ApplyOffsets(left, right);
		_isExpanded = false;
		uploadModPanel.Hide(animate: false);
	}

	private void OnRectTransformDimensionsChange()
	{
		if (base.isActiveAndEnabled && !(expansionRect == null) && !(expansionRect.parent == null) && !_isApplyingLayout && (_expansionTween == null || !_expansionTween.IsActive() || !_expansionTween.IsPlaying()))
		{
			float left;
			float right;
			if (_isExpanded)
			{
				GetExpandedOffsets(out left, out right);
			}
			else
			{
				GetInitialOffsets(out left, out right);
			}
			ApplyOffsets(left, right);
		}
	}

	private void ApplyOffsets(float left, float right)
	{
		Vector2 offsetMin = expansionRect.offsetMin;
		Vector2 offsetMax = expansionRect.offsetMax;
		if (Mathf.Approximately(offsetMin.x, left) && Mathf.Approximately(offsetMax.x, right))
		{
			return;
		}
		_isApplyingLayout = true;
		try
		{
			expansionRect.offsetMin = new Vector2(left, offsetMin.y);
			expansionRect.offsetMax = new Vector2(right, offsetMax.y);
		}
		finally
		{
			_isApplyingLayout = false;
		}
	}

	private void GetInitialOffsets(out float left, out float right)
	{
		float num = ((RectTransform)expansionRect.parent).rect.width * 0.5f;
		float num2 = initialWidth * 0.5f;
		left = num - num2 + initialHorizontalCenterOffset;
		right = 0f - (num - num2) + initialHorizontalCenterOffset;
	}

	private void GetExpandedOffsets(out float left, out float right)
	{
		left = expandedLeftInset;
		right = 0f - expandedRightInset;
	}
}
