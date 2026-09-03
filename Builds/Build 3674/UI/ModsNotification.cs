using DG.Tweening;
using Localizor.LanguageChangeEvent;
using UI.MiniMenu;
using UnityEngine;

namespace UI;

public class ModsNotification : MonoBehaviour
{
	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private TextLocalizationComponent localizationComponent;

	public void Show(string modList)
	{
		base.gameObject.SetActive(value: true);
		InstanceBehavior<UIs>.Instance?.gameSpeed?.SetPause(newPause: true);
		canvasGroup.interactable = true;
		canvasGroup.blocksRaycasts = true;
		canvasGroup.alpha = 0f;
		canvasGroup.DOFade(1f, 1f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true);
		localizationComponent.Arguments = new { modList };
	}

	public void Hide()
	{
		canvasGroup.DOFade(0f, 1f).SetLink(base.gameObject).SetUpdate(isIndependentUpdate: true)
			.OnComplete(delegate
			{
				base.gameObject.SetActive(value: false);
			});
		canvasGroup.interactable = false;
		canvasGroup.blocksRaycasts = false;
		if (!UI.MiniMenu.MiniMenu.IsOpen && !CityMap.IsOpen)
		{
			InstanceBehavior<UIs>.Instance?.gameSpeed?.SetPause(newPause: false);
		}
	}
}
