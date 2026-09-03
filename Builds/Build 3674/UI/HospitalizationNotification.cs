using DG.Tweening;
using UI.MiniMenu;
using UnityEngine;

namespace UI;

public class HospitalizationNotification : MonoBehaviour
{
	[SerializeField]
	private CanvasGroup canvasGroup;

	public bool IsVisible => canvasGroup.alpha > 0f;

	public void Show()
	{
		InstanceBehavior<UIs>.Instance.gameSpeed.SetPlayerPause(newPause: true);
		canvasGroup.interactable = true;
		canvasGroup.blocksRaycasts = true;
		canvasGroup.DOFade(1f, 1f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
	}

	public void Hide()
	{
		canvasGroup.DOFade(0f, 1f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
		canvasGroup.interactable = false;
		canvasGroup.blocksRaycasts = false;
		if (!UI.MiniMenu.MiniMenu.IsOpen && !CityMap.IsOpen)
		{
			InstanceBehavior<UIs>.Instance.gameSpeed.SetPlayerPause(newPause: false);
		}
	}
}
