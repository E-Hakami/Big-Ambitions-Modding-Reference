using Helpers;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

public class AppButton : MonoBehaviour
{
	[SerializeField]
	private TextLocalizationComponent appTitle;

	[SerializeField]
	private Image icon;

	public void SetTitle(AppName appName)
	{
		appTitle.Key = appName.GetLocalizeKey();
	}

	public void SetIcon(Sprite sprite)
	{
		icon.sprite = sprite;
	}
}
