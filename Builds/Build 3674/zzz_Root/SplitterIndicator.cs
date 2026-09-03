using DG.Tweening;
using JimmysUnityUtilities;
using UnityEngine;

public class SplitterIndicator : MonoBehaviour
{
	[SerializeField]
	private float indicatorAnimationTime = 0.2f;

	[SerializeField]
	private RectTransform indicatorRect;

	public void Set(RectTransform target)
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			indicatorRect.sizeDelta = new Vector2(target.rect.width, indicatorRect.sizeDelta.y);
			indicatorRect.DOMoveX(target.position.x, indicatorAnimationTime).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
		});
	}
}
