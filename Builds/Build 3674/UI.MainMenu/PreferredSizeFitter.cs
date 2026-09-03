using DG.Tweening;
using JimmysUnityUtilities;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu;

public class PreferredSizeFitter : MonoBehaviour
{
	[SerializeField]
	private int maxHeight;

	[SerializeField]
	private int padding;

	[Space]
	[SerializeField]
	private bool useRectTransformHeight;

	[HideIf("useRectTransformHeight")]
	[SerializeField]
	private LayoutElement contentLayoutElement;

	[ShowIf("useRectTransformHeight")]
	[SerializeField]
	private RectTransform contentRectTransform;

	[SerializeField]
	private RectTransform syncRect;

	[SerializeField]
	private float dampingDuration = 0.3f;

	public void ForceUpdate()
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			float targetHeight = Mathf.Min(maxHeight, syncRect.rect.height + (float)padding);
			if (useRectTransformHeight)
			{
				HandleRectTransformHeightChange(targetHeight);
			}
			else
			{
				HandleLayoutElementHeightChange(targetHeight);
			}
		});
	}

	private void HandleRectTransformHeightChange(float targetHeight)
	{
		contentRectTransform.DOKill();
		DOTween.To(() => contentRectTransform.rect.height, delegate(float x)
		{
			contentRectTransform.sizeDelta = new Vector2(contentRectTransform.sizeDelta.x, x);
		}, targetHeight, dampingDuration).SetEase(Ease.OutQuad).SetId(contentRectTransform)
			.SetUpdate(isIndependentUpdate: true);
	}

	private void HandleLayoutElementHeightChange(float targetHeight)
	{
		contentLayoutElement.DOKill();
		DOTween.To(() => contentLayoutElement.preferredHeight, delegate(float x)
		{
			contentLayoutElement.preferredHeight = x;
		}, targetHeight, dampingDuration).SetEase(Ease.OutQuad).SetUpdate(isIndependentUpdate: true);
	}
}
