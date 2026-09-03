using BAModAPI;
using DG.Tweening;
using Helpers;
using UI.Load;
using UnityEngine;
using UnityEngine.UI;

namespace Intro;

public class IntroToGameTransition : MonoBehaviour
{
	public float durationBetween = 4f;

	public Image backgroundImage;

	public GameObject directionalLight;

	private void OnEnable()
	{
		if (!TutorialHelper.IsTutorialEnabled())
		{
			Continue();
			return;
		}
		backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 0f);
		Sequence sequence = DOTween.Sequence().SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
		sequence.SetDelay(1f);
		foreach (Transform item in base.transform)
		{
			item.GetComponent<CanvasGroup>().alpha = 0f;
		}
		sequence.Append(backgroundImage.DOFade(1f, durationBetween / 2f));
		foreach (Transform item2 in base.transform)
		{
			CanvasGroup component = item2.GetComponent<CanvasGroup>();
			sequence.Append(component.DOFade(1f, durationBetween));
		}
	}

	public void Continue()
	{
		Resources.UnloadUnusedAssets();
		Object.Destroy(directionalLight);
		GlobalEvents.Init();
		LoadScene.LoadGame(ModActivationScope.Intro);
	}

	public void Skip()
	{
		if (!TutorialHelper.IsTutorialEnabled())
		{
			return;
		}
		DOTween.KillAll();
		backgroundImage.color = new Color(backgroundImage.color.r, backgroundImage.color.g, backgroundImage.color.b, 1f);
		foreach (Transform item in base.transform)
		{
			item.GetComponent<CanvasGroup>().alpha = 1f;
		}
	}
}
